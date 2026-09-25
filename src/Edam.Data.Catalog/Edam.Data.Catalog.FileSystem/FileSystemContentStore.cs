using Edam.Data.Catalog.Contracts;

namespace Edam.Data.Catalog.FileSystem;

/// <summary>
/// FileSystem-<see cref="IContentStore"/> content back-end (BL-7.2, ADR-0006/0007). Binary content
/// is stored as real files under a <c>content/</c> directory, addressed path/URI-style
/// (<c>/docs/readme</c> → <c>&lt;content&gt;/docs/readme</c>). Provider-specifics stay behind the seam.
/// <para>
/// <b>LM-2b:</b> a store created with a <b>container scope</b> namespaces its content per container
/// (<c>&lt;content&gt;/&lt;scope&gt;/…</c>), so two containers may hold the same path. A store created
/// without a scope keeps the legacy layout, so existing single-container content still resolves.
/// </para>
/// </summary>
public sealed class FileSystemContentStore : IContentStore
{
   private readonly string _contentRoot;

   /// <param name="rootPath">The catalog root (content lives under its <c>content/</c> folder).</param>
   /// <param name="containerScope">The container this store namespaces content for (LM-2b); <c>null</c>
   /// or empty keeps the legacy unscoped layout.</param>
   public FileSystemContentStore(string rootPath, string? containerScope = null)
   {
      var root = Path.Combine(Path.GetFullPath(rootPath), "content");
      ContainerScope = string.IsNullOrWhiteSpace(containerScope) ? null : SafeScope(containerScope!);
      _contentRoot = ContainerScope is null ? root : Path.Combine(root, ContainerScope);
      Directory.CreateDirectory(_contentRoot);
   }

   /// <summary>The container whose content this instance addresses (<c>null</c> = unscoped/legacy).</summary>
   public string? ContainerScope { get; }

   public string RootPath => _contentRoot;

   /// <summary>
   /// A scope is a <b>folder name, never a path</b>: anything outside letters, digits, '.', '-' and '_'
   /// becomes '_' (so a scope can never escape the content root).
   /// </summary>
   private static string SafeScope(string scope)
   {
      var buffer = new System.Text.StringBuilder(scope.Trim().Length);
      foreach (var c in scope.Trim())
         buffer.Append(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' ? c : '_');
      return buffer.ToString();
   }

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
