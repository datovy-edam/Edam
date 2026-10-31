using Edam.Data.Projects.DependencyInjection;
using Edam.Settings.Contracts;
using Edam.Settings.Core;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>ST-1…ST-3 (ADR-0016)</b> prove the <b>settings surface</b> headlessly — the part a user interface may rely on
/// but must never have to re-implement:
/// <list type="bullet">
/// <item><b>ST-1 the catalog</b> — every setting is described once, the three tiers are kept apart, and an item is
/// only editable while changing it would actually mean something;</item>
/// <item><b>ST-2 provenance</b> — a value names the layer that stated it, <b>environment wins</b>, and a
/// stated-but-unusable value is reported rather than replaced;</item>
/// <item><b>ST-3 the editor and apply</b> — one write path, validation before anything is stored, and a change that
/// claims to be live really is applied (a save that nothing reads is the "configured but inert" defect).</item>
/// </list>
/// </summary>
public static class SettingsSurfaceScenario
{
   private static UserConfiguration User(
      String root, params (String Key, String Value)[] stated)
   {
      Directory.CreateDirectory(root);

      var config = new ConfigurationBuilder()
         .AddInMemoryCollection(stated.Select(v =>
            new KeyValuePair<String, String?>(v.Key, v.Value)))
         .Build();

      return new UserConfiguration(
         config, new FileStateStore(root), new InMemoryStateStore("(ST installation)"));
   }

   private sealed class RecordingApplier : ISettingsApplier
   {
      public List<String> Applied { get; } = new();
      public String? Failure { get; set; }

      public Task<String?> ApplyAsync(
         SettingsItemInfo item, String? value, CancellationToken ct = default)
      {
         Applied.Add(item.Id);
         return Task.FromResult(Failure);
      }
   }

   // ---- ST-1: the catalog ---------------------------------------------------------------------

   public static List<ProjectScenario.Check> RunCatalog(String root)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(String name, Boolean passed, String detail) =>
         checks.Add(new ProjectScenario.Check(name, passed, detail));

      var user = User(root);
      var catalog = new ProjectSettingsCatalog(user);

      var registryIds = ConfigurableItems.All
         .Where(i => i.AppliesTo(user.Effective))
         .Select(i => i.Id).ToList();

      Check("Every applicable registry item appears exactly ONCE in the catalog",
         registryIds.All(id => catalog.Find(id) is not null) &&
         catalog.Items.Select(i => i.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() ==
            catalog.Items.Count,
         $"{catalog.Items.Count} row(s): {String.Join(", ", catalog.Items.Select(i => i.Id))}");

      Check("The catalog is GENERATED from the registry: every row carries a section, a kind and its explanation",
         catalog.Items.All(i => !String.IsNullOrWhiteSpace(i.Group) &&
            !String.IsNullOrWhiteSpace(i.WhatItIs) && !String.IsNullOrWhiteSpace(i.WhatItAffects)),
         String.Join(" | ", catalog.Items.Select(i => $"{i.Id}:{i.Group}/{i.Kind}")));

      Check("Sections come from the CONTRACT's order (so every host groups the same way)",
         catalog.Groups.SequenceEqual(catalog.Groups.OrderBy(g => IndexOf(g))) &&
         catalog.Groups.All(g => SettingsGroups.Order.Contains(g)),
         String.Join(", ", catalog.Groups));

      var facts = catalog.InGroup(SettingsGroups.Installation);
      Check("Deployment facts are READ-ONLY and say why (storage target / location / storage)",
         facts.Count >= 2 && facts.All(f => !f.IsEditable && !String.IsNullOrWhiteSpace(f.Reason) &&
            f.Tier == SettingsTier.Installation),
         $"{facts.Count} fact(s): {String.Join(", ", facts.Select(f => f.Id))}");

      var markers = catalog.InGroup(SettingsGroups.Recorded);
      Check("Recorded state is READ-ONLY, advanced, and named as evidence rather than a preference",
         markers.Count >= 2 && markers.All(m => !m.IsEditable && m.IsAdvanced &&
            m.Tier == SettingsTier.RecordedState && m.Kind == SettingsKind.ReadOnly),
         $"{markers.Count} marker(s): {String.Join(", ", markers.Select(m => m.Id))}");

      var container = catalog.Find(ConfigurableItems.CONTAINER_KEY);
      Check("An item that does not APPLY is not put in front of the person (no catalog, no container question)",
         container is null,
         container is null ? "absent (no catalog configured)" : "present (WRONG)");

      var withCatalog = new ProjectSettingsCatalog(User(root + "-cat",
         ("ConnectionStrings:catalog", "Server=.;Database=edam;Integrated Security=True")));

      Check("...and it APPEARS once a catalog is configured, still action-backed (editable until it exists)",
         withCatalog.Find(ConfigurableItems.CONTAINER_KEY) is { IsEditable: true },
         $"{withCatalog.Find(ConfigurableItems.CONTAINER_KEY)?.IsEditable}");

      // an action-backed answer stops being editable once the action really happened
      var pending = catalog.Find(ConfigurableItems.DEFAULT_PROJECT_NAME_KEY);
      user.MarkStarterProjectOffered();
      var completed = new ProjectSettingsCatalog(user).Find(ConfigurableItems.DEFAULT_PROJECT_NAME_KEY);

      Check("An ACTION-BACKED value is editable while the action is pending, and read-only once it is done",
         pending is { IsEditable: true } && completed is { IsEditable: false } &&
         !String.IsNullOrWhiteSpace(completed?.Reason),
         $"pending={pending?.IsEditable} completed={completed?.IsEditable} reason='{completed?.Reason}'");

      return checks;
   }

