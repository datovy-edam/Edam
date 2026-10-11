using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>What the application does when an item is unset (CF-2 / ADR-0013 decision 3).</summary>
public enum ConfigurationPolicy
{
   /// <summary>A sane default exists — <b>never ask</b>.</summary>
   SilentDefault,

   /// <summary>Ask once when unset, remember the answer, never ask again.</summary>
   AskOnceWhenUnset,

   /// <summary>Ask; the <b>feature</b> that needs it stays unavailable until answered — never the application.</summary>
   Required
}

/// <summary>Where an answer is kept (CF-2 / ADR-0013 decision 6).</summary>
public enum ConfigurationStorage
{
   /// <summary>The packaged per-user state — used for the once-per-installation marker.</summary>
   PackagedSettings,

   /// <summary>The per-user app-data <b>overlay</b> file — user answers; never the packaged seed.</summary>
   AppDataOverlay
}

/// <summary>
/// One configurable item (CF-2 / ADR-0013 decision 2): the single definition of something the
/// application may have to <b>ask</b> rather than guess.
/// </summary>
/// <param name="Id">Stable id (also the configuration key for the first entries).</param>
/// <param name="Key">The configuration key the value is read from.</param>
/// <param name="Title">Short label for a prompt.</param>
/// <param name="WhatItIs">One sentence: what this is (ADR-0013 decision 8).</param>
/// <param name="WhatItAffects">What changes once it is answered (ADR-0013 decision 8).</param>
/// <param name="Policy">Whether/when to ask (ADR-0013 decision 3).</param>
/// <param name="Default">The value offered when asking.</param>
/// <param name="Storage">Where the answer lives (ADR-0013 decision 6).</param>
/// <param name="Validate">Returns why a stated value is unusable, or null when it is fine.</param>
/// <param name="CompletedByKey">
/// The configuration key that proves this item's <b>action</b> was completed. When set, answering the item
/// is <b>not</b> completion: the item keeps being asked — with the answer pre-filled — until that key is
/// stated. An answer to a question that implies an action ("name the starter project") is only valid once
/// the action (the project exists) has actually happened.
/// </param>
/// <param name="Applies">
/// When set, whether the item applies to the configuration at hand at all — a <b>catalog container</b> is
/// only meaningful when a catalog is configured, so it must not be asked for a file-system installation.
/// </param>
public sealed record ConfigurableItemInfo(
   string Id,
   string Key,
   string Title,
   string WhatItIs,
   string WhatItAffects,
   ConfigurationPolicy Policy,
   string? Default = null,
   ConfigurationStorage Storage = ConfigurationStorage.AppDataOverlay,
   Func<string?, string?>? Validate = null,
   string? CompletedByKey = null,
   Func<IConfiguration, bool>? Applies = null)
{
   /// <summary>True when this item may be put in front of the user.</summary>
   public bool IsAskable => Policy != ConfigurationPolicy.SilentDefault;

   /// <summary>True when the answer is provisional until <see cref="CompletedByKey"/> is stated.</summary>
   public bool IsActionBacked => !String.IsNullOrWhiteSpace(CompletedByKey);

   /// <summary>
   /// True when the item is meaningful for this configuration (an item with no condition always is) — so a
   /// question that does not apply to the installation is never put in front of the user.
   /// </summary>
   public bool AppliesTo(IConfiguration? configuration) =>
      Applies is null || configuration is null || Applies(configuration);
}

/// <summary>One item that needs the user's input, and why (CF-2).</summary>
/// <param name="Item">The registry entry.</param>
/// <param name="State">Unset (nothing stated it) or Invalid (stated but unusable).</param>
/// <param name="Current">What is stated now — never replaced, only reported.</param>
/// <param name="Problem">Why a stated value is unusable.</param>
/// <param name="Reason">Why the item is still being asked about (e.g. its action is not completed).</param>
public sealed record ConfigurableAsk(
   ConfigurableItemInfo Item,
   ConfigurationState State,
   string? Current,
   string? Problem,
   string? Reason = null);

