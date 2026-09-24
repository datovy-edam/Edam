using Edam.Application;
using Edam.Data.AssetConsole;
using Edam.Data.AssetConsole.Services;
using Edam.Data.AssetManagement.Helpers;
using Edam.Data.AssetSchema;
using Edam.Data.Projects.Catalog;
using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.DependencyInjection;
using Edam.Data.Projects.FileSystem;
using Edam.InOut;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Edam.WinUI.Controls.DataModels
{
   /// <summary>
   /// The Studio's bridge onto the project platform (PE-5c part 2): the UI resolves the project
   /// surface from the DI composition root (<see cref="ProjectServices.AddProjectServices"/>) instead
   /// of calling the static <c>Edam.Data.AssetProject.Project</c> / <c>AppSettings</c> surface.
   /// <para>
   /// The target follows the app's configuration — a configured catalog connection selects the
   /// <b>catalog</b> target (projects in the catalog, ADR-0009); otherwise the <b>file system</b>
   /// target is used with today's <c>AppSettings:AssetConsolePath</c> as the collection root, so an
   /// existing installation behaves as before.
   /// </para>
   /// <para>
   /// The UI's tree still needs <see cref="FolderFileItemInfo"/> nodes, so this maps project
   /// resources onto that shape. For the file-system target the node paths are <b>real disk paths</b>
   /// (so the parts of the UI that still work with files — e.g. the console execution path — keep
   /// working); for the catalog target they are project-relative catalog paths.
   /// </para>
   /// </summary>
   public static class ProjectServicesHelper
   {
      private static readonly object Gate = new();
      private static IServiceProvider? m_Provider;
      private static bool m_PhysicalPaths;

      /// <summary>Collection id used when the configuration does not name one.</summary>
      public static IProjectCatalog Catalog => Services.GetRequiredService<IProjectCatalog>();
      public static IProjectStore Store => Services.GetRequiredService<IProjectStore>();
      public static IProjectResources Resources => Services.GetRequiredService<IProjectResources>();
      public static IProjectRunner Runner => Services.GetRequiredService<IProjectRunner>();

      /// <summary>Seeding is a capability of its own (LM-5/LM-6): structure stays scaffolding.</summary>
      public static IProjectSeeder Seeder => Services.GetRequiredService<IProjectSeeder>();

      /// <summary>True when the resolved target stores projects on disk (node paths are disk paths).</summary>
      public static bool UsesPhysicalPaths
      {
         get { _ = Services; return m_PhysicalPaths; }
      }

      private static IServiceProvider Services
      {
         get
         {
            lock (Gate)
            {
               if (m_Provider is null)
               {
                  var built = Build();
                  m_Provider = built.Provider;
                  m_PhysicalPaths = built.PhysicalPaths;
               }
               return m_Provider;
            }
         }
      }

      /// <summary>Build the composition root from the application's configuration.</summary>
      private static (IServiceProvider Provider, bool PhysicalPaths) Build()
      {
         // LM-6: the Studio reads its configuration through the ONE settings reader (ADR-0011) rather
         // than poking individual keys, so EDAM_ROOT, bindings and the legacy keys behave identically
         // here and in any other host.
         var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
         {
            [ProjectSettings.TARGET_KEY] = "filesystem",
         };

         var connection = AppSettings.GetConnectionString("catalog");
         if (!string.IsNullOrWhiteSpace(connection))
         {
            // projects live in the catalog: the provider family is catalog, and the reader derives the
            // container's BINDING from the catalog's own keys (LM-4)
            values[ProjectSettings.TARGET_KEY] = "catalog";
            values["Edam:Catalog:Target"] = "postgres";
            values["ConnectionStrings:catalog"] = connection;
         }
         else
         {
            // hand the reader the legacy spelling; it translates (and reports) it
            var consolePath = AppSettings.GetString(ProjectSettings.LEGACY_CONSOLE_PATH_KEY)
                              ?? AppSettings.GetString(ProjectSettings.LEGACY_SETTINGS_CONSOLE_PATH_KEY);
            if (!string.IsNullOrWhiteSpace(consolePath))
               values[ProjectSettings.LEGACY_CONSOLE_PATH_KEY] = consolePath!;
         }

         var bootstrap = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
         var settings = ProjectSettings.Read(bootstrap);

         // the Studio's own host policy: with nothing configured (and no EDAM_ROOT), its app-data
         // folder is the root — resolved the same way the legacy surface resolved it.
         if (!settings.HasRoot &&
             !string.Equals(settings.Target, "catalog", StringComparison.OrdinalIgnoreCase))
         {
            var fallback = AppData.GetApplicationDataFolder();
            if (!string.IsNullOrWhiteSpace(fallback))
            {
               var absolute = ConfigurationHelper.GetAbsoluteAppDataPath(fallback);
               values[ProjectSettings.ROOT_KEY] =
                  (string.IsNullOrWhiteSpace(absolute) ? fallback : absolute).Trim();
            }
         }
         else if (settings.HasRoot)
         {
            values[ProjectSettings.ROOT_KEY] = settings.Root!;
         }

         var services = new ServiceCollection();
         services.AddProjectServices(values);

         var physical = !string.Equals(settings.Target, "catalog", StringComparison.OrdinalIgnoreCase);
         return (services.BuildServiceProvider(), physical);
      }

      // ---------------------------------------------------------------------
      // collections / projects
      // ---------------------------------------------------------------------

      /// <summary>The registered collections (the default one is required and always present).</summary>
      public static async Task<IReadOnlyList<ProjectCollectionInfo>> GetCollectionsAsync(
         CancellationToken ct = default)
         => await Catalog.GetCollectionsAsync(ct).ConfigureAwait(false);

      /// <summary>
      /// The projects tree of a collection, in the shape the Studio's items-tree expects
      /// (replaces <c>Project.GetProjectItems</c>).
      /// </summary>
      public static async Task<FolderFileItemInfo?> GetProjectsTreeAsync(
         string? collectionUri = null, CancellationToken ct = default)
      {
         var collection = await ResolveCollectionAsync(collectionUri, ct).ConfigureAwait(false);
         if (collection is null) return null;

         var root = new FolderFileItemInfo(collection.Uri, null) { Type = ItemType.Folder };

         foreach (var project in await Catalog.GetProjectsAsync(collection.CollectionId, ct)
                     .ConfigureAwait(false))
         {
            root.Children.Add(await GetProjectTreeAsync(collection, project, ct).ConfigureAwait(false));
         }

         return root;
      }

      /// <summary>One project's resources as a tree (used after creating a project).</summary>
      public static async Task<FolderFileItemInfo> GetProjectTreeAsync(
         ProjectInfo project, string? collectionUri = null, CancellationToken ct = default)
      {
         var collection = await ResolveCollectionAsync(collectionUri, ct).ConfigureAwait(false);
         return await GetProjectTreeAsync(collection, project, ct).ConfigureAwait(false);
      }

      private static async Task<FolderFileItemInfo> GetProjectTreeAsync(
         ProjectCollectionInfo? collection, ProjectInfo project, CancellationToken ct)
      {
         var resources = await Resources.ListAsync(project, ProjectPath.Root, ct: ct)
            .ConfigureAwait(false);

         var projectRoot = PhysicalRoot(collection, project);
         return BuildTree(projectRoot, resources);
      }

      /// <summary>
      /// Create a project through the platform (replaces <c>Project.CreateProject</c>) and return it.
      /// </summary>
      public static async Task<ProjectInfo> CreateProjectAsync(
         string? collectionUri, string name, string? description = null,
         CancellationToken ct = default)
      {
         var collection = await ResolveCollectionAsync(collectionUri, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No project collection is configured.");

         return await Store.CreateAsync(collection.CollectionId, name, description, ct)
            .ConfigureAwait(false);
      }

      /// <summary>
      /// <b>LM-6:</b> seed a new project's <c>Arguments</c> folder from the configured arguments
      /// template — the behaviour the legacy <c>Project.CreateProject</c> had, now expressed as
      /// <i>read an address, write an address</i>. Best effort: a missing template just means no
      /// starter file (it never fails project creation).
      /// </summary>
      public static async Task<ProjectPath?> SeedArgumentsAsync(
         ProjectInfo project, CancellationToken ct = default)
      {
         if (project is null) return null;

         try
         {
            // the legacy setting names the template (e.g. "Templates/ToAssets.Args.json")
            var configured = AppSettings.GetString(ArgumentsTemplateKey);
            var fileName = string.IsNullOrWhiteSpace(configured)
               ? "ToAssets.Args.json"
               : Path.GetFileName(configured!);

            var scope = new CatalogAddress(project.CollectionId, project.Path);
            if (!ProjectLocations.TryResolve(
                   ProjectLocations.AppScheme + ":" + ProjectLocations.Templates + "/" + fileName,
                   scope, out var template, out _))
            {
               return null;
            }

            return await Seeder.SeedArgumentsAsync(project, template, ct: ct).ConfigureAwait(true);
         }
         catch (Exception)
         {
            // seeding is best effort: a host without a template still creates projects
            return null;
         }
      }

      /// <summary>The legacy key naming the arguments template (ADR-0011's compatibility reader reports it).</summary>
      public const string ArgumentsTemplateKey = "AppSettings:AssetArgumentsTemplatePath";

      private static async Task<ProjectCollectionInfo?> ResolveCollectionAsync(
         string? collectionUri, CancellationToken ct)
      {
         var collections = await GetCollectionsAsync(ct).ConfigureAwait(false);
         if (collections.Count == 0) return null;

         if (!string.IsNullOrWhiteSpace(collectionUri))
         {
            var match = collections.FirstOrDefault(c =>
               string.Equals(c.Uri, collectionUri, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(c.Name, collectionUri, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
         }

         return collections.FirstOrDefault(c => c.IsDefault) ?? collections[0];
      }

      // ---------------------------------------------------------------------
      // PE-5d option A: execute through the platform and derive assets from the artifact
      // ---------------------------------------------------------------------

      /// <summary>Configuration key that opts the Studio into platform execution.</summary>
      public const string PROCESS_MODE_KEY = "Edam:Projects:Process";

      /// <summary>
      /// Whether the Studio should execute projects through the platform (<see cref="IProjectRunner"/>)
      /// rather than the legacy static console. <b>Opt-in</b> — set
      /// <c>Edam:Projects:Process = runner</c> — so nothing changes unless it is asked for.
      /// </summary>
      public static bool ProcessRunnerEnabled
      {
         get
         {
            var mode = AppSettings.GetString(PROCESS_MODE_KEY);
            return string.Equals(mode, "runner", StringComparison.OrdinalIgnoreCase);
         }
      }

      /// <summary>
      /// Resolve a <b>file-system path</b> (e.g. a Studio tree item) to the platform's project and
      /// project-relative resource path. Only meaningful for the file-system target: a catalog
      /// project has no external disk address — that is the point of ADR-0009.
      /// </summary>
      public static bool TryResolveProject(
         string? physicalPath, out ProjectInfo? project, out ProjectPath? resourcePath)
      {
         project = null;
         resourcePath = null;

         if (string.IsNullOrWhiteSpace(physicalPath) || !m_PhysicalPaths) return false;

         return Catalog is FileSystemProjectCatalog fileSystem &&
                fileSystem.TryResolveResource(physicalPath!, out _, out project, out resourcePath);
      }

      /// <summary>
      /// Run a project's process through the platform, given the file-system path of its arguments
      /// document; the documents it produces are <b>captured back into the project</b>.
      /// Returns <c>null</c> when the path cannot be resolved to a project (caller falls back).
      /// </summary>
      public static async Task<ProjectRunResult?> RunProjectAsync(
         string? physicalArgumentsPath, string? outputFile = null, CancellationToken ct = default)
      {
         if (!TryResolveProject(physicalArgumentsPath, out var project, out var resourcePath) ||
             project is null || resourcePath is not { } argumentsPath)
         {
            return null;
         }

         ProjectPath? output = string.IsNullOrWhiteSpace(outputFile)
            ? null
            : ProjectPath.Parse(outputFile!);
         return await Runner.RunAsync(project, argumentsPath, output, ct).ConfigureAwait(false);
      }

      /// <summary>Read a document the platform captured back into the project.</summary>
      public static async Task<byte[]?> ReadArtifactAsync(
         ProjectInfo project, ProjectPath artifact, CancellationToken ct = default)
      {
         using var stream = await Resources.OpenReadAsync(project, artifact, ct).ConfigureAwait(false);
         if (stream is null) return null;

         using var buffer = new MemoryStream();
         await stream.CopyToAsync(buffer, ct).ConfigureAwait(false);
         return buffer.ToArray();
      }

      /// <summary>
      /// <b>Option A (PE-5d):</b> derive the assets from a document the process produced, by feeding
      /// that artifact to the console's own input procedure
      /// (<c>JsdToAssets</c>/<c>XsdToAssets</c>/<c>DdlToAssets</c>). No reader is rewritten and no
      /// contract is extended — the produced artifact becomes the input, which is what makes
      /// "the Catalog contains all artifact content" (ADR-0009) the working model.
      /// </summary>
      /// <returns>The assets, or <c>null</c> when the artifact's format is not asset-bearing.</returns>
      public static async Task<List<AssetData>?> TryLoadAssetsFromArtifactAsync(
         ProjectInfo project, ProjectPath artifact, AssetConsoleArgumentsInfo? template,
         CancellationToken ct = default)
      {
         var extension = artifact.Extension?.ToLowerInvariant() ?? string.Empty;
         var procedure = extension switch
         {
            ".jsd" or ".json" or ".jsonld" => AssetConsoleProcedure.JsdToAssets,
            ".xsd" or ".xml" => AssetConsoleProcedure.XsdToAssets,
            ".ddl" or ".sql" => AssetConsoleProcedure.DdlToAssets,
            _ => AssetConsoleProcedure.Unknown,
         };

         if (procedure == AssetConsoleProcedure.Unknown) return null;

         var bytes = await ReadArtifactAsync(project, artifact, ct).ConfigureAwait(false);
         if (bytes is null) return null;

         // the input procedures read from a physical file, so materialize the artifact for them only
         var input = Path.Combine(Path.GetTempPath(),
            "edam-artifact-" + Guid.NewGuid().ToString("N") + extension);
         try
         {
            await File.WriteAllBytesAsync(input, bytes, ct).ConfigureAwait(false);

            var arguments = template is null
               ? new AssetConsoleArgumentsInfo()
               : AssetConsoleArgumentsInfo.Duplicate(template);
            arguments.Procedure = procedure;
            arguments.UriList = new List<Uri> { new Uri(input) };
            if (arguments.InputFile is not null) arguments.InputFile.Full = input;

            switch (procedure)
            {
               case AssetConsoleProcedure.JsdToAssets:
                  AssetServiceHelper.JsdToAssets(arguments);
                  break;
               case AssetConsoleProcedure.XsdToAssets:
                  AssetServiceHelper.XsdToAssets(arguments);
                  break;
               default:
                  AssetServiceHelper.DdlToAssets(arguments);
                  break;
            }

            return arguments.AssetDataItems as List<AssetData>;
         }
         finally
         {
            try { File.Delete(input); } catch { /* best effort */ }
         }
      }

      // ---------------------------------------------------------------------
      // resources -> the Studio's tree shape
      // ---------------------------------------------------------------------

      /// <summary>The project's folder on disk (file-system target), or its catalog root path.</summary>
      private static string PhysicalRoot(ProjectCollectionInfo? collection, ProjectInfo project)
      {
         var projectPath = project.Path.Value;
         if (!m_PhysicalPaths || collection is null) return projectPath;

         return Path.Combine(collection.Uri,
            projectPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
      }

      /// <summary>Build a folder/file tree from a project's (recursive) resources.</summary>
      private static FolderFileItemInfo BuildTree(
         string projectRoot, IReadOnlyList<ProjectResourceInfo> resources)
      {
         var root = new FolderFileItemInfo(projectRoot, null) { Type = ItemType.Folder };
         var folders = new Dictionary<string, FolderFileItemInfo>(StringComparer.OrdinalIgnoreCase)
         {
            ["/"] = root,
         };

         foreach (var resource in resources.OrderBy(
                     r => r.Path.Value, StringComparer.OrdinalIgnoreCase))
         {
            var segments = resource.Path.Value.Trim('/')
               .Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) continue;

            var current = root;
            var prefix = string.Empty;
            for (var i = 0; i < segments.Length - 1; i++)
            {
               prefix += "/" + segments[i];
               current = folders.TryGetValue(prefix, out var existing)
                  ? existing
                  : AddFolder(folders, prefix, current, projectRoot);
            }

            if (resource.IsFolder)
            {
               var folderPath = "/" + string.Join('/', segments);
               if (!folders.ContainsKey(folderPath))
                  AddFolder(folders, folderPath, current, projectRoot);
            }
            else
            {
               current.AddFile(NodePath(projectRoot, resource.Path.Value), current);
            }
         }

         return root;
      }

      private static FolderFileItemInfo AddFolder(
         Dictionary<string, FolderFileItemInfo> folders, string path,
         FolderFileItemInfo parent, string projectRoot)
      {
         var folder = parent.AddFolder(NodePath(projectRoot, path), parent);
         folder.Type = ItemType.Folder;
         folders[path] = folder;
         return folder;
      }

      /// <summary>Disk path for the file-system target; the project-relative catalog path otherwise.</summary>
      private static string NodePath(string projectRoot, string relativePath)
      {
         if (!m_PhysicalPaths) return relativePath;

         return Path.Combine(projectRoot,
            relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
      }
   }
}
