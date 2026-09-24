using Edam.Data.Projects.Contracts;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>
/// What the project composition root resolved from configuration (LM-3 / ADR-0011).
/// <para>
/// <see cref="Root"/> is the <b>one</b> place a location is stated for the default collection;
/// <see cref="Collections"/> are the additional declared ones. <see cref="LegacyKeysUsed"/> lists the
/// pre-ADR-0011 keys that were <b>translated</b> (so a host can warn), and
/// <see cref="NotYetTranslated"/> lists legacy keys that are <b>recognized but not yet acted on</b> —
/// recorded so nothing is silently dropped while the remaining LM steps land.
/// </para>
/// </summary>
public sealed record ProjectSettingsInfo(
   string? Target,
   string? Root,
   string? DefaultCollectionId,
   string? WorkingRoot,
   IReadOnlyList<ProjectCollectionInfo> Collections,
   IReadOnlyList<string> LegacyKeysUsed,
   IReadOnlyList<string> NotYetTranslated)
{
   /// <summary>True when the configuration stated a location for the default collection.</summary>
   public bool HasRoot => !string.IsNullOrWhiteSpace(Root);
}

/// <summary>
/// The project settings reader (LM-3): resolves the composition root's configuration from the
/// ADR-0011 shape, translating the legacy keys that have a direct equivalent and <b>reporting</b>
/// everything else.
/// <para>
/// The equivalence guarantee that makes this a <i>single source of truth</i>: a configuration written
/// the old way (<c>AppSettings:AssetConsolePath</c>) and the same configuration written the new way
/// (<c>Edam:Projects:Root</c>) resolve to the <b>same</b> root — so consumers can migrate one at a
/// time without a flag day, and a host can warn on the legacy keys until they are gone.
/// </para>
/// </summary>
public static class ProjectSettings
{
   /// <summary>The provider target: <c>filesystem</c> (default) or <c>catalog</c>.</summary>
   public const string TARGET_KEY = "Edam:Projects:Target";

   /// <summary>The default collection's location — the one location statement (ADR-0011).</summary>
   public const string ROOT_KEY = "Edam:Projects:Root";

   /// <summary>Additional collections: <c>Edam:Projects:Collections:&lt;name&gt; = &lt;uri&gt;</c>.</summary>
   public const string COLLECTIONS_SECTION = "Edam:Projects:Collections";

   /// <summary>The default collection's id (required by the catalog target).</summary>
   public const string DEFAULT_COLLECTION_KEY = "Edam:Projects:DefaultCollection";

   /// <summary>Where the runner materializes inputs (default: the temp folder).</summary>
   public const string WORKING_ROOT_KEY = "Edam:Projects:WorkingRoot";

   // ---- legacy keys that are TRANSLATED today (reported in LegacyKeysUsed) ---------------------

   /// <summary>Legacy: today's app-data root (<c>appsettings.json</c>).</summary>
   public const string LEGACY_CONSOLE_PATH_KEY = "AppSettings:AssetConsolePath";

   /// <summary>Legacy: <c>Edam.Settings.json</c>'s <c>App.ConsolePath</c> (when a host maps it).</summary>
   public const string LEGACY_SETTINGS_CONSOLE_PATH_KEY = "ConsolePath";

   // ---- legacy keys RECOGNIZED but not yet acted on (reported so they are never dropped) -------

   /// <summary>Legacy project-folder segment (<c>/Projects/</c>); becomes part of the project family's declared location.</summary>
   public const string LEGACY_PROJECTS_PATH_KEY = "AppSettings:AssetProjectsPath";

   /// <summary>Legacy app-data folder name — a <b>binding</b> concern (LM-4).</summary>
   public const string LEGACY_DATA_PATH_KEY = "AppSettings:AssetDataPath";

   /// <summary>Legacy default input folder → <c>project:files</c> (LM-5).</summary>
   public const string LEGACY_IN_PATH_KEY = "AppSettings:DefaultInPath";

   /// <summary>Legacy default output folder → <c>project:documents</c> (LM-5).</summary>
   public const string LEGACY_OUT_PATH_KEY = "AppSettings:DefaultOutPath";

   /// <summary>Legacy text-map folder → <c>app:textMaps</c> (LM-5).</summary>
   public const string LEGACY_TEXT_MAP_FOLDER_KEY = "AppSettings:DefaultTextMapFolder";

   /// <summary>Legacy committed connection string — becomes a <b>named reference</b> (LM-4).</summary>
   public const string LEGACY_CONNECTION_STRING_KEY = "DataSource:DefaultConnectionString";

   private static readonly string[] PendingKeys =
   {
      LEGACY_PROJECTS_PATH_KEY,
      LEGACY_DATA_PATH_KEY,
      LEGACY_IN_PATH_KEY,
      LEGACY_OUT_PATH_KEY,
      LEGACY_TEXT_MAP_FOLDER_KEY,
      LEGACY_CONNECTION_STRING_KEY,
   };

   /// <summary>Read the project settings; never throws for absent values (callers validate).</summary>
   public static ProjectSettingsInfo Read(IConfiguration config)
   {
      if (config is null) throw new ArgumentNullException(nameof(config));

      var legacy = new List<string>();

      // ---- the root: ONE location statement (ADR-0011) ---------------------------------------
      var root = config[ROOT_KEY];
      if (string.IsNullOrWhiteSpace(root))
      {
         root = config[LEGACY_CONSOLE_PATH_KEY];
         if (!string.IsNullOrWhiteSpace(root)) legacy.Add(LEGACY_CONSOLE_PATH_KEY);
      }
      if (string.IsNullOrWhiteSpace(root))
      {
         root = config[LEGACY_SETTINGS_CONSOLE_PATH_KEY];
         if (!string.IsNullOrWhiteSpace(root)) legacy.Add(LEGACY_SETTINGS_CONSOLE_PATH_KEY);
      }

      // ---- additional declared collections --------------------------------------------------
      var collections = new List<ProjectCollectionInfo>();
      foreach (var child in config.GetSection(COLLECTIONS_SECTION).GetChildren())
      {
         if (string.IsNullOrWhiteSpace(child.Value)) continue;
         collections.Add(new ProjectCollectionInfo(child.Key, child.Key, child.Value!));
      }

      // ---- legacy keys that land in a later step: report them, never drop them ---------------
      var pending = PendingKeys
         .Where(key => !string.IsNullOrWhiteSpace(config[key]))
         .ToList();

      return new ProjectSettingsInfo(
         config[TARGET_KEY]?.Trim().ToLowerInvariant(),
         string.IsNullOrWhiteSpace(root) ? null : root!.Trim(),
         config[DEFAULT_COLLECTION_KEY],
         config[WORKING_ROOT_KEY],
         collections,
         legacy,
         pending);
   }
}
