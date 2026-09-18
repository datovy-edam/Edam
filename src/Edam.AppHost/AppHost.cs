var builder = DistributedApplication.CreateBuilder(args);

// BL-6.1: orchestrate the Wave-1 services as Aspire resources. The generated
// Projects.* types are produced at build time from the AppHost project references.

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
    .WithEnvironment("ConnectionStrings__catalog",
        "Server=localhost;Port=5432;Database=edam;User Id=edam;Password=edam");

builder.AddProject<Projects.Edam_WebApi>("edam-web-api");

builder.Build().Run();
