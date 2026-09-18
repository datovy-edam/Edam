// -----------------------------------------------------------------------------
using Edam.Data.Catalog.DependencyInjection;
using Edam.Data.CatalogServiceClient;
using Edam.Data.Catalog.Contracts;

namespace Edam.Data.CatalogService;

/// <summary>
/// The Wave-1 catalog REST service (BL-7.x). <c>BuildApp</c> builds + wires the app so it can be
/// hosted in-process (e.g. by Edam.Data.Catalog.HttpConformance), while <c>Main</c> runs it as a
/// standalone web host. The back-end is resolved via DI/configuration (ADR-0006/0007).
/// <para>
/// When launched by <c>Edam.AppHost</c> it is a first-class Aspire service: ServiceDefaults adds
/// OpenTelemetry, the <c>/health</c> + <c>/alive</c> probes (readiness for the dashboard), and
/// service discovery. The API is served under <c>/catalogservice/</c>.
/// </para>
/// </summary>
public static class Program
{
   public static void Main(string[] args) => BuildApp(args).Run();

   /// <summary>Build the catalog service app (map + DI). Provider selected by configuration.</summary>
   public static WebApplication BuildApp(string[]? args = null)
   {
      var builder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());

      // BL-6.1: ServiceDefaults — OTel, health checks (/health, /alive), resilience, discovery.
      // This is what makes the catalog API an operational, observable Aspire resource.
      builder.AddServiceDefaults();

      // Add services to the container.
      builder.Services.AddProblemDetails();
      builder.Services.AddCors();

      // Wire the catalog back-end behind the ICatalogStore/IContentStore seams (BL-7.2/BL-7.4).
      // The consolidated composition root selects the provider by configuration
      // (Edam:Catalog:Target; here PostgreSQL via ConnectionStrings:catalog, or FileSystem).
      builder.Services.AddCatalogServices(builder.Configuration);

      var app = builder.Build();

      // Configure the HTTP request pipeline.
      app.UseExceptionHandler();

      // Aspire health probes (/health readiness, /alive liveness) for the AppHost dashboard.
      app.MapDefaultEndpoints();

      // setup service container
      CatalogServiceMap map = new(
         app,
         app.Services.GetRequiredService<ICatalogStore>());

      return app;
   }
}
