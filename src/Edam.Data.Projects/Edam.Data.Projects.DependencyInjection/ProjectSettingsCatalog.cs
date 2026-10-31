using System.Threading;
using System.Threading.Tasks;
using Edam.Settings.Contracts;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>
/// The <b>settings catalog</b> for the projects configuration (ADR-0016): every setting a surface may show, as a
/// read model, built <b>from the same registry</b> the first-run prompt asks about — so adding a setting is still
/// one registry entry, never a new page.
/// <para>
/// It answers the question <see cref="ConfigurableItems.ToAsk"/> deliberately does not: not "what must I ask
/// now?" but "what can I see and change?". It therefore includes three different kinds of row, kept apart
/// because they are not the same thing:
/// </para>
/// <list type="bullet">
/// <item>the person's <b>values</b> (editable, overlay-backed) — the registry's askable and silent-default items;</item>
/// <item>the installation's <b>facts</b> (read-only, deployment/environment) — where projects actually live; and</item>
/// <item><b>recorded state</b> (read-only) — evidence that an action happened, which is not a preference.</item>
/// </list>
/// <para>
/// Provenance is resolved honestly: a value's <see cref="SettingsSource"/> names the layer that stated it, and an
/// <b>environment override wins</b> — a surface that showed "your value" while an environment variable was in force
/// would be the silent fallback ADR-0013 exists to prevent, wearing a friendlier face.
/// </para>
/// </summary>
public sealed class ProjectSettingsCatalog : ISettingsCatalog
{
   private readonly UserConfiguration _user;
   private readonly IReadOnlyDictionary<String, String?>? _environment;

   /// <param name="user">The answers service (registry + overlay + packaged marker).</param>
   /// <param name="environment">
   /// The override source for the installation facts (null = the process environment). Passed explicitly by tests
   /// so they never depend on ambient variables — the same rule <see cref="ProjectSettings.Read"/> follows.
   /// </param>
   public ProjectSettingsCatalog(
      UserConfiguration user, IReadOnlyDictionary<String, String?>? environment = null)
   {
      _user = user ?? throw new ArgumentNullException(nameof(user));
      _environment = environment;
   }

   /// <summary>Every setting this installation can show, in presentation order.</summary>
   public IReadOnlyList<SettingsItemInfo> Items => Build();

   /// <summary>The sections that actually hold something, in the contract's order.</summary>
   public IReadOnlyList<String> Groups =>
      Items.Select(i => i.Group).Distinct(StringComparer.OrdinalIgnoreCase)
         .OrderBy(IndexOf).ToList();

   /// <summary>The settings in one section, in presentation order.</summary>
   public IReadOnlyList<SettingsItemInfo> InGroup(String group) =>
      Items.Where(i => String.Equals(i.Group, group, StringComparison.OrdinalIgnoreCase)).ToList();

