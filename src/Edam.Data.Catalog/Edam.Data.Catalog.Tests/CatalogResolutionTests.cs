using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.DependencyInjection;
using Edam.Data.Catalog.FileSystem;
using Edam.Data.Catalog.PostgreSql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
// 'CatalogServices' exists in both DependencyInjection and PostgreSql namespaces; alias the DI one.
using diCatalogServices = Edam.Data.Catalog.DependencyInjection.CatalogServices;

namespace Edam.Data.Catalog.Tests;

/// <summary>
/// BL-7.4 tests: the per-<b>Container</b> provider registry (<see cref="CatalogProviderRegistry"/>)
/// resolves a <see cref="ContainerBinding"/> to the right back-end by <see cref="ContainerType"/>
/// (FileSystem vs PostgreSQL) and falls back to the default target; default-target string mapping.
/// Construction of the PostgreSQL provider is lazy (no connection is opened), so these are offline.
/// </summary>
[TestClass]
public class CatalogResolutionTests
{
   private static string NewTempDir()
   {
      var dir = Path.Combine(Path.GetTempPath(), "edam-resolve-tests-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(dir);
      return dir;
   }

   [TestMethod]
   public void Registry_Resolves_PerContainer_Targets()
   {
      var fsRoot = NewTempDir();
      try
      {
         var fsStore = new FileSystemCatalogStore(fsRoot);
         var fsContent = new FileSystemContentStore(fsRoot);
         var pgStore = new PostgreSqlCatalogStore("Server=localhost;Port=5432;Database=edam;User Id=edam;Password=edam");
         var pgContent = new PostgreSqlContentStore("Server=localhost;Port=5432;Database=edam;User Id=edam;Password=edam");

         var stores = new Dictionary<ContainerType, ICatalogStore?>
         {
            [ContainerType.FileSystem] = fsStore,
            [ContainerType.PostgreSql] = pgStore
         };
         var contents = new Dictionary<ContainerType, IContentStore?>
         {
            [ContainerType.FileSystem] = fsContent,
            [ContainerType.PostgreSql] = pgContent
         };
         var registry = new CatalogProviderRegistry(stores, contents, ContainerType.PostgreSql);

         var fsBinding = new ContainerBinding("fs", ContainerType.FileSystem);
         Assert.AreSame(fsStore, registry.ResolveStore(fsBinding), "FileSystem container -> FileSystem store");
         Assert.AreSame(fsContent, registry.ResolveContent(fsBinding), "FileSystem container -> FileSystem content");

         var pgBinding = new ContainerBinding("pg", ContainerType.PostgreSql);
         Assert.AreSame(pgStore, registry.ResolveStore(pgBinding), "PostgreSql container -> PostgreSql store");
         Assert.AreSame(pgContent, registry.ResolveContent(pgBinding), "PostgreSql container -> PostgreSql content");

         // A target with no registered provider falls back to the default target (hidden back-end).
         var svcBinding = new ContainerBinding("svc", ContainerType.Service);
         Assert.AreSame(pgStore, registry.ResolveStore(svcBinding));
         Assert.AreSame(pgStore, registry.DefaultStore, "default hidden back-end is the default target's store");
      }
      finally { Directory.Delete(fsRoot, true); }
   }

   [TestMethod]
   public void Registry_Unconfigured_Target_Resolves_Null()
   {
      var registry = new CatalogProviderRegistry(
         new Dictionary<ContainerType, ICatalogStore?> { [ContainerType.FileSystem] = null },
         new Dictionary<ContainerType, IContentStore?> { [ContainerType.FileSystem] = null },
         ContainerType.FileSystem);
      Assert.IsNull(registry.ResolveStore(new ContainerBinding("f", ContainerType.FileSystem)));
      Assert.IsNull(registry.DefaultStore);
      Assert.IsNull(registry.DefaultContent);
   }

   [TestMethod]
   public void ParseTarget_Maps_Strings()
   {
      Assert.AreEqual(ContainerType.PostgreSql, diCatalogServices.ParseTarget("postgres"));
      Assert.AreEqual(ContainerType.PostgreSql, diCatalogServices.ParseTarget("pg"));
      Assert.AreEqual(ContainerType.PostgreSql, diCatalogServices.ParseTarget("postgresql"));
      Assert.AreEqual(ContainerType.FileSystem, diCatalogServices.ParseTarget("filesystem"));
      Assert.AreEqual(ContainerType.FileSystem, diCatalogServices.ParseTarget("fs"));
      Assert.AreEqual(ContainerType.Service, diCatalogServices.ParseTarget("service"));
      Assert.AreEqual(ContainerType.PostgreSql, diCatalogServices.ParseTarget(null));
      Assert.AreEqual(ContainerType.PostgreSql, diCatalogServices.ParseTarget("unknown"));
   }
}