   // ---- ST-2: provenance ----------------------------------------------------------------------

   public static List<ProjectScenario.Check> RunProvenance(String root)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(String name, Boolean passed, String detail) =>
         checks.Add(new ProjectScenario.Check(name, passed, detail));

      var domainItem = ConfigurableItems.DOMAIN_ID_KEY;

      // 1 — nothing stated: the documented default is what is in force, and that is stated as such
      var unstated = new ProjectSettingsCatalog(User(root + "-a")).Find(domainItem);
      Check("Nothing stated: the value is UNSET and the source names the documented DEFAULT",
         unstated is { State: SettingsValueState.Unset, Value: null, Source: SettingsSource.Default } &&
         unstated.Display == ConfigurableItems.DEFAULT_DOMAIN_ID,
         $"state={unstated?.State} source={unstated?.Source} display='{unstated?.Display}'");

      // 2 — the installation states it
      var deployed = new ProjectSettingsCatalog(User(root + "-b",
         (domainItem, "Contoso.Claims"))).Find(domainItem);
      Check("The installation's configuration is reported as DEPLOYMENT (not as the person's value)",
         deployed is { State: SettingsValueState.Set, Source: SettingsSource.Deployment } &&
         deployed.Value == "Contoso.Claims",
         $"source={deployed?.Source} value='{deployed?.Value}'");

      // 3 — the person states it, and the answer WINS over the deployment
      var answering = User(root + "-c", (domainItem, "Contoso.Claims"));
      answering.Answer(ConfigurableItems.Find(domainItem)!, "Edam.Claims");
      var answered = new ProjectSettingsCatalog(answering).Find(domainItem);
      Check("An answer is reported as the person's VALUE and overrides the deployment's",
         answered is { State: SettingsValueState.Set, Source: SettingsSource.User } &&
         answered.Value == "Edam.Claims",
         $"source={answered?.Source} value='{answered?.Value}'");

      // 4 — the environment WINS over everything, and says so
      var environment = new Dictionary<String, String?>(StringComparer.OrdinalIgnoreCase)
      {
         [ProjectSettings.ENV_ROOT] = "/srv/edam/projects"
      };
      var overridden = new ProjectSettingsCatalog(User(root + "-d",
         (ProjectSettings.ROOT_KEY, "/home/edam/projects")), environment).Find("projects.root");

      Check("An ENVIRONMENT override wins over the deployment, and the row says so (no silent fallback)",
         overridden is { Source: SettingsSource.Environment } &&
         overridden.Value == "/srv/edam/projects",
         $"source={overridden?.Source} value='{overridden?.Value}'");

      // 5 — a stated-but-unusable value is REPORTED, never replaced
      var broken = User(root + "-e");
      broken.Overlay.Write(domainItem, "../not a domain");
      var invalid = new ProjectSettingsCatalog(broken).Find(domainItem);

      Check("A stated-but-unusable value is INVALID, keeps what was stated, and carries the reason",
         invalid is { State: SettingsValueState.Invalid } &&
         invalid.Value == "../not a domain" &&
         !String.IsNullOrWhiteSpace(invalid.Problem) &&
         invalid.Source == SettingsSource.User,
         $"state={invalid?.State} value='{invalid?.Value}' problem='{invalid?.Problem}'");

      // 6 — a binding NOBODY stated is the documented default, not "the installation's configuration"
      var defaultStorage = new ProjectSettingsCatalog(User(root + "-f")).Find("projects.storage");
      Check("A binding nothing stated is reported as the documented DEFAULT (not as deployment)",
         defaultStorage is { Source: SettingsSource.Default },
         $"source={defaultStorage?.Source} value='{defaultStorage?.Value}'");

