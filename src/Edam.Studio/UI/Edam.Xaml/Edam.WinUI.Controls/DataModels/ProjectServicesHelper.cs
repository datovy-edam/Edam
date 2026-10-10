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

      private static Edam.Data.Projects.DependencyInjection.UserConfiguration? m_UserConfiguration;

      /// <summary>
      /// Ask about configuration that is <b>still missing</b> (D / ADR-0013) — the lazy safety net for a
      /// person who was not signed in when the shell started, or who deferred the question. Nothing is
      /// asked unless the session is signed in and something is genuinely missing, and the ask is
      /// best-effort: it must never stop the caller from doing its work.
      /// </summary>
      /// <param name="root">The XAML root that can host the dialog (null when unavailable).</param>
      /// <returns>True when at least one answer was saved.</returns>
      private static bool m_ConfigurationAskedThisRun;
      private static string? m_LastConfigurationAskFailure;

      /// <summary>
      /// True once this run has actually <b>shown</b> the ask. Callers retry until it is true, so a failed
      /// attempt (for example another dialog being open at that moment) is not mistaken for "asked".
      /// </summary>
      public static bool ConfigurationAskedThisRun => m_ConfigurationAskedThisRun;

      /// <summary>
      /// The host half of the gated default project (DP-3 / ADR-0012): whether the bound collection is
      /// empty, and how a project is created in it.
      /// </summary>
      private sealed class DefaultProjectHost
         : Edam.Data.Projects.DependencyInjection.IDefaultProjectHost
      {
         /// <summary>
         /// <b>DP-4:</b> seed the project that was just created, through the existing LM-6 helper — so a new
         /// project has a starter arguments file instead of an empty <c>Arguments</c> folder. Best effort by
         /// contract: the project exists either way, and the outcome reports a failure rather than hiding it.
         /// </summary>
         public async Task<bool> SeedAsync(
            string projectName, string templateAddress, CancellationToken ct)
         {
            var collection = await ResolveCollectionAsync(null, ct).ConfigureAwait(false);
            if (collection is null)
            {
               return false;
            }

            var projects = await Catalog.GetProjectsAsync(collection.CollectionId, ct)
               .ConfigureAwait(false);
            var project = projects.FirstOrDefault(p =>
               string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));
            if (project is null)
            {
               return false;
            }

            var seeded = await SeedArgumentsAsync(project, templateAddress, ct)
               .ConfigureAwait(false);

            // ---- DP-4 (input half) ------------------------------------------------------------------
            // The starter arguments name their input precisely — ./Files/schema.local.v1r0.xsd, per
            // Templates/ToAssets.Args.json — so the project is still not runnable until the sample INPUT
            // sits in Files/. The sample ships in the app-data seed (Samples/), which is file-system content
            // by design (ADR-0010: the seed is content to import); the WRITE goes through the resources
            // seam, so the project's content is never written behind the platform's back.
            try
            {
               var sample = FindSampleInput("schema.local.v1r0.xsd");
               if (sample is not null)
               {
                  using var content = System.IO.File.OpenRead(sample);
                  await Resources.WriteAsync(project,
                     Edam.Data.Projects.Contracts.ProjectFolders
                        .Path(Edam.Data.Projects.Contracts.ProjectFolders.Files)
                        .Combine("schema.local.v1r0.xsd"),
                     content, ct).ConfigureAwait(false);

                  Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
                     "Default project: sample input seeded into Files/schema.local.v1r0.xsd.",
                     Microsoft.Extensions.Logging.LogLevel.Information, "Edam.Studio");
               }
            }
            catch (Exception ex)
            {
               Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
                  "Default project: sample input seeding failed (" + ex.Message + ") — the project has " +
                  "its recipe but not its data.",
                  Microsoft.Extensions.Logging.LogLevel.Warning, "Edam.Studio");
            }

            return seeded is not null;
         }

         /// <summary>
         /// DP-4: locate the shipped sample input in the app-data seed. The seed's shape differs between a
         /// packaged run and a development one (<c>Edam.Studio/Edam.App.Data</c> versus <c>Edam.App.Data</c>),
         /// so both are tried rather than assuming one.
         /// </summary>
         private static string? FindSampleInput(string fileName)
         {
            try
            {
               var root = AppData.GetApplicationDataLocation();
               if (string.IsNullOrWhiteSpace(root))
               {
                  return null;
               }

               foreach (var shape in new[] { "Edam.Studio/Edam.App.Data", "Edam.App.Data" })
               {
                  var candidate = System.IO.Path.Combine(
                     root!, shape.Replace('/', System.IO.Path.DirectorySeparatorChar),
                     "Samples", fileName);
                  if (System.IO.File.Exists(candidate))
                  {
                     return candidate;
                  }
               }
            }
            catch (Exception)
            {
               // a missing seed is not fatal: the project is simply seeded without its input
            }

            return null;
         }

         public async Task<bool> IsContainerEmptyAsync(CancellationToken ct)
         {
            // the SAME resolution the creation uses, so the gate can never check one container while the
            // project is created in another (it honours Edam:Projects:DefaultCollection)
            var collection = await ResolveCollectionAsync(null, ct).ConfigureAwait(false);
            if (collection is null)
            {
               return false;
            }

            var projects = await Catalog.GetProjectsAsync(collection.CollectionId, ct);
            return projects.Count == 0;
         }

         public async Task<bool> CreateProjectAsync(string name, CancellationToken ct)
         {
            var created = await ProjectServicesHelper.CreateProjectAsync(
               null, name, "Created by the first-run setup.", ct);
            return created is not null;
         }
      }

      /// <summary>The gated default project, wired to this host's collection and binding.</summary>
      public static Edam.Data.Projects.DependencyInjection.DefaultProjectService
         BuildDefaultProjectService() =>
            new(UserConfiguration, new DefaultProjectHost(), IsLocalTarget);

      /// <summary>
      /// True when this installation is <b>local</b> (ADR-0012's condition for offering the starter
      /// project): the project target stores on disk, <b>or</b> its catalog names this machine — so a
      /// localhost PostgreSQL catalog counts as local, while a remote one does not.
      /// </summary>
      public static Boolean IsLocalTarget =>
         UsesPhysicalPaths ||
         Edam.Data.Projects.DependencyInjection.LocalTarget.IsLocalServer(
            AppSettings.GetConnectionString("catalog"));

      /// <summary>
      /// <b>The set of projects changed</b>, raised by this bridge when it changes it <b>from outside a
      /// view model</b> — today the first-run starter project, which the configuration prompt creates while
      /// the projects panel is on screen. A panel that only learns about projects when it loads would keep
      /// showing "no projects yet" until the next restart, which is exactly the defect this notification
      /// closes (DP-5).
      /// <para>
      /// A view model that performs the change itself refreshes directly (the manual New-Project flow adds
      /// the node it created, and DP-2's delete reloads) — this is for the changes made <i>behind</i> it.
      /// Subscribers own their lifecycle: subscribe while the surface is loaded, unsubscribe when it goes.
      /// </para>
      /// </summary>
      public static event Action? ProjectsChanged;

      /// <summary>Tell the loaded surfaces that the set of projects changed (see <see cref="ProjectsChanged"/>).</summary>
      public static void NotifyProjectsChanged() => ProjectsChanged?.Invoke();

      /// <summary>
      /// Complete the <b>action</b> behind an answered item (DP-3 / ADR-0012): for the starter-project
      /// name, create the project in the bound collection. Returns null on success, or why it failed — and
      /// on failure the marker is <b>not</b> written, so the question stays pending and is asked again
      /// (ADR-0013 decision 12).
      /// </summary>
      public static async Task<string?> CompleteConfigurationActionAsync(
         Edam.Data.Projects.DependencyInjection.ConfigurableItemInfo item, string value)
      {
         try
         {
            if (item is null ||
                item.Id != Edam.Data.Projects.DependencyInjection.ConfigurableItems
                   .DEFAULT_PROJECT_NAME_KEY)
            {
               return null;
            }

            var outcome = await BuildDefaultProjectService().TryCreateAsync();

            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               outcome.Created
                  ? "Default project: CREATED '" + outcome.Name + "' (the answer is now complete)."
                  : "Default project: not created" +
                    (String.IsNullOrWhiteSpace(outcome.Problem)
                       ? " (not applicable here)."
                       : " (" + outcome.Problem + ") — the question stays pending."),
               outcome.Created
                  ? Microsoft.Extensions.Logging.LogLevel.Information
                  : Microsoft.Extensions.Logging.LogLevel.Warning,
               "Edam.Studio");

            // DP-5: the project was created WITHOUT the projects panel being involved, so nothing would
            // refresh its tree — it would say "no projects yet" until the next restart. Tell it now.
            if (outcome.Created)
            {
               NotifyProjectsChanged();
            }

            return outcome.Created ? null : outcome.Problem;
         }
         catch (Exception ex)
         {
            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Default project: failed (" + ex.Message + ") — the question stays pending.",
               Microsoft.Extensions.Logging.LogLevel.Warning, "Edam.Studio");
            return ex.Message;
         }
      }

      /// <summary>
      /// Ask about configuration that is <b>still missing</b> (A + D / ADR-0013) — the path to use once a
      /// person is signed in: from the login completion, from a signed-in navigation, and (unconditionally)
      /// just before a project is created, where the answers are actually needed.
      /// <para>
      /// Nothing is asked unless the session is signed in and something is genuinely missing, the dialog is
      /// shown <b>at most once per run</b> unless <paramref name="force"/> says otherwise (so a deferral
      /// never turns into nagging), and the ask is <b>best effort</b>: a failure is logged, never thrown.
      /// </para>
      /// </summary>
      /// <param name="root">The XAML root that can host the dialog (null = resolve the shell's own).</param>
      /// <param name="force">True to ask again even if this run already asked (the New-Project moment).</param>
      /// <returns>True when at least one answer was saved.</returns>
      public static async Task<bool> AskForConfigurationIfNeededAsync(
         Microsoft.UI.Xaml.XamlRoot? root, bool force = false)
      {
         try
         {
            if (!force && m_ConfigurationAskedThisRun)
            {
               return false;
            }

            // fall back to the shell's root — the same one the app's own dialogs use (see DialogBox)
            root ??= Edam.WinUI.Controls.Application.ApplicationHelper.MainWindow?.Content?.XamlRoot;

            if (root is null ||
                !Edam.WinUI.Controls.Configuration.ConfigurationReadiness.IsSignedIn)
            {
               Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
                  "Configuration ask: WAITING (root=" + (root is null ? "none" : "ok") +
                  ", " + Edam.WinUI.Controls.Configuration.ConfigurationReadiness.Describe() + ").",
                  Microsoft.Extensions.Logging.LogLevel.Information, "Edam.Studio");
               return false;
            }

            var batch = UserConfiguration.ToAsk();
            if (batch.Count == 0)
            {
               Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
                  "Configuration ask: nothing is missing (nothing to ask).",
                  Microsoft.Extensions.Logging.LogLevel.Information, "Edam.Studio");
               return false;
            }

            // DP-3 design point: never ask for a name that would not be used. When the only thing to ask
            // is the starter-project name and the gated starter project is not applicable here (a
            // shared/remote collection, the switch off, or it was handled before), stay silent.
            var onlyStarterName = batch.All(a => a.Item.Id ==
               Edam.Data.Projects.DependencyInjection.ConfigurableItems.DEFAULT_PROJECT_NAME_KEY);

            if (onlyStarterName && !BuildDefaultProjectService().IsApplicable)
            {
               Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
                  "Configuration ask: the starter project is not applicable here (local binding=" +
                  UsesPhysicalPaths + ") — not asking.",
                  Microsoft.Extensions.Logging.LogLevel.Information, "Edam.Studio");
               return false;
            }

            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Configuration ask: SHOWING " + batch.Count + " item(s): " + batch[0].Item.Id,
               Microsoft.Extensions.Logging.LogLevel.Information, "Edam.Studio");

            var saved = await Edam.WinUI.Controls.Configuration.ConfigurationPrompt
               .ShowIfNeededAsync(root, UserConfiguration, CompleteConfigurationActionAsync);

            // only NOW is the ask really done: a FAILED attempt must stay retryable (WinUI allows only one
            // ContentDialog at a time, so asking while another is open throws), which is why the guard is
            // set after the prompt completes rather than before it
            m_ConfigurationAskedThisRun = true;

            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Configuration ask: CLOSED (saved=" + saved + ").",
               Microsoft.Extensions.Logging.LogLevel.Information, "Edam.Studio");

            return saved;
         }
         catch (Exception ex)
         {
            // report the FIRST occurrence of a given failure (the retry loop would otherwise repeat it
            // every couple of seconds), and leave the guard unset so the ask is retried
            if (!String.Equals(m_LastConfigurationAskFailure, ex.Message, StringComparison.Ordinal))
            {
               m_LastConfigurationAskFailure = ex.Message;
               Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
                  "Configuration ask failed (will retry): " + ex.Message,
                  Microsoft.Extensions.Logging.LogLevel.Warning, "Edam.Studio");
            }
            return false;
         }
      }

      /// <summary>
      /// The user's <b>answers</b> (CF-4 / ADR-0013): the application's own configuration, the per-user
      /// app-data <b>overlay</b> for answers, and the packaged store for the once-per-installation
      /// marker. The overlay sits at the app-data <b>root</b> — never inside the copied seed (ADR-0010),
      /// so a seed refresh can never fight an answer.
      /// </summary>
      public static Edam.Data.Projects.DependencyInjection.UserConfiguration UserConfiguration
      {
         get
         {
            if (m_UserConfiguration is null)
            {
               var root = AppData.GetApplicationDataLocation();
               m_UserConfiguration = new Edam.Data.Projects.DependencyInjection.UserConfiguration(
                  new ConfigurationBuilder()
                     .AddJsonFile("appsettings.json", optional: true)
                     .Build(),
                  new Edam.Data.Projects.DependencyInjection.FileStateStore(root),
                  new Edam.WinUI.Controls.Configuration.PackagedStateStore());
            }

            return m_UserConfiguration;
         }
      }

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
            // container's BINDING from the catalog's own keys (LM-4). The STORAGE kind is CONFIGURED
            // (`Edam:Catalog:Target`) rather than pinned, so MS-SQL (LM-8) is selectable exactly like
            // PostgreSQL — and PostgreSQL stays the default for a catalog that does not say.
            values[ProjectSettings.TARGET_KEY] = "catalog";
            values["Edam:Catalog:Target"] = ProjectSettings.NormalizeTarget(
               AppSettings.GetString("Edam:Catalog:Target") ?? "postgres");
            values["ConnectionStrings:catalog"] = connection;

            // a file-system-backed catalog states its root the same way (the reader expects these keys in
            // the bootstrap it is handed, not only in appsettings.json)
            var catalogRoot = AppSettings.GetString("Edam:Catalog:FileSystemRoot");
            if (!string.IsNullOrWhiteSpace(catalogRoot))
            {
               values["Edam:Catalog:FileSystemRoot"] = catalogRoot!;
            }
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
      /// <b>Delete a project through the platform</b> (DP-2) — the precondition for ever creating one on
      /// a person's behalf, and the reason ADR-0012 can promise that a deleted project stays deleted.
      /// <para>
      /// A node in the projects tree is a <b>path</b> (a real disk path for a file-system target, a
      /// catalog path otherwise — LM-6), so the project is matched by <b>name</b> first, by path as a
      /// fallback, and finally by the path's last segment. The store does the work
      /// (<see cref="IProjectStore.DeleteAsync"/>); nothing here touches the file system directly.
      /// </para>
      /// </summary>
      /// <returns>True when a project was removed.</returns>
      public static async Task<Boolean> DeleteProjectAsync(
         String nameOrPath, CancellationToken ct = default)
      {
         var collection = await ResolveCollectionAsync(null, ct).ConfigureAwait(false);
         if (collection is null)
         {
            return false;
         }

         var projects = await Catalog.GetProjectsAsync(collection.CollectionId, ct)
            .ConfigureAwait(false);

         var leaf = System.IO.Path.GetFileName((nameOrPath ?? String.Empty).TrimEnd('/'));

         var project = projects.FirstOrDefault(p =>
                  String.Equals(p.Name, nameOrPath, StringComparison.OrdinalIgnoreCase) ||
                  String.Equals(p.Path.ToString(), nameOrPath, StringComparison.OrdinalIgnoreCase))
               ?? projects.FirstOrDefault(p =>
                  String.Equals(p.Name, leaf, StringComparison.OrdinalIgnoreCase));

         if (project is null)
         {
            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Project delete: nothing matches '" + nameOrPath + "' in this collection.",
               Microsoft.Extensions.Logging.LogLevel.Warning, "Edam.Studio");
            return false;
         }

         var deleted = await Store.DeleteAsync(collection.CollectionId, project.Name, ct)
            .ConfigureAwait(false);

         Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
            deleted
               ? "Project deleted: '" + project.Name + "'."
               : "Project NOT deleted: '" + project.Name + "' (the store refused).",
            deleted
               ? Microsoft.Extensions.Logging.LogLevel.Information
               : Microsoft.Extensions.Logging.LogLevel.Warning,
            "Edam.Studio");

         return deleted;
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
         ProjectInfo project, string? templateFileName = null, CancellationToken ct = default)
      {
         if (project is null) return null;

         try
         {
            // DP-4: the registry's template wins, else the legacy setting, else the shipped default
            var configured = templateFileName ?? AppSettings.GetString(ArgumentsTemplateKey);
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

         // the CONFIGURED default comes first: `Edam:Projects:DefaultCollection` names the container this
         // installation is meant to use, and it must win over whatever the provider happens to flag (or
         // over "the first one") — otherwise the key would say something the application ignores.
         var preferred = AppSettings.GetString(
            Edam.Data.Projects.DependencyInjection.ProjectSettings.DEFAULT_COLLECTION_KEY);

         if (!string.IsNullOrWhiteSpace(preferred))
         {
            var configured = collections.FirstOrDefault(c =>
               string.Equals(c.CollectionId, preferred, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(c.Name, preferred, StringComparison.OrdinalIgnoreCase));
            if (configured is not null) return configured;
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
