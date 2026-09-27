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
public sealed record ConfigurableItemInfo(
   string Id,
   string Key,
   string Title,
   string WhatItIs,
   string WhatItAffects,
   ConfigurationPolicy Policy,
   string? Default = null,
   ConfigurationStorage Storage = ConfigurationStorage.AppDataOverlay,
   Func<string?, string?>? Validate = null)
{
   /// <summary>True when this item may be put in front of the user.</summary>
   public bool IsAskable => Policy != ConfigurationPolicy.SilentDefault;
}

/// <summary>One item that needs the user's input, and why (CF-2).</summary>
/// <param name="Item">The registry entry.</param>
/// <param name="State">Unset (nothing stated it) or Invalid (stated but unusable).</param>
/// <param name="Current">What is stated now — never replaced, only reported.</param>
/// <param name="Problem">Why a stated value is unusable.</param>
public sealed record ConfigurableAsk(
   ConfigurableItemInfo Item,
   ConfigurationState State,
   string? Current,
   string? Problem);

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
            Policy: ConfigurationPolicy.AskOnceWhenUnset,
            Default: DEFAULT_PROJECT_NAME,
            Storage: ConfigurationStorage.AppDataOverlay,
            Validate: ValidateProjectName),

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

      foreach (var item in All.Where(i => i.IsAskable))
      {
         var state = StateOf(config, item);

         if (state.State == ConfigurationState.Unset)
         {
            asks.Add(new ConfigurableAsk(item, ConfigurationState.Unset, null, null));
            continue;
         }

         if (state.State == ConfigurationState.Invalid)
         {
            asks.Add(new ConfigurableAsk(
               item, ConfigurationState.Invalid, state.Value, state.Problem));
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
}
