// -----------------------------------------------------------------------------
// PE-2 / PE-3 conformance runner. Runs the SAME project scenario against three interchangeable
// back-ends — the file-system providers, the Catalog-backed providers on a local file-system
// catalog, and the Catalog-backed providers over the REST API (the service hosted in-process) —
// and asserts the process current directory is never touched.
//   Edam.Data.Projects.Conformance
// -----------------------------------------------------------------------------
using System.Net;
using System.Net.Sockets;
using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.FileSystem;
using Edam.Data.Catalog.PostgreSql;
using Edam.Data.Projects.Catalog;
using Edam.Data.Projects.Conformance;
using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.DependencyInjection;
using Edam.Data.Projects.FileSystem;
using Microsoft.Extensions.DependencyInjection;

const string ContainerId = "pe3-projects";

// Optional: a PostgreSQL DSN as the first argument adds the postgres targets (the default run is
// hermetic — file-system + in-process HTTP — so it needs no database).
var dsn = args.Length > 0 && args[0].StartsWith("Server=", StringComparison.OrdinalIgnoreCase)
   ? args[0]
   : null;

var all = new Dictionary<string, List<ProjectScenario.Check>>(StringComparer.OrdinalIgnoreCase);
var cwdBefore = Directory.GetCurrentDirectory();
var temp = Path.Combine(Path.GetTempPath(), "edam-pe3-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);

try
{
   // ---- 1. file-system providers ------------------------------------------------------------
   {
      var root = Path.Combine(temp, "fs");
      Directory.CreateDirectory(root);
      var catalog = new FileSystemProjectCatalog(root);
      var store = new FileSystemProjectStore(catalog);
      var resources = new FileSystemProjectResources(root, catalog);

      var checks = await ProjectScenario.RunAsync(
         catalog, store, resources, Path.Combine(temp, "fs-work"));

      // file-system specific: project paths map to the documented physical layout.
      // (Asserts the arguments file + the produced output — the scenario deletes the archive input.)
      var physicalArgs = Path.Combine(root, "Projects", "Datovy.HC.CD", "Arguments", "Datovy.HC.CD.ToAssets.Args.json");
      var physicalOutput = Path.Combine(root, "Projects", "Datovy.HC.CD", "Documents", "datovy.hc.cd.dictionary.xlsx");
      checks.Add(new ProjectScenario.Check(
         "Project paths map to the documented physical layout",
         File.Exists(physicalArgs) && File.Exists(physicalOutput),
         Path.GetRelativePath(root, physicalArgs)));

      all["file-system"] = checks;
   }

   // ---- 2. Catalog-backed providers, local file-system catalog ------------------------------
   {
      var root = Path.Combine(temp, "cat");
      var store = new FileSystemCatalogStore(root);
      var content = new FileSystemContentStore(root);
      store.EnlistContainer(ContainerId, "PE-3 conformance collection", null, ContainerType.FileSystem);

      all["catalog (local)"] = await RunCatalogAsync(
         store, store, content, Path.Combine(temp, "cat-work"), ContainerId);
   }

   // ---- 3. Catalog-backed providers over the REST API ---------------------------------------
   {
      var root = Path.Combine(temp, "remote");
      Directory.CreateDirectory(root);
      Environment.SetEnvironmentVariable("Edam__Catalog__Target", "filesystem");
      Environment.SetEnvironmentVariable("Edam__Catalog__FileSystemRoot", root);
      // keep the in-process host's request logging out of the report
      Environment.SetEnvironmentVariable("Logging__LogLevel__Default", "Warning");

      var port = GetFreePort();
      using var app = Edam.Data.CatalogService.Program.BuildApp();
      app.Urls.Add($"http://127.0.0.1:{port}");
      await app.StartAsync();

      try
      {
         var client = new Edam.Data.CatalogServiceClient.CatalogHttpClient(
            "pe3", $"http://127.0.0.1:{port}/catalogservice/");
         await client.InitializeClientAsync("pe3", "");
         client.Container.EnlistContainer(ContainerId, "PE-3 remote collection", null, ContainerType.FileSystem);

         all["catalog (remote HTTP)"] = await RunCatalogAsync(
            client.Container, client.Item, client.Content, Path.Combine(temp, "remote-work"), ContainerId);
      }
      finally
      {
         await app.StopAsync();
      }
   }

   // ---- 4/5. PostgreSQL targets (only when a DSN is supplied) --------------------------------
   if (dsn is not null)
   {
      var run = Guid.NewGuid().ToString("N")[..8];

      // local provider
      {
         var store = new PostgreSqlCatalogStore(dsn);
         var content = new PostgreSqlContentStore(dsn);
         var containerId = "pe3-pg-" + run;
         store.EnlistContainer(containerId, "PE-3 postgres collection", null, ContainerType.FileSystem);

         all["catalog (postgres, local)"] = await RunCatalogAsync(
            store, store, content, Path.Combine(temp, "pg-work"), containerId);
      }

      // remote over the REST API, service backed by postgres
      {
         var root = Path.Combine(temp, "pg-remote");
         Directory.CreateDirectory(root);
         Environment.SetEnvironmentVariable("Edam__Catalog__Target", "postgres");
         Environment.SetEnvironmentVariable("ConnectionStrings__catalog", dsn);
         Environment.SetEnvironmentVariable("Logging__LogLevel__Default", "Warning");

         var port = GetFreePort();
         using var app = Edam.Data.CatalogService.Program.BuildApp();
         app.Urls.Add($"http://127.0.0.1:{port}");
         await app.StartAsync();

         try
         {
            var client = new Edam.Data.CatalogServiceClient.CatalogHttpClient(
               "pe3", $"http://127.0.0.1:{port}/catalogservice/");
            await client.InitializeClientAsync("pe3", "");

            var containerId = "pe3-pg-remote-" + run;
            client.Container.EnlistContainer(
               containerId, "PE-3 postgres (remote) collection", null, ContainerType.FileSystem);

            all["catalog (postgres, remote HTTP)"] = await RunCatalogAsync(
               client.Container, client.Item, client.Content,
               Path.Combine(temp, "pg-remote-work"), containerId);
         }
         finally
         {
            await app.StopAsync();
         }
      }
   }

   // ---- 6/7/8. through the DI composition root (PE-4) ----------------------------------------
   {
      var root = Path.Combine(temp, "di-fs");
      using var provider = new ServiceCollection()
         .AddProjectServices(new Dictionary<string, string>
         {
            ["Edam:Projects:Target"] = "filesystem",
            ["Edam:Projects:Root"] = root,
         })
         .BuildServiceProvider();

      all["di (file-system)"] = await RunViaProviderAsync(provider, Path.Combine(temp, "di-fs-work"));
   }
   {
      // the collection must already exist: registering collections is a configuration concern
      var root = Path.Combine(temp, "di-cat");
      using var provider = new ServiceCollection()
         .AddProjectServices(new Dictionary<string, string>
         {
            ["Edam:Projects:Target"] = "catalog",
            ["Edam:Projects:DefaultCollection"] = "di-collection",
            ["Edam:Catalog:Target"] = "filesystem",
            ["Edam:Catalog:FileSystemRoot"] = root,
         })
         .BuildServiceProvider();

      provider.GetRequiredService<ICatalogStore>()
         .EnlistContainer("di-collection", "DI catalog collection", null, ContainerType.FileSystem);

      all["di (catalog, local file-system)"] =
         await RunViaProviderAsync(provider, Path.Combine(temp, "di-cat-work"));
   }
   if (dsn is not null)
   {
      var containerId = "di-pg-" + Guid.NewGuid().ToString("N")[..8];
      using var provider = new ServiceCollection()
         .AddProjectServices(new Dictionary<string, string>
         {
            ["Edam:Projects:Target"] = "catalog",
            ["Edam:Projects:DefaultCollection"] = containerId,
            ["Edam:Catalog:Target"] = "postgres",
            ["ConnectionStrings:catalog"] = dsn,
         })
         .BuildServiceProvider();

      provider.GetRequiredService<ICatalogStore>()
         .EnlistContainer(containerId, "DI postgres collection", null, ContainerType.FileSystem);

      all["di (catalog, postgres)"] =
         await RunViaProviderAsync(provider, Path.Combine(temp, "di-pg-work"));
   }
}
finally
{
   try { Directory.Delete(temp, true); } catch { }
}

// ---- report ---------------------------------------------------------------------------------
var failed = 0;
foreach (var (target, checks) in all)
{
   var targetFailed = checks.Count(c => !c.Passed);
   failed += targetFailed;
   Console.WriteLine($"--- {target}: {(targetFailed == 0 ? "ALL CONFORM" : $"{targetFailed} FAILED")} ({checks.Count} checks)");
   foreach (var check in checks)
      Console.WriteLine($"  [{(check.Passed ? "PASS" : "FAIL")}] {check.Name}: {check.Detail}");
}

var cwdAfter = Directory.GetCurrentDirectory();
var cwdUnchanged = cwdBefore == cwdAfter;
Console.WriteLine($"  [{(cwdUnchanged ? "PASS" : "FAIL")}] No process current-directory change: '{cwdBefore}' -> '{cwdAfter}'");
if (!cwdUnchanged) failed++;

Console.WriteLine($"result: projects conformance (PE-2/PE-3/PE-4) {(failed == 0 ? "ALL CONFORM" : $"{failed} FAILED")}");

// ---------------------------------------------------------------------------------------------

static async Task<List<ProjectScenario.Check>> RunCatalogAsync(
   ICatalogContainer containers, ICatalogItem items, IContentStore content,
   string workRoot, string containerId)
{
   var catalog = new CatalogProjectCatalog(containers, items, containerId);
   var store = new CatalogProjectStore(catalog, containers, items, content);
   var resources = new CatalogProjectResources(containers, items, content);
   return await ProjectScenario.RunAsync(catalog, store, resources, workRoot);
}

static async Task<List<ProjectScenario.Check>> RunViaProviderAsync(
   IServiceProvider provider, string workRoot)
   => await ProjectScenario.RunAsync(
      provider.GetRequiredService<IProjectCatalog>(),
      provider.GetRequiredService<IProjectStore>(),
      provider.GetRequiredService<IProjectResources>(),
      workRoot);

static int GetFreePort()
{
   var listener = new TcpListener(IPAddress.Loopback, 0);
   listener.Start();
   var port = ((IPEndPoint)listener.LocalEndpoint).Port;
   listener.Stop();
   return port;
}
