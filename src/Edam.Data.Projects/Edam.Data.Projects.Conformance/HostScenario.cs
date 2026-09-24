using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>LM-6</b>: the consumer story end to end, driven by <b>configuration only</b>. A host registers the
/// project services with the ADR-0011 shape and then works <b>exclusively through the interfaces</b>:
/// discover collections, create a project, resolve a template from an alias
/// (<c>app:templates/…</c>), seed the project, write and read a resource through the seam, and
/// <b>run</b> the project's process — no consumer computes a path, and the process current directory is
/// never touched.
/// </summary>
public static class HostScenario
{
   private const string TemplateName = "Starter.Args.json";

   private const string TemplateContent = """
   {
     "Project": { "Name": "Host.Project", "VersionId": "v1r0" },
     "Process": { "Name": "Host.Project.ToFile", "ProcedureName": "XsdToFile" },
     "OutputFile": { "Extension": "txt", "Name": "out", "Path": "./Documents", "Full": "./Documents/out.txt" },
     "UriList": [ "./Archive/input.txt" ]
   }
   """;

   public static async Task<List<ProjectScenario.Check>> RunAsync(
      string workRoot, CancellationToken ct = default)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      // ---- the host's whole configuration: a location, a working root, a provider family ---------
      var root = Path.Combine(workRoot, "host-root");
      Directory.CreateDirectory(Path.Combine(root, "Templates"));
      File.WriteAllText(Path.Combine(root, "Templates", TemplateName), TemplateContent);

      using var provider = new ServiceCollection()
         .AddProjectServices(new Dictionary<string, string>
         {
            [ProjectSettings.TARGET_KEY] = "filesystem",
            [ProjectSettings.ROOT_KEY] = root,
            [ProjectSettings.WORKING_ROOT_KEY] = Path.Combine(workRoot, "work"),
         })
         .AddSingleton<IProjectProcess>(_ => new ProjectRunScenario.EchoProcess())
         .BuildServiceProvider();

      var catalog = provider.GetRequiredService<IProjectCatalog>();
      var store = provider.GetRequiredService<IProjectStore>();
      var resources = provider.GetRequiredService<IProjectResources>();
      var seeder = provider.GetRequiredService<IProjectSeeder>();
      var runner = provider.GetRequiredService<IProjectRunner>();

      var collections = await catalog.GetCollectionsAsync(ct).ConfigureAwait(false);
      var collection = collections.FirstOrDefault(c => c.IsDefault);
      Check("The host resolves the project surface from configuration alone",
         collection is not null,
         string.Join(", ", collections.Select(c => $"{c.CollectionId}={c.Uri}")));

      if (collection is null) return checks;

      var project = await store.CreateAsync(collection.CollectionId, "Host.Project", ct: ct)
         .ConfigureAwait(false);

      var children = await resources.ListAsync(project, ProjectPath.Root, ct: ct).ConfigureAwait(false);
      Check("Scaffolding is structure-only (the standard folders, no file)",
         ProjectFolders.All.All(f => children.Any(c => c.IsFolder && c.Name == f)) &&
         !children.Any(c => !c.IsFolder),
         string.Join(",", children.Select(c => c.Name)));

      // ---- the template is an ADDRESS, not a path ----------------------------------------------
      var scope = new CatalogAddress(collection.CollectionId, project.Path);
      var resolved = ProjectLocations.TryResolve(
         ProjectLocations.AppScheme + ":" + ProjectLocations.Templates + "/" + TemplateName,
         scope, out var template, out var aliasError);

      Check("A template is addressed by alias, not by a path",
         resolved && template.ToString() == $"catalog://{collection.CollectionId}/Templates/{TemplateName}",
         resolved ? template.ToString() : aliasError ?? "<failed>");

      var seeded = await seeder.SeedArgumentsAsync(project, template, ct: ct).ConfigureAwait(false);
      var seededPath = ProjectPath.Parse("/Arguments/Host.Project." + TemplateName);
      Check("The project is seeded from that address (legacy naming preserved)",
         seeded == seededPath &&
         await resources.ExistsAsync(project, seededPath, ct).ConfigureAwait(false),
         seeded?.Value ?? "<none>");

      // ---- artifacts are read/written through the seam -----------------------------------------
      var input = ProjectPath.Parse("/Archive/input.txt");
      await resources.WriteAsync(project, input,
         new MemoryStream(System.Text.Encoding.UTF8.GetBytes("host-input")), ct).ConfigureAwait(false);

      using (var read = await resources.OpenReadAsync(project, input, ct).ConfigureAwait(false))
      {
         string? text = null;
         if (read is not null)
         {
            using var reader = new StreamReader(read);
            text = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
         }
         Check("A resource round-trips through the seam (no consumer-side path)",
            text == "host-input", text ?? "<none>");
      }

      // ---- and the project runs, capturing what the process produced ---------------------------
      var run = await runner.RunAsync(project, seededPath, ct).ConfigureAwait(false);
      var produced = ProjectPath.Parse("/Documents/out.txt");

      string? output = null;
      using (var stream = await resources.OpenReadAsync(project, produced, ct).ConfigureAwait(false))
      {
         if (stream is not null)
         {
            using var reader = new StreamReader(stream);
            output = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
         }
      }

      Check("The same host runs the project and the produced document is captured",
         run.Success && output == "echo:host-input",
         $"success={run.Success} output='{output}' artifacts={string.Join(",", run.Artifacts?.Select(a => a.Value) ?? Array.Empty<string>())}");

      return checks;
   }
}