/// <summary>
/// The <b>registry</b> of configurable items (CF-2 / ADR-0013). Adding a new thing to ask about is a
/// new entry here — <b>not</b> a new dialog: the prompt surface is generated from this list, the batch
/// comes from <see cref="ToAsk"/>, and "invalid" has exactly one definition (<see cref="ConfigurableItemInfo.Validate"/>).
/// </summary>
public static class ConfigurableItems
{
   /// <summary>The starter project's name — the first instance of this pattern (ADR-0012).</summary>
   public const string DEFAULT_PROJECT_NAME_KEY = "Edam:Projects:DefaultProjectName";

   /// <summary>What is offered when nothing is stated: the organization is <b>Edam</b> (ADR-0012).</summary>
   public const string DEFAULT_PROJECT_NAME = "Edam.Sample";

   /// <summary>
   /// Records that the starter project was already created/offered, so it is <b>never offered twice</b>
   /// and a project the user deleted is never resurrected (ADR-0012 decision 4).
   /// </summary>
   public const string DEFAULT_PROJECT_MARKER_KEY = "Edam:Projects:DefaultProjectOffered";

   /// <summary>
   /// The starter-arguments <b>template</b> a newly created project is seeded from (DP-4) — a configurable
   /// item with a <b>silent default</b>, because a sane value ships with the application: it is documented
   /// and overridable, and it never has to ask.
   /// </summary>
   public const string DEFAULT_TEMPLATE_KEY = "Edam:Projects:DefaultTemplate";

   /// <summary>The template the packaged seed ships, so a new project is never inert (DP-4).</summary>
   public const string DEFAULT_TEMPLATE = "Templates/ToAssets.Args.json";

   /// <summary>
   /// The catalog <b>container</b> (the collection) the projects live in (ADR-0014 decision 1). A name is a
   /// <b>choice</b>, so it is asked for rather than generated: a container called <c>pe3-mssql-d3ca8a12</c>
   /// is not a name, and a person should never have to look at one.
   /// </summary>
   public const string CONTAINER_KEY = "Edam:Projects:DefaultCollection";

   /// <summary>What is offered when nothing is stated — organization-first, like the starter project.</summary>
   public const string DEFAULT_CONTAINER_NAME = "Edam.Projects";

   /// <summary>
   /// Records that the answered container <b>really exists</b> — the proof of the action, so the question
   /// stops being asked (ADR-0013 decision 12). Kept with the packaged state, like the starter-project marker.
   /// </summary>
   public const string CONTAINER_READY_KEY = "Edam:Catalog:ContainerReady";

   /// <summary>Longest accepted container name (it becomes a URI authority and a folder-name-like key).</summary>
   public const int CONTAINER_NAME_MAX_LENGTH = 64;

   /// <summary>
   /// The catalog's <b>connection string</b> — the binding the deployment states (ADR-0014 decision 1), which a
   /// person may now <b>deliberately override</b> from the container manager. An override is an ordinary answer
   /// (the per-user overlay) and is always <b>reported as the source</b>, so a value can never look like it came
   /// from somewhere it did not.
   /// </summary>
   public const string CATALOG_CONNECTION_KEY = "ConnectionStrings:catalog";

   /// <summary>Longest accepted project name (it also becomes a folder and an artifact name).</summary>
   public const int PROJECT_NAME_MAX_LENGTH = 64;

   /// <summary>Location families that already exist at container level — never a project name.</summary>
   private static readonly string[] ReservedProjectNames =
      { "Projects", "Templates", "TextMaps", "Samples", "Temp" };

   /// <summary>Windows device names, which cannot be used as a folder name.</summary>
   private static readonly string[] DeviceNames =
   {
      "CON", "PRN", "AUX", "NUL",
      "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
      "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
   };

