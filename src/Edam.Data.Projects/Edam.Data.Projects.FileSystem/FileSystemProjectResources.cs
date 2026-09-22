using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.FileSystem;

/// <summary>
/// File-system <see cref="IProjectResources"/> (PE-2) — <b>the hinge</b>. Resolves a
/// project-relative <see cref="ProjectPath"/> to a file under the project folder, so callers
/// (<c>*.Args.json</c> inputs such as <c>./Archive/x.xlsx</c>, outputs such as
/// <c>./Documents/y.xlsx</c>) never touch a path string, never resolve against the process current
/// directory, and never call <c>Directory.SetCurrentDirectory</c>.
/// <para>
/// The project folder is <c>&lt;collection root&gt;/&lt;project.Path&gt;</c> — the catalog
/// implementation resolves the same <see cref="ProjectInfo"/> to a catalog branch instead.
/// </para>
/// </summary>
public sealed class FileSystemProjectResources : IProjectResources
{
   private readonly string _collectionRoot;
   private readonly IProjectCatalog _catalog;

   public FileSystemProjectResources(string collectionRoot, IProjectCatalog? catalog = null)
   {
      _collectionRoot = Path.GetFullPath(collectionRoot ??
         throw new ArgumentNullException(nameof(collectionRoot)));
      _catalog = catalog ?? new FileSystemProjectCatalog(_collectionRoot);
   }

   /// <summary>The physical folder of a project (see the class remarks).</summary>
   public string ProjectRoot(ProjectInfo project) => Resolve(project, ProjectPath.Root);

   public Task<IReadOnlyList<ProjectResourceInfo>> ListAsync(
      ProjectInfo project, ProjectPath folder, string? extension = null, CancellationToken ct = default)
   {
      var results = new List<ProjectResourceInfo>();
      var physical = Resolve(project, folder);
      if (!Directory.Exists(physical))
         return Task.FromResult<IReadOnlyList<ProjectResourceInfo>>(results);

      var wanted = NormalizeExtension(extension);

      // recursive: the asset pipeline scans a folder for every matching file (spec §2.3 step 7)
      foreach (var dir in SafeEnumerate(physical, directories: true))
      {
         ct.ThrowIfCancellationRequested();
         var relative = ToProjectPath(project, dir);
         results.Add(new ProjectResourceInfo(relative, Path.GetFileName(dir), true,
            UpdatedAt: Directory.GetLastWriteTimeUtc(dir)));
      }

      foreach (var file in SafeEnumerate(physical, directories: false))
      {
         ct.ThrowIfCancellationRequested();
         if (wanted is not null &&
             !string.Equals(Path.GetExtension(file), wanted, StringComparison.OrdinalIgnoreCase))
            continue;

         var relative = ToProjectPath(project, file);
         results.Add(new ProjectResourceInfo(relative, Path.GetFileName(file), false,
            new FileInfo(file).Length, File.GetLastWriteTimeUtc(file)));
      }

      results.Sort((a, b) => string.Compare(a.Path.Value, b.Path.Value, StringComparison.OrdinalIgnoreCase));
      return Task.FromResult<IReadOnlyList<ProjectResourceInfo>>(results);
   }

   public Task<bool> ExistsAsync(ProjectInfo project, ProjectPath path, CancellationToken ct = default)
   {
      var physical = Resolve(project, path);
      return Task.FromResult(File.Exists(physical) || Directory.Exists(physical));
   }

   public Task<Stream?> OpenReadAsync(ProjectInfo project, ProjectPath path, CancellationToken ct = default)
   {
      var physical = Resolve(project, path);
      if (!File.Exists(physical)) return Task.FromResult<Stream?>(null);
      return Task.FromResult<Stream?>(new MemoryStream(File.ReadAllBytes(physical)));
   }

   public Task WriteAsync(
      ProjectInfo project, ProjectPath path, Stream content, CancellationToken ct = default)
   {
      var physical = Resolve(project, path);
      var parent = Path.GetDirectoryName(physical);
      if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

      using var buffer = new MemoryStream();
      content.CopyTo(buffer);
      File.WriteAllBytes(physical, buffer.ToArray());
      return Task.CompletedTask;
   }

   public Task<ProjectPath> CreateFolderAsync(
      ProjectInfo project, ProjectPath folder, CancellationToken ct = default)
   {
      Directory.CreateDirectory(Resolve(project, folder));
      return Task.FromResult(folder);
   }

   public Task<bool> DeleteAsync(ProjectInfo project, ProjectPath path, CancellationToken ct = default)
   {
      var physical = Resolve(project, path);
      if (File.Exists(physical)) { File.Delete(physical); return Task.FromResult(true); }
      if (Directory.Exists(physical)) { Directory.Delete(physical, true); return Task.FromResult(true); }
      return Task.FromResult(false);
   }

   // ---------------------------------------------------------------------
   // path resolution — the only place a physical path is formed
   // ---------------------------------------------------------------------

   private string Resolve(ProjectInfo project, ProjectPath path)
   {
      var projectRoot = Path.GetFullPath(Path.Combine(
         _collectionRoot, Relative(project.Path)));
      var full = path.IsRoot
         ? projectRoot
         : Path.GetFullPath(Path.Combine(projectRoot, Relative(path)));

      var guard = projectRoot.EndsWith(Path.DirectorySeparatorChar)
         ? projectRoot : projectRoot + Path.DirectorySeparatorChar;
      if (!full.Equals(projectRoot, StringComparison.OrdinalIgnoreCase) &&
          !full.StartsWith(guard, StringComparison.OrdinalIgnoreCase))
      {
         throw new UnauthorizedAccessException(
            $"Project path '{path}' resolves outside the project '{project.Name}'.");
      }
      return full;
   }

   private ProjectPath ToProjectPath(ProjectInfo project, string physical)
      => ProjectPath.Parse("/" + Path.GetRelativePath(Resolve(project, ProjectPath.Root), physical));

   private static string Relative(ProjectPath path)
      => path.IsRoot ? string.Empty
         : path.Value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

   private static string? NormalizeExtension(string? extension)
   {
      if (string.IsNullOrWhiteSpace(extension)) return null;
      var value = extension.Trim();
      return value.StartsWith('.') ? value : "." + value;
   }

   /// <summary>Depth-first walk tolerating unreadable sub-folders (skips them rather than throwing).</summary>
   private static IEnumerable<string> SafeEnumerate(string root, bool directories)
   {
      var pending = new Stack<string>();
      pending.Push(root);

      while (pending.Count > 0)
      {
         var current = pending.Pop();

         string[] subFolders;
         try { subFolders = Directory.GetDirectories(current); }
         catch { subFolders = Array.Empty<string>(); }

         foreach (var subFolder in subFolders)
         {
            if (directories) yield return subFolder;
            pending.Push(subFolder);
         }

         if (directories) continue;

         string[] files;
         try { files = Directory.GetFiles(current); }
         catch { files = Array.Empty<string>(); }

         foreach (var file in files) yield return file;
      }
   }
}
