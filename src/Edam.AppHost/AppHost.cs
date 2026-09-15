var builder = DistributedApplication.CreateBuilder(args);

// BL-6.1: orchestrate the Wave-1 services as Aspire resources. The generated
// Projects.* type is produced at build time from the AppHost project references.
builder.AddProject<Projects.Edam_WebApi>("edam-web-api");

builder.Build().Run();
