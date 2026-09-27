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

   /// <summary>
   /// Every item the reader looked at, with its <b>state</b> (CF-1 / ADR-0013). This is what makes
   /// "nothing stated this" distinguishable from "stated as empty" — the distinction a first-run
   /// prompt depends on.
   /// </summary>
   public IReadOnlyList<ConfigurationItemInfo> Items { get; init; } =
      Array.Empty<ConfigurationItemInfo>();

   /// <summary>The item for a key, or null when the reader did not look at it.</summary>
   public ConfigurationItemInfo? Item(string key) =>
      Items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));

   /// <summary>The state of a key (<see cref="ConfigurationState.Unset"/> when not looked at).</summary>
   public ConfigurationState StateOf(string key) =>
      Item(key)?.State ?? ConfigurationState.Unset;

   /// <summary>Everything stated but unusable — reported, never silently replaced.</summary>
   public IReadOnlyList<ConfigurationItemInfo> Problems =>
      Items.Where(i => i.State == ConfigurationState.Invalid).ToList();

   /// <summary>The items nothing has stated yet (the ones a host may ask about).</summary>
   public IReadOnlyList<ConfigurationItemInfo> Unset =>
      Items.Where(i => i.State == ConfigurationState.Unset).ToList();
}

/// <summary>
/// The three states configuration can be in (CF-1 / ADR-0013): <b>Unset</b> (nothing stated it),
/// <b>Set</b> (stated — possibly to an empty value) and <b>Invalid</b> (stated, but unusable).
/// <para>
/// Only <c>Unset</c> may cause the application to <i>ask</i> the user. A value that was deliberately
/// set to <b>empty</b> is respected, and an <b>invalid</b> one is reported rather than silently
/// replaced — the distinction whose absence caused a real startup defect
/// (<c>AppSettings:AssetConsolePath</c> is deliberately empty, and code that treated empty as missing
/// threw).
/// </para>
/// </summary>
public enum ConfigurationState
{
   /// <summary>Nothing has stated this value.</summary>
   Unset,

   /// <summary>Stated — the value may legitimately be empty.</summary>
   Set,

   /// <summary>Stated, but the value cannot be used; see <see cref="ConfigurationItemInfo.Problem"/>.</summary>
   Invalid
}

