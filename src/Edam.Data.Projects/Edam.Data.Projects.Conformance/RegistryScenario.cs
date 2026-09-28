using Edam.Data.Projects.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>CF-2 (ADR-0013)</b> checks the <b>registry of configurable items</b>: one definition per askable
/// item, each saying what it is and what it affects; policies that decide whether it is ever asked; the
/// batch (<see cref="ConfigurableItems.ToAsk"/>) built from the CF-1 states; and one definition of
/// "invalid" — the registry's validator — so a stated-but-unusable value is <b>reported, never
/// replaced</b>.
/// </summary>
public static class RegistryScenario
{
   private static IConfiguration Config(params (string Key, string Value)[] values)
      => new ConfigurationBuilder()
         .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
         .Build();

   public static List<ProjectScenario.Check> Run()
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      var registry = ConfigurableItems.All;
      var nameItem = ConfigurableItems.Find(ConfigurableItems.DEFAULT_PROJECT_NAME_KEY);
      var markerItem = ConfigurableItems.Find(ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY);

      // ---- the registry itself -----------------------------------------------------------------
      Check("The registry declares the first entries with unique ids",
         registry.Count >= 2 &&
         registry.Select(i => i.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == registry.Count &&
         nameItem is not null && markerItem is not null,
         string.Join(", ", registry.Select(i => i.Id)));

      Check("Every item says WHAT IT IS and WHAT IT AFFECTS (so a prompt can explain itself)",
         registry.All(i => !string.IsNullOrWhiteSpace(i.WhatItIs) &&
                           !string.IsNullOrWhiteSpace(i.WhatItAffects) &&
                           !string.IsNullOrWhiteSpace(i.Title)),
         $"{registry.Count} item(s) described");

      Check("The starter-project name is ACTION-BACKED: required until the project exists",
         nameItem is not null &&
         nameItem.Policy == ConfigurationPolicy.Required &&
         nameItem.IsActionBacked &&
         nameItem.CompletedByKey == ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY &&
         nameItem.Default == ConfigurableItems.DEFAULT_PROJECT_NAME &&
         nameItem.Storage == ConfigurationStorage.AppDataOverlay,
         $"{nameItem?.Policy} completedBy='{nameItem?.CompletedByKey}' default='{nameItem?.Default}'");

      Check("The once-per-installation marker is NEVER asked (silent default, packaged settings)",
         markerItem is not null && !markerItem.IsAskable &&
         markerItem.Storage == ConfigurationStorage.PackagedSettings,
         $"{markerItem?.Policy} askable={markerItem?.IsAskable}");

      // ---- the batch ---------------------------------------------------------------------------
      var unset = ConfigurableItems.ToAsk(Config());
      Check("Nothing stated: the batch asks for the name — and only for it (the marker is silent)",
         unset.Count == 1 &&
         unset[0].Item.Id == ConfigurableItems.DEFAULT_PROJECT_NAME_KEY &&
         unset[0].State == ConfigurationState.Unset &&
         unset[0].Problem is null,
         string.Join(", ", unset.Select(a => a.Item.Id + "=" + a.State)));

      var stated = ConfigurableItems.ToAsk(Config(
         (ConfigurableItems.DEFAULT_PROJECT_NAME_KEY, "Edam.Sample"),
         (ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY, "true")));
      Check("A stated value asks NOTHING once its ACTION is completed too (the marker)",
         stated.Count == 0,
         stated.Count == 0 ? "no asks" : string.Join(", ", stated.Select(a => a.Item.Id)));

      var answeredNotDone = ConfigurableItems.ToAsk(Config(
         (ConfigurableItems.DEFAULT_PROJECT_NAME_KEY, "Edam.Sample")));
      Check("An ANSWER alone does NOT end the asking: with the action pending it asks again, PRE-FILLED",
         answeredNotDone.Count == 1 &&
         answeredNotDone[0].Item.Id == ConfigurableItems.DEFAULT_PROJECT_NAME_KEY &&
         answeredNotDone[0].Current == "Edam.Sample" &&
         !string.IsNullOrWhiteSpace(answeredNotDone[0].Reason),
         answeredNotDone.Count == 0
            ? "asked nothing (WRONG: the action was never completed)"
            : $"asks again, pre-filled '{answeredNotDone[0].Current}': {answeredNotDone[0].Reason}");

      var emptyName = ConfigurableItems.ToAsk(Config(
         (ConfigurableItems.DEFAULT_PROJECT_NAME_KEY, ""),
         (ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY, "true")));
      Check("A name stated as EMPTY is not 'unset' — it is stated-but-unusable, so it asks to FIX it",
         emptyName.Count == 1 &&
         emptyName[0].State == ConfigurationState.Invalid &&
         !string.IsNullOrWhiteSpace(emptyName[0].Problem),
         $"{emptyName.FirstOrDefault()?.State}: {emptyName.FirstOrDefault()?.Problem}");

      var badName = ConfigurableItems.ToAsk(Config(
         (ConfigurableItems.DEFAULT_PROJECT_NAME_KEY, "../Templates")));
      Check("An unusable name is reported for repair, and its stated value is NEVER replaced",
         badName.Count == 1 &&
         badName[0].State == ConfigurationState.Invalid &&
         badName[0].Current == "../Templates",
         $"{badName.FirstOrDefault()?.State} current='{badName.FirstOrDefault()?.Current}'");

      var noMarkerAsk = ConfigurableItems.ToAsk(Config(
         (ConfigurableItems.DEFAULT_PROJECT_NAME_KEY, "Edam.Sample"),
         (ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY, "")));
      Check("A silent-default item that was stated (even empty) is never asked",
         noMarkerAsk.Count == 0,
         noMarkerAsk.Count == 0 ? "no asks" : string.Join(", ", noMarkerAsk.Select(a => a.Item.Id)));

      // ---- one definition of "invalid": the name rules (ADR-0013 decision 7) -------------------
      string[] accepted = { "Edam.Sample", "Datovy.HC.CD", "CalTrans_OLP", "Edam.Sample.v1", "abc123" };
      var rejectedAccepted = accepted
         .Where(n => ConfigurableItems.ValidateProjectName(n) is not null)
         .ToList();
      Check("The name rules ACCEPT the conventional, dotted, org-first names",
         rejectedAccepted.Count == 0,
         rejectedAccepted.Count == 0 ? string.Join(", ", accepted) : string.Join(", ", rejectedAccepted));

      string[] rejected = { "..", "../Templates", "a/b", "a\\b", "CON", "Projects", "Templates",
                            "Edam.", ".Edam", "two  spaces", "" , "  padded" };
      var wronglyAccepted = rejected
         .Where(n => ConfigurableItems.ValidateProjectName(n) is null)
         .ToList();
      Check("...and REJECT traversal, separators, device names, reserved families and padding",
         wronglyAccepted.Count == 0,
         wronglyAccepted.Count == 0
            ? string.Join(", ", rejected.Select(n => $"'{n}'"))
            : "accepted: " + string.Join(", ", wronglyAccepted.Select(n => $"'{n}'")));

      Check("A name longer than the cap is rejected (it becomes a folder AND an artifact name)",
         ConfigurableItems.ValidateProjectName(new string('a', ConfigurableItems.PROJECT_NAME_MAX_LENGTH))
            is null &&
         ConfigurableItems.ValidateProjectName(new string('a', ConfigurableItems.PROJECT_NAME_MAX_LENGTH + 1))
            is not null,
         $"cap={ConfigurableItems.PROJECT_NAME_MAX_LENGTH}");

      return checks;
   }
}
