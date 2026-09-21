using Edam.Data.Catalog.Conformance;
using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.FileSystem;
using Edam.Data.Catalog.Folder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Edam.Data.Catalog.Tests;

/// <summary>
/// Folder ingestion (BL-7.x / ADR-0007): a real folder tree is indexed INTO a catalog by driving
/// the Contracts seams, so "open a folder as a catalog" works against any provider. This replaces
/// the two retired Model-client harnesses (<c>TestFileSystemClient</c>/<c>TestCatalogClone</c>) for
/// this behavior — the indexer is exercised here and end-to-end by the HTTP conformance runner.
/// </summary>
[TestClass]
public class FolderCatalogIndexerTests
{
   private static string NewTempDir()
   {
      var dir = Path.Combine(Path.GetTempPath(), "edam-index-tests-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(dir);
      return dir;
   }

   /// <summary>A folder to index (2 files in 1 sub-folder) plus a separate dir to store the catalog in.</summary>
   private static async Task<(string root, string storeDir)> NewFolder()
   {
      var root = NewTempDir();
      Directory.CreateDirectory(Path.Combine(root, "sub"));
      await File.WriteAllTextAsync(Path.Combine(root, "readme.txt"), "hello-folder");
      await File.WriteAllTextAsync(Path.Combine(root, "sub", "nested.txt"), "nested");
      return (root, NewTempDir());
   }

   [TestMethod]
   public async Task IndexesFolderAsItemsAndContent()
   {
      var (root, storeDir) = await NewFolder();
      try
      {
         // any provider works — the indexer drives the seams, not an implementation
         var store = new FileSystemCatalogStore(storeDir);
         var content = new FileSystemContentStore(storeDir);

         var indexed = await FolderCatalogIndexer.IndexAsync(store, content, "folder-test", root);
         Assert.IsTrue(indexed >= 4, $"expected at least 4 items, got {indexed}");

         var leaf = await store.GetItemByPathAsync("/readme.txt");
         Assert.IsNotNull(leaf, "file item was not indexed");
         Assert.AreEqual(ItemType.Leaf, leaf!.Type);

         var branch = await store.GetItemByPathAsync("/sub");
         Assert.IsNotNull(branch, "folder item was not indexed");
         Assert.AreEqual(ItemType.Branch, branch!.Type);

         using var stream = await content.OpenReadAsync("/readme.txt");
         Assert.IsNotNull(stream, "indexed content is missing");
         using var buffer = new MemoryStream();
         await stream!.CopyToAsync(buffer);
         Assert.AreEqual("hello-folder", Encoding.UTF8.GetString(buffer.ToArray()));
      }
      finally
      {
         Directory.Delete(root, true);
         Directory.Delete(storeDir, true);
      }
   }

   [TestMethod]
   public async Task ReIndexingIsIdempotent()
   {
      var (root, storeDir) = await NewFolder();
      try
      {
         var store = new FileSystemCatalogStore(storeDir);
         var content = new FileSystemContentStore(storeDir);

         await FolderCatalogIndexer.IndexAsync(store, content, "folder-test", root);
         await FolderCatalogIndexer.IndexAsync(store, content, "folder-test", root);

         var container = await store.GetContainerAsync("folder-test");
         Assert.IsNotNull(container, "indexed container missing");

         // '/' + '/readme.txt' + '/sub' + '/sub/nested.txt' — deterministic ids, so no duplicates
         var items = store.GetContainerItems(container!.Id);
         Assert.AreEqual(4, items.Count, string.Join(",", items.Select(i => i.FullPath)));
      }
      finally
      {
         Directory.Delete(root, true);
         Directory.Delete(storeDir, true);
      }
   }

   [TestMethod]
   public async Task MissingFolderIsANoOp()
   {
      var storeDir = NewTempDir();
      try
      {
         var store = new FileSystemCatalogStore(storeDir);
         var missing = Path.Combine(Path.GetTempPath(), "edam-absent-" + Guid.NewGuid().ToString("N"));

         var indexed = await FolderCatalogIndexer.IndexAsync(store, null, "missing", missing);
         Assert.AreEqual(0, indexed);
      }
      finally { Directory.Delete(storeDir, true); }
   }
}
