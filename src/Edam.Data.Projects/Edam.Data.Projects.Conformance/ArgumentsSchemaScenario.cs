using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Edam.Data.AssetConsole;
using Edam.Data.Projects.DependencyInjection;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>Arguments-file validation (the Projects specification §2.3)</b>: the schema that ships with the application
/// is <b>found</b>, it accepts every arguments document this repository ships and the specification's own
/// example, it <b>refuses</b> the mistakes a person actually makes, it stays <b>lenient</b> about what the
/// specification leaves TBD, and its procedure enumeration is <b>equal to the code's</b> — so the published
/// contract cannot drift away from the runner in silence.
/// <para>
/// Everything here is offline: the schema is read from the repository's seed (the same file that ships), and the
/// validator is pure.
/// </para>
/// </summary>
public static class ArgumentsSchemaScenario
{
   /// <summary>Where the schema ships, relative to the repository's seed folder.</summary>
   public const string SchemaRelativePath = "Templates/Arguments.schema.json";

   public static List<ProjectScenario.Check> Run()
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      // ---- 1. the schema is FOUND, at the address the configuration states ----------------------
      var seed = FindSeedRoot();
      var schemaPath = seed is null ? null : Path.Combine(
         seed!, SchemaRelativePath.Replace('/', Path.DirectorySeparatorChar));

      var schemaJson = schemaPath is not null && File.Exists(schemaPath)
         ? File.ReadAllText(schemaPath)
         : null;

      Check("The arguments schema ships where the configuration points (app:templates/…) and parses",
         schemaJson is not null &&
         schemaJson!.Contains("json-schema.org/draft-07", StringComparison.OrdinalIgnoreCase),
         schemaPath ?? "the repository seed was not found from here");

      var procedures = Enum.GetNames(typeof(AssetConsoleProcedure));
      var validator = new ArgumentsValidator(schemaJson, strict: false, knownProcedures: procedures);

      Check("A schema was applied (so the shape rules ran, not only the semantic ones)",
         validator.HasSchema,
         "draft-07 schema loaded: " + validator.HasSchema);

      // ---- 2. drift control: the schema's enumeration IS the code's ------------------------------
      var schemaProcedures = validator.SchemaProcedures.ToList();
      var missing = procedures.Except(schemaProcedures, StringComparer.OrdinalIgnoreCase).ToList();
      var extra = schemaProcedures.Except(procedures, StringComparer.OrdinalIgnoreCase).ToList();

      Check("The schema's ProcedureName enumeration EQUALS the code's AssetConsoleProcedure values",
         schemaProcedures.Count > 0 && missing.Count == 0 && extra.Count == 0,
         $"{schemaProcedures.Count} in the schema, {procedures.Length} in code" +
         (missing.Count > 0 ? "; missing: " + string.Join(",", missing) : String.Empty) +
         (extra.Count > 0 ? "; unknown: " + string.Join(",", extra) : String.Empty));

      // ---- 3. every arguments document THIS REPOSITORY SHIPS must validate ------------------------
      var shipped = new[] { "Templates/ToAssets.Args.json", "Templates/ToOpenXml.Args.json" };
      foreach (var relative in shipped)
      {
         var path = seed is null
            ? null
            : Path.Combine(seed!, relative.Replace('/', Path.DirectorySeparatorChar));

         if (path is null || !File.Exists(path))
         {
            Check("The shipped document '" + relative + "' validates", false,
               "not found under the repository seed");
            continue;
         }

         var result = validator.Validate(File.ReadAllText(path));
         Check("The shipped document '" + relative + "' validates (warnings tolerated)",
            result.Valid,
            result.Issues.Count == 0
               ? "no issues"
               : string.Join(" | ", result.Issues.Select(i => i.Severity + " " + i.Code)));
      }

      // ---- 4. the specification's own example (§2.3) validates ------------------------------------
      var example = validator.Validate(ProjectScenario.ArgsJson, "Datovy.HC.CD");
      Check("The specification's own example arguments document validates",
         example.Valid,
         example.Issues.Count == 0
            ? "no issues"
            : string.Join(" | ", example.Issues.Select(i => i.Severity + " " + i.Code)));