   /// <summary>Every configurable item the application knows about.</summary>
   public static IReadOnlyList<ConfigurableItemInfo> All { get; } =
      new ConfigurableItemInfo[]
      {
         new(
            Id: DEFAULT_PROJECT_NAME_KEY,
            Key: DEFAULT_PROJECT_NAME_KEY,
            Title: "Starter project name",
            WhatItIs: "The name of the sample project the application offers on a new installation.",
            WhatItAffects: "It becomes the project's address (/Projects/<name>), its folder on a " +
               "file-system collection, and the name of its starter arguments file.",
            Policy: ConfigurationPolicy.Required,
            Default: DEFAULT_PROJECT_NAME,
            Storage: ConfigurationStorage.AppDataOverlay,
            Validate: ValidateProjectName,
            CompletedByKey: DEFAULT_PROJECT_MARKER_KEY),

         new(
            Id: DEFAULT_TEMPLATE_KEY,
            Key: DEFAULT_TEMPLATE_KEY,
            Title: "Starter arguments template",
            WhatItIs: "The template a newly created project's Arguments folder is seeded from.",
            WhatItAffects: "It becomes the project's starter arguments file — without it a new project " +
               "has its folder structure but nothing to run, which reads as a broken project rather " +
               "than an empty one.",
            Policy: ConfigurationPolicy.SilentDefault,
            Default: DEFAULT_TEMPLATE,
            Storage: ConfigurationStorage.AppDataOverlay,
            Validate: null),

         // ADR-0014 (amended): the binding is a deployment fact, and a person may override it — deliberately and
         // visibly. Silent default, so it is never ASKED (a connection string is not a first-run question); it
         // is edited in the container manager, and an override lives in the per-user overlay.
         new(
            Id: CATALOG_CONNECTION_KEY,
            Key: CATALOG_CONNECTION_KEY,
            Title: "Catalog connection string",
            WhatItIs: "Where the catalog keeps its data — the database your projects and their content live in.",
            WhatItAffects: "Pointing it at another catalog changes which containers, projects and content the " +
               "application sees. The deployment states it; keeping your own value overrides that, visibly.",
            Policy: ConfigurationPolicy.SilentDefault,
            Default: null,
            Storage: ConfigurationStorage.AppDataOverlay,
            Validate: ValidateConnectionString),

         // ADR-0014 decision 1: the container is the ONE part of the storage binding a person states. It is
         // asked only when a catalog is configured (Applies), it is asked until the container really EXISTS
         // (CompletedByKey — an answer is not completion), and a name is validated by the catalog address rule
         // so a name the prompt accepts can never be refused by the address parser later.
         new(
            Id: CONTAINER_KEY,
            Key: CONTAINER_KEY,
            Title: "Catalog container",
            WhatItIs: "The name of the catalog container (the collection) that will hold your projects.",
            WhatItAffects: "It is the container's address: your projects live at /Projects/<name> inside " +
               "it. The container's NAME is its address and cannot be changed later, while its DESCRIPTION " +
               "(what you see everywhere, and what you may edit in the Containers dialog) starts as this text. " +
               "The catalog itself is configured for this installation — the container is the part that is " +
               "yours to name.",
            Policy: ConfigurationPolicy.Required,
            Default: DEFAULT_CONTAINER_NAME,
            Storage: ConfigurationStorage.AppDataOverlay,
            Validate: ValidateContainerName,
            CompletedByKey: CONTAINER_READY_KEY,
            Applies: configuration =>
               String.Equals((configuration[ProjectSettings.TARGET_KEY] ?? String.Empty).Trim(),
                  "catalog", StringComparison.OrdinalIgnoreCase) ||
               !String.IsNullOrWhiteSpace(configuration["ConnectionStrings:catalog"])),

         new(
            Id: DEFAULT_PROJECT_MARKER_KEY,
            Key: DEFAULT_PROJECT_MARKER_KEY,
            Title: "Starter project offered (marker)",
            WhatItIs: "A marker recording that the starter project was already offered or created.",
            WhatItAffects: "It stops the application from offering the starter project twice, and from " +
               "recreating one the user deleted.",
            Policy: ConfigurationPolicy.SilentDefault,
            Default: null,
            Storage: ConfigurationStorage.PackagedSettings,
            Validate: null),

         // the proof of the CONTAINER action (ADR-0014): never asked — it records that the container the
         // person named really exists, which is what stops the container question being asked again
         new(
            Id: CONTAINER_READY_KEY,
            Key: CONTAINER_READY_KEY,
            Title: "Catalog container ready (marker)",
            WhatItIs: "A marker recording that the answered catalog container exists.",
            WhatItAffects: "It stops the application asking for a container name that has already been " +
               "created — and, because it is packaged state, resetting the answers does not re-ask.",
            Policy: ConfigurationPolicy.SilentDefault,
            Default: null,
            Storage: ConfigurationStorage.PackagedSettings,
            Validate: null)
      };

