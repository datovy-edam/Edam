using System;
using System.Threading.Tasks;
using System.Collections.Generic;

// -----------------------------------------------------------------------------
using Edam.Application;
using Edam.Data.CatalogModel;
using Edam.Data.Catalog.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using catContracts = Edam.Data.Catalog.Contracts;
using catSvcClient = Edam.Data.CatalogServiceClient;
using Edam.Diagnostics;

namespace Edam.UI.CatalogExplorer;

/// <summary>
/// Resolves the catalog surface for the WinUI shell. Both paths come from the DI composition root
/// (BL-7.4/BL-7.5) and the caller only ever sees the Model <see cref="ICatalogService"/>:
/// <list type="bullet">
/// <item><b>Remote</b> — a configured catalog service base URI resolves the Contracts
/// <see cref="catContracts.ICatalogClient"/> (<c>CatalogHttpClient</c>) talking to the catalog REST
/// API over HTTP. The retired Model <c>CatalogInstance</c>/<c>CatalogClient</c> path is gone.</item>
/// <item><b>Local</b> — otherwise the platform <see cref="catContracts.ICatalogStore"/> provider
/// (PostgreSQL/FileSystem, hidden behind DI) is used in-process.</item>
/// </list>
/// Selection is <b>configuration-driven</b> (an HTTP(S) service base URI wins), not OS-driven.
/// </summary>
public class CatalogServiceHelper
{
   private const string INVARIANT_CATALOG_DB = "edam";
   private const string CATALOG_DEFAULT_DSN =
      "Server=localhost;Port=5432;Database=edam;User Id=edam;Password=edam";
   private const string CATALOG_SERVICE_BASE_URI_KEY = "CatalogServiceBaseUri";

   /// <summary>
   /// Get the catalog service over the HTTP catalog API (BL-7.5). Uses the Contracts
   /// <c>CatalogHttpClient</c> resolved from the DI composition root, then adapts it to the Model
   /// surface the WinUI consumes.
   /// </summary>
   /// <param name="baseUri">catalog service base URI (default: the configured one)</param>
   /// <returns>Catalog Service instance, or null when no base URI is configured</returns>
   public static async Task<ICatalogService> GetClientInstanceAsync(
       string? baseUri = null)
   {
      var _conUri = String.IsNullOrWhiteSpace(baseUri) ?
          AppSettings.GetString(CATALOG_SERVICE_BASE_URI_KEY) :
          baseUri;

      if (String.IsNullOrWhiteSpace(_conUri))
      {
         // No remote catalog configured — the caller falls back to the local back-end.
         return null;
      }

      var client = GetRemoteProvider(_conUri)
         .GetRequiredService<catContracts.ICatalogClient>();

      await client.InitializeClientAsync(CatalogBaseClient.SessionId, "");
      var instance = new catSvcClient.StoreBackedCatalogService(
         client, CatalogBaseClient.SessionId);
      instance.Container.SetContainer(CatalogBaseClient.SessionId, "");
      return instance;
   }

   /// <summary>
   /// Get Catalog Service instance...
   /// </summary>
   /// <param name="connectionUri">connection string (default: null </param>
   /// <param name="invariantName">resource invariant name (default:
   /// EDAM_CATALOG_DB)</param>
   /// <returns>Catalog Service instance is returned</returns>
   public static ICatalogService GetLocalInstance(
       string? connectionString = null,
       string invariantName = INVARIANT_CATALOG_DB)
   {
      // Local catalog back-end is resolved via the DI composition root (BL-7.4 / ADR-0006/0007):
      // the provider is hidden behind the ICatalogStore seam and selected by configuration
      // (Edam:Catalog:Target = postgres; ConnectionStrings:catalog). The consumer never names a
      // provider implementation; the retired EF Edam.Data.CatalogDb back-end is gone (BL-7.2).
      var _conString = String.IsNullOrWhiteSpace(connectionString) ?
          (AppSettings.GetConnectionString("catalog") ?? CATALOG_DEFAULT_DSN) :
          connectionString;

      var store = GetProvider(_conString).GetRequiredService<catContracts.ICatalogStore>();
      var instance = new catSvcClient.StoreBackedCatalogService(
         store, CatalogBaseClient.SessionId);
      instance.Container.SetContainer(CatalogBaseClient.SessionId, "");
      return instance;
   }