   /// <summary>One setting by id, or null when this host does not show it.</summary>
   public SettingsItemInfo? Find(String id) =>
      String.IsNullOrWhiteSpace(id)
         ? null
         : Items.FirstOrDefault(i => String.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));

   private static Int32 IndexOf(String group)
   {
      var order = SettingsGroups.Order;
      for (var i = 0; i < order.Count; i++)
      {
         if (String.Equals(order[i], group, StringComparison.OrdinalIgnoreCase))
         {
            return i;
         }
      }
      return order.Count;   // an unknown section sorts last rather than disappearing
   }

   /// <summary>
   /// Build the read model <b>now</b>: reading is cheap, and a snapshot taken once would show a stale value the
   /// moment an answer is stored — which is the one thing a settings surface must never do.
   /// </summary>
   private IReadOnlyList<SettingsItemInfo> Build()
   {
      var effective = _user.Effective;
      var overlay = _user.Overlay.ReadAll();
      var installation = _user.Installation.ReadAll();
      var items = new List<SettingsItemInfo>();

      foreach (var definition in ConfigurableItems.All)
      {
         // an item that does not apply to this installation is never put in front of the person (ADR-0013)
         if (!definition.AppliesTo(effective))
         {
            continue;
         }

         items.Add(Build(definition, effective, overlay, installation));
      }

      items.AddRange(BuildInstallationFacts(effective));

      // stable sort: the registry's own order is preserved inside each section
      return items.OrderBy(i => IndexOf(i.Group)).ToList();
   }

   private static SettingsItemInfo Build(
      ConfigurableItemInfo definition, IConfiguration effective,
      IReadOnlyDictionary<String, String?> overlay,
      IReadOnlyDictionary<String, String?> installation)
   {
      var state = ConfigurableItems.StateOf(effective, definition);

      // an ACTION-backed answer is provisional: once the action really happened the name is no longer in play,
      // so editing it here would promise something nothing reads
      var actionPending = definition.CompletedByKey is not null &&
         ProjectSettings.ReadItem(effective, definition.CompletedByKey).State == ConfigurationState.Unset;

      var editable = definition.Tier != SettingsTier.RecordedState &&
         definition.Kind != SettingsKind.ReadOnly &&
         (definition.CompletedByKey is null || actionPending);

      String? reason = null;
      if (!editable)
      {
         reason = definition.Tier == SettingsTier.RecordedState
            ? "this records that an action happened — it is evidence, not a preference"
            : "the action this answer enabled has already been completed, so the value is no longer used";
      }

      return new SettingsItemInfo(
         Id: definition.Id,
         Key: definition.Key,
         Title: definition.Title,
         Group: definition.Group ?? SettingsGroups.Projects,
         WhatItIs: definition.WhatItIs,
         WhatItAffects: definition.WhatItAffects,
         Kind: editable ? definition.Kind : SettingsKind.ReadOnly,
         Tier: definition.Tier,
         State: state.State switch
         {
            ConfigurationState.Set => SettingsValueState.Set,
            ConfigurationState.Invalid => SettingsValueState.Invalid,
            _ => SettingsValueState.Unset
         },
         Value: state.Value,
         Source: SourceOf(definition, state, overlay, installation),
         Problem: state.Problem,
         IsEditable: editable,
         ApplyScope: definition.ApplyScope,
         Default: definition.Default,
         Choices: definition.Choices,
         IsAdvanced: definition.Tier == SettingsTier.RecordedState,
         Reason: reason);
   }

   /// <summary>
   /// Which layer actually stated the value — the person's answer, the packaged marker, the installation's
   /// configuration, or nothing at all (in which case the documented default is what is in force).
   /// </summary>
   private static SettingsSource SourceOf(
      ConfigurableItemInfo definition, ConfigurationItemInfo state,
      IReadOnlyDictionary<String, String?> overlay,
      IReadOnlyDictionary<String, String?> installation)
   {
      if (overlay.ContainsKey(definition.Key))
      {
         return SettingsSource.User;
      }

      if (installation.ContainsKey(definition.Key))
      {
         return SettingsSource.Installation;
      }

      if (state.State != ConfigurationState.Unset)
      {
         return SettingsSource.Deployment;
      }

      return definition.Default is null ? SettingsSource.Unset : SettingsSource.Default;
   }

   /// <summary>
   /// <b>Where projects actually live</b> (ADR-0011 / ADR-0014): deployment facts, shown read-only. They are in the
   /// catalog because "where is my data?" is the question a settings surface is opened to answer, and because
   /// hiding them is what sends a person hunting through a dialog for a value they cannot change anyway.
   /// </summary>
   private IReadOnlyList<SettingsItemInfo> BuildInstallationFacts(IConfiguration effective)
   {
      var facts = new List<SettingsItemInfo>();

      try
      {
         var settings = ProjectSettings.Read(effective, _environment);

         var rootFromEnvironment = settings.EnvironmentOverrides.Any(o =>
            String.Equals(o, ProjectSettings.ENV_ROOT, StringComparison.OrdinalIgnoreCase));

         facts.Add(new SettingsItemInfo(
            Id: "projects.target",
            Key: ProjectSettings.TARGET_KEY,
            Title: "Project storage target",
            Group: SettingsGroups.Installation,
            WhatItIs: "Which provider family the project platform is built from.",
            WhatItAffects: "It decides whether projects are addressed on a file system or in a catalog. It is " +
               "stated by the installation (ADR-0011), so it is shown here and not edited from the application.",
            Kind: SettingsKind.ReadOnly,
            Tier: SettingsTier.Installation,
            State: String.IsNullOrWhiteSpace(settings.Target)
               ? SettingsValueState.Unset
               : SettingsValueState.Set,
            Value: settings.Target,
            Source: String.IsNullOrWhiteSpace(settings.Target)
               ? SettingsSource.Unset
               : SettingsSource.Deployment,
            Problem: null,
            IsEditable: false,
            ApplyScope: SettingsApplyScope.Restart,
            Reason: "the storage target is a deployment fact — change the installation's configuration"));

         facts.Add(new SettingsItemInfo(
            Id: "projects.root",
            Key: ProjectSettings.ROOT_KEY,
            Title: "Project location",
            Group: SettingsGroups.Installation,
            WhatItIs: "Where the default collection of projects lives.",
            WhatItAffects: "It is the one location statement the project platform resolves (ADR-0011); every " +
               "project address is relative to it. An environment variable overrides it.",
            Kind: SettingsKind.ReadOnly,
            Tier: SettingsTier.Installation,
            State: String.IsNullOrWhiteSpace(settings.Root)
               ? SettingsValueState.Unset
               : SettingsValueState.Set,
            Value: settings.Root,
            Source: rootFromEnvironment
               ? SettingsSource.Environment
               : String.IsNullOrWhiteSpace(settings.Root)
                  ? SettingsSource.Unset
                  : SettingsSource.Deployment,
            Problem: null,
            IsEditable: false,
            ApplyScope: SettingsApplyScope.Restart,
            Reason: rootFromEnvironment
               ? "an environment variable states it, and the environment wins over everything else"
               : "the location is a deployment fact — change the installation's configuration"));

         var binding = settings.DefaultBinding;
         if (binding is not null)
         {
            var location = String.IsNullOrWhiteSpace(binding.Location)
               ? "(no location yet)"
               : binding.Location!;

            var credential = String.IsNullOrWhiteSpace(binding.Credential)
               ? String.Empty
               : "  ·  credential: " + binding.Credential;

            // provenance, honestly: when nothing stated a target or a location, the binding in force is the
            // DOCUMENTED DEFAULT — calling that "the installation's configuration" is the same small lie as
            // showing a person's value while an environment variable wins
            var stated = !String.IsNullOrWhiteSpace(settings.Target) ||
               !String.IsNullOrWhiteSpace(binding.Location);

            facts.Add(new SettingsItemInfo(
               Id: "projects.storage",
               Key: ProjectSettings.BINDINGS_SECTION,
               Title: "Storage",
               Group: SettingsGroups.Installation,
               WhatItIs: "The storage that backs the default collection, its non-secret address, and the name of " +
                  "the credential it uses (never the secret itself).",
               WhatItAffects: "It is where a project's content really is. A container is a namespace inside this " +
                  "one storage, not a separate one (ADR-0014).",
               Kind: SettingsKind.ReadOnly,
               Tier: SettingsTier.Installation,
               State: SettingsValueState.Set,
               Value: binding.Target + "  ·  " + location + credential,
               Source: rootFromEnvironment
                  ? SettingsSource.Environment
                  : stated ? SettingsSource.Deployment : SettingsSource.Default,
               Problem: null,
               IsEditable: false,
               ApplyScope: SettingsApplyScope.Restart,
               Reason: "storage is a deployment fact — manage containers and the catalog connection in the " +
                  "sections above"));
         }
      }
      catch (Exception ex)
      {
         // the catalog is a READ surface: failing to resolve the storage is reported, never thrown, so the
         // person still sees every setting they can change
         facts.Add(new SettingsItemInfo(
            Id: "projects.storage",
            Key: ProjectSettings.BINDINGS_SECTION,
            Title: "Storage",
            Group: SettingsGroups.Installation,
            WhatItIs: "The storage that backs the default collection.",
            WhatItAffects: "It could not be resolved, so nothing here is claimed about where projects live.",
            Kind: SettingsKind.ReadOnly,
            Tier: SettingsTier.Installation,
            State: SettingsValueState.Invalid,
            Value: null,
            Source: SettingsSource.Unset,
            Problem: ex.Message,
            IsEditable: false,
            ApplyScope: SettingsApplyScope.Restart,
            Reason: "the storage could not be resolved"));
      }

      return facts;
   }
}

