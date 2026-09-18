var builder = DistributedApplication.CreateBuilder(args);

// BL-6.1: orchestrate the Wave-1 services as Aspire resources. The generated
// Projects.* types are produced at build time from the AppHost project references.

// The catalog DSN is shared by both consumers of the real catalog back-end (the catalog REST
// service and the WebApi's catalog/asset boundary, which resolves the same platform store via DI).
const string catalogConnection = "Server=localhost;Port=5432;Database=edam;User Id=edam;Password=edam";

// PostgreSQL backing the catalog service (mirrors compose.yaml's edam-postgres so the
// AppHost can either reuse that container or run its own).
var catalogDb = builder.AddContainer("catalogdb", "postgres", "17-alpine")
    .WithEnvironment("POSTGRES_USER", "edam")
    .WithEnvironment("POSTGRES_PASSWORD", "edam")
    .WithEnvironment("POSTGRES_DB", "edam")
    .WithEndpoint("tcp", ep => { ep.Port = 5432; ep.TargetPort = 5432; })
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
