using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.DependencyInjection;

namespace Edam.Data.Catalog.Conformance;

/// <summary>
/// <b>Container administration conformance</b> (ADR-0014 decision 8 / LM-9): the operations the container
/// manager performs — <b>add</b>, <b>describe</b>, <b>edit the description</b> and <b>remove</b> — proven against
/// whatever provider is supplied, exactly like the catalog scenario beside it.
/// <para>
/// The check that matters is the removal: it must take the container row, its <b>items</b> and their
/// <b>content</b>. The item rows carry a <b>foreign key</b> to the container (so a bare delete fails outright)
/// and content carries <b>none</b> (so it is orphaned rather than blocked) — the two halves of the defect that
/// made a Remove button appear to do nothing. Here "nothing was left behind" is counted, not asserted.
/// </para>
/// </summary>
public static class CatalogContainerScenario
{
   public static async Task<IReadOnlyList<CatalogScenario.Check>> RunAsync(
      ICatalogStore store, IContentStore? content, CancellationToken ct = default)
   {
      var checks = new List<CatalogScenario.Check>();
      static CatalogScenario.Check New(string name, bool passed, string detail) => new(name, passed, detail);

      // the manager's own service, over the seams only (no provider is named anywhere here)
      var administration = new CatalogContainerAdministration(store, _ => content, content);
      var id = "manager-" + Guid.NewGuid().ToString("N")[..8];

      // ---- add (and the upsert that an edit really is) --------------------------------------
      var added = await administration.AddAsync(
         id, "A container the manager created", ContainerType.DataContext, null, ct);

      checks.Add(New("Add creates a container with its description",
         added is not null && added.ContainerId == id && added.Description == "A container the manager created",
         (added?.ContainerId ?? "<null>") + " / " + (added?.Description ?? "<null>")));

      await administration.AddAsync(id, "Description updated by the manager", ContainerType.DataContext, null, ct);

      var all = await store.GetContainersAsync(ct);
      var same = all.Where(c => String.Equals(c.ContainerId, id, StringComparison.OrdinalIgnoreCase)).ToList();

      checks.Add(New("Adding an existing name keeps ONE container and NEVER rewrites it (enlist is idempotent)",
         same.Count == 1 && same[0].Description == "A container the manager created",
         same.Count + " container(s), description='" + (same.FirstOrDefault()?.Description ?? "<none>") + "'"));

      // ---- describe -------------------------------------------------------------------------
      var described = await administration.DescribeAsync(id, ct);
      checks.Add(New("Describe reports the description, the kind and the item count",
         described is not null && described.Items == 0 && described.Type == ContainerType.DataContext,
         described is null ? "<null>" : described.Items + " item(s), " + described.Type));

      // ---- edit the description (the only editable part) -------------------------------------
      var saved = await administration.SaveDescriptionAsync(id, "The description the person edited", ct);
      checks.Add(New("SaveDescription edits the description and PRESERVES the kind (and base uri)",
         saved is not null && saved.Description == "The description the person edited" &&
         saved.ContainerType == ContainerType.DataContext,
         (saved?.Description ?? "<null>") + " / " + saved?.ContainerType));

      // ---- a container that actually HOLDS something (the production shape) -------------------
      var container = store.GetContainer(id);
      if (container is null)
      {
         checks.Add(New("The container is there to be filled", false, "the container vanished"));
         return checks;
      }

      const string leafPath = "/manager/files/note.txt";

      store.CreateBranch("/manager/files", "files", container.Id);

      var now = DateTimeOffset.UtcNow;
      store.AddItem(new ItemInfo(
         Guid.NewGuid(), container.Id, leafPath, "note.txt", null, ItemType.Leaf, now, now));

      if (content is not null)
      {
         var bytes = System.Text.Encoding.UTF8.GetBytes("removal-proof");
         await content.WriteAsync(leafPath, new MemoryStream(bytes), ct);
      }

      var filled = await administration.DescribeAsync(id, ct);
      checks.Add(New("Describe counts what the container holds",
         filled is not null && filled.Items >= 2, (filled?.Items ?? 0) + " item(s)"));

      // ---- REMOVE: the reason this scenario exists -------------------------------------------
      var removal = await administration.RemoveAsync(id, ct);

      var gone = store.GetContainer(id) is null;
      var itemsLeft = store.GetContainerItems(container.Id).Count;
      var contentLeft = content is not null && await content.ExistsAsync(leafPath, ct);

      checks.Add(New("Remove takes the container, its items AND their content — nothing is left behind",
         removal.Removed && gone && itemsLeft == 0 && !contentLeft &&
         removal.Items >= 2 && (content is null || removal.Contents >= 1),
         $"removed={removal.Removed} containerGone={gone} itemsLeft={itemsLeft} contentLeft={contentLeft} " +
         $"counts=({removal.Items} items, {removal.Contents} content)"));

      // ---- and removing nothing is a reported no-op, never a throw ---------------------------
      var missing = await administration.RemoveAsync("no-such-" + id, ct);
      checks.Add(New("Removing a container that does not exist is reported, not thrown",
         !missing.Removed && !String.IsNullOrWhiteSpace(missing.Problem), missing.Problem ?? "<none>"));

      return checks;
   }
}
