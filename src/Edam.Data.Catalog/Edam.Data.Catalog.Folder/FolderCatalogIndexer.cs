using Edam.Data.Catalog.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace Edam.Data.Catalog.Folder;

/// <summary>
/// Ingests a real folder tree <b>into</b> a catalog (ADR-0007) through the Contracts seams:
/// directories become branch items, files become leaf items, and (optionally) file bytes are
/// written to the <see cref="IContentStore"/> at the same path.
/// <para>
/// Provider-agnostic by construction — it drives <see cref="ICatalogContainer"/> /
/// <see cref="ICatalogItem"/> / <see cref="IContentStore"/>, so the SAME ingestion works against any
/// local provider (PostgreSQL, FileSystem) or a remote <c>ICatalogClient</c> over the wire. This is
/// what lets "open a folder as a catalog" be resolved through DI like every other catalog target
/// instead of a bespoke client.
/// </para>
/// <para>
/// Item ids are <b>deterministic</b> (derived from container + relative path), so re-indexing the
/// same folder is idempotent: existing items are updated in place rather than duplicated. The
/// catalog is a <b>snapshot</b> — call again to pick up folder changes.
/// </para>
/// </summary>
public static class FolderCatalogIndexer
{
   /// <summary>Index <paramref name="rootPath"/> into <paramref name="store"/> (convenience overload).</summary>
   public static Task<int> IndexAsync(
      ICatalogStore store, IContentStore? content, string containerId, string rootPath,
      string? description = null, bool indexContent = true, CancellationToken ct = default)
      => IndexAsync(store, store, content, containerId, rootPath, description, indexContent, ct);

   /// <summary>
   /// Index <paramref name="rootPath"/> into the catalog, driving the three Contracts surfaces
   /// directly so both a local store and a remote client can be used.
   /// </summary>
   /// <returns>the number of items indexed (0 when the folder does not exist).</returns>
   public static async Task<int> IndexAsync(
      ICatalogContainer containers, ICatalogItem items, IContentStore? content,
      string containerId, string rootPath, string? description = null,
      bool indexContent = true, CancellationToken ct = default)
   {
      if (string.IsNullOrWhiteSpace(containerId) || string.IsNullOrWhiteSpace(rootPath))
         return 0;
      if (!Directory.Exists(rootPath))
         return 0;

      var root = Path.GetFullPath(rootPath);

      var container = containers.EnlistContainer(
         containerId, description ?? ("Folder catalog: " + root), root, ContainerType.FileSystem);

      var indexed = 0;

      // the container's root item
      await items.CreateBranchAsync("/", "root", container.Id, ct);
      indexed++;

      foreach (var dir in Walk(root, directories: true))
      {
         ct.ThrowIfCancellationRequested();
         var relative = Relative(root, dir);
         var info = new DirectoryInfo(dir);
         await items.AddItemAsync(new ItemInfo(
            DeterministicId(containerId, relative), container.Id, relative, info.Name, null,
            ItemType.Branch, info.CreationTimeUtc, info.LastWriteTimeUtc), ct);
         indexed++;
      }

      foreach (var file in Walk(root, directories: false))
      {
         ct.ThrowIfCancellationRequested();
         var relative = Relative(root, file);
         var info = new FileInfo(file);
         await items.AddItemAsync(new ItemInfo(
            DeterministicId(containerId, relative), container.Id, relative, info.Name, null,
            ItemType.Leaf, info.CreationTimeUtc, info.LastWriteTimeUtc), ct);
         indexed++;

         if (indexContent && content is not null)
         {
            await using var stream = File.OpenRead(file);
            await content.WriteAsync(relative, stream, ct);
         }
      }

      return indexed;
   }

   /// <summary>Path relative to the root, normalized to a leading-slash catalog path.</summary>
   private static string Relative(string root, string fullPath)
   {
      var relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
      return relative.StartsWith('/') ? relative : "/" + relative;
   }

   /// <summary>Stable id for a (container, path) pair — makes re-indexing idempotent.</summary>
   private static Guid DeterministicId(string containerId, string relativePath)
   {
      var hash = SHA256.HashData(Encoding.UTF8.GetBytes(containerId + "|" + relativePath));
      return new Guid(hash.AsSpan(0, 16));
   }

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
