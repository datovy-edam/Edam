using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.FileSystem;

/// <summary>
/// File-system <see cref="IProjectStore"/> (PE-2): create a project with the standard folder
/// structure, and move a project in and out of a collection — the <b>upload / download</b> pair the
/// specification anticipates (a folder today; the Catalog — including remotely — in PE-3).
/// <para>
/// Replaces the static <c>Edam.Data.AssetProject.Project.CreateProject</c>, which failed when the
/// project existed, mutated the process current directory and copied an app-settings template.
/// Template seeding is an application concern and is deliberately not done here; creation is
/// idempotent instead.
/// </para>
/// </summary>
public sealed class FileSystemProjectStore : IProjectStore
{
   private readonly IProjectCatalog _catalog;

   public FileSystemProjectStore(IProjectCatalog catalog)
      => _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

   public async Task<ProjectInfo> CreateAsync(
      string collectionId, string name, string? description = null, CancellationToken ct = default)
   {
      var collection = await RequireCollectionAsync(collectionId, ct).ConfigureAwait(false);
      var project = new ProjectInfo(
         name, name, string.Empty, collection.CollectionId,
         FileSystemProjectCatalog.ProjectAddress(name), description);

      var root = Root(collection, project);
      if (Directory.Exists(root)) return project;   // idempotent

      Directory.CreateDirectory(root);
      foreach (var folder in ProjectFolders.All)
         Directory.CreateDirectory(Path.Combine(root, folder));

      return project;
   }

   public async Task<ProjectImportResult> ImportAsync(
      string collectionId, string sourceLocation, string? projectName = null,
      CancellationToken ct = default)
   {
      if (string.IsNullOrWhiteSpace(sourceLocation) || !Directory.Exists(sourceLocation))
         throw new DirectoryNotFoundException(
            $"Import source '{sourceLocation}' does not exist.");

      var name = string.IsNullOrWhiteSpace(projectName)
         ? new DirectoryInfo(Path.GetFullPath(sourceLocation)).Name
         : projectName!;

      var project = await CreateAsync(collectionId, name, ct: ct).ConfigureAwait(false);
      var collection = await RequireCollectionAsync(collectionId, ct).ConfigureAwait(false);

      var (folders, files, issues) = CopyTree(
         Path.GetFullPath(sourceLocation), Root(collection, project));

      return new ProjectImportResult(project, folders, files, issues.Count == 0 ? null : issues);
   }

   public async Task ExportAsync(
      string collectionId, string projectName, string targetLocation, CancellationToken ct = default)
   {
      var collection = await RequireCollectionAsync(collectionId, ct).ConfigureAwait(false);
      var project = await _catalog.GetProjectAsync(collectionId, projectName, ct).ConfigureAwait(false)
         ?? throw new DirectoryNotFoundException(
            $"Project '{projectName}' was not found in collection '{collection.CollectionId}'.");

      var source = Root(collection, project);
      if (!Directory.Exists(source))
         throw new DirectoryNotFoundException($"Project '{projectName}' has no content to export.");

      CopyTree(source, Path.GetFullPath(targetLocation));
   }

   public async Task<bool> DeleteAsync(
      string collectionId, string projectName, CancellationToken ct = default)
   {
      var collection = await RequireCollectionAsync(collectionId, ct).ConfigureAwait(false);
      var project = await _catalog.GetProjectAsync(collectionId, projectName, ct).ConfigureAwait(false);
      if (project is null) return false;

      var root = Root(collection, project);
      if (!Directory.Exists(root)) return false;

      Directory.Delete(root, true);
      return true;
   }

   // ---------------------------------------------------------------------

   private static string Root(ProjectCollectionInfo collection, ProjectInfo project)
      => Path.GetFullPath(Path.Combine(
         collection.Uri,
         project.Path.Value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

   private async Task<ProjectCollectionInfo> RequireCollectionAsync(string collectionId, CancellationToken ct)
      => await _catalog.GetCollectionAsync(collectionId, ct).ConfigureAwait(false)
         ?? throw new InvalidOperationException($"Collection '{collectionId}' is not registered.");

   /// <summary>Copy a folder tree, reporting how much moved and what could not be read.</summary>
   private static (int Folders, int Files, List<string> Issues) CopyTree(string source, string target)
   {
      var issues = new List<string>();
      var folders = 0;
      var files = 0;

      void Copy(string from, string to)
      {
         try { Directory.CreateDirectory(to); } catch { }

         string[] childFolders;
         try { childFolders = Directory.GetDirectories(from); }
         catch (Exception ex) { issues.Add($"{from}: {ex.Message}"); childFolders = Array.Empty<string>(); }

         foreach (var child in childFolders)
         {
            folders++;
            Copy(child, Path.Combine(to, Path.GetFileName(child)));
         }

         string[] childFiles;
         try { childFiles = Directory.GetFiles(from); }
         catch (Exception ex) { issues.Add($"{from}: {ex.Message}"); childFiles = Array.Empty<string>(); }

         foreach (var file in childFiles)
         {
            try
            {
               File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);
               files++;
            }
            catch (Exception ex)
            {
               issues.Add($"{file}: {ex.Message}");
            }
         }
      }

      Copy(source, target);
      return (folders, files, issues);
   }
}
