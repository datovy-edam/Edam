using Edam.Data.Catalog.Conformance;
using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Edam.Data.Catalog.Tests;

/// <summary>
/// Provider-conformance + durability tests for the FileSystem back-end (BL-7.2, ADR-0006/0007).
/// The SAME provider-conformance scenario (ADR-0006) is driven against the file-system store via
/// <see cref="CatalogScenario"/> — the same checks that PostgreSQL and in-memory pass.
/// </summary>
[TestClass]
public class FileSystemCatalogStoreTests
{
   private static string NewTempDir()
   {
      var dir = Path.Combine(Path.GetTempPath(), "edam-fs-tests-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(dir);
      return dir;
   }

   /// <summary>FileSystem store + content store pass the shared provider-conformance scenario (ADR-0006).</summary>
   [TestMethod]
   public async Task FileSystem_Provider_Conforms_ToSharedScenario()
   {
      var tmp = NewTempDir();
      try
      {
         var store = new FileSystemCatalogStore(tmp);
         var content = new FileSystemContentStore(tmp);
         var checks = await CatalogScenario.RunAsync(store, content);
         var failed = checks.Where(c => !c.Passed).ToList();
         Assert.AreEqual(0, failed.Count, string.Join("; ", failed.Select(f => $"{f.Name}: {f.Detail}")));
         Assert.AreEqual("filesystem", store.DescribeStore());
      }
      finally { Directory.Delete(tmp, true); }
   }

   /// <summary>The in-memory reference provider also passes the scenario (guardrail).</summary>
   [TestMethod]
   public async Task InMemory_Reference_Conforms_ToSharedScenario()
   {
      var checks = await CatalogScenario.RunAsync(new InMemoryCatalogStore(), null);
      var failed = checks.Where(c => !c.Passed).ToList();
      Assert.AreEqual(0, failed.Count, string.Join("; ", failed.Select(f => $"{f.Name}: {f.Detail}")));
   }

   /// <summary>State is durable across store instances (re-open same root → data still there).</summary>
   [TestMethod]
   public void FileSystem_Store_Persists_AcrossInstances()
   {
      var tmp = NewTempDir();
      try
      {
         var containerId = "durable";
         var s1 = new FileSystemCatalogStore(tmp);
         var c1 = s1.EnlistContainer(containerId, "durable container", null, ContainerType.FileSystem);
         var b1 = s1.CreateBranch("/x", null, c1.Id);
         var d1 = s1.CreateDataLeaf(b1, "note", dataValue: "kept");

         var s2 = new FileSystemCatalogStore(tmp); // re-open the same root
         var c2 = s2.GetContainer(containerId);
         Assert.IsNotNull(c2, "container survives re-open");
         Assert.AreEqual(c1.Id, c2!.Id);
         var b2 = s2.GetItemByPath("/x");
         Assert.IsNotNull(b2, "item survives re-open");
         Assert.AreEqual(b1.Id, b2!.Id);
         var d2 = s2.GetDataByName(b1.Id, "note");
         Assert.IsNotNull(d2, "item-data survives re-open");
         Assert.AreEqual("kept", d2!.Value);
      }
      finally { Directory.Delete(tmp, true); }
   }

   /// <summary>IContentStore round-trips binary content as real files, path/URI-addressed.</summary>
   [TestMethod]
   public async Task FileSystem_ContentStore_RoundTrips()
   {
      var tmp = NewTempDir();
      try
      {
         var cs = new FileSystemContentStore(tmp);
         var bytes = Encoding.UTF8.GetBytes("hello-content");
         await cs.WriteAsync("/docs/readme", new MemoryStream(bytes));

         Assert.IsTrue(await cs.ExistsAsync("/docs/readme"), "write/exists");
         using var opened = await cs.OpenReadAsync("/docs/readme");
         var roundtrip = opened is not null && ((MemoryStream)opened).ToArray().SequenceEqual(bytes);
         Assert.IsTrue(roundtrip, "read roundtrip");
         Assert.IsTrue(File.Exists(Path.Combine(tmp, "content", "docs", "readme")), "content stored as a physical file");

         Assert.IsTrue(await cs.DeleteAsync("/docs/readme"), "delete");
         Assert.IsFalse(await cs.ExistsAsync("/docs/readme"), "gone after delete");
         Assert.IsFalse(await cs.DeleteAsync("/docs/readme"), "delete of absent returns false");
      }
      finally { Directory.Delete(tmp, true); }
   }
}
