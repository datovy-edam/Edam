using Edam.Data.Projects.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>CF-1 (ADR-0013)</b> checks that configuration has <b>three distinguishable states</b> —
/// <c>Unset</c>, <c>Set</c> (possibly to an empty value) and <c>Invalid</c>.
/// <para>
/// The point is the distinction: <b>"nothing stated this"</b> must never be confused with
/// <b>"stated as empty"</b> (the <c>AppSettings:AssetConsolePath</c> defect, where a deliberately empty
/// value was treated as missing and startup threw), and an unusable value must be <b>reported</b>
/// rather than silently replaced. The reader never throws for a bad value — it states it, and names
/// <b>what</b> stated it, so "where did this value come from?" is answerable.
/// </para>
/// </summary>
public static class ConfigurationScenario
{
   private static IConfiguration Config(params (string Key, string Value)[] values)
      => new ConfigurationBuilder()
         .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
         .Build();

   /// <summary>No ambient environment: these checks never depend on the machine's variables.</summary>
   private static readonly Dictionary<string, string?> NoEnvironment = new();

   private static ProjectSettingsInfo Read(IConfiguration config)
      => ProjectSettings.Read(config, NoEnvironment);

   public static List<ProjectScenario.Check> Run()
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      // ---- unset is not empty ----------------------------------------------------------------
      var unset = Read(Config(
         (ProjectSettings.DEFAULT_COLLECTION_KEY, "edam.studio")));

      Check("An absent key is UNSET (nothing has stated it)",
         unset.StateOf(ProjectSettings.ROOT_KEY) == ConfigurationState.Unset &&
         unset.Item(ProjectSettings.ROOT_KEY)?.IsConfigured == false,
         unset.StateOf(ProjectSettings.ROOT_KEY).ToString());

      var empty = Read(Config(
         (ProjectSettings.ROOT_KEY, ""),
         (ProjectSettings.DEFAULT_COLLECTION_KEY, "edam.studio")));

      Check("A key stated as EMPTY is SET, not unset — so it must not trigger a prompt",
         empty.StateOf(ProjectSettings.ROOT_KEY) == ConfigurationState.Set &&
         empty.Item(ProjectSettings.ROOT_KEY)?.IsConfigured == true,
         $"{empty.StateOf(ProjectSettings.ROOT_KEY)} value='{empty.Item(ProjectSettings.ROOT_KEY)?.Value}'");

      Check("...and the empty value names its source (nothing is invented in its place)",
         empty.Item(ProjectSettings.ROOT_KEY)?.Source == ProjectSettings.ROOT_KEY,
         empty.Item(ProjectSettings.ROOT_KEY)?.Source ?? "<none>");

      // ---- set, with value and source --------------------------------------------------------
      var set = Read(Config((ProjectSettings.ROOT_KEY, "/data/edam")));

      Check("A stated value is SET and carries its value",
         set.StateOf(ProjectSettings.ROOT_KEY) == ConfigurationState.Set &&
         set.Item(ProjectSettings.ROOT_KEY)?.Value == "/data/edam",
         set.Item(ProjectSettings.ROOT_KEY)?.Value ?? "<none>");

      var legacy = Read(Config((ProjectSettings.LEGACY_CONSOLE_PATH_KEY, "/legacy/edam")));

      Check("A TRANSLATED legacy value is SET and names the legacy key as its source",
         legacy.StateOf(ProjectSettings.ROOT_KEY) == ConfigurationState.Set &&
         legacy.Item(ProjectSettings.ROOT_KEY)?.Source == ProjectSettings.LEGACY_CONSOLE_PATH_KEY,
         legacy.Item(ProjectSettings.ROOT_KEY)?.Source ?? "<none>");

      var environment = ProjectSettings.Read(
         Config((ProjectSettings.ROOT_KEY, "/from/config")),
         new Dictionary<string, string?> { [ProjectSettings.ENV_ROOT] = "/from/environment" });

      Check("An environment override WINS and names the variable as its source",
         environment.Item(ProjectSettings.ROOT_KEY)?.Source == ProjectSettings.ENV_ROOT &&
         environment.Item(ProjectSettings.ROOT_KEY)?.Value == "/from/environment",
         $"{environment.Item(ProjectSettings.ROOT_KEY)?.Source}={environment.Item(ProjectSettings.ROOT_KEY)?.Value}");

