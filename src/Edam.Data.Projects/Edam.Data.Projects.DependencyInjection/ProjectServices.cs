using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.DependencyInjection;
using Edam.Data.Projects.Catalog;
using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>
/// The project composition root (PE-4 / ADR-0009): registers <see cref="IProjectCatalog"/>,
/// <see cref="IProjectStore"/> and <see cref="IProjectResources"/> for the configured target, so
/// consumers resolve the project surface <b>by contract</b> and never construct a provider
/// themselves — and swapping file-system ↔ catalog is a configuration change.
/// <para>Configuration keys:</para>
/// <list type="bullet">
/// <item><c>Edam:Projects:Target</c> — <c>filesystem</c> (default) | <c>catalog</c></item>
/// <item><c>Edam:Projects:Root</c> — the app-data root (file-system target); falls back to
/// <c>AppSettings:AssetConsolePath</c>, today's key, so existing installs need no change</item>
/// <item><c>Edam:Projects:Collections:&lt;name&gt;</c> — an extra collection's URI (file-system)</item>
/// <item><c>Edam:Projects:DefaultCollection</c> — the default collection id (catalog target)</item>
/// </list>
/// <para>
/// The catalog target delegates to <see cref="CatalogServices.AddCatalogServices"/> for the local
/// provider, so the catalog keys (<c>Edam:Catalog:Target</c>, <c>ConnectionStrings:catalog</c>,
/// <c>Edam:Catalog:FileSystemRoot</c>) are read there.
/// </para>
/// </summary>
public static class ProjectServices
{
   public const string TARGET_KEY = "Edam:Projects:Target";
   public const string ROOT_KEY = "Edam:Projects:Root";
   public const string COLLECTIONS_SECTION = "Edam:Projects:Collections";
   public const string DEFAULT_COLLECTION_KEY = "Edam:Projects:DefaultCollection";

   /// <summary>Today's app-data root key — the fallback for <see cref="ROOT_KEY"/>.</summary>
   public const string CONSOLE_PATH_KEY = "AppSettings:AssetConsolePath";

   /// <summary>
   /// Register the project surface from a flat key/value map — for hosts (like the WinUI desktop or
   /// the CLI) that do not load <c>appsettings.json</c>.
   /// </summary>
   public static IServiceCollection AddProjectServices(
      this IServiceCollection services, IReadOnlyDictionary<string, string> config)
      => AddProjectServices(services, new ConfigurationBuilder()
         .AddInMemoryCollection(config).Build());

   /// <summary>Register the project surface for the configured target.</summary>
   public static IServiceCollection AddProjectServices(
      this IServiceCollection services, IConfiguration config)
   {
      if (services is null) throw new ArgumentNullException(nameof(services));
      if (config is null) throw new ArgumentNullException(nameof(config));

      var target = (config[TARGET_KEY] ?? "filesystem").Trim().ToLowerInvariant();
      return target switch
      {
         "filesystem" or "fs" or "folder" => AddFileSystem(services, config),
         "catalog" or "cat" => AddCatalog(services, config),
         _ => throw new InvalidOperationException(
            $"Unknown {TARGET_KEY} '{target}' — expected 'filesystem' or 'catalog'."),
      };
   }

   // ---------------------------------------------------------------------

   private static IServiceCollection AddFileSystem(IServiceCollection services, IConfiguration config)
   {
      var root = config[ROOT_KEY];
      if (string.IsNullOrWhiteSpace(root)) root = config[CONSOLE_PATH_KEY];
      if (string.IsNullOrWhiteSpace(root))
         throw new InvalidOperationException(
            $"A project root is required: set {ROOT_KEY} (or {CONSOLE_PATH_KEY}).");

      var collections = ReadCollections(config);

      services.AddSingleton(_ => new FileSystemProjectCatalog(root, collections));
      services.AddSingleton<IProjectCatalog>(sp => sp.GetRequiredService<FileSystemProjectCatalog>());
      services.AddSingleton<IProjectStore, FileSystemProjectStore>();
      services.AddSingleton<IProjectResources>(sp =>
         new FileSystemProjectResources(root, sp.GetRequiredService<IProjectCatalog>()));

      return services;
   }

   private static IServiceCollection AddCatalog(IServiceCollection services, IConfiguration config)
   {
      // the catalog composition root supplies the local ICatalogStore/IContentStore for this target
      services.AddCatalogServices(config);

      var defaultCollection = config[DEFAULT_COLLECTION_KEY];

      services.AddSingleton<IProjectCatalog>(provider =>
      {
         var surfaces = ResolveSurfaces(provider);
         return new CatalogProjectCatalog(surfaces.Containers, surfaces.Items, defaultCollection);
      });
      services.AddSingleton<IProjectStore>(provider =>
      {
         var surfaces = ResolveSurfaces(provider);
         return new CatalogProjectStore(
            provider.GetRequiredService<IProjectCatalog>(),
            surfaces.Containers, surfaces.Items, surfaces.Content);
      });
      services.AddSingleton<IProjectResources>(provider =>
      {
         var surfaces = ResolveSurfaces(provider);
         return new CatalogProjectResources(surfaces.Containers, surfaces.Items, surfaces.Content);
      });

      return services;
   }

   /// <summary>The three catalog surfaces the project providers drive.</summary>
   private static (ICatalogContainer Containers, ICatalogItem Items, IContentStore Content)
      ResolveSurfaces(IServiceProvider provider)
   {
      var store = provider.GetService<ICatalogStore>();
      var content = provider.GetService<IContentStore>();

      if (store is null || content is null)
      {
         if (provider.GetService<ICatalogClient>() is not null)
            throw new NotSupportedException(
               "Projects over a REMOTE catalog are not wired by DI yet. Resolve ICatalogClient, " +
               "await InitializeClientAsync(...), then construct the project providers over " +
               "client.Container / client.Item / client.Content (PE-5 wires this for the hosts).");

         throw new InvalidOperationException(
            "No local catalog provider resolved. Configure Edam:Catalog:Target (postgres | filesystem) " +
            "with ConnectionStrings:catalog or Edam:Catalog:FileSystemRoot.");
      }

      return (store, store, content);
   }

   /// <summary>Extra file-system collections: <c>Edam:Projects:Collections:&lt;name&gt; = &lt;uri&gt;</c>.</summary>
   private static List<ProjectCollectionInfo> ReadCollections(IConfiguration config)
   {
      var collections = new List<ProjectCollectionInfo>();
      foreach (var child in config.GetSection(COLLECTIONS_SECTION).GetChildren())
      {
         if (string.IsNullOrWhiteSpace(child.Value)) continue;
         collections.Add(new ProjectCollectionInfo(child.Key, child.Key, child.Value!));
      }
      return collections;
   }
}
