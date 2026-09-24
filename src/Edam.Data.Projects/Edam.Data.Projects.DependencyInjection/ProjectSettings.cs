using Edam.Data.Projects.Contracts;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>
/// What the project composition root resolved from configuration (LM-3/LM-4 / ADR-0011).
/// <para>
/// <see cref="Root"/> is the <b>one</b> location statement for the default collection;
/// <see cref="Collections"/> are the additional declared ones; <see cref="Bindings"/> say <b>where the
/// storage actually is</b> — deliberately separate from the locations inside it.
/// <see cref="LegacyKeysUsed"/> lists the pre-ADR-0011 keys that were <b>translated</b> (so a host can
/// warn), <see cref="NotYetTranslated"/> lists legacy keys that are <b>recognized but not yet acted
/// on</b>, and <see cref="EnvironmentOverrides"/> lists the environment variables that were applied.
/// </para>
/// </summary>
public sealed record ProjectSettingsInfo(
   string? Target,
   string? Root,
   string? DefaultCollectionId,
   string? WorkingRoot,
   IReadOnlyList<ProjectCollectionInfo> Collections,
   IReadOnlyList<ProjectBindingInfo> Bindings,
   IReadOnlyList<string> LegacyKeysUsed,
   IReadOnlyList<string> NotYetTranslated,
   IReadOnlyList<string> EnvironmentOverrides)
{
   /// <summary>True when the configuration stated a location for the default collection.</summary>
   public bool HasRoot => !string.IsNullOrWhiteSpace(Root);

   /// <summary>The default collection's binding — the storage that backs it.</summary>
   public ProjectBindingInfo? DefaultBinding =>
      Bindings.FirstOrDefault(b => b.IsDefault) ?? Bindings.FirstOrDefault();

   /// <summary>The binding for a named collection, when declared.</summary>
   public ProjectBindingInfo? BindingFor(string collectionId)
      => Bindings.FirstOrDefault(b =>
         string.Equals(b.CollectionId, collectionId, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// <b>Where a container's storage actually is</b> (LM-4 / ADR-0011) — one statement per container,
/// separate from the locations inside it, so switching a container from a folder to PostgreSQL or a
/// service changes <b>only this</b>.
/// </summary>
/// <param name="CollectionId">The container (collection) being bound.</param>
/// <param name="Target">The storage kind: <c>filesystem</c>, <c>postgres</c> or <c>service</c>.</param>
/// <param name="Location">The <b>non-secret</b> address: a folder (file system) or a base URI (service).</param>
/// <param name="Credential">The <b>name</b> of the secret — a configuration key such as
/// <c>ConnectionStrings:catalog</c>, or a <c>vault://</c> reference — <b>never the secret itself</b>.</param>
/// <param name="IsDefault">True for the default collection's binding.</param>
public sealed record ProjectBindingInfo(
   string CollectionId,
   string Target,
   string? Location,
   string? Credential,
   bool IsDefault);

/// <summary>
/// The project settings reader (LM-3/LM-4): resolves the composition root's configuration from the
/// ADR-0011 shape, translating the legacy keys that have a direct equivalent, <b>reporting</b>
/// everything else, and resolving each container's <b>binding</b> (storage) with documented
/// precedence: <c>defaults → configuration → environment</c>.
/// <para>
/// The equivalence guarantee that makes this a <i>single source of truth</i>: a configuration written
/// the old way (<c>AppSettings:AssetConsolePath</c>) and the same configuration written the new way
/// (<c>Edam:Projects:Root</c>) resolve to the <b>same</b> root — so consumers migrate one at a time
/// with no flag day.
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

   /// <summary>Per-container bindings: <c>Edam:Projects:Bindings:&lt;id&gt;:Target|Location|Credential</c>.</summary>
   public const string BINDINGS_SECTION = "Edam:Projects:Bindings";

   /// <summary>The credential <b>name</b> for the default collection (a key or <c>vault://</c> reference).</summary>
   public const string CREDENTIAL_KEY = "Edam:Projects:Credential";

   /// <summary>The collection id used for the implicit default binding when none is configured.</summary>
   public const string DEFAULT_BINDING_ID = "(default)";

   // ---- environment overrides (LM-4): environment WINS over configuration ---------------------

   /// <summary>Overrides the default collection's location.</summary>
   public const string ENV_ROOT = "EDAM_ROOT";

   /// <summary>Prefix for a named collection's overrides: <c>EDAM_COLLECTION_&lt;ID&gt;__ROOT</c> etc.</summary>
   public const string ENV_COLLECTION_PREFIX = "EDAM_COLLECTION_";

   public const string ENV_SUFFIX_ROOT = "__ROOT";
   public const string ENV_SUFFIX_TARGET = "__TARGET";
   public const string ENV_SUFFIX_CREDENTIAL = "__CREDENTIAL";

   // ---- legacy keys that are TRANSLATED today (reported in LegacyKeysUsed) ---------------------

   /// <summary>Legacy: today's app-data root (<c>appsettings.json</c>).</summary>
   public const string LEGACY_CONSOLE_PATH_KEY = "AppSettings:AssetConsolePath";

   /// <summary>Legacy: <c>Edam.Settings.json</c>'s <c>App.ConsolePath</c> (when a host maps it).</summary>
   public const string LEGACY_SETTINGS_CONSOLE_PATH_KEY = "ConsolePath";

   // ---- legacy keys RECOGNIZED but not yet acted on (reported so they are never dropped) -------

   /// <summary>Legacy project-folder segment (<c>/Projects/</c>) — a provider convention until LM-6.</summary>
   public const string LEGACY_PROJECTS_PATH_KEY = "AppSettings:AssetProjectsPath";

   /// <summary>Legacy app-data folder name — superseded by the host's writable root (ADR-0010).</summary>
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

   /// <summary>Read the settings, consulting the process environment for overrides.</summary>
   public static ProjectSettingsInfo Read(IConfiguration config)
      => Read(config, environment: null);

   /// <summary>
   /// Read the settings. <paramref name="environment"/> is the override source (null = the process
   /// environment) — passed explicitly by tests so they never depend on ambient variables.
   /// </summary>
   public static ProjectSettingsInfo Read(
      IConfiguration config, IReadOnlyDictionary<string, string?>? environment)
   {
      if (config is null) throw new ArgumentNullException(nameof(config));

      string? Env(string name) => environment is null
         ? Environment.GetEnvironmentVariable(name)
         : environment.TryGetValue(name, out var value) ? value : null;

      var legacy = new List<string>();
      var applied = new List<string>();

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

      // ---- bindings: where the storage actually is (LM-4) ------------------------------------
      // NOTE the two questions are different: `Edam:Projects:Target` chooses the PROVIDER FAMILY
      // (filesystem | catalog), while a binding names the STORAGE (filesystem | postgres | service).
      var providerTarget = config[TARGET_KEY]?.Trim().ToLowerInvariant();
      var defaultId = config[DEFAULT_COLLECTION_KEY];
      var defaultBindingId = string.IsNullOrWhiteSpace(defaultId) ? DEFAULT_BINDING_ID : defaultId!;

      var bindings = new List<ProjectBindingInfo>();

      // the default collection's binding: declared keys win, else derive from today's configuration
      var declaredDefault = ReadBinding(config, defaultBindingId, isDefault: true);
      if (declaredDefault is not null)
      {
         bindings.Add(declaredDefault);
      }
      else
      {
         string bindingTarget;
         string? location;
         string? credential;

         if (string.Equals(providerTarget, "catalog", StringComparison.OrdinalIgnoreCase))
         {
            // the catalog provider's storage is stated by the catalog's own keys
            bindingTarget = NormalizeTarget(config["Edam:Catalog:Target"] ?? "filesystem");
            location = config["Edam:Catalog:FileSystemRoot"];
            credential = config["ConnectionStrings:catalog"] is not null
               ? "ConnectionStrings:catalog"
               : null;
         }
         else
         {
            bindingTarget = "filesystem";
            location = root;
            credential = null;
         }

         bindings.Add(new ProjectBindingInfo(
            defaultBindingId, bindingTarget, location, credential, true));
      }

      // declared collections may each carry their own binding
      foreach (var collection in collections)
      {
         var declared = ReadBinding(config, collection.CollectionId, isDefault: false);
         if (declared is not null) bindings.Add(declared);
      }

      // ---- environment overrides win over configuration --------------------------------------
      var rootOverride = Env(ENV_ROOT);
      if (!string.IsNullOrWhiteSpace(rootOverride))
      {
         root = rootOverride;
         applied.Add(ENV_ROOT);
         bindings[0] = bindings[0] with { Location = rootOverride };
      }

      for (var i = 0; i < bindings.Count; i++)
      {
         var binding = bindings[i];
         var segment = EnvironmentSegment(binding.CollectionId);

         var location = Env(ENV_COLLECTION_PREFIX + segment + ENV_SUFFIX_ROOT);
         if (!string.IsNullOrWhiteSpace(location))
         {
            binding = binding with { Location = location };
            applied.Add(ENV_COLLECTION_PREFIX + segment + ENV_SUFFIX_ROOT);
         }

         var envTarget = Env(ENV_COLLECTION_PREFIX + segment + ENV_SUFFIX_TARGET);
         if (!string.IsNullOrWhiteSpace(envTarget))
         {
            binding = binding with { Target = NormalizeTarget(envTarget!) };
            applied.Add(ENV_COLLECTION_PREFIX + segment + ENV_SUFFIX_TARGET);
         }

         var credential = Env(ENV_COLLECTION_PREFIX + segment + ENV_SUFFIX_CREDENTIAL);
         if (!string.IsNullOrWhiteSpace(credential))
         {
            binding = binding with { Credential = credential };
            applied.Add(ENV_COLLECTION_PREFIX + segment + ENV_SUFFIX_CREDENTIAL);
         }

         bindings[i] = binding;
      }

      // ---- legacy keys that land in a later step: report them, never drop them ---------------
      var pending = PendingKeys
         .Where(key => !string.IsNullOrWhiteSpace(config[key]))
         .ToList();

      return new ProjectSettingsInfo(
         providerTarget,
         string.IsNullOrWhiteSpace(root) ? null : root!.Trim(),
         defaultId,
         config[WORKING_ROOT_KEY],
         collections,
         bindings,
         legacy,
         pending,
         applied);
   }

   /// <summary>
   /// The environment-variable segment for a collection id: upper-cased with every character that is
   /// not a letter or digit replaced by <c>_</c> (so <c>edam.studio</c> → <c>EDAM_STUDIO</c>, giving
   /// <c>EDAM_COLLECTION_EDAM_STUDIO__ROOT</c>).
   /// </summary>
   public static string EnvironmentSegment(string collectionId)
   {
      var text = (collectionId ?? string.Empty).Trim();
      var buffer = new System.Text.StringBuilder(text.Length);
      foreach (var c in text)
         buffer.Append(char.IsLetterOrDigit(c) ? char.ToUpperInvariant(c) : '_');
      return buffer.ToString();
   }

   /// <summary>Normalise a storage target word to <c>filesystem</c>, <c>postgres</c> or <c>service</c>.</summary>
   public static string NormalizeTarget(string target)
   {
      var value = (target ?? string.Empty).Trim().ToLowerInvariant();
      return value switch
      {
         "fs" or "folder" or "file-system" or "filesystem" => "filesystem",
         "postgresql" or "postgres" or "pg" => "postgres",
         "http" or "https" or "rest" or "service" => "service",
         _ => value,
      };
   }

   /// <summary>True when a credential value is a <b>reference</b> rather than a configuration key.</summary>
   public static bool IsCredentialReference(string? credential)
      => !string.IsNullOrWhiteSpace(credential) &&
         credential!.StartsWith("vault://", StringComparison.OrdinalIgnoreCase);

   private static ProjectBindingInfo? ReadBinding(
      IConfiguration config, string collectionId, bool isDefault)
   {
      var section = config.GetSection(BINDINGS_SECTION).GetSection(collectionId);
      var target = section["Target"] ?? section["Type"];
      var location = section["Location"];
      var credential = section["Credential"];

      if (string.IsNullOrWhiteSpace(target) && string.IsNullOrWhiteSpace(location) &&
          string.IsNullOrWhiteSpace(credential))
      {
         return null;
      }

      return new ProjectBindingInfo(
         collectionId,
         NormalizeTarget(target ?? "filesystem"),
         string.IsNullOrWhiteSpace(location) ? null : location!.Trim(),
         string.IsNullOrWhiteSpace(credential) ? null : credential!.Trim(),
         isDefault);
   }
}
