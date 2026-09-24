using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.FileSystem;

/// <summary>
/// File-system <see cref="IProjectSeeder"/> (LM-5): the template's <b>address</b> resolves to a file
/// under the collection root (e.g. <c>catalog://collection/Templates/ToAssets.Args.json</c> →
/// <c>&lt;root&gt;/Templates/ToAssets.Args.json</c>) and the copy is written through
/// <see cref="IProjectResources"/>, so nothing configures a path and the process current directory is
/// never touched.
/// </summary>
public sealed class FileSystemProjectSeeder : IProjectSeeder
{
   private readonly string _root;
   private readonly IProjectResources _resources;

   /// <param name="root">The collection root (the app-data root).</param>
   /// <param name="resources">The project resources the seeded copy is written through.</param>
   public FileSystemProjectSeeder(string root, IProjectResources resources)
   {
      if (string.IsNullOrWhiteSpace(root))
         throw new ArgumentException("A collection root is required.", nameof(root));

      _root = Path.GetFullPath(root);
      _resources = resources ?? throw new ArgumentNullException(nameof(resources));
   }

   public async Task<ProjectPath?> SeedArgumentsAsync(
      ProjectInfo project, CatalogAddress template, string? fileName = null,
      CancellationToken ct = default)
   {
      ArgumentNullException.ThrowIfNull(project);

      var relative = template.Path.Value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
      var source = Path.Combine(_root, relative);
      if (!File.Exists(source)) return null;

      var target = ProjectSeeding.TargetPath(project, template, fileName);

      await using var stream = File.OpenRead(source);
      await _resources.WriteAsync(project, target, stream, ct).ConfigureAwait(false);
      return target;
   }
}