      // ---- 5. the mistakes a person actually makes are caught -------------------------------------
      var unknownProcedure = Replace(ProjectScenario.ArgsJson,
         "\"ProcedureName\": \"DdlImportToAssets\"", "\"ProcedureName\": \"NotAProcedure\"");
      var unknownResult = validator.Validate(unknownProcedure, "Datovy.HC.CD");
      Check("A procedure the runner does not know is REFUSED, by name",
         !unknownResult.Valid &&
         unknownResult.Issues.Any(i => i.Code == "procedure-unknown" && i.Severity == ArgumentsIssueSeverity.Error),
         string.Join(" | ", unknownResult.Issues.Select(i => i.Code)));

      var absolute = Replace(ProjectScenario.ArgsJson,
         "\"./Archive/datovy.hc.cd.schema.xlsx\"", "\"C:\\\\Archive\\\\x.xlsx\"");
      var absoluteResult = validator.Validate(absolute, "Datovy.HC.CD");
      Check("An ABSOLUTE path in UriList is refused (a reference must stay inside the project)",
         !absoluteResult.Valid && absoluteResult.Issues.Any(i => i.Code == "path-absolute"),
         string.Join(" | ", absoluteResult.Issues.Select(i => i.Code)));

      var climbing = Replace(ProjectScenario.ArgsJson,
         "\"./Archive/datovy.hc.cd.schema.xlsx\"", "\"../Files/x.xlsx\"");
      var climbingResult = validator.Validate(climbing, "Datovy.HC.CD");
      Check("A reference that climbs OUT of the project with '..' is reported — and does NOT invalidate the file",
         climbingResult.Valid && climbingResult.Issues.Any(i => i.Code == "path-leaves-project"),
         string.Join(" | ", climbingResult.Issues.Select(i => i.Code)));

      var wrongName = validator.Validate(ProjectScenario.ArgsJson, "Another.Project");
      Check("A Project.Name that differs from the project FOLDER is refused (§2.3 step 4)",
         !wrongName.Valid && wrongName.Issues.Any(i => i.Code == "project-name-mismatch"),
         string.Join(" | ", wrongName.Issues.Select(i => i.Code)));

      var broken = "{ \"Process\": { \"ProcedureName\": \"XsdToAssets\" } }";
      var brokenResult = validator.Validate(broken, "Any.Project");
      Check("A document missing the required sections is refused, and the missing domain is reported",
         !brokenResult.Valid && brokenResult.Issues.Any(i => i.Code == "shape") &&
         brokenResult.Issues.Any(i => i.Code == "domain-missing"),
         string.Join(" | ", brokenResult.Issues.Select(i => i.Code)));

      // ---- 6. leniency, and the strict switch that a CI gate can turn on -------------------------
      var extended = Replace(ProjectScenario.ArgsJson,
         "\"TextMapFilePath\"", "\"SomethingNew\": 42,\n   \"TextMapFilePath\"");

      var lenient = validator.Validate(extended, "Datovy.HC.CD");
      var strict = new ArgumentsValidator(schemaJson, strict: true, knownProcedures: procedures)
         .Validate(extended, "Datovy.HC.CD");

      Check("An unknown property is TOLERATED by default (reported, not fatal) and REFUSED in strict mode",
         lenient.Valid &&
         lenient.Issues.Any(i => i.Code == "unknown-property" && i.Severity == ArgumentsIssueSeverity.Warning) &&
         !strict.Valid &&
         strict.Issues.Any(i => i.Code == "unknown-property" && i.Severity == ArgumentsIssueSeverity.Error),
         $"lenient valid={lenient.Valid}, strict valid={strict.Valid}");

      return checks;
   }

   private static string Replace(string text, string find, string replacement)
      => text.Replace(find, replacement, StringComparison.Ordinal);

   /// <summary>
   /// The repository's seed (<c>app-data/Edam.App.Data</c>) — found by walking up from the build output, exactly
   /// as the DP-5 runs find it, so the checks read the file that SHIPS rather than a copy.
   /// </summary>
   private static string? FindSeedRoot()
   {
      var directory = new DirectoryInfo(AppContext.BaseDirectory);

      while (directory is not null)
      {
         var candidate = Path.Combine(directory.FullName, "app-data", "Edam.App.Data");
         if (File.Exists(Path.Combine(candidate, "Templates", "ToAssets.Args.json")))
         {
            return candidate;
         }

         directory = directory.Parent;
      }

      return null;
   }
}
