using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>LM-3 (ADR-0011)</b> checks for the settings reader: the ADR-0011 shape
/// (<c>Edam:Projects:Root</c> + declared collections), the <b>compatibility reader</b> that translates
/// the legacy keys, the <b>equivalence guarantee</b> (legacy and new spellings of the same
/// configuration resolve to the same root), the reporting of keys that are recognized but not yet
/// acted on, and the fail-fast behaviour preserved by the composition root.
/// </summary>
public static class SettingsScenario
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

      // ---- the ADR-0011 shape -----------------------------------------------------------------
      var modern = ProjectSettings.Read(Config(
         (ProjectSettings.ROOT_KEY, "/data/edam"),
         (ProjectSettings.DEFAULT_COLLECTION_KEY, "edam.studio"),
         (ProjectSettings.COLLECTIONS_SECTION + ":other", "/data/other"),
         (ProjectSettings.WORKING_ROOT_KEY, "/tmp/work")));

      Check("New shape: the root is the default collection's location",
         modern.Root == "/data/edam" && modern.HasRoot, modern.Root ?? "<none>");

      Check("New shape: the default collection id and the working root are read",
         modern.DefaultCollectionId == "edam.studio" && modern.WorkingRoot == "/tmp/work",
         $"{modern.DefaultCollectionId} / {modern.WorkingRoot}");

      Check("New shape: additional collections are declared once",
         modern.Collections.Count == 1 && modern.Collections[0].CollectionId == "other" &&
         modern.Collections[0].Uri == "/data/other",
         string.Join(", ", modern.Collections.Select(c => c.CollectionId + "=" + c.Uri)));

      Check("New shape: no legacy key is reported",
         modern.LegacyKeysUsed.Count == 0, modern.LegacyKeysUsed.Count.ToString());

      // ---- the compatibility reader -----------------------------------------------------------
      var legacy = ProjectSettings.Read(Config(
         (ProjectSettings.LEGACY_CONSOLE_PATH_KEY, "/data/edam"),
         (ProjectSettings.DEFAULT_COLLECTION_KEY, "edam.studio")));

      Check("Legacy shape: AppSettings:AssetConsolePath is translated to the root",
         legacy.Root == "/data/edam", legacy.Root ?? "<none>");

      Check("Legacy shape: the translated key is REPORTED (so a host can warn)",
         legacy.LegacyKeysUsed.Contains(ProjectSettings.LEGACY_CONSOLE_PATH_KEY),
         string.Join(", ", legacy.LegacyKeysUsed));

      var settingsFile = ProjectSettings.Read(Config(
         (ProjectSettings.LEGACY_SETTINGS_CONSOLE_PATH_KEY, "/data/edam")));

      Check("Legacy shape: Edam.Settings.json's ConsolePath is translated as the fallback",
         settingsFile.Root == "/data/edam" &&
         settingsFile.LegacyKeysUsed.Contains(ProjectSettings.LEGACY_SETTINGS_CONSOLE_PATH_KEY),
         settingsFile.Root ?? "<none>");

      // ---- the equivalence guarantee (the "single source of truth") ---------------------------
      Check("Equivalence: the legacy spelling and the ADR-0011 spelling resolve to the SAME root",
         legacy.Root == modern.Root,
         $"{legacy.Root} == {modern.Root}");

      var precedence = ProjectSettings.Read(Config(
         (ProjectSettings.ROOT_KEY, "/data/new"),
         (ProjectSettings.LEGACY_CONSOLE_PATH_KEY, "/data/old")));

      Check("Precedence: Edam:Projects:Root wins over the legacy key, which is then not reported",
         precedence.Root == "/data/new" && precedence.LegacyKeysUsed.Count == 0,
         precedence.Root ?? "<none>");

      // ---- nothing is silently dropped --------------------------------------------------------
      var pending = ProjectSettings.Read(Config(
         (ProjectSettings.ROOT_KEY, "/data/edam"),
         (ProjectSettings.LEGACY_PROJECTS_PATH_KEY, "/Projects/"),
         (ProjectSettings.LEGACY_DATA_PATH_KEY, "Edam.Studio/Edam.App.Data/"),
         (ProjectSettings.LEGACY_TEXT_MAP_FOLDER_KEY, "../../TextMaps/"),
         (ProjectSettings.LEGACY_CONNECTION_STRING_KEY, "Data Source=.;Initial Catalog=x")));

      Check("Legacy keys that land in a later step are RECOGNIZED and reported, not dropped",
         pending.NotYetTranslated.Count == 4 &&
         pending.NotYetTranslated.Contains(ProjectSettings.LEGACY_PROJECTS_PATH_KEY) &&
         pending.NotYetTranslated.Contains(ProjectSettings.LEGACY_DATA_PATH_KEY) &&
         pending.NotYetTranslated.Contains(ProjectSettings.LEGACY_TEXT_MAP_FOLDER_KEY) &&
         pending.NotYetTranslated.Contains(ProjectSettings.LEGACY_CONNECTION_STRING_KEY),
         string.Join(", ", pending.NotYetTranslated));

      Check("A configuration with no location resolves to no root (the reader never throws)",
         !ProjectSettings.Read(Config()).HasRoot, "no root");

      // ---- the composition root keeps its fail-fast behaviour --------------------------------
      var threw = false;
      try
      {
         new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddProjectServices(new Dictionary<string, string>
            {
               [ProjectSettings.TARGET_KEY] = "filesystem",
            });
      }
      catch (InvalidOperationException)
      {
         threw = true;
      }
      Check("Composition root still fails fast when no root is configured", threw,
         threw ? "InvalidOperationException" : "no exception");

      return checks;
   }
}
