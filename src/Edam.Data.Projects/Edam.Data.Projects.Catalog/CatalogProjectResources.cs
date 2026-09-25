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
   private readonly IProjectContentStoreResolver? _contentResolver;

   /// <param name="containers">The container surface.</param>
   /// <param name="items">The item surface.</param>
   /// <param name="content">The (unscoped) content store — the fallback.</param>
   /// <param name="contentResolver">
   /// LM-2b-ii: resolves the <b>container-scoped</b> content store, so two containers may hold the same
   /// path. Absent (the default) keeps the single injected store, i.e. the behaviour before scoping.
   /// </param>
   public CatalogProjectResources(
      ICatalogContainer containers, ICatalogItem items, IContentStore content,
      IProjectContentStoreResolver? contentResolver = null)
   {
      _containers = containers ?? throw new ArgumentNullException(nameof(containers));
      _items = items ?? throw new ArgumentNullException(nameof(items));
      _content = content ?? throw new ArgumentNullException(nameof(content));
      _contentResolver = contentResolver;
   }

   /// <summary>LM-2b-ii: the container-scoped store when a resolver is configured, else the injected one.</summary>
   private IContentStore Content(ContainerInfo container)
      => _contentResolver?.ForContainer(container) ?? _content;

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
      var container = await CatalogProjectSupport
         .RequireContainerAsync(_containers, project.CollectionId, ct).ConfigureAwait(false);

      // LM-2c: the item must live in THIS container (the lookup itself is container-blind)
      var item = _items.GetItemByPath(CatalogProjectSupport.Full(project, path));
      return item is not null && item.ContainerId == container.Id;
   }

   public async Task<Stream?> OpenReadAsync(
      ProjectInfo project, ProjectPath path, CancellationToken ct = default)
   {
      var container = await CatalogProjectSupport
         .RequireContainerAsync(_containers, project.CollectionId, ct).ConfigureAwait(false);
      return await Content(container)
         .OpenReadAsync(CatalogProjectSupport.Full(project, path), ct)
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

      await Content(container).WriteAsync(full, content, ct).ConfigureAwait(false);
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
      var container = await CatalogProjectSupport.RequireContainerAsync(_containers, project.CollectionId, ct)
         .ConfigureAwait(false);

      var full = CatalogProjectSupport.Full(project, path);
      var item = _items.GetItemByPath(full);
      var itemDeleted = item is not null && item.ContainerId == container.Id && _items.DeleteItem(item.Id);
      var contentDeleted = await Content(container).DeleteAsync(full, ct).ConfigureAwait(false);

      return itemDeleted || contentDeleted;
   }
}
