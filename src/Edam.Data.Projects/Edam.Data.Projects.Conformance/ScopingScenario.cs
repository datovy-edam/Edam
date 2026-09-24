using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.FileSystem;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>LM-2a (ADR-0011)</b> checks that the catalog's <b>item index is container-scoped</b>: the index is
/// keyed by <b>container + path</b>, and an existing branch is matched <b>inside</b> the container it
/// belongs to.
/// <para>
/// This is the PE-3 defect made structurally impossible: creating <c>/Projects/&lt;name&gt;</c> in one
/// collection used to find <i>another</i> collection's item at the same path and return it early, so the
/// second container's project was never scaffolded.
/// </para>
/// <para>
/// <b>Recorded residuals (not asserted here):</b> content keys are still global (<c>IContentStore</c> is
/// path-only, so two containers share one blob namespace) and PostgreSQL still has a global
/// <c>resource_path</c> primary key — both are <b>LM-2b</b>, which needs the 2a/2b instance-or-contract
/// decision and the database up. Dropping the projects-side <c>/&lt;collectionId&gt;/</c> path prefix is
/// <b>LM-2c</b>, once content is scoped too.
/// </para>
/// </summary>
public static class ScopingScenario
{
   public static async Task<List<ProjectScenario.Check>> RunAsync(
      string workRoot, CancellationToken ct = default)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      Directory.CreateDirectory(workRoot);
      var store = new FileSystemCatalogStore(Path.Combine(workRoot, "catalog"));

      // two collections (containers) …
      store.EnlistContainer("collection.a", "LM-2a A", null, ContainerType.FileSystem);
      store.EnlistContainer("collection.b", "LM-2a B", null, ContainerType.FileSystem);

      var a = store.GetContainer("collection.a")!;
      var b = store.GetContainer("collection.b")!;
      const string shared = "/Projects/Shared.Name";

      // … the FIRST one creates the path
      var createdInA = await store.CreateBranchAsync(shared, "in A", a.Id, ct).ConfigureAwait(false);

      var bItemsBefore = store.GetContainerItems(b.Id);
      Check("A container does not inherit another container's items",
         bItemsBefore.Count == 0, $"{bItemsBefore.Count} item(s) in B");

      // … and the SECOND one creates the SAME path and must get ITS OWN item
      var createdInB = await store.CreateBranchAsync(shared, "in B", b.Id, ct).ConfigureAwait(false);
      var bItems = store.GetContainerItems(b.Id);

      Check("Two containers may hold the SAME path, each with its own item",
         createdInB.Id != createdInA.Id && createdInB.ContainerId == b.Id &&
         bItems.Any(i => i.Id == createdInB.Id),
         $"A={createdInA.Id.ToString()[..8]} B={createdInB.Id.ToString()[..8]}");

      Check("The item is matched INSIDE its container (the PE-3 defect)",
         createdInA.ContainerId == a.Id && createdInB.ContainerId == b.Id &&
         store.GetContainerItems(a.Id).Any(i => i.Id == createdInA.Id),
         $"A keeps {createdInA.Id.ToString()[..8]}, B keeps {createdInB.Id.ToString()[..8]}");

      // … and creating it twice in one container stays idempotent
      var againInA = await store.CreateBranchAsync(shared, "in A", a.Id, ct).ConfigureAwait(false);
      Check("Creating the same branch twice in one container is idempotent",
         againInA.Id == createdInA.Id, againInA.Id.ToString()[..8]);

      Check("The legacy container-blind lookup still resolves (ambiguous, kept for compatibility)",
         store.GetItemByPath(shared) is not null, store.GetItemByPath(shared)?.ContainerId.ToString()[..8] ?? "<null>");

      return checks;
   }
}
