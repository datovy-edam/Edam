using Edam.Data.Projects.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>CF-3 (ADR-0013)</b> checks the <b>answers</b>: the per-user app-data overlay file, the packaged
/// marker that must survive a reset, validation at the point of entry — and the loop that matters:
/// <b>ask once, remember, never ask again</b>, i.e. answering an item makes it stop being asked.
/// </summary>
public static class StateScenario
{
   private static IConfiguration Config(params (string Key, string Value)[] values)
      => new ConfigurationBuilder()
         .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
         .Build();

   public static List<ProjectScenario.Check> Run(string root)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      Directory.CreateDirectory(root);

      var nameItem = ConfigurableItems.Find(ConfigurableItems.DEFAULT_PROJECT_NAME_KEY)!;
      var markerItem = ConfigurableItems.Find(ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY)!;

      // a root that does NOT exist yet: the first write must create it
      var stateRoot = Path.Combine(root, "overlay");
      var overlay = new FileStateStore(stateRoot);
      var installation = new InMemoryStateStore("(packaged)");
      var user = new UserConfiguration(Config(), overlay, installation);

      Check("The overlay reports where it lives (so 'where did this come from?' is answerable)",
         overlay.Location == Path.Combine(stateRoot, "Edam.UserState.json") &&
         !File.Exists(overlay.Location),
         overlay.Location);

      Check("Before any answer, the starter-project name is the ONE thing to ask about",
         user.ToAsk().Count == 1 && user.ToAsk()[0].Item.Id == nameItem.Id,
         string.Join(", ", user.ToAsk().Select(a => a.Item.Id)));

      // ---- answer it ---------------------------------------------------------------------------
      var refused = user.Answer(nameItem, ConfigurableItems.DEFAULT_PROJECT_NAME);

      Check("Answering stores the value — creating the app-data folder and file on the way",
         refused is null && File.Exists(overlay.Location) &&
         overlay.Read(nameItem.Key) == ConfigurableItems.DEFAULT_PROJECT_NAME,
         $"refused='{refused}' file={File.Exists(overlay.Location)} value='{overlay.Read(nameItem.Key)}'");

      Check("The ANSWER alone does NOT stop the asking — the ACTION does (the project must exist first)",
         user.ToAsk().Count == 1 &&
         user.ToAsk()[0].Current == ConfigurableItems.DEFAULT_PROJECT_NAME,
         user.ToAsk().Count == 0
            ? "asked nothing (WRONG: the action was never completed)"
            : "still asked, pre-filled '" + user.ToAsk()[0].Current + "'");

      user.MarkStarterProjectOffered();

      Check("Once the ACTION is completed (the marker), the item stops being asked",
         user.ToAsk().Count == 0,
         string.Join(", ", user.ToAsk().Select(a => a.Item.Id)));

      Check("The answer is configuration from then on (the overlay layers onto the base)",
         user.Effective[nameItem.Key] == ConfigurableItems.DEFAULT_PROJECT_NAME &&
         user.StateOf(nameItem).State == ConfigurationState.Set,
         $"{user.StateOf(nameItem).State} '{user.Effective[nameItem.Key]}'");

      // ---- an unusable answer is refused, never stored ------------------------------------------
      var problem = user.Answer(nameItem, "../Templates");

      Check("An answer the validator rejects is REFUSED and nothing is written",
         !string.IsNullOrWhiteSpace(problem) &&
         overlay.Read(nameItem.Key) == ConfigurableItems.DEFAULT_PROJECT_NAME,
         $"{problem} (stored='{overlay.Read(nameItem.Key)}')");

      // ---- reset one, then all -----------------------------------------------------------------
      user.Reset(nameItem.Key);

      Check("Reset(key) forgets exactly that answer, so the item is asked about again",
         user.ToAsk().Count == 1 && overlay.Read(nameItem.Key) is null,
         string.Join(", ", user.ToAsk().Select(a => a.Item.Id)));

      user.Answer(nameItem, ConfigurableItems.DEFAULT_PROJECT_NAME);
      user.MarkStarterProjectOffered();

      Check("The marker is written to the PACKAGED store (not the resettable overlay)",
         installation.Read(markerItem.Key) == "true" && overlay.Read(markerItem.Key) is null,
         $"packaged='{installation.Read(markerItem.Key)}' overlay='{overlay.Read(markerItem.Key)}'");

      user.Reset();

      Check("Reset() forgets the user's answers but NOT the marker (a deleted project stays deleted)",
         overlay.Read(nameItem.Key) is null &&
         installation.Read(markerItem.Key) == "true",
         $"answer='{overlay.Read(nameItem.Key)}' marker='{installation.Read(markerItem.Key)}'");

      Check("A silent-default item is never in the batch — before or after a reset",
         user.ToAsk().All(a => a.Item.Id != markerItem.Id),
         string.Join(", ", user.ToAsk().Select(a => a.Item.Id)));

      // ---- durability and robustness -----------------------------------------------------------
      user.Answer(nameItem, "Edam.Sample.v1");
      var reopened = new FileStateStore(stateRoot);

      Check("A new store over the same folder sees the answers (they are durable, not in-process)",
         reopened.Read(nameItem.Key) == "Edam.Sample.v1",
         reopened.Read(nameItem.Key) ?? "<none>");

      File.WriteAllText(overlay.Location, "{ this is not json");

      Check("A CORRUPT state file is treated as 'nothing stated' — never fatal",
         reopened.ReadAll().Count == 0,
         $"{reopened.ReadAll().Count} entr(y/ies)");

      user.Answer(nameItem, ConfigurableItems.DEFAULT_PROJECT_NAME);
      var text = File.ReadAllText(overlay.Location);
      var parses = true;
      try { using var _ = JsonDocument.Parse(text); } catch (Exception) { parses = false; }

      Check("The overlay is plain, readable JSON a user can inspect or delete",
         parses && text.Contains(nameItem.Key, StringComparison.Ordinal),
         parses ? "valid JSON, key present" : "not valid JSON");

      return checks;
   }
}
