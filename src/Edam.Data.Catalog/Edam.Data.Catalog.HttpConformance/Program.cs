// -----------------------------------------------------------------------------
// HTTP conformance runner (BL-7.x). Hosts the catalog REST service in-process (Kestrel) and drives
// it through the real Contracts REST client (CatalogHttpClient / CatalogHttpContainer|Item|ItemData),
// mirroring the store-conformance operations over the wire. Back-end by argument: filesystem (default,
// temp root) | postgres (live local DSN). This is the HTTP round-trip test the store-level conformance
// cannot cover — it proves the API methods are reachable and wire-conformant to the Contracts client.
// -----------------------------------------------------------------------------
using System.Net;
using System.Net.Sockets;
using System.Text;
using Edam.Data.Catalog.Contracts;
using Edam.Data.CatalogServiceClient;

var target = args.Length > 0 ? args[0] : "filesystem";
var fsRoot = Path.Combine(Path.GetTempPath(), "edam-http-conformance-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fsRoot);

Environment.SetEnvironmentVariable("Edam__Catalog__Target", target);
if (target.Equals("postgres", StringComparison.OrdinalIgnoreCase))
{
   Environment.SetEnvironmentVariable("ConnectionStrings__catalog",
      "Server=localhost;Port=5432;Database=edam;User Id=edam;Password=edam");
}
else
{
   Environment.SetEnvironmentVariable("Edam__Catalog__FileSystemRoot", fsRoot);
}

var port = GetFreePort();
using var app = Edam.Data.CatalogService.Program.BuildApp();
app.Urls.Add($"http://127.0.0.1:{port}");
await app.StartAsync();

try
{
   var baseUri = $"http://127.0.0.1:{port}/catalogservice/";
   var client = new CatalogHttpClient("httpCon", baseUri);
   var checks = await RunChecksAsync(client);

   var failed = checks.Count(c => !c.Passed);
   foreach (var c in checks)
      Console.WriteLine($"  [{(c.Passed ? "PASS" : "FAIL")}] {c.Name}: {c.Detail}");
   Console.WriteLine($"result: HTTP conformance {(failed == 0 ? "ALL CONFORM" : $"{failed} FAILED")} (target={target})");
}
finally
{
   await app.StopAsync();
   try { Directory.Delete(fsRoot, true); } catch { }
}

static async Task<IReadOnlyList<(string Name, bool Passed, string Detail)>> RunChecksAsync(CatalogHttpClient client)
{
   var checks = new List<(string, bool, string)>();
   void Add(string name, bool passed, string detail) => checks.Add((name, passed, detail));

   // session init (session/info) — required to construct the underlying WebApiClient
   var init = await client.InitializeClientAsync("httpCon", "");
   Add("Initialize session (session/info)", init != null, init?.ContainerId);

   var runId = Guid.NewGuid().ToString("N")[..8];
   var conId = "http-conformance-" + runId;
   var branchRoot = "/http-" + runId;
   var branchPath = branchRoot + "/branch";

   // 1 enlist container (container/enlist)
   var c = client.Container.EnlistContainer(conId, "HTTP conformance", null, ContainerType.FileSystem);
   Add("Enlist container (container/enlist)", c?.ContainerId == conId, c?.ContainerId);

   // 2 get container by name (container/info)
   var byName = client.Container.GetContainer(conId);
   Add("Get container by name (container/info)", byName?.ContainerId == conId, byName?.ContainerId);

   // 3 get container by id (container/id, client Guid lookup)
   var byId = client.Container.GetContainer(c!.Id);
   Add("Get container by id (container/id)", byId?.Id == c.Id, byId?.Id.ToString());

   // 4 create branch (catalog/item POST)
   var branch = client.Item.CreateBranch(branchPath, "HTTP branch", c.Id);
   Add("Create branch (catalog/item POST)", branch?.FullPath == branchPath, branch?.FullPath);

   // 5 get item by path (catalog/item/path)
   var byPath = client.Item.GetItemByPath(branchPath);
   Add("Get item by path (catalog/item/path)", byPath?.Id == branch?.Id, byPath?.FullPath);

   // 6 list containers (container/list)
   var list = client.Container.GetContainers();
   Add("List containers (container/list)", list.Any(x => x.ContainerId == conId), string.Join(",", list.Select(x => x.ContainerId)));

   // 7 get container items (container/items/id)
   var cItems = client.Item.GetContainerItems(c.Id);
   Add("Get container items (container/items/id)", cItems.Any(i => i.FullPath == branchPath), string.Join(",", cItems.Select(i => i.FullPath)));

   // 8 add data leaf (catalog/data/item POST)
   var leaf = new ItemDataInfo(Guid.NewGuid(), branch!.Id, "leaf", "text/plain", "default", "hello-http");
   var added = client.ItemData.AddItem(leaf);
   Add("Add data leaf (catalog/data/item POST)", added?.Id == leaf.Id, added?.Id.ToString());

   // 9 get data by name (catalog/data/item/name)
   var dn = client.ItemData.GetDataByName(branch.Id, "leaf");
   Add("Get data by name (catalog/data/item/name)", dn?.Name == "leaf" && dn.Value == "hello-http", dn?.Value);

   // 10 delete data (catalog/data/id DELETE)
   var del = client.ItemData.DeleteData(leaf.Id);
   Add("Delete data (catalog/data/id DELETE)", del, "");

   // 11 get content type (catalog/content/type/id)
   var ct = client.ItemData.GetContentType("application/json");
   Add("Get content type (catalog/content/type/id)", ct?.TypeId == "application/json", ct?.TypeId);

   // 12 get branch items (catalog/branch/items)
   var branchItems = client.Item.GetBranch(branchRoot);
   Add("Get branch items (catalog/branch/items)", branchItems.Any(i => i.FullPath == branchPath), string.Join(",", branchItems.Select(i => i.FullPath)));

   // 13-15 BL-7.5: the WinUI consumer path — the Model facade (StoreBackedCatalogService) adapted
   // over this SAME remote Contracts client, exercising the Model ICatalogService surface end-to-end.
   // This is what guarantees the desktop's local/remote swap works over the wire.
   var modelPath = branchRoot + "/model";
   var model = new StoreBackedCatalogService(client, "httpCon");

   var mBranch = model.Item.CreateBranch(modelPath, "model branch", c.Id);
   Add("Model facade: create branch", mBranch?.FullPath == modelPath, mBranch?.FullPath);

   var mLeaf = model.ItemData.AddItem(new Edam.Data.CatalogModel.ItemDataInfo
   {
      Id = Guid.NewGuid(),
      ItemId = mBranch!.Id,
      Name = "mleaf",
      ContentTypeId = "text/plain",
      PartitionId = "default",
      DataText = "hello-model"
   });
   Add("Model facade: add data leaf", mLeaf?.ItemId == mBranch.Id, mLeaf?.Id.ToString());

   var mRead = model.ItemData.GetDataByName(mBranch.Id, "mleaf");
   Add("Model facade: get data by name", mRead?.DataText == "hello-model", mRead?.DataText);

   // 16-19 BL-7.x / ADR-0007: content (IContentStore) over the wire — path-addressed, binary-safe
   // blob content on the same session. This is the piece that had no REST surface before.
   var contentPath = branchRoot + "/content/blob.bin";
   using (var payload = new MemoryStream(Encoding.UTF8.GetBytes("hello-content-http")))
      await client.Content.WriteAsync(contentPath, payload);

   var cExists = await client.Content.ExistsAsync(contentPath);
   Add("Content: write + exists (content/info)", cExists, contentPath);

   var readBack = await client.Content.OpenReadAsync(contentPath);
   string? readText = null;
   if (readBack is not null)
   {
      using var rms = new MemoryStream();
      await readBack.CopyToAsync(rms);
      readText = Encoding.UTF8.GetString(rms.ToArray());
      readBack.Dispose();
   }
   Add("Content: read round-trip (content/item GET)", readText == "hello-content-http", readText);

   var cDeleted = await client.Content.DeleteAsync(contentPath);
   var cGone = !await client.Content.ExistsAsync(contentPath);
   Add("Content: delete (content/item DELETE)", cDeleted && cGone, $"deleted={cDeleted} gone={cGone}");

   var missing = await client.Content.OpenReadAsync(branchRoot + "/content/absent.bin");
   Add("Content: missing returns null", missing is null, missing is null ? "(null)" : "(unexpected content)");

   return checks;
}

static int GetFreePort()
{
   var l = new TcpListener(IPAddress.Loopback, 0);
   l.Start();
   var port = ((IPEndPoint)l.LocalEndpoint).Port;
   l.Stop();
   return port;
}
