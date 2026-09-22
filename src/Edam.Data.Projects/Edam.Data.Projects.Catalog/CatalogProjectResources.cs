using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.Folder;
using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Catalog;

/// <summary>
/// Catalog-backed <see cref="IProjectResources"/> (PE-3 / ADR-0009) — <b>the hinge</b>, now over the
/// catalog instead of a disk. A project-relative <see cref="ProjectPath"/> maps to a catalog
/// <see cref="ItemInfo"/> (branch = folder, leaf = file) whose <b>full path is also the content
/// key</b> in <see cref="IContentStore"/>, so artifact bytes live in the catalog on whatever
/// provider is configured.
/// <para>
/// Drives the Contracts surfaces only, so the identical code works against a local provider or a
/// remote catalog client (inputs and outputs over the wire).
/// </para>
/// </summary>
public sealed class CatalogProjectResources : IProjectResources
{
   private readonly ICatalogContainer _containers;
   private readonly ICatalogItem _items;
   private readonly IContentStore _content;

   public CatalogProjectResources(
      ICatalogContainer containers, ICatalogItem items, IContentStore content)
   {
      _containers = containers ?? throw new ArgumentNullException(nameof(containers));
      _items = items ?? throw new ArgumentNullException(nameof(items));
      _content = content ?? throw new ArgumentNullException(nameof(content));
   }

   public async Task<IReadOnlyList<ProjectResourceInfo>> ListAsync(
      ProjectInfo project, ProjectPath folder, string? extension = null, CancellationToken ct = default)
   {
      var list = new List<ProjectResourceInfo>();
      ArgumentNullException.ThrowIfNull(project);
      var wanted = CatalogProjectSupport.NormalizeExtension(extension);
      var container = await CatalogProjectSupport
         .RequireContainerAsync(_containers, project.CollectionId, ct).ConfigureAwait(false);

      var scope = CatalogProjectSupport.Full(project, folder);

      // GetBranch is prefix-based, so this is recursive (the pipeline scans folders for matches)
      foreach (var item in _items.GetBranch(scope))
      {
         ct.ThrowIfCancellationRequested();
         if (item.ContainerId != container.Id) continue;
         if (wanted is not null && item.Type == ItemType.Leaf &&
             !string.Equals(Path.GetExtension(item.Name), wanted, StringComparison.OrdinalIgnoreCase))
            continue;

         list.Add(CatalogProjectSupport.ToResource(project, item));
      }

      list.Sort((a, b) => string.Compare(a.Path.Value, b.Path.Value, StringComparison.OrdinalIgnoreCase));
      return list;
   }

   public async Task<bool> ExistsAsync(
      ProjectInfo project, ProjectPath path, CancellationToken ct = default)
   {
      await CatalogProjectSupport.RequireContainerAsync(_containers, project.CollectionId, ct)
         .ConfigureAwait(false);
      return _items.GetItemByPath(CatalogProjectSupport.Full(project, path)) is not null;
   }

   public async Task<Stream?> OpenReadAsync(
      ProjectInfo project, ProjectPath path, CancellationToken ct = default)
   {
      await CatalogProjectSupport.RequireContainerAsync(_containers, project.CollectionId, ct)
         .ConfigureAwait(false);
      return await _content.OpenReadAsync(CatalogProjectSupport.Full(project, path), ct)
         .ConfigureAwait(false);
   }

   public async Task WriteAsync(
      ProjectInfo project, ProjectPath path, Stream content, CancellationToken ct = default)
   {
      var container = await CatalogProjectSupport
         .RequireContainerAsync(_containers, project.CollectionId, ct).ConfigureAwait(false);

      var full = CatalogProjectSupport.Full(project, path);
      var now = DateTimeOffset.UtcNow;

      // same deterministic id as the folder indexer, so an import and a later write address one item
      await _items.AddItemAsync(new ItemInfo(
         FolderCatalogIndexer.DeterministicId(container.Id, full), container.Id, full,
         path.Name, null, ItemType.Leaf, now, now), ct).ConfigureAwait(false);

      await _content.WriteAsync(full, content, ct).ConfigureAwait(false);
   }

   public async Task<ProjectPath> CreateFolderAsync(
      ProjectInfo project, ProjectPath folder, CancellationToken ct = default)
   {
      var container = await CatalogProjectSupport
         .RequireContainerAsync(_containers, project.CollectionId, ct).ConfigureAwait(false);

      await _items.CreateBranchAsync(
         CatalogProjectSupport.Full(project, folder), null, container.Id, ct).ConfigureAwait(false);

      return folder;
   }

   public async Task<bool> DeleteAsync(
      ProjectInfo project, ProjectPath path, CancellationToken ct = default)
   {
      await CatalogProjectSupport.RequireContainerAsync(_containers, project.CollectionId, ct)
         .ConfigureAwait(false);

      var full = CatalogProjectSupport.Full(project, path);
      var item = _items.GetItemByPath(full);
      var itemDeleted = item is not null && _items.DeleteItem(item.Id);
      var contentDeleted = await _content.DeleteAsync(full, ct).ConfigureAwait(false);

      return itemDeleted || contentDeleted;
   }
}