      // ---- invalid: reported, never thrown ---------------------------------------------------
      var badFamily = Read(Config(
         (ProjectSettings.ROOT_KEY, "/data/edam"),
         (ProjectSettings.TARGET_KEY, "nosuchfamily")));

      Check("An unknown provider family is INVALID (the reader states it instead of throwing)",
         badFamily.StateOf(ProjectSettings.TARGET_KEY) == ConfigurationState.Invalid &&
         badFamily.Problems.Any(p => p.Key == ProjectSettings.TARGET_KEY),
         badFamily.Item(ProjectSettings.TARGET_KEY)?.Problem ?? "<none>");

      Check("...and the problem distinguishes a provider family from a storage kind",
         (badFamily.Item(ProjectSettings.TARGET_KEY)?.Problem ?? string.Empty)
            .Contains("provider family") &&
         (badFamily.Item(ProjectSettings.TARGET_KEY)?.Problem ?? string.Empty)
            .Contains("storage kind"),
         "problem mentions both vocabularies");

      var badBinding = Read(Config(
         (ProjectSettings.ROOT_KEY, "/data/edam"),
         (ProjectSettings.DEFAULT_COLLECTION_KEY, "shared"),
         (ProjectSettings.BINDINGS_SECTION + ":shared:Target", "postgres")));

      Check("A 'postgres' binding with no credential NAME is INVALID",
         badBinding.StateOf(ProjectSettings.BINDINGS_SECTION + ":shared") == ConfigurationState.Invalid,
         badBinding.Item(ProjectSettings.BINDINGS_SECTION + ":shared")?.Problem ?? "<none>");

      Check("Only the unusable items are listed as problems",
         badBinding.Problems.Count == 1 &&
         badBinding.Problems[0].Key == ProjectSettings.BINDINGS_SECTION + ":shared",
         string.Join(", ", badBinding.Problems.Select(p => p.Key)));

      // ---- pending is not invalid ------------------------------------------------------------
      var pending = Read(Config((ProjectSettings.DEFAULT_COLLECTION_KEY, "edam.studio")));

      Check("A local binding with no location yet is UNSET (pending), not invalid — the first-run case",
         pending.StateOf(ProjectSettings.BINDINGS_SECTION + ":edam.studio") == ConfigurationState.Unset &&
         pending.Problems.Count == 0,
         $"{pending.StateOf(ProjectSettings.BINDINGS_SECTION + ":edam.studio")}, " +
         $"{pending.Problems.Count} problem(s)");

      var good = Read(Config(
         (ProjectSettings.ROOT_KEY, "/data/edam"),
         (ProjectSettings.DEFAULT_COLLECTION_KEY, "shared"),
         (ProjectSettings.BINDINGS_SECTION + ":shared:Target", "postgres"),
         (ProjectSettings.BINDINGS_SECTION + ":shared:Credential", "ConnectionStrings:catalog")));

      Check("A usable binding is SET, has no problems, and carries only its non-secret address",
         good.StateOf(ProjectSettings.BINDINGS_SECTION + ":shared") == ConfigurationState.Set &&
         good.Problems.Count == 0 &&
         good.Item(ProjectSettings.BINDINGS_SECTION + ":shared")?.Value is null,
         $"problems={good.Problems.Count} value='{good.Item(ProjectSettings.BINDINGS_SECTION + ":shared")?.Value}'");

      // ---- the whole surface is inspectable --------------------------------------------------
      Check("Every item the reader looked at is listed with a state (nothing is implicit)",
         set.Items.Count >= 4 &&
         set.Items.All(i => !string.IsNullOrWhiteSpace(i.Key)) &&
         set.Items.Any(i => i.Key == ProjectSettings.TARGET_KEY) &&
         set.Items.Any(i => i.Key == ProjectSettings.WORKING_ROOT_KEY),
         string.Join(", ", set.Items.Select(i => i.Key + "=" + i.State)));

      return checks;
   }
}
