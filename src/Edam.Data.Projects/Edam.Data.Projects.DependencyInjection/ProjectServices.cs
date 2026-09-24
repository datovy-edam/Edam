using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.DependencyInjection;
using Edam.Data.Projects.Catalog;
using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.FileSystem;
using Edam.Data.Projects.Runner;
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
   // Aliases of the LM-3 settings keys, so there is exactly one definition of each key name.
   public const string TARGET_KEY = ProjectSettings.TARGET_KEY;
   public const string ROOT_KEY = ProjectSettings.ROOT_KEY;
   public const string COLLECTIONS_SECTION = ProjectSettings.COLLECTIONS_SECTION;
   public const string DEFAULT_COLLECTION_KEY = ProjectSettings.DEFAULT_COLLECTION_KEY;

   /// <summary>Where the runner materializes inputs for a process (default: the temp folder).</summary>
   public const string WORKING_ROOT_KEY = ProjectSettings.WORKING_ROOT_KEY;

   /// <summary>Today's app-data root key — the legacy fallback for <see cref="ROOT_KEY"/>.</summary>
   public const string CONSOLE_PATH_KEY = ProjectSettings.LEGACY_CONSOLE_PATH_KEY;

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

      // LM-3: one settings reader — the ADR-0011 shape, with the legacy keys translated and
      // everything else reported (never silently dropped).
      var settings = ProjectSettings.Read(config);

      var target = settings.Target ?? "filesystem";
      switch (target)
      {
         case "filesystem" or "fs" or "folder":
            AddFileSystem(services, settings);
            break;
         case "catalog" or "cat":
            AddCatalog(services, config, settings);
            break;
         default:
            throw new InvalidOperationException(
               $"Unknown {ProjectSettings.TARGET_KEY} '{target}' — expected 'filesystem' or 'catalog'.");
      }

      // The runner is provider-independent (it works through IProjectResources). A bound
      // IProjectProcess supplies the execution itself; without one, running fails with guidance.
      services.AddSingleton<IProjectRunner>(provider => new ProjectArgumentRunner(
         provider.GetRequiredService<IProjectResources>(),
         (IProjectProcess?)provider.GetService<IProjectProcess>() ?? new UnboundProjectProcess(),
         settings.WorkingRoot));

      return services;
   }

   // ---------------------------------------------------------------------

   private static IServiceCollection AddFileSystem(
      IServiceCollection services, ProjectSettingsInfo settings)
   {
      var root = settings.Root;
      if (string.IsNullOrWhiteSpace(root))
         throw new InvalidOperationException(
            $"A project root is required: set {ProjectSettings.ROOT_KEY} " +
            $"(or the legacy {ProjectSettings.LEGACY_CONSOLE_PATH_KEY}).");

      var collections = settings.Collections;

      services.AddSingleton(_ => new FileSystemProjectCatalog(root, collections));
      services.AddSingleton<IProjectCatalog>(sp => sp.GetRequiredService<FileSystemProjectCatalog>());
      services.AddSingleton<IProjectStore, FileSystemProjectStore>();
      services.AddSingleton<IProjectResources>(sp =>
         new FileSystemProjectResources(root, sp.GetRequiredService<IProjectCatalog>()));

      return services;
   }

   private static IServiceCollection AddCatalog(
      IServiceCollection services, IConfiguration config, ProjectSettingsInfo settings)
   {
      // the catalog composition root supplies the local ICatalogStore/IContentStore for this target
      services.AddCatalogServices(config);

      var defaultCollection = settings.DefaultCollectionId;

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

}
