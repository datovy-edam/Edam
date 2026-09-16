using Edam.Data.Catalog.Conformance;
using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.FileSystem;
using Edam.Data.Catalog.PostgreSql;

// Provider-conformance runner (ADR-0006). Same scenario, swappable providers:
//   Edam.Data.Catalog.Conformance                          -> in-memory (offline, reference)
//   Edam.Data.Catalog.Conformance postgres "<connection>" -> PostgreSQL (live DB)
//   Edam.Data.Catalog.Conformance filesystem "<root dir>" -> file-system (durable, offline)
var mode = args.Length > 0 ? args[0] : "memory";

object store;
IContentStore? content = null;
string providerName;
if (mode.Equals("postgres", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2) { Console.Error.WriteLine("postgres requires a connection string"); return 2; }
    var pg = new PostgreSqlCatalogStore(args[1]);
    await pg.EnsureSchemaAsync();
    store = pg;
    content = new PostgreSqlContentStore(args[1]);
    providerName = "postgres";
}
else if (mode.Equals("filesystem", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2) { Console.Error.WriteLine("filesystem requires a root directory"); return 2; }
    store = new FileSystemCatalogStore(args[1]);
    content = new FileSystemContentStore(args[1]);
    providerName = "filesystem";
}
else
{
    store = new InMemoryCatalogStore();
    providerName = "in-memory";
}

var checks = await CatalogScenario.RunAsync((ICatalogStore)store, content);

Console.WriteLine($"Provider-conformance: {providerName}");
var failed = 0;
foreach (var c in checks)
{
    Console.WriteLine($"  [{(c.Passed ? "PASS" : "FAIL")}] {c.Name}  ({c.Detail})");
    if (!c.Passed) failed++;
}
Console.WriteLine($"\nresult: {(failed == 0 ? "ALL CONFORM" : $"{failed} FAILED")}");
return failed == 0 ? 0 : 1;
