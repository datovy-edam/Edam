using Edam.Data.Catalog.Contracts;

namespace Edam.Data.Catalog.FileSystem;

/// <summary>
/// FileSystem-<see cref="IContentStore"/> content back-end (BL-7.2, ADR-0006/0007). Binary content
/// is stored as real files under a <c>content/</c> directory, addressed path/URI-style
/// (<c>/docs/readme</c> → <c>&lt;content&gt;/docs/readme</c>). Provider-specifics stay behind the seam.
/// </summary>
public sealed class FileSystemContentStore : IContentStore
{
   private readonly string _contentRoot;

   public FileSystemContentStore(string rootPath)
   {
      _contentRoot = Path.Combine(Path.GetFullPath(rootPath), "content");
      Directory.CreateDirectory(_contentRoot);
   }

   public string RootPath => _contentRoot;

   private static string Resolve(string root, string resourcePath)
   {
      var rel = (resourcePath ?? string.Empty).Trim('/').Replace('/', Path.DirectorySeparatorChar);
      return string.IsNullOrEmpty(rel) ? Path.Combine(root, "_root_") : Path.Combine(root, rel);
   }

   public Task<Stream?> OpenReadAsync(string resourcePath, CancellationToken ct = default)
   {
      var p = Resolve(_contentRoot, resourcePath);
      if (!File.Exists(p)) return Task.FromResult<Stream?>(null);
      return Task.FromResult<Stream?>(new MemoryStream(File.ReadAllBytes(p)));
   }

   public Task WriteAsync(string resourcePath, Stream content, CancellationToken ct = default)
   {
      var p = Resolve(_contentRoot, resourcePath);
      Directory.CreateDirectory(Path.GetDirectoryName(p)!);
      using var ms = new MemoryStream();
      content.CopyTo(ms);
      File.WriteAllBytes(p, ms.ToArray());
      return Task.CompletedTask;
   }

   public Task<bool> DeleteAsync(string resourcePath, CancellationToken ct = default)
   {
      var p = Resolve(_contentRoot, resourcePath);
      if (!File.Exists(p)) return Task.FromResult(false);
      File.Delete(p);
      return Task.FromResult(true);
   }

   public Task<bool> ExistsAsync(string resourcePath, CancellationToken ct = default)
      => Task.FromResult(File.Exists(Resolve(_contentRoot, resourcePath)));
}
