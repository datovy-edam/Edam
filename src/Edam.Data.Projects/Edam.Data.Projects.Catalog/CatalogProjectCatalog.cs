using Edam.Data.Catalog.Contracts;
using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Catalog;

/// <summary>
/// Catalog-backed <see cref="IProjectCatalog"/> (PE-3 / ADR-0009): a <b>Collection is a container</b>
/// and a <b>Project is the branch</b> <c>/Projects/&lt;name&gt;</c> within it.
/// <para>
/// It drives <see cref="ICatalogContainer"/> / <see cref="ICatalogItem"/>, so the <b>same</b>
/// implementation serves a local provider (PostgreSQL/FileSystem) and a remote catalog client over
/// the REST API — nothing here knows or cares which.
/// </para>
/// </summary>
public sealed class CatalogProjectCatalog : IProjectCatalog
{
   private readonly ICatalogContainer _containers;
   private readonly ICatalogItem _items;
   private readonly string? _defaultCollectionId;

   /// <param name="containers">Container surface (a local store, or a remote <c>ICatalogClient</c>).</param>
   /// <param name="items">Item surface (same source).</param>
   /// <param name="defaultCollectionId">
   /// Which container is the default collection; when omitted the first container is treated as
   /// default (the specification requires a default collection to exist).
   /// </param>
   public CatalogProjectCatalog(
      ICatalogContainer containers, ICatalogItem items, string? defaultCollectionId = null)
   {
      _containers = containers ?? throw new ArgumentNullException(nameof(containers));
      _items = items ?? throw new ArgumentNullException(nameof(items));
      _defaultCollectionId = defaultCollectionId;
   }

   public async Task<IReadOnlyList<ProjectCollectionInfo>> GetCollectionsAsync(CancellationToken ct = default)
   {
      var containers = await _containers.GetContainersAsync(ct).ConfigureAwait(false);
      var list = new List<ProjectCollectionInfo>(containers.Count);

      for (var index = 0; index < containers.Count; index++)
      {
         var container = containers[index];
         var isDefault = _defaultCollectionId is null
            ? index == 0
            : string.Equals(container.ContainerId, _defaultCollectionId, StringComparison.OrdinalIgnoreCase);

         list.Add(new ProjectCollectionInfo(
            container.ContainerId, container.ContainerId, container.ContainerUri,
            ProjectCollectionType.Catalog, isDefault));
      }

      return list;
   }

   public async Task<ProjectCollectionInfo?> GetCollectionAsync(
      string collectionId, CancellationToken ct = default)
      => (await GetCollectionsAsync(ct).ConfigureAwait(false))
         .FirstOrDefault(c => Matches(c, collectionId));

   public async Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync(
      string collectionId, CancellationToken ct = default)
   {
      var projects = new List<ProjectInfo>();

      ContainerInfo? container;
      try { container = await CatalogProjectSupport.RequireContainerAsync(_containers, collectionId, ct); }
      catch (InvalidOperationException) { return projects; }

      var prefix = CatalogProjectSupport.ProjectsRoot + "/";
      foreach (var item in _items.GetContainerItems(container.Id))
      {
         // a project is a DIRECT branch child of /Projects
         if (item.Type != ItemType.Branch) continue;
         if (!item.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
         if (item.FullPath.IndexOf('/', prefix.Length) >= 0) continue;

         projects.Add(ToProject(item, container));
      }

      return projects;
   }

   public async Task<ProjectInfo?> GetProjectAsync(
      string collectionId, string projectName, CancellationToken ct = default)
      => (await GetProjectsAsync(collectionId, ct).ConfigureAwait(false))
         .FirstOrDefault(p => Matches(p, projectName));

   /// <summary>
   /// Match a project by name, or by the leaf of its path — providers disagree on whether an item's
   /// Name is the leaf or the full path, and a lookup must not depend on that.
   /// </summary>
   internal static bool Matches(ProjectInfo project, string? name)
      => string.Equals(project.Name, name, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(project.Path.Name, name, StringComparison.OrdinalIgnoreCase);

   /// <summary>Map a catalog branch item to the project model.</summary>
   internal static ProjectInfo ToProject(ItemInfo item, ContainerInfo container)
      => new(item.Id.ToString(), item.Name, string.Empty, container.ContainerId,
         ProjectPath.Parse(item.FullPath), item.Description);

   /// <summary>Match a collection by its id or name.</summary>
   internal static bool Matches(ProjectCollectionInfo collection, string? id)
      => string.Equals(collection.CollectionId, id, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(collection.Name, id, StringComparison.OrdinalIgnoreCase);
}
