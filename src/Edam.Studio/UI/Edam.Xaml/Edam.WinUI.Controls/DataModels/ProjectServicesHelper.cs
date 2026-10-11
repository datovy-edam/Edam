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
            var folder = FindSeedFolder("Samples");
            if (folder is null) return null;

            var candidate = System.IO.Path.Combine(folder, fileName);
            return System.IO.File.Exists(candidate) ? candidate : null;
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
            if (item is null)
            {
               return null;
            }

            // the CONTAINER the person named (ADR-0014): create it, seed it, and only then record that it
            // exists — because an answer is not completion (ADR-0013 decision 12)
            if (string.Equals(
                   item.Id,
                   Edam.Data.Projects.DependencyInjection.ConfigurableItems.CONTAINER_KEY,
                   StringComparison.OrdinalIgnoreCase))
            {
               return await CompleteContainerActionAsync(value).ConfigureAwait(true);
            }

            if (item.Id != Edam.Data.Projects.DependencyInjection.ConfigurableItems
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
      /// Complete the <b>container</b> action (ADR-0014): the answer names a container, so the container is
      /// made real — enlisted <b>idempotently</b> (an existing container is returned; a seed never rewrites a
      /// binding) and seeded with the shared locations the starter recipe reads — and only then is the
      /// "ready" marker written, because the question must keep being asked until the thing exists.
      /// </summary>
      private static async Task<string?> CompleteContainerActionAsync(string containerId)
      {
         var problem = Edam.Data.Projects.DependencyInjection.ConfigurableItems
            .ValidateContainerName(containerId);
         if (problem is not null)
         {
            return problem;
         }

         try
         {
            var store = Services.GetService<Edam.Data.Catalog.Contracts.ICatalogStore>();
            if (store is null)
            {
               return "the catalog storage is not available (" + StorageSummary + ")";
            }

            var kind = Edam.Data.Catalog.DependencyInjection.CatalogServices.ParseTarget(
               AppSettings.GetString("Edam:Catalog:Target"));

            var container = store.EnlistContainer(containerId, "EDAM Studio projects", null, kind);

            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Projects storage: collection '" + container.ContainerId + "' created (or already present, " +
               "which is the same thing here) as '" + kind + "' — projects live at /Projects/<name> inside it.",
               Microsoft.Extensions.Logging.LogLevel.Information, StorageCategory);

            // the answered container IS the container from now on: re-ensure against it, seed it, and only
            // then mark it ready (so a failure leaves the question pending rather than pretending)
            await SeedSharedLocationsAsync(store, container.Id, containerId, default).ConfigureAwait(true);

            UserConfiguration.MarkContainerReady();
            m_ContainerEnsured = true;
            m_EnsuredContainerId = containerId;
            m_LastContainerFailure = null;

            return null;
         }
         catch (Exception ex)
         {
            return ExplainFailure(ex);
         }
      }

      /// <summary>
      /// <b>Where the catalog will store this installation's data</b>, in one readable block for the info
      /// surface: the storage kind, the location (<b>redacted</b> — a password never reaches a screen),
      /// whether it is local, <b>where the value came from</b> (the deployment, or the person's own answer),
      /// and what the catalog currently holds. Assembled here, never in the UI: redaction and provenance have
      /// exactly one definition (ADR-0014 decisions 5 and 8).
      /// </summary>
      public static async Task<string> DescribeStorageAsync(
         string? collectionUri = null, CancellationToken ct = default)
      {
         try
         {
            var answered = UserConfiguration.Answers.ContainsKey(ProjectSettings.DEFAULT_COLLECTION_KEY);
            var configured = ConfiguredContainerId;

            var lines = new List<string>
            {
               (UsesDatabase
                  ? "Projects are stored in a database, through the CATALOG."
                  : "Projects are stored as files, in a folder (the catalog is not configured)."),
               "Storage: " + StorageKind,
               "Where: " + m_StorageLocation,
               "Container: " + (configured ?? "(none named yet — the catalog's own containers are listed)"),
               "Container source: " + (answered
                  ? "your answer (kept with your settings)"
                  : (configured is null
                     ? "not stated yet"
                     : "the deployment (appsettings.json)")),
               "This machine: " + (IsLocalTarget ? "yes — local" : "no — shared/remote"),
            };

            if (UsesDatabase)
            {
               var collections = await Catalog.GetCollectionsAsync(ct).ConfigureAwait(false);
               lines.Add("Containers in the database: " + collections.Count);

               var match = collections
                  .FirstOrDefault(c => string.Equals(c.CollectionId, collectionUri,
                     StringComparison.OrdinalIgnoreCase))
                  ?? collections.FirstOrDefault(c => string.Equals(c.CollectionId, configured,
                     StringComparison.OrdinalIgnoreCase))
                  ?? collections.FirstOrDefault(c => c.IsDefault)
                  ?? collections.FirstOrDefault();

               if (match is not null)
               {
                  var projects = await Catalog.GetProjectsAsync(match.CollectionId, ct)
                     .ConfigureAwait(false);
                  lines.Add("Projects in '" + match.CollectionId + "': " + projects.Count);
               }
            }

            return string.Join(Environment.NewLine, lines);
         }
         catch (Exception ex)
         {
            return "The storage could not be described — " + ExplainFailure(ex);
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
               // a composition root that FAILED is reported once and not rebuilt on every call: its values
               // come from the application's (static) configuration, so a retry cannot succeed — and a retry
               // loop both floods the diagnostics log and repeats the same work on every navigation
               if (m_BuildFailure is not null)
               {
                  System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(m_BuildFailure).Throw();
               }

               if (m_Provider is null)
               {
                  try
                  {
                     var built = Build();
                     m_Provider = built.Provider;
                     m_PhysicalPaths = built.PhysicalPaths;
                  }
                  catch (Exception ex)
                  {
                     m_BuildFailure = ex;

                     Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
                        "Projects storage: the project composition root could not be built — " +
                        ExplainFailure(ex),
                        Microsoft.Extensions.Logging.LogLevel.Error, StorageCategory);

                     throw;
                  }
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

            // the CONTAINER the projects live in (ADR-0009/0011): the composition root reads exactly this
            // key as the collection id, so it is handed over explicitly — a key that configuration states
            // and the composition root never sees is a setting that does nothing.
            var defaultCollection = AppSettings.GetString(ProjectSettings.DEFAULT_COLLECTION_KEY);
            if (!string.IsNullOrWhiteSpace(defaultCollection))
            {
               values[ProjectSettings.DEFAULT_COLLECTION_KEY] = defaultCollection!;
            }

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

         // the user's ANSWERS are configuration from then on (ADR-0013): layered over the application's own
         // values, so an answered container is USED by the composition root. An answer the store never sees
         // would be exactly the "configured but inert" defect this area keeps producing.
         foreach (var answer in UserConfiguration.Answers)
         {
            values[answer.Key] = answer.Value;
         }

         // the configuration every policy sees, the root fallback included — ONE source for the composition
         // root, the storage description and the container-scoped content factory (LM-2b-ii)
         bootstrap = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
         m_StorageConfiguration = bootstrap;

         // WHERE projects are stored, stated once from the ONE place that resolves it (and kept for the
         // storage-description surface): a storage the application never names is a storage the user has
         // to guess at.
         LogStorageSummary(values);

         var services = new ServiceCollection();
         services.AddProjectServices(values);

         var physical = !string.Equals(settings.Target, "catalog", StringComparison.OrdinalIgnoreCase);
         return (services.BuildServiceProvider(), physical);
      }

      // ---------------------------------------------------------------------
      // where the projects are stored (stated, never guessed)
      // ---------------------------------------------------------------------

      private const string StorageCategory = "Edam.Studio";

      private static Exception? m_BuildFailure;
      private static string? m_LastStorageSummary;

      private static string m_StorageTarget = "(not resolved)";
      private static string m_StorageKind = "(not resolved)";
      private static string m_StorageCollection = "(not resolved)";
      private static string m_StorageLocation = "(not resolved)";

      /// <summary>
      /// <b>Where the projects are stored</b> — one line, reported to the diagnostics panel the moment the
      /// composition root is built (so it is on the record before anything is read or written) and available
      /// to the UI as the storage description, so a person never has to infer it from behaviour. The
      /// location is <b>redacted</b>: a password never reaches a log or a screen.
      /// </summary>
      public static string StorageSummary =>
         "target=" + m_StorageTarget + " storage=" + m_StorageKind +
         " collection=" + m_StorageCollection + " location=" + m_StorageLocation;

      /// <summary>
      /// True when projects are stored in a Database (the catalog), false when they are files in a folder.
      /// </summary>
      public static bool UsesDatabase =>
         string.Equals(m_StorageTarget, "catalog", StringComparison.OrdinalIgnoreCase);

      /// <summary>The storage kind as the person should read it (MS-SQL, PostgreSQL, file system, service).</summary>
      public static string StorageKind =>
         string.Equals(m_StorageKind, "mssql", StringComparison.OrdinalIgnoreCase) ? "MS-SQL" :
         string.Equals(m_StorageKind, "postgres", StringComparison.OrdinalIgnoreCase) ? "PostgreSQL" :
         string.Equals(m_StorageKind, "service", StringComparison.OrdinalIgnoreCase) ? "a catalog service" :
         "the file system";

      private static void LogStorageSummary(IReadOnlyDictionary<string, string> values)
      {
         values.TryGetValue(ProjectSettings.TARGET_KEY, out var target);
         target = string.IsNullOrWhiteSpace(target) ? "filesystem" : target!;

         var catalog = string.Equals(target, "catalog", StringComparison.OrdinalIgnoreCase);

         values.TryGetValue(ProjectSettings.DEFAULT_COLLECTION_KEY, out var collection);
         values.TryGetValue("Edam:Catalog:Target", out var kind);
         values.TryGetValue("ConnectionStrings:catalog", out var dsn);
         values.TryGetValue("Edam:Catalog:FileSystemRoot", out var catalogRoot);
         values.TryGetValue(ProjectSettings.ROOT_KEY, out var root);

         m_StorageTarget = target!;
         m_StorageKind = catalog ? (string.IsNullOrWhiteSpace(kind) ? "postgres" : kind!) : "filesystem";
         m_StorageCollection = string.IsNullOrWhiteSpace(collection) ? "(default)" : collection!;
         m_StorageLocation = Redact(catalog
            ? (string.IsNullOrWhiteSpace(dsn) ? catalogRoot : dsn)
            : root);

         // announced once per distinct resolution: the composition root is built once in practice, but a
         // failure path can resolve again, and the SAME storage must not be re-announced on every navigation
         if (String.Equals(m_LastStorageSummary, StorageSummary, StringComparison.Ordinal)) return;
         m_LastStorageSummary = StorageSummary;

         Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
            "Projects storage: " + StorageSummary, Microsoft.Extensions.Logging.LogLevel.Information,
            StorageCategory);

         // the trap this exists to prevent: a catalog configured for ONE engine while the connection string
         // names ANOTHER — the project would fail obscurely (or land somewhere unexpected)
         if (catalog && LooksLikeMsSql(dsn) &&
             !string.Equals(m_StorageKind, "mssql", StringComparison.OrdinalIgnoreCase))
         {
            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Projects storage: the catalog is configured as '" + m_StorageKind + "' but the connection " +
               "string looks like MS-SQL (Server=/Data Source=/Initial Catalog=/Integrated Security=). " +
               "Set Edam:Catalog:Target=mssql, or the projects will not be reachable.",
               Microsoft.Extensions.Logging.LogLevel.Warning, StorageCategory);
         }
         else if (catalog && LooksLikePostgres(dsn) &&
             !string.Equals(m_StorageKind, "postgres", StringComparison.OrdinalIgnoreCase))
         {
            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Projects storage: the catalog is configured as '" + m_StorageKind + "' but the connection " +
               "string looks like PostgreSQL (Host=/Username=/Port=). Set Edam:Catalog:Target=postgres, " +
               "or the projects will not be reachable.",
               Microsoft.Extensions.Logging.LogLevel.Warning, StorageCategory);
         }
      }

      /// <summary>
      /// The <b>one</b> definition of redaction: a credential in a location string is replaced before the
      /// value is logged or shown. (Never redact in the UI — there would be more than one rule.)
      /// </summary>
      private static string Redact(string? value)
      {
         if (string.IsNullOrWhiteSpace(value)) return "(none)";

         var text = value!;
         foreach (var key in new[] { "Password", "Pwd" })
         {
            text = System.Text.RegularExpressions.Regex.Replace(
               text, "(?i)(" + key + "\\s*=\\s*)[^;]*", "$1***");
         }
         return text;
      }

      private static bool LooksLikeMsSql(string? dsn) =>
         !string.IsNullOrWhiteSpace(dsn) &&
         (dsn!.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
          dsn.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
          dsn.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase) ||
          dsn.Contains("Integrated Security=", StringComparison.OrdinalIgnoreCase));

      private static bool LooksLikePostgres(string? dsn) =>
         !string.IsNullOrWhiteSpace(dsn) &&
         (dsn!.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
          dsn.Contains("Username=", StringComparison.OrdinalIgnoreCase) ||
          dsn.Contains("Port=", StringComparison.OrdinalIgnoreCase));

      /// <summary>
      /// The container the projects live in: <b>the user's answer first</b> (an answer is configuration,
      /// ADR-0013), then the deployment's own statement. Nothing here guesses, and nothing reads only the
      /// file while an answer exists — that is how a stated container ends up unused.
      /// </summary>
      private static string? ConfiguredContainerId
      {
         get
         {
            if (UserConfiguration.Answers.TryGetValue(
                   ProjectSettings.DEFAULT_COLLECTION_KEY, out var answered) &&
                !string.IsNullOrWhiteSpace(answered))
            {
               return answered;
            }

            var stated = AppSettings.GetString(ProjectSettings.DEFAULT_COLLECTION_KEY);
            return string.IsNullOrWhiteSpace(stated) ? null : stated;
         }
      }

      private static bool m_ContainerEnsured;
      private static string? m_EnsuredContainerId;
      private static string? m_LastContainerFailure;

      /// <summary>
      /// Make sure the configured <b>collection</b> (container) exists before it is browsed: a catalog has
      /// to hold a location before it can hold projects, and enlistment is <b>idempotent</b> (an existing
      /// container is returned, never rewritten — a seed can never change a binding, ADR-0011).
      /// <para>
      /// Host policy, reported: a file-system collection is a folder and needs nothing; a missing
      /// configuration or an unreachable database is <b>reported</b>, never thrown, so browsing still shows
      /// what the catalog really has. It runs again when the container the person named <b>changes</b>.
      /// </para>
      /// </summary>
      public static async Task EnsureDefaultContainerAsync(CancellationToken ct = default)
      {
         var id = ConfiguredContainerId;

         if (m_ContainerEnsured &&
             string.Equals(m_EnsuredContainerId, id, StringComparison.OrdinalIgnoreCase))
         {
            return;
         }

         var connection = AppSettings.GetConnectionString("catalog");
         if (string.IsNullOrWhiteSpace(connection))
         {
            m_ContainerEnsured = true;   // a file-system collection is a folder: nothing to enlist
            m_EnsuredContainerId = id;
            return;
         }

         if (string.IsNullOrWhiteSpace(id))
         {
            m_ContainerEnsured = true;
            m_EnsuredContainerId = null;
            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Projects storage: no container is named yet (the catalog is configured, and the name is the " +
               "part a person states) — the catalog's own containers are shown as they are until it is " +
               "answered.",
               Microsoft.Extensions.Logging.LogLevel.Information, StorageCategory);
            return;
         }

         try
         {
            var store = Services.GetService<Edam.Data.Catalog.Contracts.ICatalogStore>();
            if (store is null)
            {
               ReportOnce(
                  "the catalog storage could not be resolved (" + StorageSummary + ") — browsing shows " +
                  "nothing until that is fixed");
               return;
            }

            var kind = Edam.Data.Catalog.DependencyInjection.CatalogServices.ParseTarget(
               AppSettings.GetString("Edam:Catalog:Target"));

            var container = store.EnlistContainer(id, "EDAM Studio projects", null, kind);
            m_ContainerEnsured = true;
            m_EnsuredContainerId = id;
            m_LastContainerFailure = null;

            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Projects storage: collection '" + container.ContainerId + "' is present (" + kind +
               ") — projects live at /Projects/<name> inside it.",
               Microsoft.Extensions.Logging.LogLevel.Information, StorageCategory);

            // a catalog container starts EMPTY: the shared locations the starter recipe reads (the
            // arguments template, the text map, the sample input) are seeded from the app-data seed —
            // the same "read a folder, write a location" step the conformance suite uses (LM-5) — and
            // only when a location is missing, so a curated template is never overwritten by the default
            await SeedSharedLocationsAsync(store, container.Id, id, ct).ConfigureAwait(false);
         }
         catch (Exception ex)
         {
            // reported once per distinct reason: the database may come back, so a retry stays allowed
            ReportOnce("the collection '" + id + "' could not be ensured: " + ExplainFailure(ex));

            // a folder that can also be a collection root is not a failure: the file system needs no
            // enlistment, and reporting it as a fault would train people to ignore the panel
         }
      }

      private static void ReportOnce(string reason)
      {
         if (String.Equals(m_LastContainerFailure, reason, StringComparison.Ordinal)) return;
         m_LastContainerFailure = reason;

         Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
            "Projects storage: " + reason + ".",
            Microsoft.Extensions.Logging.LogLevel.Warning, StorageCategory);
      }

      private static IConfiguration? m_StorageConfiguration;

      /// <summary>
      /// Explain a failure in terms a person can act on. A <b>missing Edam.* assembly</b> is a
      /// <b>deployment</b> fault — the configuration is fine and the code is present, but the layout does not
      /// carry a provider the composition root resolves <b>by type</b> — so say that, and say what to do,
      /// instead of leaving a raw loader error that reads like a code defect. (It cost a round trip: a
      /// deployed build with a green compile died with *"Could not load file or assembly
      /// 'Edam.Data.Catalog.MsSql'"*.)
      /// </summary>
      public static string ExplainFailure(Exception ex)
      {
         var missing = (ex as FileNotFoundException)?.FileName
                    ?? (ex as FileLoadException)?.FileName
                    ?? (ex?.InnerException as FileNotFoundException)?.FileName
                    ?? (ex?.InnerException as FileLoadException)?.FileName;

         if (!string.IsNullOrWhiteSpace(missing) &&
             missing!.StartsWith("Edam.", StringComparison.OrdinalIgnoreCase))
         {
            return ex!.Message +
               " — the deployed application does not carry '" + missing + "'. This is a DEPLOYMENT fault, not " +
               "a configuration one: clean, rebuild and deploy again (an incremental copy can leave a " +
               "provider assembly out of the layout).";
         }

         return ex?.Message ?? "(no detail)";
      }

      /// <summary>
      /// The app-data <b>seed</b> folder for a shared location family (<c>Templates</c>, <c>TextMaps</c>,
      /// <c>Samples</c>) — the ONE definition of where the packaged seed lives. Its shape differs between a
      /// packaged run and a development one, so both are tried rather than one being assumed. A missing seed
      /// is never fatal: the caller decides what to report.
      /// </summary>
      private static string? FindSeedFolder(string family)
      {
         try
         {
            var root = AppData.GetApplicationDataLocation();
            if (string.IsNullOrWhiteSpace(root)) return null;

            foreach (var shape in new[] { "Edam.Studio/Edam.App.Data", "Edam.App.Data" })
            {
               var candidate = System.IO.Path.Combine(
                  root!, shape.Replace('/', System.IO.Path.DirectorySeparatorChar), family);
               if (System.IO.Directory.Exists(candidate)) return candidate;
            }
         }
         catch (Exception)
         {
            // a missing seed is not fatal: the caller reports what it could not find
         }

         return null;
      }

      /// <summary>
      /// Seed the collection's <b>shared locations</b> from the app-data seed (LM-5): the starter recipe
      /// reads <c>Templates/&lt;arguments&gt;</c>, <c>TextMaps/…</c> and <c>Samples/…</c>, and a catalog
      /// container starts empty — without this, the first project would be created with its folder
      /// structure and <b>no recipe at all</b> (exactly what DP-4 set out to prevent). Each location is
      /// seeded only when its probe file is <b>missing</b>, and every outcome is reported.
      /// </summary>
      private static async Task SeedSharedLocationsAsync(
         Edam.Data.Catalog.Contracts.ICatalogStore store, Guid containerId, string collectionId,
         CancellationToken ct)
      {
         // the file the starter arguments are written from, plus the two the shipped recipe references
         var locations = new (string Family, string Probe)[]
         {
            ("Templates", System.IO.Path.GetFileName(
               Edam.Data.Projects.DependencyInjection.ConfigurableItems.DEFAULT_TEMPLATE)),
            ("TextMaps", "XsdTextMap.json"),
            ("Samples", "schema.local.v1r0.xsd"),
         };

         // ONE content policy (LM-2b-ii): a named container gets its scoped store, while the sentinel
         // `default` resolves to the unscoped one — the same rule the removal follows
         var content = ContainerContent(collectionId)
            ?? Services.GetService<Edam.Data.Catalog.Contracts.IContentStore>();

         if (content is null)
         {
            ReportOnce("the catalog content store could not be resolved, so the collection's shared " +
                       "locations could not be seeded");
            return;
         }

         foreach (var (family, probe) in locations)
         {
            try
            {
               if (await content.ExistsAsync("/" + family + "/" + probe, ct).ConfigureAwait(false))
               {
                  continue;   // present already: a curated location is never overwritten by the seed
               }

               var source = FindSeedFolder(family);
               if (source is null)
               {
                  ReportOnce("the app-data seed has no '" + family + "' folder, so collection '" +
                             collectionId + "' will lack it (the starter project may have no recipe)");
                  continue;
               }

               var seeded = await CatalogFolderSeeder
                  .SeedAsync(containerId, store, content, source, "/" + family, ct)
                  .ConfigureAwait(false);

               Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
                  "Projects storage: seeded " + seeded + " file(s) into '/" + family +
                  "' of collection '" + collectionId + "' from the app-data seed.",
                  Microsoft.Extensions.Logging.LogLevel.Information, StorageCategory);
            }
            catch (Exception ex)
            {
               ReportOnce("seeding '/" + family + "' failed: " + ex.Message);
            }
         }
      }

      // ---------------------------------------------------------------------
      // collections / projects
      // ---------------------------------------------------------------------

      /// <summary>The registered collections (the default one is required and always present).</summary>
      public static async Task<IReadOnlyList<ProjectCollectionInfo>> GetCollectionsAsync(
         CancellationToken ct = default)
      {
         // every path that browses projects comes through here (discovery, then the tree, then creation),
         // so this is the one place the collection has to exist before it is used
         await EnsureDefaultContainerAsync(ct).ConfigureAwait(false);
         return await Catalog.GetCollectionsAsync(ct).ConfigureAwait(false);
      }

      /// <summary>
      /// The projects tree of a collection, in the shape the Studio's items-tree expects
      /// (replaces <c>Project.GetProjectItems</c>).
      /// </summary>
      public static async Task<FolderFileItemInfo?> GetProjectsTreeAsync(
         string? collectionUri = null, CancellationToken ct = default)
      {
         var collection = await ResolveCollectionAsync(collectionUri, ct).ConfigureAwait(false);
         if (collection is null) return null;

         // the root node's address: a catalog container's URI is empty by design, so fall back to its id —
         // an empty root path is how a tree ends up with no identity at all
         var rootPath = string.IsNullOrWhiteSpace(collection.Uri) ? collection.CollectionId : collection.Uri;
         var root = new FolderFileItemInfo(rootPath, null) { Type = ItemType.Folder };

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
         String nameOrPath, String? collectionId = null, CancellationToken ct = default)
      {
         // the container the person is LOOKING AT, not the configured default: deleting from a container the
         // UI is not showing is how a project disappears from under someone else's tree
         var collection = await ResolveCollectionAsync(collectionId, ct).ConfigureAwait(false);
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
      /// <b>Remove a container and everything it holds.</b> The cascade lives in the platform
      /// (<c>CatalogContainerAdministration</c>) because the order is not optional: the item rows carry a
      /// <b>foreign key</b> to the container row (so removing the container first fails outright), and content
      /// carries <b>none</b> (so it would be orphaned). That is the defect behind "the Remove button does
      /// nothing"; this removes content, items and the container, and reports the counts.
      /// </summary>
      public static async Task<Boolean> DeleteContainerAsync(
         String collectionId, CancellationToken ct = default)
      {
         if (String.IsNullOrWhiteSpace(collectionId))
         {
            return false;
         }

         try
         {
            var store = Services.GetService<Edam.Data.Catalog.Contracts.ICatalogStore>();
            if (store is null)
            {
               ReportOnce("the catalog storage could not be resolved (" + StorageSummary +
                          "), so the container could not be removed");
               return false;
            }

            var administration = new Edam.Data.Catalog.DependencyInjection.CatalogContainerAdministration(
               store, ContainerContent, Services.GetService<Edam.Data.Catalog.Contracts.IContentStore>());

            var result = await administration.RemoveAsync(collectionId, ct).ConfigureAwait(true);

            // the ensure step must re-check: the container that was just removed may be the configured one
            m_ContainerEnsured = false;
            m_EnsuredContainerId = null;

            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               result.Removed
                  ? "Projects storage: container '" + result.ContainerId + "' removed, with " +
                    result.Items + " item(s) and " + result.Contents + " content entry(ies)."
                  : "Projects storage: container '" + collectionId + "' was NOT removed" +
                    (String.IsNullOrWhiteSpace(result.Problem) ? "." : " (" + result.Problem + ")."),
               result.Removed
                  ? Microsoft.Extensions.Logging.LogLevel.Information
                  : Microsoft.Extensions.Logging.LogLevel.Warning,
               StorageCategory);

            return result.Removed;
         }
         catch (Exception ex)
         {
            ReportOnce("container '" + collectionId + "' could not be removed: " + ExplainFailure(ex));
            return false;
         }
      }

      /// <summary>
      /// What a container holds, in words — used in the removal confirmation, so a person removes a location
      /// knowing exactly what goes with it (items, their content, and the projects they make up).
      /// </summary>
      public static async Task<String> DescribeContainerContentsAsync(
         String? collectionId, CancellationToken ct = default)
      {
         if (String.IsNullOrWhiteSpace(collectionId))
         {
            return "everything it holds";
         }

         try
         {
            var store = Services.GetService<Edam.Data.Catalog.Contracts.ICatalogStore>();
            if (store is null)
            {
               return "everything it holds";
            }

            var items = new Edam.Data.Catalog.DependencyInjection.CatalogContainerAdministration(store)
               .Items(collectionId!);

            var projects = await Catalog.GetProjectsAsync(collectionId!, ct).ConfigureAwait(true);

            return projects.Count + " project(s) and " + items.Count + " item(s), with their content";
         }
         catch (Exception)
         {
            return "everything it holds";
         }
      }

      /// <summary>
      /// The content policy for one container — the <b>same</b> policy seeding and the projects layer use
      /// (LM-2b-ii): a named container gets its scoped store, and the sentinel <c>default</c> resolves to null
      /// by design, i.e. the caller's unscoped store.
      /// </summary>
      private static Edam.Data.Catalog.Contracts.IContentStore? ContainerContent(String containerId)
         => m_StorageConfiguration is null
            ? null
            : Edam.Data.Catalog.DependencyInjection.CatalogScopedContent
               .Factory(m_StorageConfiguration)(containerId);

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
            // IDENTITY FIRST: a collection is named by its **id**. A catalog container's URI is empty by
            // design (`ContainerInfo.ContainerUri` defaults to ""), so matching on the URI made every
            // selection fall through to the default container — a project created in one container then
            // appeared under every container, which is a correctness bug, not a display one. Uri and Name
            // remain as fallbacks for a file-system collection (where the id is the folder name).
            var requested = collections.FirstOrDefault(c =>
               string.Equals(c.CollectionId, collectionUri, StringComparison.OrdinalIgnoreCase))
               ?? collections.FirstOrDefault(c =>
                  string.Equals(c.Uri, collectionUri, StringComparison.OrdinalIgnoreCase))
               ?? collections.FirstOrDefault(c =>
                  string.Equals(c.Name, collectionUri, StringComparison.OrdinalIgnoreCase));

            if (requested is not null) return requested;

            // a SPECIFIC collection was asked for and is not there: say so and return nothing. Falling back
            // would silently show ANOTHER container's projects — exactly the confusion this fixes.
            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Projects: the selected container '" + collectionUri + "' was not found in the catalog — " +
               "nothing is shown for it (the containers present are: " +
               string.Join(", ", collections.Select(c => c.CollectionId)) + ").",
               Microsoft.Extensions.Logging.LogLevel.Warning, StorageCategory);
            return null;
         }

         // the CONFIGURED default comes first: `Edam:Projects:DefaultCollection` names the container this
         // installation is meant to use, and it must win over whatever the provider happens to flag (or
         // over "the first one") — otherwise the key would say something the application ignores.
         // the CONTAINER comes from the ANSWER first, then from configuration (the person's answer is what the
         // composition root and the store both use — one container, named once)
         var preferred = ConfiguredContainerId;

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

      /// <summary>
      /// Disk path for the file-system target; the <b>project address</b> otherwise
      /// (<c>/Projects/&lt;name&gt;/&lt;resource…&gt;</c>).
      /// <para>
      /// The catalog form deliberately keeps the <b>project</b> in the path: a bare project-relative path
      /// (<c>/Arguments/x.Args.json</c>) does not say which project it belongs to — and Windows then reads it
      /// as a drive path (<c>C:\Arguments\x.Args.json</c>), which is how selecting a catalog file threw
      /// <c>DirectoryNotFoundException</c> at a click.
      /// </para>
      /// </summary>
      private static string NodePath(string projectRoot, string relativePath)
      {
         if (!m_PhysicalPaths)
         {
            return projectRoot.TrimEnd('/') + "/" + relativePath.TrimStart('/');
         }

         return Path.Combine(projectRoot,
            relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
      }

      /// <summary>
      /// Split a project address of the shape <c>/Projects/&lt;name&gt;/&lt;resource…&gt;</c> into the project's name and
      /// the project-relative resource path — the inverse of <see cref="NodePath"/> for the catalog target, and
      /// the mapping a consumer holding a path (the Studio's tree) needs in order to read through the seam.
      /// </summary>
      private static bool TrySplitProjectPath(
         string? path, out string projectName, out ProjectPath resource)
      {
         projectName = String.Empty;
         resource = ProjectPath.Root;

         if (String.IsNullOrWhiteSpace(path)) return false;

         var segments = path!.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
         if (segments.Length < 2) return false;
         if (!String.Equals(segments[0], ProjectCollectionFolders.Projects,
                StringComparison.OrdinalIgnoreCase))
         {
            return false;
         }

         projectName = segments[1];
         resource = segments.Length == 2
            ? ProjectPath.Root
            : ProjectPath.Parse("/" + String.Join('/', segments[2..]));
         return true;
      }

      /// <summary>
      /// <b>Read a project resource's text through the platform.</b> A catalog target has no disk path: the
      /// resource is addressed (<c>/Projects/&lt;name&gt;/&lt;resource…&gt;</c>) and read with
      /// <see cref="IProjectResources"/>, so the Studio edits what the catalog holds. The file-system target
      /// keeps reading the file it always read. Returns null — never throws — when the resource is not there,
      /// so a click can never bring the application down.
      /// </summary>
      public static async Task<string?> ReadProjectTextAsync(
         string? path, string? collectionId = null, CancellationToken ct = default)
      {
         if (String.IsNullOrWhiteSpace(path)) return null;

         try
         {
            if (m_PhysicalPaths)
            {
               return File.Exists(path!) ? await File.ReadAllTextAsync(path!, ct).ConfigureAwait(true) : null;
            }

            if (!TrySplitProjectPath(path, out var projectName, out var resource))
            {
               ReportOnce("'" + path + "' is not a project address, so its content could not be read");
               return null;
            }

            var collection = await ResolveCollectionAsync(collectionId ?? ConfiguredContainerId, ct)
               .ConfigureAwait(true);
            if (collection is null) return null;

            var project = await Catalog.GetProjectAsync(collection.CollectionId, projectName, ct)
               .ConfigureAwait(true);
            if (project is null)
            {
               ReportOnce("the project '" + projectName + "' was not found in container '" +
                          collection.CollectionId + "', so '" + path + "' could not be read");
               return null;
            }

            using var stream = await Resources.OpenReadAsync(project, resource, ct).ConfigureAwait(true);
            if (stream is null)
            {
               ReportOnce("'" + path + "' has no content in the catalog yet");
               return null;
            }

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(ct).ConfigureAwait(true);
         }
         catch (Exception ex)
         {
            ReportOnce("reading '" + path + "' failed: " + ExplainFailure(ex));
            return null;
         }
      }

      /// <summary>
      /// <b>Delete one project resource</b> (a file inside a project, not the project itself) through the
      /// platform: the file-system target deletes the file it always deleted, a catalog target deletes the
      /// catalog item and its content. Both are reported; neither throws at a click. (This retires the
      /// long-recorded defect that a catalog resource node was deleted with <c>System.IO.File.Delete</c>.)
      /// </summary>
      public static async Task<Boolean> DeleteResourceAsync(
         string? path, string? collectionId = null, CancellationToken ct = default)
      {
         if (String.IsNullOrWhiteSpace(path)) return false;

         try
         {
            if (m_PhysicalPaths)
            {
               if (!File.Exists(path!)) return false;

               File.Delete(path!);
               Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
                  "Project item deleted: '" + path + "'.",
                  Microsoft.Extensions.Logging.LogLevel.Information, StorageCategory);
               return true;
            }

            if (!TrySplitProjectPath(path, out var projectName, out var resource)) return false;

            var collection = await ResolveCollectionAsync(collectionId ?? ConfiguredContainerId, ct)
               .ConfigureAwait(true);
            if (collection is null) return false;

            var project = await Catalog.GetProjectAsync(collection.CollectionId, projectName, ct)
               .ConfigureAwait(true);
            if (project is null) return false;

            var deleted = await Resources.DeleteAsync(project, resource, ct).ConfigureAwait(true);

            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               deleted
                  ? "Project item deleted: '" + path + "'."
                  : "Project item NOT deleted: '" + path + "' (nothing to remove).",
               deleted
                  ? Microsoft.Extensions.Logging.LogLevel.Information
                  : Microsoft.Extensions.Logging.LogLevel.Warning,
               StorageCategory);

            return deleted;
         }
         catch (Exception ex)
         {
            ReportOnce("deleting '" + path + "' failed: " + ExplainFailure(ex));
            return false;
         }
      }
      /// <summary>
      /// <b>Write a project resource's text through the platform</b> — the counterpart of
      /// <see cref="ReadProjectTextAsync"/>, and the reason editing must not go through the file system: a
      /// catalog target's item is an address, so a file write would land in a stray
      /// <c>C:\Projects\…</c> path (or throw). Returns false when it could not be written; never throws.
      /// </summary>
      public static async Task<Boolean> WriteProjectTextAsync(
         string? path, string? text, string? collectionId = null, CancellationToken ct = default)
      {
         if (String.IsNullOrWhiteSpace(path)) return false;

         try
         {
            if (m_PhysicalPaths)
            {
               var folder = Path.GetDirectoryName(path!);
               if (!String.IsNullOrWhiteSpace(folder)) Directory.CreateDirectory(folder!);

               await File.WriteAllTextAsync(path!, text ?? String.Empty, ct).ConfigureAwait(true);
               return true;
            }

            if (!TrySplitProjectPath(path, out var projectName, out var resource))
            {
               ReportOnce("'" + path + "' is not a project address, so it could not be written");
               return false;
            }

            var collection = await ResolveCollectionAsync(collectionId ?? ConfiguredContainerId, ct)
               .ConfigureAwait(true);
            if (collection is null) return false;

            var project = await Catalog.GetProjectAsync(collection.CollectionId, projectName, ct)
               .ConfigureAwait(true);
            if (project is null)
            {
               ReportOnce("the project '" + projectName + "' was not found in container '" +
                          collection.CollectionId + "', so '" + path + "' could not be written");
               return false;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(text ?? String.Empty);
            using var content = new MemoryStream(bytes);

            await Resources.WriteAsync(project, resource, content, ct).ConfigureAwait(true);

            Edam.WinUI.Controls.Logging.AppDiagnostics.Write(
               "Project item saved: '" + path + "'.",
               Microsoft.Extensions.Logging.LogLevel.Information, StorageCategory);

            return true;
         }
         catch (Exception ex)
         {
            ReportOnce("writing '" + path + "' failed: " + ExplainFailure(ex));
            return false;
         }
      }
   }
}
