using Edam.Application;
using Edam.Data.Projects.Catalog;
using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.DependencyInjection;
using Edam.InOut;
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
         var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

         var connection = AppSettings.GetConnectionString("catalog");
         if (!string.IsNullOrWhiteSpace(connection))
         {
            // projects live in the catalog
            config["Edam:Projects:Target"] = "catalog";
            config["Edam:Catalog:Target"] = "postgres";
            config["ConnectionStrings:catalog"] = connection;
         }
         else
         {
            var root = AppSettings.GetString("AppSettings:AssetConsolePath")
                       ?? AppSettings.GetString("AssetConsolePath")
                       ?? AppSettings.GetString("AppSettings:DefaultRootFileFolder");
            config["Edam:Projects:Target"] = "filesystem";
            config["Edam:Projects:Root"] = root ?? string.Empty;
         }

         var services = new ServiceCollection();
         services.AddProjectServices(config);

         var physical = !string.Equals(config["Edam:Projects:Target"], "catalog",
            StringComparison.OrdinalIgnoreCase);
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
