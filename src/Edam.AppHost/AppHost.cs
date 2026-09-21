var builder = DistributedApplication.CreateBuilder(args);

// BL-6.1: orchestrate the Wave-1 services as Aspire resources. The generated
// Projects.* types are produced at build time from the AppHost project references.

// The AppHost provisions its OWN PostgreSQL on host port 5433 so it can never collide with
// compose.yaml's `edam-postgres` (host 5432 — the instance the standalone service, the CLI and the
// conformance runners use). Before this both bound 5432, so `docker compose up -d` plus the AppHost
// fought over the port and the catalog services blocked on WaitFor(catalogDb).
//
// (5433 is a SEPARATE database from compose's — its data lives in the `catalogdb-data` volume.)
// To point the services at a different database instead (e.g. the compose instance, for one shared
// catalog), override the DSN:
//   dotnet run --project src/Edam.AppHost -- --CatalogConnectionString "Server=localhost;Port=5432;Database=edam;User Id=edam;Password=edam"
const string defaultCatalogConnection =
    "Server=localhost;Port=5433;Database=edam;User Id=edam;Password=edam";
var catalogConnection = builder.Configuration["CatalogConnectionString"] ?? defaultCatalogConnection;

// The AppHost's own PostgreSQL container (host 5433 -> container 5432).
var catalogDb = builder.AddContainer("catalogdb", "postgres", "17-alpine")
    .WithEnvironment("POSTGRES_USER", "edam")
    .WithEnvironment("POSTGRES_PASSWORD", "edam")
    .WithEnvironment("POSTGRES_DB", "edam")
    .WithEndpoint("tcp", ep => { ep.Port = 5433; ep.TargetPort = 5432; })
    .WithVolume("catalogdb-data", "/var/lib/postgresql/data");

// Wave-1 catalog REST service (BL-7.x). The consolidated AddCatalogServices composition root
// reads the provider from ConnectionStrings:catalog — injected here as ConnectionStrings__catalog
// (double-underscore maps to ':' in .NET config).
builder.AddProject<Projects.Edam_Data_CatalogService>("edam-catalog-service")
    .WaitFor(catalogDb)
    .WithEnvironment("Edam__Catalog__Target", "postgres")
    .WithEnvironment("ConnectionStrings__catalog", catalogConnection);

// BL-7.5 seam swap: the WebApi's catalog/asset boundary (/catalog/items) resolves the SAME real
// catalog platform store via AddCatalogServices, so the default stack surfaces one real catalog
// instead of the in-memory stand-in. Falls back to in-memory if this config is absent.
builder.AddProject<Projects.Edam_WebApi>("edam-web-api")
    .WaitFor(catalogDb)
    .WithEnvironment("Edam__Catalog__Target", "postgres")
    .WithEnvironment("ConnectionStrings__catalog", catalogConnection);

builder.Build().Run();
