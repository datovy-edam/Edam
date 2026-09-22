// -----------------------------------------------------------------------------
// PE-2 conformance runner. Proves the project seam (IProjectCatalog / IProjectStore /
// IProjectResources) works over a real folder and that the specification's project-relative paths
// ("./Archive/x.xlsx", "./Documents/y.xlsx") resolve through the interface — with the crucial
// property that the PROCESS CURRENT DIRECTORY IS NEVER TOUCHED. Run: Edam.Data.Projects.Conformance
// -----------------------------------------------------------------------------
using System.Text;
using System.Text.Json;
using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.FileSystem;

const string ArgsJson = """
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

var checks = new List<(string Name, bool Passed, string Detail)>();
void Add(string name, bool passed, string detail) => checks.Add((name, passed, detail));

var cwdBefore = Directory.GetCurrentDirectory();
var root = Path.Combine(Path.GetTempPath(), "edam-pe2-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);

try
{
   var catalog = new FileSystemProjectCatalog(root);
   var store = new FileSystemProjectStore(catalog);
   var resources = new FileSystemProjectResources(root, catalog);
   var collectionId = (await catalog.GetCollectionsAsync()).First(c => c.IsDefault).CollectionId;

   // 1 — the default collection is discovered from the root (spec §2.1.1: the default is required)
   var collections = await catalog.GetCollectionsAsync();
   Add("Default collection registered", collections.Any(c => c.IsDefault), collectionId);

   // 2 — a new project gets the standard folder structure
   var project = await store.CreateAsync(collectionId, "Datovy.HC.CD", "Communicable Diseases");
   var tree = await resources.ListAsync(project, ProjectPath.Root);
   var folders = tree.Where(r => r.IsFolder).Select(r => r.Name).ToList();
   Add("Create project scaffolds the standard folders",
      ProjectFolders.All.All(f => folders.Contains(f, StringComparer.OrdinalIgnoreCase)),
      string.Join(",", folders));

   // 3 — the spec's relative paths normalize to project paths
   var schemaPath = ProjectPath.Parse("./Archive/datovy.hc.cd.schema.xlsx");
   Add("Spec-relative path normalizes to a project path",
      schemaPath.Value == "/Archive/datovy.hc.cd.schema.xlsx", schemaPath.Value);

   var escaped = ProjectPath.Parse("../../outside.txt");
   Add("Traversal is normalized away", escaped.Value == "/outside.txt", escaped.Value);

   // 4 — write the spec's artifacts through the seam (binary-safe)
   var schemaBytes = Encoding.UTF8.GetBytes("xlsx-binary-placeholder\u0000\u0001");
   await resources.WriteAsync(project, schemaPath, new MemoryStream(schemaBytes));
   var argsPath = ProjectPath.Parse("./Arguments/Datovy.HC.CD.ToAssets.Args.json");
   await resources.WriteAsync(project, argsPath, new MemoryStream(Encoding.UTF8.GetBytes(ArgsJson)));

   var physical = Path.Combine(root, "Projects", "Datovy.HC.CD", "Archive", "datovy.hc.cd.schema.xlsx");
   Add("Project path maps to the expected physical location",
      File.Exists(physical), Path.GetRelativePath(root, physical));

   using var read = await resources.OpenReadAsync(project, schemaPath);
   byte[]? roundTrip = null;
   if (read is not null)
   {
      using var buffer = new MemoryStream();
      await read.CopyToAsync(buffer);
      roundTrip = buffer.ToArray();
   }
   Add("Archive content round-trips (binary-safe)",
      roundTrip is not null && roundTrip.SequenceEqual(schemaBytes), $"{roundTrip?.Length ?? 0} bytes");

   // 5 — list filters by folder + extension (what the pipeline's ScanFilesFolder needs)
   var archives = await resources.ListAsync(project, ProjectFolders.Path(ProjectFolders.Archive), "xlsx");
   Add("List filters by folder + extension",
      archives.Count == 1 && archives[0].Path == schemaPath,
      string.Join(",", archives.Select(a => a.Path.Value)));

   // 6 — the hinge: the arguments' OWN input/output paths resolve through the seam, no CWD
   var (inputs, output) = ResolveArgsPaths(ArgsJson);
   var outputPath = ProjectPath.Parse(output);
   await resources.WriteAsync(project, outputPath, new MemoryStream(Encoding.UTF8.GetBytes("dictionary")));
   Add("Args input/output paths resolve inside the project",
      inputs.Contains(schemaPath.Value) &&
      outputPath.Value == "/Documents/datovy.hc.cd.dictionary.xlsx",
      string.Join(",", inputs) + " -> " + outputPath.Value);

   // 7 — exists / delete
   var existed = await resources.ExistsAsync(project, schemaPath);
   var deleted = await resources.DeleteAsync(project, schemaPath);
   var gone = !await resources.ExistsAsync(project, schemaPath);
   Add("Exists / Delete semantics", existed && deleted && gone,
      $"existed={existed} deleted={deleted} gone={gone}");

   // 8 — import ("upload") an existing folder as a project
   var incoming = Path.Combine(root, "incoming", "PSJ.Courts");
   Directory.CreateDirectory(Path.Combine(incoming, "Arguments"));
   await File.WriteAllTextAsync(Path.Combine(incoming, "Arguments", "x.Args.json"), ArgsJson);
   await File.WriteAllTextAsync(Path.Combine(incoming, "readme.txt"), "hello");
   var imported = await store.ImportAsync(collectionId, incoming, "PSJ.Courts");
   Add("Import (upload) stores a project from a folder",
      imported.FileCount == 2 && imported.FolderCount >= 1 && imported.Issues is null,
      $"folders={imported.FolderCount} files={imported.FileCount}");

   var importedProject = await catalog.GetProjectAsync(collectionId, "PSJ.Courts");
   var importedFiles = await resources.ListAsync(importedProject!, ProjectPath.Root);
   Add("Imported project browses via the catalog",
      importedProject is not null && importedFiles.Any(f => f.Path.Value == "/readme.txt"),
      string.Join(",", importedFiles.Select(f => f.Path.Value)));

   // 9 — export ("download") it back out
   var exportTarget = Path.Combine(root, "exported");
   await store.ExportAsync(collectionId, "PSJ.Courts", exportTarget);
   Add("Export (download) materializes the project",
      File.Exists(Path.Combine(exportTarget, "readme.txt")) &&
      File.Exists(Path.Combine(exportTarget, "Arguments", "x.Args.json")),
      Path.GetRelativePath(root, exportTarget));

   // 10 — discovery sees both projects
   var projects = await catalog.GetProjectsAsync(collectionId);
   Add("Catalog enumerates the projects", projects.Count == 2,
      string.Join(",", projects.Select(p => p.Name)));

   // 11 — project addresses are collection-relative (/Projects/<name>)
   Add("Project address is /Projects/<name>",
      project.Path.Value == "/Projects/Datovy.HC.CD", project.Path.Value);
}
finally
{
   try { Directory.Delete(root, true); } catch { }
}

// 12 — the whole point of the work: the process current directory is untouched
var cwdAfter = Directory.GetCurrentDirectory();
Add("No process current-directory change", cwdBefore == cwdAfter,
   $"'{cwdBefore}' -> '{cwdAfter}'");

var failed = checks.Count(c => !c.Passed);
foreach (var check in checks)
   Console.WriteLine($"  [{(check.Passed ? "PASS" : "FAIL")}] {check.Name}: {check.Detail}");
Console.WriteLine($"result: PE-2 conformance {(failed == 0 ? "ALL CONFORM" : $"{failed} FAILED")}");

// -----------------------------------------------------------------------------
// Pull the input/output paths out of an arguments document (spec §2.3 steps 6-7) and resolve them
// as project paths. NOTE: this is the fixture's own tiny reader — the product-level binding of
// AssetConsoleArgumentsInfo onto IProjectResources belongs with IProjectRunner (PE-5).
// -----------------------------------------------------------------------------
static (List<string> Inputs, string? Output) ResolveArgsPaths(string json)
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
