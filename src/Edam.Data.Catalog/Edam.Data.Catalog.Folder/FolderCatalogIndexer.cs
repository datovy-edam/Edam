using Edam.Data.Catalog.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace Edam.Data.Catalog.Folder;

/// <summary>What a folder index produced.</summary>
/// <param name="Folders">Folder items created (including the root/project branch).</param>
/// <param name="Files">File items created.</param>
public sealed record FolderIndexResult(int Folders, int Files)
{
   /// <summary>Total items created (folders + files).</summary>
   public int Total => Folders + Files;
}

/// <summary>
/// Ingests a real folder tree <b>into</b> a catalog (ADR-0007) through the Contracts seams:
/// directories become branch items, files become leaf items, and (optionally) file bytes are
/// written to the <see cref="IContentStore"/> at the same path (the item's full path is the content
/// key, so content never collides between containers/projects).
/// <para>
/// Provider-agnostic by construction — it drives <see cref="ICatalogContainer"/> /
/// <see cref="ICatalogItem"/> / <see cref="IContentStore"/>, so the SAME ingestion works against any
/// local provider (PostgreSQL, FileSystem) or a remote <c>ICatalogClient</c> over the wire.
/// </para>
/// <para>
/// <b><c>pathPrefix</c></b> places the folder <i>under</i> an existing path — used to import a
/// project as the branch <c>/Projects/&lt;name&gt;</c>. Callers that already own the container (e.g.
/// the project store) use the <see cref="Guid"/> overload, which does <b>not</b> enlist — so an
/// import can never rewrite the container's URI/description.
/// </para>
/// <para>
/// Item ids are <b>deterministic</b> (container id + full path — see <see cref="DeterministicId"/>),
/// so re-indexing is idempotent: existing items are updated in place rather than duplicated. The
/// catalog is a <b>snapshot</b> — call again to pick up folder changes.
/// </para>
/// </summary>
public static class FolderCatalogIndexer
{
   /// <summary>Index <paramref name="rootPath"/> into <paramref name="store"/> (convenience overload).</summary>
   public static async Task<int> IndexAsync(
      ICatalogStore store, IContentStore? content, string containerId, string rootPath,
      string? description = null, bool indexContent = true, CancellationToken ct = default)
      => (await IndexDetailedAsync(store, store, content, containerId, rootPath,
            description, indexContent, ct, pathPrefix: null).ConfigureAwait(false)).Total;

   /// <summary>
   /// Index <paramref name="rootPath"/> into the catalog, driving the three Contracts surfaces
   /// directly so both a local store and a remote client can be used.
   /// </summary>
   /// <returns>the number of items indexed (0 when the folder does not exist).</returns>
   public static async Task<int> IndexAsync(
      ICatalogContainer containers, ICatalogItem items, IContentStore? content,
      string containerId, string rootPath, string? description = null,
      bool indexContent = true, CancellationToken ct = default)
      => (await IndexDetailedAsync(containers, items, content, containerId, rootPath,
            description, indexContent, ct, pathPrefix: null).ConfigureAwait(false)).Total;

   /// <summary>Index a folder into a store, reporting folder/file counts separately.</summary>
   public static Task<FolderIndexResult> IndexDetailedAsync(
      ICatalogStore store, IContentStore? content, string containerId, string rootPath,
      string? description = null, bool indexContent = true, CancellationToken ct = default,
      string? pathPrefix = null)
      => IndexDetailedAsync(store, store, content, containerId, rootPath,
            description, indexContent, ct, pathPrefix);

   /// <summary>
   /// Index <paramref name="rootPath"/> into the catalog (optionally under
   /// <paramref name="pathPrefix"/>), enlisting <paramref name="containerId"/> on the way.
   /// </summary>
   public static async Task<FolderIndexResult> IndexDetailedAsync(
      ICatalogContainer containers, ICatalogItem items, IContentStore? content,
      string containerId, string rootPath, string? description = null,
      bool indexContent = true, CancellationToken ct = default, string? pathPrefix = null)
   {
      if (string.IsNullOrWhiteSpace(containerId))
         return new FolderIndexResult(0, 0);

      var container = containers.EnlistContainer(
         containerId,
         description ?? ("Folder catalog: " + rootPath),
         Directory.Exists(rootPath) ? Path.GetFullPath(rootPath) : rootPath,
         ContainerType.FileSystem);

      return await IndexDetailedAsync(container.Id, items, content, rootPath,
         indexContent, ct, pathPrefix).ConfigureAwait(false);
   }

