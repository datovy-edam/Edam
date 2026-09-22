using System.Text.Json;
using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Runner;

/// <summary>
/// The path-relevant view of an EDAM arguments document (<c>*.Args.json</c>) — specification
/// "Understanding Projects" §2.3 steps 6-8: the referenced input files (<c>UriList</c>),
/// <c>InputFile</c>, <c>OutputFile</c> and <c>TextMapFilePath</c>.
/// <para>
/// This is deliberately <b>not</b> a model of the whole document (the asset console owns
/// <c>AssetConsoleArgumentsInfo</c>); it reads only what the runner needs to materialize inputs and
/// capture outputs.
/// </para>
/// </summary>
/// <param name="Inputs">Referenced inputs — files, or folders to expand (e.g. <c>./Archive</c>, <c>./Files</c>).</param>
/// <param name="InputFolder">The declared input folder (<c>InputFile.Path</c>), when present.</param>
/// <param name="OutputFile">The declared output file.</param>
/// <param name="OutputFolder">The declared output folder (<c>OutputFile.Path</c>).</param>
/// <param name="TextMapFile">The declared text map file, when present.</param>
public sealed record ProjectArguments(
   IReadOnlyList<ProjectPath> Inputs,
   ProjectPath? InputFolder = null,
   ProjectPath? OutputFile = null,
   ProjectPath? OutputFolder = null,
   ProjectPath? TextMapFile = null)
{
   /// <summary>Read the path-relevant parts of an arguments document.</summary>
   public static ProjectArguments Parse(string json)
   {
      var inputs = new List<ProjectPath>();
      ProjectPath? inputFolder = null;
      ProjectPath? outputFile = null;
      ProjectPath? outputFolder = null;
      ProjectPath? textMap = null;

      using var document = JsonDocument.Parse(json);
      var root = document.RootElement;

      // step 7 — the input file(s)
      if (root.TryGetProperty("UriList", out var uriList) && uriList.ValueKind == JsonValueKind.Array)
      {
         foreach (var uri in uriList.EnumerateArray())
         {
            var value = uri.GetString();
            if (!string.IsNullOrWhiteSpace(value)) inputs.Add(ProjectPath.Parse(value));
         }
      }

      // step 6 — input/output defaults (paths are project-relative, e.g. "./Files", "./Documents")
      if (root.TryGetProperty("InputFile", out var inputFile) && inputFile.ValueKind == JsonValueKind.Object)
      {
         var path = ReadPath(inputFile, "Path");
         if (path is not null) inputFolder = ProjectPath.Parse(path);
      }

      if (root.TryGetProperty("OutputFile", out var output) && output.ValueKind == JsonValueKind.Object)
      {
         var folder = ReadPath(output, "Path");
         if (folder is not null) outputFolder = ProjectPath.Parse(folder);

         var path = ReadPath(output, "Full") ?? Compose(output, folder);
         if (path is not null) outputFile = ProjectPath.Parse(path);
      }

      // step 9 — text map (guides traversal / language mapping)
      if (root.TryGetProperty("TextMapFilePath", out var textMapElement) &&
          textMapElement.ValueKind == JsonValueKind.String)
      {
         var value = textMapElement.GetString();
         if (!string.IsNullOrWhiteSpace(value)) textMap = ProjectPath.Parse(value);
      }

      return new ProjectArguments(inputs, inputFolder, outputFile, outputFolder, textMap);
   }

   private static string? ReadPath(JsonElement element, string name)
      => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
         ? value.GetString()
         : null;

   /// <summary>Compose <c>&lt;Path&gt;/&lt;Name&gt;.&lt;Extension&gt;</c> when no explicit full path is given.</summary>
   private static string? Compose(JsonElement output, string? folder)
   {
      if (string.IsNullOrWhiteSpace(folder)) return null;

      var name = ReadPath(output, "Name");
      if (string.IsNullOrWhiteSpace(name)) return null;

      var extension = ReadPath(output, "Extension");
      return string.IsNullOrWhiteSpace(extension)
         ? $"{folder}/{name}"
         : $"{folder}/{name}.{extension.TrimStart('.')}";
   }
}
