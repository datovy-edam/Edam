using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

// -----------------------------------------------------------------------------
using Edam.Application;
using Edam.Data.CatalogModel;
using Edam.Data.Catalog.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using catContracts = Edam.Data.Catalog.Contracts;
using catSvcClient = Edam.Data.CatalogServiceClient;
using catSrv = Edam.Data.CatalogService;
using Edam.Diagnostics;

namespace Edam.UI.CatalogExplorer;

public class CatalogServiceHelper
{
   private const string INVARIANT_CATALOG_DB = "edam";
   private const string CATALOG_DEFAULT_DSN =
      "Server=localhost;Port=5432;Database=edam;User Id=edam;Password=edam";

   /// <summary>
   /// Get HTTP based Catalog API Service instance...
   /// </summary>
   /// <returns>Catalog Service instance is returned</returns>
   public static async Task<ICatalogService> GetClientInstanceAsync(
       string? baseUri = null)
   {
      var _conUri = String.IsNullOrWhiteSpace(baseUri) ?
          AppSettings.GetString("CatalogServiceBaseUri") :
          baseUri;

      // initialize repository
      catSrv.CatalogInstance instance = new catSrv.CatalogInstance();
      var instResults = instance.GetCatalog(CatalogBaseClient.SessionId,
         catSrv.CatalogInstance.EDAM_BASE_URI, _conUri);

      if (instResults.Success)
      {
         await instResults.Instance.InitializeClientAsync(
            CatalogBaseClient.SessionId, "");
         return instResults.Instance;
      }
      return null;
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

   /// <summary>Built-once-per-connection-string DI container for the local catalog back-end.</summary>
   private static readonly Dictionary<string, IServiceProvider> _providers =
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

   /// <summary>
   /// Get Catalog Service instance...
   /// </summary>
   /// <param name="connectionUri">connection URI 
   /// (Connection String or Base URI)</param>
   /// <param name="invariantName">resource invariant name (default:
   /// EDAM_CATALOG_DB)</param>
   /// <returns>Catalog Service instance is returned</returns>
   public static async Task<ICatalogService> GetInstanceAsync(
       string? connectionUri = null,
       string invariantName = INVARIANT_CATALOG_DB)
   {
      ICatalogService result = null;

      if (Environment.OSVersion.Platform == PlatformID.Other)
      {
         // initialize repository
         result = await CatalogServiceHelper.GetClientInstanceAsync(
            connectionUri);
      }
      else
      {
         result = GetLocalInstance(connectionUri, invariantName);
      }

      return result;
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