/// <summary>
/// The <b>one</b> way a projects setting is stored (ADR-0016) — validation happens before anything is written, so an
/// unusable value never reaches the overlay (ADR-0013 decision 7). A surface keeps no storage logic of its own,
/// which is what lets the first-run prompt, a settings page and an API endpoint share one behavior.
/// </summary>
public sealed class ProjectSettingsEditor : ISettingsEditor
{
   private readonly UserConfiguration _user;

   public ProjectSettingsEditor(UserConfiguration user) =>
      _user = user ?? throw new ArgumentNullException(nameof(user));

   /// <summary>Store a value for a registry item; null clears it (the deployment/default applies again).</summary>
   public Task<SettingsChangeResult> SetAsync(
      String id, String? value, CancellationToken ct = default)
   {
      var item = ConfigurableItems.Find(id);
      if (item is null)
      {
         return Task.FromResult(SettingsChangeResult.Refused(
            $"'{id}' is not a project setting that can be stored (installation facts are read-only)"));
      }

      try
      {
         var refused = _user.Answer(item, value);
         return Task.FromResult(refused is null
            ? SettingsChangeResult.Saved(item.ApplyScope)
            : SettingsChangeResult.Refused(refused));
      }
      catch (Exception ex)
      {
         // a store that cannot be written is REPORTED, not thrown at a surface
         return Task.FromResult(SettingsChangeResult.Refused(
            "the value could not be stored: " + ex.Message));
      }
   }

   /// <summary>Forget one answer, so the deployment's value or the documented default applies again.</summary>
   public Task<SettingsChangeResult> ResetAsync(String id, CancellationToken ct = default)
   {
      var item = ConfigurableItems.Find(id);
      if (item is null)
      {
         return Task.FromResult(SettingsChangeResult.Refused(
            $"'{id}' is not a project setting that can be reset"));
      }

      _user.Reset(item.Key);
      return Task.FromResult(SettingsChangeResult.Saved(item.ApplyScope));
   }

   /// <summary>Forget every answer; the once-per-installation markers are deliberately spared.</summary>
   public Task<Int32> ResetAllAsync(CancellationToken ct = default)
   {
      var forgotten = _user.Overlay.ReadAll().Count;
      _user.Reset();
      return Task.FromResult(forgotten);
   }
}