   /// <summary>The registry entry for an id, or null.</summary>
   public static ConfigurableItemInfo? Find(string id) =>
      All.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));

   /// <summary>
   /// State an item using the registry's own validator — so <b>invalid</b> means the same thing
   /// wherever it is reported (ADR-0013 decision 7). A stated-but-unusable value is reported, never
   /// replaced: <see cref="ConfigurationItemInfo.Value"/> still holds what was stated.
   /// </summary>
   public static ConfigurationItemInfo StateOf(IConfiguration config, ConfigurableItemInfo item)
   {
      var state = ProjectSettings.ReadItem(config, item.Key);
      if (state.State == ConfigurationState.Unset)
      {
         return state;
      }

      var problem = item.Validate?.Invoke(state.Value);
      return problem is null
         ? state
         : state with { State = ConfigurationState.Invalid, Problem = problem };
   }

   /// <summary>
   /// The <b>batch</b> to ask about: askable items that nothing has stated, plus stated items whose
   /// validator rejects them (ask to fix — the value is never discarded silently). Silent-default items
   /// are never asked.
   /// </summary>
   public static IReadOnlyList<ConfigurableAsk> ToAsk(IConfiguration config)
   {
      if (config is null) throw new ArgumentNullException(nameof(config));

      var asks = new List<ConfigurableAsk>();

      foreach (var item in All.Where(i => i.IsAskable && i.AppliesTo(config)))
      {
         var state = StateOf(config, item);

         // A stated-but-unusable VALUE outranks everything: the person must fix the value before the
         // action can even be attempted, so a pending action never masks a validation problem.
         if (state.State == ConfigurationState.Invalid)
         {
            asks.Add(new ConfigurableAsk(
               item, ConfigurationState.Invalid, state.Value, state.Problem));
            continue;
         }

         // ACTION-BACKED ITEMS: the answer is provisional. While the key that proves the ACTION is not
         // stated, the item stays in the batch — with the answer pre-filled, so it is never retyped. This
         // is what makes "the answer is only valid once the action happened" true rather than a promise.
         if (item.IsActionBacked &&
             ProjectSettings.ReadItem(config, item.CompletedByKey!).State == ConfigurationState.Unset)
         {
            asks.Add(new ConfigurableAsk(
               item, ConfigurationState.Unset, state.Value, null,
               "the action this answer enables has not been completed yet"));
            continue;
         }

         if (state.State == ConfigurationState.Unset)
         {
            asks.Add(new ConfigurableAsk(item, ConfigurationState.Unset, null, null));
         }
      }

      return asks;
   }

   /// <summary>
   /// The path-safety rules a project name must satisfy (ADR-0013 decision 7). A name becomes a catalog
   /// path segment, a folder and an artifact name, so it may contain <b>no separator at all</b> — which
   /// is what makes it impossible for a name to leave <c>/Projects</c>.
   /// </summary>
   /// <returns>Why the name is unusable, or null when it is fine.</returns>
   public static string? ValidateProjectName(string? name)
   {
      var raw = name ?? string.Empty;

      if (raw.Length == 0)
      {
         return "a project name is required";
      }

      if (raw != raw.Trim())
      {
         return "a project name may not start or end with a space";
      }

      if (raw.Length > PROJECT_NAME_MAX_LENGTH)
      {
         return $"a project name may be at most {PROJECT_NAME_MAX_LENGTH} characters";
      }

      if (raw.StartsWith('.') || raw.EndsWith('.'))
      {
         return "a project name may not start or end with a dot";
      }

      if (raw.Any(c => !(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_')))
      {
         return "a project name may contain only letters, digits, '.', '-' and '_' — " +
            "no separators, so it can never point outside /Projects";
      }

      var firstSegment = raw.Split('.')[0];
      if (DeviceNames.Contains(firstSegment, StringComparer.OrdinalIgnoreCase))
      {
         return $"'{firstSegment}' is a reserved device name and cannot be a folder";
      }

      if (ReservedProjectNames.Contains(raw, StringComparer.OrdinalIgnoreCase))
      {
         return $"'{raw}' is reserved for a container-level location family";
      }

      return null;
   }

   /// <summary>
   /// The rules a <b>connection string</b> must satisfy: it must look like a connection string, and it must
   /// <b>not carry a password</b> — the application accepts <b>Integrated Security</b> only, so no secret is
   /// ever written to a file, a log or a screen (ADR-0010; ADR-0014 decision 2).
   /// </summary>
   /// <returns>Why the value is unusable, or null when it is fine.</returns>
   public static string? ValidateConnectionString(string? value)
   {
      var raw = value ?? String.Empty;

      if (raw.Trim().Length == 0)
      {
         return "a connection string is required (or use the deployment's value)";
      }

      if (raw.Contains("Password=", StringComparison.OrdinalIgnoreCase) ||
          raw.Contains("Pwd=", StringComparison.OrdinalIgnoreCase))
      {
         return "a password is not accepted here — use Integrated Security " +
            "(Integrated Security=True / Trusted_Connection=True) so no secret has to be stored anywhere";
      }

      if (!raw.Contains('='))
      {
         return "a connection string is written as key=value pairs, e.g. " +
            "Server=.;Database=Edam.Database;Integrated Security=True";
      }

      return null;
   }

   /// <summary>
   /// The rules a <b>container</b> name must satisfy. They mirror the catalog address rule
   /// (<c>CatalogAddress.IsValidContainer</c>: letters, digits, <c>.</c>, <c>-</c> and <c>_</c>) and are
   /// deliberately <b>stricter</b> — a name must start with a letter — so that anything the prompt accepts is
   /// certain to be accepted by the address parser. A container name is not a file path: separators are
   /// rejected, so a name cannot describe a location outside the container it names.
   /// </summary>
   /// <returns>Why the name is unusable, or null when it is fine.</returns>
   public static string? ValidateContainerName(string? name)
   {
      var raw = name ?? String.Empty;

      if (raw.Length == 0)
      {
         return "a container name is required";
      }

      if (raw != raw.Trim())
      {
         return "a container name may not start or end with a space";
      }

      if (raw.Length > CONTAINER_NAME_MAX_LENGTH)
      {
         return $"a container name may be at most {CONTAINER_NAME_MAX_LENGTH} characters";
      }

      if (!Char.IsLetter(raw[0]))
      {
         return "a container name must start with a letter";
      }

      if (raw.Any(c => !(Char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_')))
      {
         return "a container name may contain only letters, digits, '.', '-' and '_' " +
            "(the catalog address rule) — no separators, so it cannot describe a location";
      }

      if (raw.StartsWith('.') || raw.EndsWith('.'))
      {
         return "a container name may not start or end with a dot";
      }

      return null;
   }
}
