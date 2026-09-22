namespace Edam.Data.Projects.Contracts;

/// <summary>
/// <b>Discovery</b> of collections and projects (read-only). A consumer can browse what exists
/// without knowing — or caring — whether the collection is a file-system folder, a catalog, or a
/// remote/cloud location.
/// </summary>
public interface IProjectCatalog
{
   /// <summary>All registered collections (the default one is required and should be present).</summary>
   Task<IReadOnlyList<ProjectCollectionInfo>> GetCollectionsAsync(CancellationToken ct = default);

   /// <summary>A collection by id, or <c>null</c> when it is not registered.</summary>
   Task<ProjectCollectionInfo?> GetCollectionAsync(string collectionId, CancellationToken ct = default);

   /// <summary>All projects in a collection (empty when the collection has none).</summary>
   Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync(string collectionId, CancellationToken ct = default);

   /// <summary>A project by name within a collection, or <c>null</c> when it does not exist.</summary>
   Task<ProjectInfo?> GetProjectAsync(string collectionId, string projectName, CancellationToken ct = default);
}
