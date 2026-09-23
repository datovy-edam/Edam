using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.FileSystem;

/// <summary>
/// File-system <see cref="IProjectCatalog"/> (PE-2). Collections are folders; the
/// <b>default collection is the app-data root itself</b> (the <c>AssetConsolePath</c> folder), and
/// Projects live in its <c>Projects/</c> sub-folder — matching today's layout, so existing
/// installations keep working.
/// <para>
/// Replaces the static <c>Edam.Data.AssetProject.Project</c> discovery surface
/// (<c>InitializeProject</c>/<c>GetProjectsPath</c>/<c>GetProjectItems</c>). Unlike it, this
/// implementation is an <b>instance</b>, takes its root by constructor, and never calls
/// <c>Directory.SetCurrentDirectory</c>.
/// </para>
/// </summary>
public sealed class FileSystemProjectCatalog : IProjectCatalog
{
   private readonly IReadOnlyList<ProjectCollectionInfo> _collections;

   /// <param name="defaultRoot">The app-data root (the default collection).</param>
   /// <param name="additionalCollections">Optional extra collections (spec §2.1.1 "UriList" entries).</param>
   public FileSystemProjectCatalog(
      string defaultRoot, IEnumerable<ProjectCollectionInfo>? additionalCollections = null)
   {
      if (string.IsNullOrWhiteSpace(defaultRoot))
         throw new ArgumentException("A default collection root is required.", nameof(defaultRoot));

      var root = Path.GetFullPath(defaultRoot);
      var collections = new List<ProjectCollectionInfo>
      {
         new(CollectionIdFor(root), new DirectoryInfo(root).Name, root,
            ProjectCollectionType.ConsolePath, IsDefault: true),
      };

      if (additionalCollections is not null)
      {
         foreach (var collection in additionalCollections)
         {
            if (collection is null || collection.IsDefault) continue;
            if (collections.Any(c => Ids(c).Contains(collection.CollectionId, StringComparer.OrdinalIgnoreCase)))
               continue;
            collections.Add(collection);
         }
      }

      _collections = collections;
   }

   public Task<IReadOnlyList<ProjectCollectionInfo>> GetCollectionsAsync(CancellationToken ct = default)
      => Task.FromResult(_collections);

   /// <summary>Matches by <see cref="ProjectCollectionInfo.CollectionId"/> or by name.</summary>
   public Task<ProjectCollectionInfo?> GetCollectionAsync(string collectionId, CancellationToken ct = default)
      => Task.FromResult(_collections.FirstOrDefault(
         c => Ids(c).Contains(collectionId ?? string.Empty, StringComparer.OrdinalIgnoreCase)));

   public Task<IReadOnlyList<ProjectInfo>> GetProjectsAsync(string collectionId, CancellationToken ct = default)
   {
      var collection = Find(collectionId);
      if (collection is null) return Task.FromResult<IReadOnlyList<ProjectInfo>>(Array.Empty<ProjectInfo>());

      var projects = new List<ProjectInfo>();
      var projectsRoot = ProjectsRoot(collection);
      if (Directory.Exists(projectsRoot))
      {
         foreach (var folder in Directory.GetDirectories(projectsRoot).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
         {
            var name = Path.GetFileName(folder);
            projects.Add(new ProjectInfo(name, name, string.Empty, collection.CollectionId,
               ProjectAddress(name)));
         }
      }
      return Task.FromResult<IReadOnlyList<ProjectInfo>>(projects);
   }

   public async Task<ProjectInfo?> GetProjectAsync(
      string collectionId, string projectName, CancellationToken ct = default)
   {
      var projects = await GetProjectsAsync(collectionId, ct).ConfigureAwait(false);
      return projects.FirstOrDefault(p => string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));
   }

   /// <summary>The <c>Projects/</c> folder of a collection (matches <c>Project.PROJECTS</c>).</summary>
   internal static string ProjectsRoot(ProjectCollectionInfo collection)
      => Path.Combine(collection.Uri, ProjectCollectionFolders.Projects);

   /// <summary>
   /// Resolve a <b>physical path</b> back to the project that owns it and the resource's
   /// project-relative path — the inverse of <see cref="ProjectAddress"/> plus the on-disk layout
   /// (<c>&lt;collection&gt;/Projects/&lt;name&gt;/…</c>).
   /// <para>
   /// This is the mapping a consumer holding a file-system path needs in order to drive the platform
   /// instead (e.g. the Studio's project tree): it lets a legacy disk path become
   /// (<see cref="ProjectInfo"/>, <see cref="ProjectPath"/>) without the consumer knowing the layout.
   /// </para>
   /// </summary>
   /// <returns><c>true</c> when the path lies inside a declared collection's <c>Projects/</c> folder.</returns>
   public bool TryResolveResource(
      string physicalPath, out ProjectCollectionInfo? collection,
      out ProjectInfo? project, out ProjectPath? resourcePath)
   {
      collection = null;
      project = null;
      resourcePath = null;

      if (string.IsNullOrWhiteSpace(physicalPath)) return false;

      string full;
      try { full = Path.GetFullPath(physicalPath); }
      catch { return false; }

      // longest matching collection root wins, in case one collection nests inside another
      foreach (var candidate in _collections
                  .OrderByDescending(c => Path.GetFullPath(c.Uri).Length))
      {
         var projectsRoot = Path.GetFullPath(ProjectsRoot(candidate));
         if (!full.StartsWith(projectsRoot + Path.DirectorySeparatorChar,
               StringComparison.OrdinalIgnoreCase))
         {
            continue;
         }

         var remainder = full[(projectsRoot.Length + 1)..];
         var segments = remainder.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
         if (segments.Length == 0) continue;

         var name = segments[0];
         collection = candidate;
         project = new ProjectInfo(name, name, string.Empty, candidate.CollectionId,
            ProjectAddress(name));
         resourcePath = segments.Length == 1
            ? ProjectPath.Root
            : ProjectPath.Parse("/" + string.Join('/', segments[1..]));
         return true;
      }

      return false;
   }

   /// <summary>Convenience overload of <see cref="TryResolveResource"/>.</summary>
   public bool TryResolveResource(
      string physicalPath, out ProjectInfo? project, out ProjectPath? resourcePath)
      => TryResolveResource(physicalPath, out _, out project, out resourcePath);


   /// <summary>A project's address within its collection: <c>/Projects/&lt;name&gt;</c>.</summary>
   internal static ProjectPath ProjectAddress(string projectName)
      => ProjectPath.Root.Combine(ProjectCollectionFolders.Projects).Combine(projectName);

   internal static string CollectionIdFor(string root) => new DirectoryInfo(root).Name;

   private ProjectCollectionInfo? Find(string collectionId)
      => _collections.FirstOrDefault(c => Ids(c).Contains(collectionId ?? string.Empty, StringComparer.OrdinalIgnoreCase));

   private static IEnumerable<string> Ids(ProjectCollectionInfo collection)
   {
      yield return collection.CollectionId;
      yield return collection.Name;
   }
}
