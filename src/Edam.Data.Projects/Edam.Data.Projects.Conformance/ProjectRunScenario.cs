using System.Text;
using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// PE-5 checks: the project runner executes a process over a project's <b>resources</b> — inputs are
/// materialized out of the project, the process runs against a private working folder, and whatever
/// it produces is captured back into the project. No process current directory is involved.
/// <para>
/// The process itself is a stand-in here: binding the real asset console is a separate adapter, so
/// the runner's plumbing (read arguments → resolve paths → materialize → run → capture) can be
/// proven without depending on the asset machinery.
/// </para>
/// </summary>
public static class ProjectRunScenario
{
   public const string InputContent = "hello-pe5";

   private const string ArgsJson = """
   {
     "Domain": { "DomainId": "Datovy.HC.CD" },
     "Project": { "Name": "Datovy.HC.CD", "VersionId": "v1r0" },
     "Process": { "Name": "Datovy.HC.CD.ToAssets", "ProcedureName": "DdlImportToAssets" },
     "OutputFile": { "Extension": "txt", "Name": "out", "Path": "./Documents", "Full": "./Documents/out.txt" },
     "UriList": [ "./Archive/input.txt" ]
   }
   """;

   /// <summary>A stand-in process: echoes the materialized input into the declared output.</summary>
   public sealed class EchoProcess : IProjectProcess
   {
      public async Task<ProjectRunResult> RunAsync(
         ProjectProcessContext context, CancellationToken ct = default)
      {
         if (!context.Inputs.Any(i => i.Value == "/Archive/input.txt"))
            return new ProjectRunResult(false,
               "the runner did not materialize /Archive/input.txt: " +
               string.Join(",", context.Inputs.Select(i => i.Value)));

         var source = Path.Combine(context.WorkingFolder, "Archive", "input.txt");
         if (!File.Exists(source))
            return new ProjectRunResult(false, $"input missing from the working folder: {source}");

         var content = await File.ReadAllTextAsync(source, ct);
         var output = context.OutputFile ?? ProjectPath.Parse("/Documents/out.txt");
         var target = Path.Combine(context.WorkingFolder,
            output.Value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

         Directory.CreateDirectory(Path.GetDirectoryName(target)!);
         await File.WriteAllTextAsync(target, "echo:" + content, ct);

         return new ProjectRunResult(true, null, new[] { output });
      }
   }

   public static async Task<List<ProjectScenario.Check>> RunAsync(
      IProjectCatalog catalog, IProjectResources resources, IProjectRunner runner,
      string workRoot, CancellationToken ct = default)
   {
      var checks = new List<ProjectScenario.Check>();
      void Add(string name, bool passed, string detail) => checks.Add(new ProjectScenario.Check(name, passed, detail));

      Directory.CreateDirectory(workRoot);

      var collection = (await catalog.GetCollectionsAsync(ct)).First(c => c.IsDefault);
      var project = await catalog.GetProjectAsync(collection.CollectionId, "Datovy.HC.CD", ct);
      if (project is null)
      {
         Add("Runner: project available", false, "Datovy.HC.CD was not found in the default collection");
         return checks;
      }

      // an input and an arguments document that references it and declares the output
      var input = ProjectPath.Parse("/Archive/input.txt");
      await resources.WriteAsync(project, input,
         new MemoryStream(Encoding.UTF8.GetBytes(InputContent)), ct);

      var argumentsFile = ProjectPath.Parse("/Arguments/Run.Args.json");
      await resources.WriteAsync(project, argumentsFile,
         new MemoryStream(Encoding.UTF8.GetBytes(ArgsJson)), ct);

      var result = await runner.RunAsync(project, argumentsFile, ct);
      Add("Runner: process executed", result.Success, result.Message ?? "ok");

      var expected = ProjectPath.Parse("/Documents/out.txt");
      var produced = await resources.OpenReadAsync(project, expected, ct);
      string? text = null;
      if (produced is not null)
      {
         using var reader = new StreamReader(produced);
         text = await reader.ReadToEndAsync(ct);
      }
      Add("Runner: output captured back into the project", text == "echo:" + InputContent, text);

      Add("Runner: artifacts reported",
         result.Artifacts?.Contains(expected) == true,
         result.Artifacts is null ? "<none>" : string.Join(",", result.Artifacts.Select(a => a.Value)));

      return checks;
   }
}