   /// <summary>
   /// BL-7.4 per-Container provider resolution: the platform store for a specific Container (e.g. a
   /// FileSystem Container addressed by its container URI) — replaces the Model
   /// <c>CatalogFileSystemClient</c>. The caller never names a provider.
   /// </summary>
   /// <param name="container">Model container (id, type, container URI)</param>
   /// <returns>the Container's <see cref="catContracts.ICatalogStore"/> provider</returns>
   public static catContracts.ICatalogStore GetContainerStore(
       ContainerInfo container)
   {
      var root = container.ContainerURI ?? String.Empty;
      var target = (catContracts.ContainerType)container.ContainerType;

      var services = new ServiceCollection();
      var config = new Dictionary<string, string>(
         StringComparer.OrdinalIgnoreCase)
      {
         ["Edam:Catalog:Target"] = TargetName(target),
      };
      if (target == catContracts.ContainerType.FileSystem)
         config["Edam:Catalog:FileSystemRoot"] = root;
      services.AddCatalogServices(config);

      var sp = services.BuildServiceProvider();
      var resolver = sp.GetRequiredService<
         catContracts.ICatalogProviderResolver<catContracts.ICatalogStore>>();
      var store = resolver.Resolve(new catContracts.ContainerBinding(
         container.ContainerId, target, root));

      if (store is null)
      {
         throw new InvalidOperationException(
            "No catalog provider resolved for container '" +
            container.ContainerId + "' (type " + target + ", uri '" + root + "').");
      }
      return store;
   }

   private static string TargetName(catContracts.ContainerType type) => type switch
   {
      catContracts.ContainerType.FileSystem => "filesystem",
      catContracts.ContainerType.PostgreSql => "postgres",
      catContracts.ContainerType.Service => "service",
      _ => "postgres",
   };

   /// <summary>True when the value is an HTTP(S) catalog service base URI (vs a DB connection string).</summary>
   public static bool IsServiceBaseUri(string? value) =>
      !String.IsNullOrWhiteSpace(value) &&
      (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
       value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

   /// <summary>Built-once-per-connection-string DI container for the local catalog back-end.</summary>
   private static readonly Dictionary<string, IServiceProvider> _providers =
      new(StringComparer.OrdinalIgnoreCase);
   private static readonly Dictionary<string, IServiceProvider> _remoteProviders =
      new(StringComparer.OrdinalIgnoreCase);
   private static readonly object _providerGate = new();

   private static IServiceProvider GetProvider(string conString)
   {
      lock (_providerGate)
      {
         if (_providers.TryGetValue(conString, out var existing)) return existing;

         var services = new ServiceCollection();
         var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
         {
            ["Edam:Catalog:Target"] = "postgres",
            ["ConnectionStrings:catalog"] = conString,
         };
         services.AddCatalogServices(config);
         var provider = services.BuildServiceProvider();
         _providers[conString] = provider;
         return provider;
      }
   }

   /// <summary>Built-once-per-base-URI DI container for the remote (HTTP) catalog client (BL-7.5).</summary>
   private static IServiceProvider GetRemoteProvider(string baseUri)
   {
      lock (_providerGate)
      {
         if (_remoteProviders.TryGetValue(baseUri, out var existing)) return existing;

         var services = new ServiceCollection();
         var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
         {
            ["Edam:Catalog:Target"] = "service",
            ["Edam:Catalog:ServiceBaseUri"] = baseUri,
         };
         services.AddCatalogServices(config);
         var provider = services.BuildServiceProvider();
         _remoteProviders[baseUri] = provider;
         return provider;
      }
   }

   /// <summary>
   /// Get Catalog Service instance...
   /// </summary>
   /// <param name="connectionUri">connection URI (base URI for the remote catalog API, or the
   /// connection string for the local back-end)</param>
   /// <param name="invariantName">resource invariant name (default:
   /// EDAM_CATALOG_DB)</param>
   /// <returns>Catalog Service instance is returned</returns>
   public static async Task<ICatalogService> GetInstanceAsync(
       string? connectionUri = null,
       string invariantName = INVARIANT_CATALOG_DB)
   {
      // BL-7.5: remote vs local is configuration-driven — an HTTP(S) catalog service base URI
      // (explicit, or the configured CatalogServiceBaseUri) selects the catalog REST API. The
      // previous PlatformID.Other guard made the remote path unreachable on Windows.
      if (IsServiceBaseUri(connectionUri))
      {
         return await GetClientInstanceAsync(connectionUri);
      }

      var configured = AppSettings.GetString(CATALOG_SERVICE_BASE_URI_KEY);
      if (IsServiceBaseUri(configured))
      {
         return await GetClientInstanceAsync(configured);
      }

      return GetLocalInstance(connectionUri, invariantName);
   }

   /// <summary>
   /// Get Catalog to build its tree and access data.
   /// </summary>
   /// <param name="connectionUri">connection string (default: null </param>
   /// <param name="invariantName">resource invariant name (default:
   /// EDAM_CATALOG_DB)</param>
   /// <returns>instance of catalog is returned</returns>
   public static async Task<CatalogInfo?> GetCatalogAsync(
       string? connectionUri = null, 
       string invariantName = INVARIANT_CATALOG_DB)
   {
      CatalogInfo catalog = null;
      try
      {
         ICatalogService instance = await GetInstanceAsync(connectionUri);
         catalog = instance.Catalog ?? 
            new CatalogInfo(instance ,instance, CatalogBaseClient.SessionId);
         await catalog.InitializeCatalogAsync(
            "", buildTree: instance.Catalog == null);
      }
      catch (Exception ex)
      {
         ResultLog.DefaultLog.Failed(ex);
      }
      return catalog;
   }
}
