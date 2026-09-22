using Edam.Data.AssetConsole;
using Edam.Data.Projects.Assets;
using Edam.Data.Projects.Contracts;
using Edam.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// PE-5b checks: the asset-console adapter (a) loads the arguments the runner materialized, (b) runs
/// the console with the <b>current directory contained</b> to the working folder and <b>restored</b>
/// afterwards, and (c) maps the console's results onto a run result.
/// <para>
/// The console invocation is substituted here, so the adapter's own contract is verified without
/// depending on the asset machinery. <see cref="ProbeRealConsoleAsync"/> additionally attempts a
/// <b>real</b> invocation and reports the outcome — informationally, never as a pass/fail.
/// </para>
/// </summary>
public static class AssetConsoleAdapterScenario
{
   private const string ArgsJson = """
   {
     "Project": { "Name": "Datovy.HC.CD", "VersionId": "v1r0" },
     "Process": { "Name": "Datovy.HC.CD.ToAssets", "ProcedureName": "DdlToAssets" },
     "OutputFile": { "Extension": "jsd", "Name": "out", "Path": "./Documents", "Full": "./Documents/out.jsd" },
     "UriList": [ "./Archive/sample.ddl" ]
   }
   """;

   public static async Task<List<ProjectScenario.Check>> RunAsync(
      string workRoot, CancellationToken ct = default)
   {
      var checks = new List<ProjectScenario.Check>();
      void Add(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      var working = Path.Combine(workRoot, "adapter-working");
      Directory.CreateDirectory(Path.Combine(working, "Arguments"));
      Directory.CreateDirectory(Path.Combine(working, "Archive"));
      // the arguments sit at their project-relative path — exactly how the runner materializes them
      File.WriteAllText(Path.Combine(working, "Arguments", "Run.Args.json"), ArgsJson);
      File.WriteAllText(Path.Combine(working, "Archive", "sample.ddl"), "CREATE TABLE dbo.Sample (Id INT);");

      var context = new ProjectProcessContext(
         new ProjectInfo("1", "Datovy.HC.CD", "v1r0", "collection", ProjectPath.Parse("/collection/Projects/Datovy.HC.CD")),
         ProjectPath.Parse("/Arguments/Run.Args.json"),
         working,
         new[] { ProjectPath.Parse("/Archive/sample.ddl") });

      var before = Directory.GetCurrentDirectory();
      string? during = null;
      AssetConsoleArgumentsInfo? loaded = null;

      var process = new AssetConsoleProjectProcess((arguments, _) =>
      {
         during = Directory.GetCurrentDirectory();
         loaded = arguments;

         var log = new ResultLog();
         log.Succeeded();
         return log;
      });

      var result = await process.RunAsync(context, ct);

      Add("Adapter: console executed and succeeded", result.Success, result.Message ?? "ok");
      Add("Adapter: arguments loaded from the working folder",
         loaded is not null && loaded.Project?.Name == "Datovy.HC.CD",
         loaded?.Project?.Name ?? "<none>");
      Add("Adapter: current directory contained to the working folder",
         string.Equals(during, working, StringComparison.OrdinalIgnoreCase), during ?? "<none>");
      Add("Adapter: current directory restored afterwards",
         string.Equals(Directory.GetCurrentDirectory(), before, StringComparison.OrdinalIgnoreCase),
         Directory.GetCurrentDirectory());

      var failing = new AssetConsoleProjectProcess((_, _) =>
      {
         var log = new ResultLog();
         log.Failed("adapter-probe", "deliberate failure");
         return log;
      });
      var failed = await failing.RunAsync(context, ct);
      Add("Adapter: a console failure maps to a failed run", !failed.Success,
         failed.Message ?? "<no message>");

      // the DI hook a host uses to bind the console
      using var provider = new ServiceCollection()
         .AddAssetConsoleProjectProcess()
         .BuildServiceProvider();
      Add("Adapter: DI registration binds the process",
         provider.GetService<IProjectProcess>() is AssetConsoleProjectProcess,
         provider.GetService<IProjectProcess>()?.GetType().Name ?? "<none>");

      return checks;
   }

   /// <summary>
   /// Informational: attempt the <b>real</b> asset-console invocation over a tiny DDL fixture. This
   /// never fails the run — it reports what the legacy pipeline actually does in this environment.
   /// </summary>
   public static async Task<string> ProbeRealConsoleAsync(
      string workRoot, CancellationToken ct = default)
   {
      var working = Path.Combine(workRoot, "real-working");
      Directory.CreateDirectory(Path.Combine(working, "Archive"));
      Directory.CreateDirectory(Path.Combine(working, "Arguments"));
      Directory.CreateDirectory(Path.Combine(working, "Documents"));
      File.WriteAllText(Path.Combine(working, "Archive", "sample.ddl"),
         "CREATE TABLE dbo.Sample (Id INT NOT NULL, Name VARCHAR(50) NULL);");
      File.WriteAllText(Path.Combine(working, "Arguments", "Run.Args.json"), ArgsJson);

      var context = new ProjectProcessContext(
         new ProjectInfo("1", "Probe", "v1r0", "collection", ProjectPath.Parse("/collection/Projects/Probe")),
         ProjectPath.Parse("/Arguments/Run.Args.json"),
         working,
         Array.Empty<ProjectPath>());

      try
      {
         var result = await new AssetConsoleProjectProcess().RunAsync(context, ct);
         var produced = Directory.GetFiles(working, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(working, f)).ToArray();
         return $"success={result.Success} message={result.Message ?? "-"} " +
                $"files=[{string.Join(",", produced)}]";
      }
      catch (Exception ex)
      {
         return $"threw {ex.GetType().Name}: {ex.Message}";
      }
   }
}