      return checks;
   }

   // ---- ST-3: the one editor, and apply ------------------------------------------------------

   public static async Task<List<ProjectScenario.Check>> RunApplyAsync(String root)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(String name, Boolean passed, String detail) =>
         checks.Add(new ProjectScenario.Check(name, passed, detail));

      var applier = new RecordingApplier();
      var user = User(root);
      var service = new SettingsService(
         new ProjectSettingsCatalog(user), new ProjectSettingsEditor(user), applier);

      var domainItem = ConfigurableItems.DOMAIN_ID_KEY;

      // 1 — a stored value is visible at once (the catalog is a snapshot-free read model)
      var stored = await service.SetAsync(domainItem, "Contoso.Claims");
      var after = service.Catalog.Find(domainItem);
      Check("A stored value is IMMEDIATELY what the surface shows (no restart of the read model)",
         stored.Changed && stored.Problem is null &&
         after is { Value: "Contoso.Claims", Source: SettingsSource.User },
         $"changed={stored.Changed} value='{after?.Value}' source={after?.Source}");

      // 2 — validation happens BEFORE anything is written
      var before = user.Overlay.ReadAll().Count;
      var refused = await service.SetAsync(domainItem, "1bad domain");
      Check("An unusable answer is REFUSED and nothing is written (validated where it is entered)",
         !refused.Changed && refused.Problem is not null &&
         user.Overlay.ReadAll().Count == before &&
         service.Catalog.Find(domainItem)?.Value == "Contoso.Claims",
         $"changed={refused.Changed} problem='{refused.Problem}'");

      // 3 — an installation fact cannot be edited from this surface
      var fact = await service.SetAsync("projects.root", "/tmp/somewhere");
      Check("An installation fact is REFUSED from the settings surface, with the reason",
         !fact.Changed && fact.Problem is not null &&
         !String.IsNullOrWhiteSpace(fact.Problem),
         $"changed={fact.Changed} problem='{fact.Problem}'");

      // 4 — an action-backed value is refused once its action is done
      user.MarkStarterProjectOffered();
      var late = await service.SetAsync(
         ConfigurableItems.DEFAULT_PROJECT_NAME_KEY, "Edam.Other");
      Check("A completed ACTION-BACKED value is refused with its reason (nothing reads a late rename)",
         !late.Changed && late.Problem is not null,
         $"changed={late.Changed} problem='{late.Problem}'");

      // 5 — the apply hook runs for IMMEDIATE items only
      var nextUse = await service.SetAsync(
         ConfigurableItems.DEFAULT_TEMPLATE_KEY, "Templates/ToAssets.Args.json");
      var immediate = await service.SetAsync(
         ConfigurableItems.CATALOG_CONNECTION_KEY,
         "Server=.;Database=edam;Integrated Security=True");

      Check("A NEXT-USE change is not applied eagerly; an IMMEDIATE one IS (so 'live' is not a promise)",
         nextUse.Changed && immediate.Changed &&
         applier.Applied.Count == 1 &&
         applier.Applied[0] == ConfigurableItems.CATALOG_CONNECTION_KEY,
         $"applied: {String.Join(", ", applier.Applied)}");

      // 6 — a change that was stored but could not be applied is REPORTED, not called a success
      applier.Failure = "the storage could not be rebuilt";
      var notApplied = await service.SetAsync(
         ConfigurableItems.CATALOG_CONNECTION_KEY,
         "Server=.;Database=edam2;Integrated Security=True");

      Check("A save that did NOT take effect is reported as stored-but-not-applied (never hidden)",
         notApplied.Changed && notApplied.Problem is not null &&
         notApplied.Problem.Contains("did not take effect", StringComparison.OrdinalIgnoreCase),
         $"changed={notApplied.Changed} problem='{notApplied.Problem}'");

      applier.Failure = null;

      // 7 — resetting one answer restores the inherited value; resetting all spares the markers
      await service.SetAsync(domainItem, "Contoso.Claims");
      var reset = await service.ResetAsync(domainItem);
      Check("Resetting ONE answer gives the deployment/default value back",
         reset.Changed && user.Overlay.ReadAll().ContainsKey(domainItem) == false &&
         service.Catalog.Find(domainItem)?.Source == SettingsSource.Default,
         $"changed={reset.Changed} source={service.Catalog.Find(domainItem)?.Source}");

      var resetAll = await service.ResetAllAsync();
      Check("Resetting EVERYTHING forgets the answers but SPARES the once-per-installation marker",
         resetAll >= 1 &&
         user.Overlay.ReadAll().Count == 0 &&
         user.Installation.Read(ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY) is not null &&
         service.Catalog.Find(ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY)?.Source ==
            SettingsSource.Installation,
         $"forgot={resetAll} overlay={user.Overlay.ReadAll().Count} marker='" +
         $"{user.Installation.Read(ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY)}'");

      // 8 — resetting a value the person did not state is refused (it is not theirs to reset)
      var notMine = await service.ResetAsync(ConfigurableItems.CATALOG_CONNECTION_KEY);
      Check("Resetting a value that came from the DEPLOYMENT is refused (it is not the person's)",
         !notMine.Changed && notMine.Problem is not null,
         $"changed={notMine.Changed} problem='{notMine.Problem}'");

      return checks;
   }

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
      return order.Count;
   }
}
