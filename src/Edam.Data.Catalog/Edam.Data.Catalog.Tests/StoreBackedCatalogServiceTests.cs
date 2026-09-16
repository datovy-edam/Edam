using Edam.Data.Catalog.FileSystem;
using Edam.Data.CatalogServiceClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Edam.Data.Catalog.Tests;

/// <summary>
/// Unit tests for <see cref="StoreBackedCatalogService"/> — the Model <c>ICatalogService</c> facade
/// that adapts the provider-agnostic Contracts <c>ICatalogStore</c> onto the Model surface the WinUI
/// desktop path consumes (BL-7.4 / BL-7.2). Driven here against the FileSystem store, so the
/// Model↔Contracts mapping + round-trip are exercised offline.
/// </summary>
[TestClass]
public class StoreBackedCatalogServiceTests
{
   private static string NewTempDir()
   {
      var dir = Path.Combine(Path.GetTempPath(), "edam-facade-tests-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(dir);
      return dir;
   }

   [TestMethod]
   public void Facade_RoundTrips_ContainerBranchAndData_ViaFileSystemStore()
   {
      var tmp = NewTempDir();
      try
      {
         var store = new FileSystemCatalogStore(tmp);
         var svc = new StoreBackedCatalogService(store);

         var container = svc.Container.EnlistContainer(
            "c1", "facade container", null, Edam.Data.CatalogModel.ContainerType.DataContext);
         Assert.IsNotNull(container);
         Assert.AreEqual("c1", container.ContainerId);

         svc.Container.SetContainer("session", container.ContainerId);
         var branch = svc.Item.CreateBranch("/a/b", "branch");
         Assert.AreEqual("/a/b", branch.FullPath);

         var found = svc.Item.GetItemByPath("/a/b");
         Assert.IsNotNull(found);
         Assert.AreEqual(branch.Id, found!.Id, "item resolvable by path through the Model facade");

         var data = svc.ItemData.CreateDataLeaf(branch, "note", dataValue: "hello-store-backed");
         var saved = svc.ItemData.AddItem(data);
         Assert.AreEqual(data.Id, saved.Id);
         var read = svc.ItemData.GetDataByName(branch.Id, "note");
         Assert.IsNotNull(read);
         Assert.AreEqual("hello-store-backed", read!.DataText, "leaf value round-trips through the facade");
      }
      finally { Directory.Delete(tmp, true); }
   }

   [TestMethod]
   public void Facade_GetContainersAndBranch_Enumerate()
   {
      var tmp = NewTempDir();
      try
      {
         var store = new FileSystemCatalogStore(tmp);
         var svc = new StoreBackedCatalogService(store);
         svc.Container.EnlistContainer("alpha", "alpha container");
         svc.Container.EnlistContainer("beta", "beta container");

         var containers = svc.Container.GetContainers();
         Assert.AreEqual(2, containers.Count);

         var container = svc.Container.GetContainer("alpha");
         Assert.IsNotNull(container);
         svc.Container.SetContainer("s", container!.ContainerId);
         var branch = svc.Item.CreateBranch("/alpha/x", "x", container.Id);
         var items = svc.Item.GetContainerItems(container.Id);
         Assert.IsTrue(items.Any(i => i.FullPath == "/alpha/x"), "branch appears in container items");
      }
      finally { Directory.Delete(tmp, true); }
   }
}