/// <summary>One configurable item, as the reader found it (CF-1 / ADR-0013).</summary>
/// <param name="Key">The configuration key of the item (or its logical name, e.g. <c>binding:&lt;id&gt;</c>).</param>
/// <param name="State">Unset / Set / Invalid.</param>
/// <param name="Value">The value as stated (null when unset); never a secret.</param>
/// <param name="Source">What stated it — the item's own key, a translated legacy key, or the environment.</param>
/// <param name="Problem">Why it is unusable (null unless <see cref="ConfigurationState.Invalid"/>).</param>
public sealed record ConfigurationItemInfo(
   string Key,
   ConfigurationState State,
   string? Value = null,
   string? Source = null,
   string? Problem = null)
{
   /// <summary>True when something stated this value (even if it is empty).</summary>
   public bool IsConfigured => State != ConfigurationState.Unset;

   /// <summary>True when the value can be used.</summary>
   public bool IsUsable => State != ConfigurationState.Invalid;
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

   /// <summary>
   /// The <b>provider families</b> a project surface can be built from (a composition choice) — not to be
   /// confused with a binding's <b>storage kind</b> (see <see cref="STORAGE_KINDS"/>).
   /// </summary>
   public static readonly string[] PROVIDER_FAMILIES = { "filesystem", "catalog" };

   /// <summary>The <b>storage kinds</b> a container's binding may name.</summary>
   public static readonly string[] STORAGE_KINDS = { "filesystem", "postgres", "service" };

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

      // which bindings were DECLARED (as opposed to derived from today's keys) — CF-1 reports the two
      // differently: a declared binding with no location is Set, a derived one with no location is Unset
      var declaredIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      // the default collection's binding: declared keys win, else derive from today's configuration
      var declaredDefault = ReadBinding(config, defaultBindingId, isDefault: true);
      if (declaredDefault is not null)
      {
         declaredIds.Add(defaultBindingId);
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
         if (declared is not null)
         {
            declaredIds.Add(collection.CollectionId);
            bindings.Add(declared);
         }
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

      // ---- CF-1 / ADR-0013: state every item the reader looked at -----------------------------
      // Resolution is deliberately UNCHANGED here — the reader STATES, the host decides what to ask
      // (ADR-0013 decisions 1 and 4). "Unset" is never conflated with "set to an empty value".
      var items = new List<ConfigurationItemInfo>
      {
         RootItem(config, root, legacy, applied),
         ReadItem(config, TARGET_KEY, problem: ProviderFamilyProblem(providerTarget)),
         ReadItem(config, DEFAULT_COLLECTION_KEY),
         ReadItem(config, WORKING_ROOT_KEY),
      };

      foreach (var binding in bindings)
      {
         items.Add(BindingItem(
            binding, declaredIds.Contains(binding.CollectionId)));
      }

      return new ProjectSettingsInfo(
         providerTarget,
         string.IsNullOrWhiteSpace(root) ? null : root!.Trim(),
         defaultId,
         config[WORKING_ROOT_KEY],
         collections,
         bindings,
         legacy,
         pending,
         applied)
      {
         Items = items
      };
   }

   /// <summary>
   /// State one configuration item. <b>Presence</b> is what makes it <c>Set</c>: an empty string is a
   /// stated value, not a missing one (ADR-0013 decision 1).
   /// </summary>
   /// <param name="config">configuration to read</param>
   /// <param name="key">the item's key</param>
   /// <param name="source">what stated it (defaults to the key itself)</param>
   /// <param name="problem">the reason it is unusable, when known</param>
   public static ConfigurationItemInfo ReadItem(
      IConfiguration config, string key, string? source = null, string? problem = null)
   {
      var section = config.GetSection(key);
      var present = section.Value is not null || section.GetChildren().Any();

      if (problem is not null)
      {
         return new ConfigurationItemInfo(
            key, ConfigurationState.Invalid, section.Value, source ?? key, problem);
      }

      return present
         ? new ConfigurationItemInfo(key, ConfigurationState.Set, section.Value, source ?? key)
         : new ConfigurationItemInfo(key, ConfigurationState.Unset);
   }

   /// <summary>
   /// The root item: the ADR-0011 key, a translated legacy key, an environment override, or unset.
   /// </summary>
   /// <param name="config">configuration to inspect</param>
   /// <param name="root">the <b>effective</b> root (after translation and environment precedence)</param>
   /// <param name="legacy">the legacy keys that were translated</param>
   /// <param name="applied">the environment variables that were applied</param>
   private static ConfigurationItemInfo RootItem(
      IConfiguration config, string? root,
      IReadOnlyList<string> legacy, IReadOnlyList<string> applied)
   {
      // the effective value is what a translated or overridden item must report: reading the item's own
      // key would report "unset" for a value that a legacy key or the environment actually stated
      var value = string.IsNullOrWhiteSpace(root) ? root : root!.Trim();

      if (applied.Contains(ENV_ROOT))
      {
         return new ConfigurationItemInfo(ROOT_KEY, ConfigurationState.Set, value, ENV_ROOT);
      }

      if (config.GetSection(ROOT_KEY).Value is not null)
      {
         return ReadItem(config, ROOT_KEY);
      }

      if (legacy.Contains(LEGACY_CONSOLE_PATH_KEY))
      {
         return new ConfigurationItemInfo(
            ROOT_KEY, ConfigurationState.Set, value, LEGACY_CONSOLE_PATH_KEY);
      }

      if (legacy.Contains(LEGACY_SETTINGS_CONSOLE_PATH_KEY))
      {
         return new ConfigurationItemInfo(
            ROOT_KEY, ConfigurationState.Set, value, LEGACY_SETTINGS_CONSOLE_PATH_KEY);
      }

      return new ConfigurationItemInfo(ROOT_KEY, ConfigurationState.Unset);
   }

   /// <summary>What is wrong with a provider family, or null when it is one we know.</summary>
   public static string? ProviderFamilyProblem(string? family)
      => string.IsNullOrWhiteSpace(family) ||
         PROVIDER_FAMILIES.Contains(family, StringComparer.OrdinalIgnoreCase)
            ? null
            : $"'{family}' is not a provider family; expected one of " +
              string.Join(", ", PROVIDER_FAMILIES) +
              ". A provider family chooses which project surface to build — it is not a storage kind " +
              "(see a container's binding)";

   /// <summary>What is wrong with a binding, or null when it is usable.</summary>
   public static string? BindingProblem(ProjectBindingInfo binding)
   {
      var kind = NormalizeTarget(binding.Target);

      if (!STORAGE_KINDS.Contains(kind, StringComparer.OrdinalIgnoreCase))
      {
         return $"'{binding.Target}' is not a storage kind; expected one of " +
            string.Join(", ", STORAGE_KINDS);
      }

      if ((kind == "postgres" || kind == "service") &&
          string.IsNullOrWhiteSpace(binding.Credential))
      {
         return $"a '{kind}' binding needs a credential NAME (a configuration key or a " +
            "vault:// reference) — the secret itself never lives in configuration";
      }

      return null;
   }

   /// <summary>
   /// State a container's binding. The item's value is the <b>address</b> (a folder or a base URI) —
   /// never the credential, which is only a name.
   /// </summary>
   private static ConfigurationItemInfo BindingItem(
      ProjectBindingInfo binding, bool declared)
   {
      var key = BINDINGS_SECTION + ":" + binding.CollectionId;
      var problem = BindingProblem(binding);

      if (problem is not null)
      {
         return new ConfigurationItemInfo(
            key, ConfigurationState.Invalid, binding.Location, key, problem);
      }

      // a DECLARED binding with no address is Set-to-empty; a DERIVED one with no address means
      // nothing has stated where the storage is yet — the first-run case, which is Unset (pending)
      if (string.IsNullOrWhiteSpace(binding.Location))
      {
         return declared
            ? new ConfigurationItemInfo(key, ConfigurationState.Set, null, key)
            : new ConfigurationItemInfo(key, ConfigurationState.Unset);
      }

      return new ConfigurationItemInfo(
         key, ConfigurationState.Set, binding.Location, key);
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
