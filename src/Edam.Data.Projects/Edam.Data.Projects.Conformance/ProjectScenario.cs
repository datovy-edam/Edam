using System.Text;
using System.Text.Json;
using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// The shared project-behaviour scenario (PE-2 / PE-3, ADR-0009): the <b>same</b> checks run against
/// the file-system providers and the Catalog-backed providers — including a <b>remote</b> catalog
/// over the REST API — proving the storage is replaceable without changing callers, and that the
/// specification's project-relative paths resolve without ever touching the process current
/// directory.
/// </summary>
public static class ProjectScenario
{
   public readonly record struct Check(string Name, bool Passed, string Detail);

   /// <summary>The specification's example arguments document (Understanding Projects §2.3).</summary>
   public const string ArgsJson = """
   {
     "@context": { "edam": "http://www.datovy.com/edam/arguments" },
     "Domain": { "DomainId": "Datovy.HC.CD", "Description": "Communicable Diseases" },
     "Namespace": {
       "OrganizationDomainId": "datovy.hc.cd",
       "Uri": "http://www.datovy.com/hc/cd",
       "Prefix": "cd",
       "Extension": "",
       "RootElementName": "cd:Disease_Surveillance_Document"
     },
     "Project": { "Name": "Datovy.HC.CD", "VersionId": "v1r0" },
     "Process": {
       "RecordId": null,
       "Name": "Datovy.HC.CD.ToAssets",
       "OrganizationId": "Datovy",
       "OrganizationDomainUri": null,
       "ProcedureName": "DdlImportToAssets",
       "ProcedureTag": "DDL.DdlImportFileReader",
       "ScanFilesFolder": false,
       "SchemaType": 1,
       "NextProcess": "",
       "NextProcedure": [""]
     },
     "InputFile": { "Extension": "xlsx", "Name": "datovy.hc.cd.mdf", "Path": "./Files", "Full": null },
     "OutputFile": {
       "Extension": "xlsx",
       "Name": "datovy.hc.cd.dictionary",
       "Path": "./Documents",
       "Full": "./Documents/datovy.hc.cd.dictionary.xlsx"
     },
     "UriList": [ "./Archive/datovy.hc.cd.schema.xlsx" ],
     "InspectArguments": { "ListLength": "1", "MaxThreshold": "1" },
     "ConnectionString": "",
     "ElementTransform": null,
     "TextMapFilePath": "./Archive/DdlTextMap.json"
   }
   """;

