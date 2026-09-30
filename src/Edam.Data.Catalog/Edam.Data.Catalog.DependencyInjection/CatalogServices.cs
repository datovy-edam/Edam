using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.FileSystem;
using Edam.Data.Catalog.MsSql;
using Edam.Data.Catalog.PostgreSql;
using Edam.Data.CatalogServiceClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edam.Data.Catalog.DependencyInjection;

/// <summary>
/// Single DI composition root for the catalog surface (BL-7.4). Registers the catalog
/// <b>interfaces</b> (<see cref="ICatalogProviderResolver{ICatalogStore}"/>,
/// <see cref="ICatalogProviderResolver{IContentStore}"/>, the hidden default
/// <see cref="ICatalogStore"/>/<see cref="IContentStore"/>, and the remote
/// <see cref="ICatalogClient"/>) and selects the back-end by <b>configuration</b>
/// (<c>Edam:Catalog:Target</c>) + <c>ContainerType</c> — matching Wave-1's
/// <c>AddWave1Services()</c> seam. Consumers resolve by contract and never name a provider
/// (ADR-0006/0007: the back-end is a variable).
///
/// Configuration keys:
///   Edam:Catalog:Target               = filesystem | postgres | mssql | service   (default: postgres)
///   ConnectionStrings:catalog         = the DSN of the configured target (or Edam:Catalog:ConnectionString)
///   ConnectionStrings:catalog-mssql   = an explicit MS-SQL DSN (used when the target is mssql)
///   Edam:Catalog:FileSystemRoot       = file-system root dir (or Edam:Catalog:BaseUri)
///   Edam:Catalog:ServiceBaseUri        = remote catalog service base URI (registers ICatalogClient)
/// </summary>
public static class CatalogServices
{
   /// <summary>
   /// Registers the full catalog surface from a flat key/value configuration map — for desktop/UI
   /// hosts (notably the WinUI shell) that don't load appsettings.json. The keys are the same as the
   /// <see cref="IConfiguration"/> overload (e.g. <c>Edam:Catalog:Target</c>, <c>ConnectionStrings:catalog</c>,
   /// <c>Edam:Catalog:FileSystemRoot</c>, <c>Edam:Catalog:ServiceBaseUri</c>).
   /// </summary>
   public static IServiceCollection AddCatalogServices(
       this IServiceCollection services, IReadOnlyDictionary<string, string> config)
       => AddCatalogServices(services, new MapConfiguration(config));

   public static IServiceCollection AddCatalogServices(this IServiceCollection services, IConfiguration config)
   {
      var defaultTarget = ParseTarget(config["Edam:Catalog:Target"]);
      var stores = new Dictionary<ContainerType, ICatalogStore?>();
      var contents = new Dictionary<ContainerType, IContentStore?>();

      var pgConnection = config["ConnectionStrings:catalog"]
                         ?? config["Edam:Catalog:ConnectionString"];

      // LM-8: with an MS-SQL target the SAME key carries a SQL Server DSN, so PostgreSQL must not
      // claim it — otherwise one connection string would be handed to two different engines.
      var msSqlConnection = config["ConnectionStrings:catalog-mssql"]
                            ?? (defaultTarget == ContainerType.MsSql ? pgConnection : null);

      if (defaultTarget != ContainerType.MsSql && !string.IsNullOrWhiteSpace(pgConnection))
      {
         stores[ContainerType.PostgreSql] = new PostgreSqlCatalogStore(pgConnection);
         contents[ContainerType.PostgreSql] = new PostgreSqlContentStore(pgConnection);
      }

      if (!string.IsNullOrWhiteSpace(msSqlConnection))
      {
         stores[ContainerType.MsSql] = new MsSqlCatalogStore(msSqlConnection);
         contents[ContainerType.MsSql] = new MsSqlContentStore(msSqlConnection);
      }

      var fsRoot = config["Edam:Catalog:FileSystemRoot"]
                   ?? config["Edam:Catalog:BaseUri"];
      if (!string.IsNullOrWhiteSpace(fsRoot))
      {
         stores[ContainerType.FileSystem] = new FileSystemCatalogStore(fsRoot);
         contents[ContainerType.FileSystem] = new FileSystemContentStore(fsRoot);
      }

      var registry = new CatalogProviderRegistry(stores, contents, defaultTarget);
      services.AddSingleton(registry);
      services.AddSingleton<ICatalogProviderResolver<ICatalogStore>>(sp => sp.GetRequiredService<CatalogProviderRegistry>());
      services.AddSingleton<ICatalogProviderResolver<IContentStore>>(sp => sp.GetRequiredService<CatalogProviderRegistry>());

      // Hidden back-end: register the default ICatalogStore/IContentStore only when the
      // configured default target resolves (an unconfigured catalog yields a DI resolve error
      // rather than a silent null), so consumers resolve a concrete instance via DI only.
      var defaultStore = registry.DefaultStore;
      if (defaultStore is not null)
         services.AddSingleton<ICatalogStore>(defaultStore);
      var defaultContent = registry.DefaultContent;
      if (defaultContent is not null)
         services.AddSingleton<IContentStore>(defaultContent);

      var serviceUri = config["Edam:Catalog:ServiceBaseUri"];
      if (!string.IsNullOrWhiteSpace(serviceUri))
         services.AddSingleton<ICatalogClient>(sp => new CatalogHttpClient("di", serviceUri));

      return services;
   }

   public static ContainerType ParseTarget(string? value)
   {
      if (string.IsNullOrWhiteSpace(value)) return ContainerType.PostgreSql;
      if (Enum.TryParse<ContainerType>(value, ignoreCase: true, out var parsed)) return parsed;
      return value.ToLowerInvariant() switch
      {
         "filesystem" or "fs" => ContainerType.FileSystem,
         "postgres" or "postgresql" or "pg" => ContainerType.PostgreSql,
         "mssql" or "sqlserver" or "sql-server" or "sql server" or "tsql" => ContainerType.MsSql,
         "service" or "rest" => ContainerType.Service,
         _ => ContainerType.PostgreSql
      };
   }
}
