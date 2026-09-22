using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.Folder;
using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Catalog;

/// <summary>
/// Catalog-backed <see cref="IProjectStore"/> (PE-3 / ADR-0009): create a project branch with the
/// standard folder branches, and move a project in and out of the catalog — <b>upload / download</b>
/// (the capability the Studio's old folder→catalog clone provided, now first-class and
/// provider-agnostic).
/// <para>
/// <see cref="ImportAsync"/> indexes the source folder <b>under</b> the project branch with
/// <see cref="FolderCatalogIndexer"/> (deterministic ids, content keyed by full path) and crucially
/// does <b>not</b> re-enlist the container — an import can never rewrite a collection's URI.
/// </para>
/// </summary>
public sealed class CatalogProjectStore : IProjectStore
{
   private readonly IProjectCatalog _catalog;
   private readonly ICatalogContainer _containers;
   private readonly ICatalogItem _items;
   private readonly IContentStore _content;

   public CatalogProjectStore(
      IProjectCatalog catalog, ICatalogContainer containers, ICatalogItem items, IContentStore content)
   {
      _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
      _containers = containers ?? throw new ArgumentNullException(nameof(containers));
      _items = items ?? throw new ArgumentNullException(nameof(items));
      _content = content ?? throw new ArgumentNullException(nameof(content));
   }

   public async Task<ProjectInfo> CreateAsync(
      string collectionId, string name, string? description = null, CancellationToken ct = default)
   {
      var container = await CatalogProjectSupport
         .RequireContainerAsync(_containers, collectionId, ct).ConfigureAwait(false);

      var projectPath = CatalogProjectSupport.ProjectBranch(container.ContainerId, name);

      // idempotent: an existing project is returned as-is
      var existing = _items.GetItemByPath(projectPath.Value);
      if (existing is not null) return CatalogProjectCatalog.ToProject(existing, container);

      await _items.CreateBranchAsync(
         CatalogProjectSupport.ProjectsRoot(container.ContainerId), "Projects", container.Id, ct)
         .ConfigureAwait(false);

      var project = await _items.CreateBranchAsync(
         projectPath.Value, description, container.Id, ct).ConfigureAwait(false);

      foreach (var folder in ProjectFolders.All)
      {
         await _items.CreateBranchAsync(
            projectPath.Combine(folder).Value, null, container.Id, ct).ConfigureAwait(false);
      }

      return CatalogProjectCatalog.ToProject(project, container);
   }

   public async Task<ProjectImportResult> ImportAsync(
      string collectionId, string sourceLocation, string? projectName = null,
      CancellationToken ct = default)
   {
      if (string.IsNullOrWhiteSpace(sourceLocation) || !Directory.Exists(sourceLocation))
         throw new DirectoryNotFoundException($"Import source '{sourceLocation}' does not exist.");

      var name = string.IsNullOrWhiteSpace(projectName)
         ? new DirectoryInfo(Path.GetFullPath(sourceLocation)).Name
         : projectName!;

      var container = await CatalogProjectSupport
         .RequireContainerAsync(_containers, collectionId, ct).ConfigureAwait(false);

      var projectPath = CatalogProjectSupport.ProjectBranch(container.ContainerId, name);

      // index the folder INTO the project branch; content keys are full paths (no collisions)
      var indexed = await FolderCatalogIndexer.IndexDetailedAsync(
         container.Id, _items, _content, sourceLocation,
         indexContent: true, ct: ct, pathPrefix: projectPath.Value).ConfigureAwait(false);

      var project = await _catalog.GetProjectAsync(collectionId, name, ct).ConfigureAwait(false)
         ?? new ProjectInfo(name, name, string.Empty, container.ContainerId, projectPath);

      return new ProjectImportResult(project, indexed.Folders, indexed.Files);
   }

   public async Task ExportAsync(
      string collectionId, string projectName, string targetLocation, CancellationToken ct = default)
   {
      await CatalogProjectSupport.RequireContainerAsync(_containers, collectionId, ct)
         .ConfigureAwait(false);

      var project = await _catalog.GetProjectAsync(collectionId, projectName, ct).ConfigureAwait(false)
         ?? throw new DirectoryNotFoundException(
            $"Project '{projectName}' was not found in collection '{collectionId}'.");

      var target = Path.GetFullPath(targetLocation);
      Directory.CreateDirectory(target);

      foreach (var item in _items.GetBranch(project.Path.Value))
      {
         ct.ThrowIfCancellationRequested();

         var relative = CatalogProjectSupport.RelativeTo(project, item.FullPath);
         var physical = Path.Combine(target, relative.Value.TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar));

         if (item.Type == ItemType.Branch)
         {
            Directory.CreateDirectory(physical);
            continue;
         }

         var parent = Path.GetDirectoryName(physical);
         if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

         using var stream = await _content.OpenReadAsync(item.FullPath, ct).ConfigureAwait(false);
         if (stream is null) continue;

         using var file = File.Create(physical);
         await stream.CopyToAsync(file, ct).ConfigureAwait(false);
      }
   }

   public async Task<bool> DeleteAsync(
      string collectionId, string projectName, CancellationToken ct = default)
   {
      await CatalogProjectSupport.RequireContainerAsync(_containers, collectionId, ct)
         .ConfigureAwait(false);

      var project = await _catalog.GetProjectAsync(collectionId, projectName, ct).ConfigureAwait(false);
      if (project is null) return false;

      var removed = false;

      // deepest first, so deleting a branch never strands its children
      foreach (var item in _items.GetBranch(project.Path.Value)
                  .OrderByDescending(i => i.FullPath.Length))
      {
         if (item.Type == ItemType.Leaf)
            await _content.DeleteAsync(item.FullPath, ct).ConfigureAwait(false);

         if (_items.DeleteItem(item.Id)) removed = true;
      }

      var root = _items.GetItemByPath(project.Path.Value);
      if (root is not null && _items.DeleteItem(root.Id)) removed = true;

      return removed;
   }
}