   public static async Task<List<Check>> RunAsync(
      IProjectCatalog catalog, IProjectStore store, IProjectResources resources,
      string workRoot, CancellationToken ct = default)
   {
      var checks = new List<Check>();
      void Add(string name, bool passed, string detail) => checks.Add(new Check(name, passed, detail));

      Directory.CreateDirectory(workRoot);

      // 1 — the default collection is discovered (spec §2.1.1: the default is required)
      var collections = await catalog.GetCollectionsAsync(ct);
      var collection = collections.FirstOrDefault(c => c.IsDefault);
      Add("Default collection registered", collection is not null, collection?.CollectionId ?? "<none>");
      var collectionId = collection!.CollectionId;

      // 2 — a new project gets the standard folder structure
      var project = await store.CreateAsync(collectionId, "Datovy.HC.CD", "Communicable Diseases", ct);
      var tree = await resources.ListAsync(project, ProjectPath.Root, ct: ct);
      var folders = tree.Where(r => r.IsFolder).Select(r => r.Name).ToList();
      Add("Create project scaffolds the standard folders",
         ProjectFolders.All.All(f => folders.Contains(f, StringComparer.OrdinalIgnoreCase)),
         string.Join(",", folders));

      // 3 — the spec's relative paths normalize to project paths
      var schemaPath = ProjectPath.Parse("./Archive/datovy.hc.cd.schema.xlsx");
      Add("Spec-relative path normalizes to a project path",
         schemaPath.Value == "/Archive/datovy.hc.cd.schema.xlsx", schemaPath.Value);

      Add("Traversal is normalized away",
         ProjectPath.Parse("../../outside.txt").Value == "/outside.txt",
         ProjectPath.Parse("../../outside.txt").Value);

      // 4 — write the spec's artifacts through the seam (binary-safe)
      var schemaBytes = Encoding.UTF8.GetBytes("xlsx-binary-placeholder\u0000\u0001");
      await resources.WriteAsync(project, schemaPath, new MemoryStream(schemaBytes), ct);
      var argsPath = ProjectPath.Parse("./Arguments/Datovy.HC.CD.ToAssets.Args.json");
      await resources.WriteAsync(project, argsPath, new MemoryStream(Encoding.UTF8.GetBytes(ArgsJson)), ct);

      using var read = await resources.OpenReadAsync(project, schemaPath, ct);
      byte[]? roundTrip = null;
      if (read is not null)
      {
         using var buffer = new MemoryStream();
         await read.CopyToAsync(buffer, ct);
         roundTrip = buffer.ToArray();
      }
      Add("Archive content round-trips (binary-safe)",
         roundTrip is not null && roundTrip.SequenceEqual(schemaBytes), $"{roundTrip?.Length ?? 0} bytes");

      // 5 — folder + extension filtering (what the pipeline's folder scan needs)
      var archives = await resources.ListAsync(
         project, ProjectFolders.Path(ProjectFolders.Archive), "xlsx", ct);
      Add("List filters by folder + extension",
         archives.Count == 1 && archives[0].Path == schemaPath,
         string.Join(",", archives.Select(a => a.Path.Value)));

      // 6 — the hinge: the arguments' OWN input/output paths resolve through the seam
      var (inputs, output) = ResolveArgsPaths(ArgsJson);
      var outputPath = ProjectPath.Parse(output);
      await resources.WriteAsync(project, outputPath, new MemoryStream(Encoding.UTF8.GetBytes("dictionary")), ct);
      Add("Args input/output paths resolve inside the project",
         inputs.Contains(schemaPath.Value) && outputPath.Value == "/Documents/datovy.hc.cd.dictionary.xlsx",
         string.Join(",", inputs) + " -> " + outputPath.Value);

      // 7 — exists / delete
      var existed = await resources.ExistsAsync(project, schemaPath, ct);
      var deleted = await resources.DeleteAsync(project, schemaPath, ct);
      var gone = !await resources.ExistsAsync(project, schemaPath, ct);
      Add("Exists / Delete semantics", existed && deleted && gone,
         $"existed={existed} deleted={deleted} gone={gone}");

      // 8 — import ("upload") an existing folder as a project
      var incoming = Path.Combine(workRoot, "incoming", "PSJ.Courts");
      Directory.CreateDirectory(Path.Combine(incoming, "Arguments"));
      await File.WriteAllTextAsync(Path.Combine(incoming, "Arguments", "x.Args.json"), ArgsJson, ct);
      await File.WriteAllTextAsync(Path.Combine(incoming, "readme.txt"), "hello", ct);

      var imported = await store.ImportAsync(collectionId, incoming, "PSJ.Courts", ct);
      Add("Import (upload) stores a project from a folder",
         imported.FileCount == 2 && imported.FolderCount >= 1 && imported.Issues is null,
         $"folders={imported.FolderCount} files={imported.FileCount}");

      var importedProject = await catalog.GetProjectAsync(collectionId, "PSJ.Courts", ct);
      if (importedProject is null)
      {
         var seen = await catalog.GetProjectsAsync(collectionId, ct);
         Add("Imported project browses via the catalog", false,
            "project not found; catalog has: " + string.Join(",", seen.Select(p => p.Name)));
      }
      else
      {
         var importedFiles = await resources.ListAsync(importedProject, ProjectPath.Root, ct: ct);
         Add("Imported project browses via the catalog",
            importedFiles.Any(f => f.Path.Value == "/readme.txt"),
            string.Join(",", importedFiles.Select(f => f.Path.Value)));
      }

      // 9 — export ("download") it back out
      var exportTarget = Path.Combine(workRoot, "exported");
      await store.ExportAsync(collectionId, "PSJ.Courts", exportTarget, ct);
      Add("Export (download) materializes the project",
         File.Exists(Path.Combine(exportTarget, "readme.txt")) &&
         File.Exists(Path.Combine(exportTarget, "Arguments", "x.Args.json")),
         exportTarget);

      // 10 — discovery sees both projects
      var projects = await catalog.GetProjectsAsync(collectionId, ct);
      Add("Catalog enumerates the projects", projects.Count == 2,
         string.Join(",", projects.Select(p => p.Name)));

      // 11 — projects are addressed as <collection>/Projects/<name> in every provider
      //      (the collection prefix keeps two collections from colliding on the same path)
      Add("Project path is <collection>/Projects/<name>",
         project.Path.Value.EndsWith("/Projects/Datovy.HC.CD", StringComparison.OrdinalIgnoreCase) &&
         project.Path.Name == "Datovy.HC.CD",
         project.Path.Value);

      return checks;
   }

   /// <summary>
   /// Pull the input/output paths out of an arguments document (spec §2.3 steps 6-7) and resolve
   /// them as project paths. The product-level binding of <c>AssetConsoleArgumentsInfo</c> onto
   /// <see cref="IProjectResources"/> belongs with <c>IProjectRunner</c> (PE-5).
   /// </summary>
   public static (List<string> Inputs, string? Output) ResolveArgsPaths(string json)
   {
      var inputs = new List<string>();
      string? output = null;

      using var document = JsonDocument.Parse(json);
      var root = document.RootElement;

      if (root.TryGetProperty("UriList", out var uriList) && uriList.ValueKind == JsonValueKind.Array)
      {
         foreach (var uri in uriList.EnumerateArray())
         {
            var value = uri.GetString();
            if (!string.IsNullOrWhiteSpace(value)) inputs.Add(ProjectPath.Parse(value).Value);
         }
      }

      if (root.TryGetProperty("OutputFile", out var outputFile) && outputFile.ValueKind == JsonValueKind.Object)
      {
         var full = outputFile.TryGetProperty("Full", out var f) ? f.GetString() : null;
         if (!string.IsNullOrWhiteSpace(full))
         {
            output = ProjectPath.Parse(full).Value;
         }
         else
         {
            var path = outputFile.TryGetProperty("Path", out var p) ? p.GetString() : null;
            var name = outputFile.TryGetProperty("Name", out var n) ? n.GetString() : null;
            var extension = outputFile.TryGetProperty("Extension", out var e) ? e.GetString() : null;
            output = ProjectPath.Parse($"{path}/{name}.{extension}").Value;
         }
      }

      return (inputs, output);
   }
}