   /// <summary>
   /// Index <paramref name="rootPath"/> into an <b>already-resolved</b> container (no enlistment).
   /// </summary>
   public static async Task<FolderIndexResult> IndexDetailedAsync(
      Guid containerId, ICatalogItem items, IContentStore? content, string rootPath,
      bool indexContent = true, CancellationToken ct = default, string? pathPrefix = null)
   {
      if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
         return new FolderIndexResult(0, 0);

      var root = Path.GetFullPath(rootPath);
      var prefix = NormalizePrefix(pathPrefix);

      var folders = 0;
      var files = 0;

      // the root item: the container root, or the project branch when a prefix is given
      await items.CreateBranchAsync(prefix.Length == 0 ? "/" : prefix, "root", containerId, ct);
      folders++;

      foreach (var dir in Walk(root, directories: true))
      {
         ct.ThrowIfCancellationRequested();
         var full = Combine(prefix, Relative(root, dir));
         var info = new DirectoryInfo(dir);
         await items.AddItemAsync(new ItemInfo(
            DeterministicId(containerId, full), containerId, full, info.Name, null,
            ItemType.Branch, info.CreationTimeUtc, info.LastWriteTimeUtc), ct);
         folders++;
      }

      foreach (var file in Walk(root, directories: false))
      {
         ct.ThrowIfCancellationRequested();
         var full = Combine(prefix, Relative(root, file));
         var info = new FileInfo(file);
         await items.AddItemAsync(new ItemInfo(
            DeterministicId(containerId, full), containerId, full, info.Name, null,
            ItemType.Leaf, info.CreationTimeUtc, info.LastWriteTimeUtc), ct);
         files++;

         if (indexContent && content is not null)
         {
            await using var stream = File.OpenRead(file);
            await content.WriteAsync(full, stream, ct);
         }
      }

      return new FolderIndexResult(folders, files);
   }

   /// <summary>
   /// Stable id for a (container, full path) pair — makes re-indexing idempotent and lets other
   /// catalog writers (e.g. the project store) address the same item.
   /// </summary>
   public static Guid DeterministicId(Guid containerId, string fullPath)
   {
      var hash = SHA256.HashData(Encoding.UTF8.GetBytes(containerId + "|" + fullPath));
      return new Guid(hash.AsSpan(0, 16));
   }

   /// <summary>Path relative to the root, normalized to a leading-slash catalog path.</summary>
   private static string Relative(string root, string fullPath)
   {
      var relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
      return relative.StartsWith('/') ? relative : "/" + relative;
   }

   private static string NormalizePrefix(string? pathPrefix)
      => string.IsNullOrWhiteSpace(pathPrefix) || pathPrefix == "/"
         ? string.Empty
         : "/" + pathPrefix.Replace('\\', '/').Trim('/');

   private static string Combine(string prefix, string relative)
      => prefix.Length == 0 ? relative : prefix + "/" + relative.TrimStart('/');

   /// <summary>
   /// Depth-first walk that tolerates unreadable sub-directories (skips them rather than throwing
   /// part-way through an enumeration).
   /// </summary>
   private static IEnumerable<string> Walk(string root, bool directories)
   {
      var pending = new Stack<string>();
      pending.Push(root);

      while (pending.Count > 0)
      {
         var current = pending.Pop();

         string[] children;
         try { children = Directory.GetDirectories(current); }
         catch { children = Array.Empty<string>(); }

         foreach (var child in children)
         {
            if (directories) yield return child;
            pending.Push(child);
         }

         if (directories) continue;

         string[] files;
         try { files = Directory.GetFiles(current); }
         catch { files = Array.Empty<string>(); }

         foreach (var file in files) yield return file;
      }
   }
}
