using System.Text;
using Edam.Data.Projects.Assets;
using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.Runner;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// PE-5c (part 1): a <b>real end-to-end process</b> — the project's artifact is materialized, the
/// genuine asset console runs it, and the document it produces is <b>captured back into the
/// project</b>. This is the piece the PE-5b probe showed still needed a properly configured process
/// (a file-producing procedure and a valid input).
/// </summary>
public static class RealProcessScenario
{
   public const string ArgumentsName = "XsdToFile.Args.json";

   private const string InputPath = "/Archive/sample.xsd";
   private const string OutputFolder = "/Documents";

   /// <summary>A minimal, valid XSD for the fixture (disease-surveillance shape from the spec).</summary>
   private const string Xsd = """
   <?xml version="1.0" encoding="utf-8"?>
   <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              xmlns:cd="http://www.datovy.com/hc/cd"
              targetNamespace="http://www.datovy.com/hc/cd"
              elementFormDefault="qualified">
     <xs:element name="Disease_Surveillance_Document">
       <xs:complexType>
         <xs:sequence>
           <xs:element name="Id" type="xs:string" />
           <xs:element name="Name" type="xs:string" />
         </xs:sequence>
       </xs:complexType>
     </xs:element>
   </xs:schema>
   """;

   private const string ArgsJson = """
   {
     "Domain": { "DomainId": "Datovy.HC.CD", "Description": "Communicable Diseases" },
     "Namespace": {
       "OrganizationDomainId": "datovy.hc.cd",
       "Uri": "http://www.datovy.com/hc/cd",
       "Prefix": "cd",
       "Extension": "",
       "RootElementName": "cd:Disease_Surveillance_Document"
     },
     "Project": { "Name": "RealProcess", "VersionId": "v1r0" },
     "Process": { "Name": "RealProcess.ToFile", "ProcedureName": "XsdToFile" },
     "InputFile": { "Extension": "xsd", "Name": "sample", "Path": "./Archive", "Full": "./Archive/sample.xsd" },
     "OutputFile": { "Extension": "jsd", "Name": "dictionary", "Path": "./Documents", "Full": "./Documents/dictionary.jsd" },
     "UriList": [ "./Archive/sample.xsd" ],
     "TextMapFilePath": ""
   }
   """;

   /// <summary>
   /// Informational: attempt a <b>real</b> end-to-end process (project → materialize → genuine
   /// console → captured document). Never fails the run — it reports what the legacy pipeline
   /// actually does, so the remaining gap stays visible instead of hidden behind a green check.
   /// </summary>
   public static async Task<string> ProbeAsync(
      IProjectCatalog catalog, IProjectStore store, IProjectResources resources,
      string workRoot, CancellationToken ct = default)
   {
      try
      {
         Directory.CreateDirectory(workRoot);

         var collection = (await catalog.GetCollectionsAsync(ct)).First(c => c.IsDefault);

         var project = await catalog.GetProjectAsync(collection.CollectionId, "RealProcess", ct)
            ?? await store.CreateAsync(collection.CollectionId, "RealProcess", "PE-5c real process", ct);

         await resources.WriteAsync(project, ProjectPath.Parse(InputPath),
            new MemoryStream(Encoding.UTF8.GetBytes(Xsd)), ct);

         var argumentsFile = ProjectPath.Parse("/Arguments/" + ArgumentsName);
         await resources.WriteAsync(project, argumentsFile,
            new MemoryStream(Encoding.UTF8.GetBytes(ArgsJson)), ct);

         // the REAL console, driven by the runner over the project's resources
         var runner = new ProjectArgumentRunner(resources, new AssetConsoleProjectProcess());
         var result = await runner.RunAsync(project, argumentsFile, ct);

         var produced = await resources.ListAsync(project, ProjectPath.Parse(OutputFolder), ct: ct);

         return $"success={result.Success} message={result.Message ?? "-"} " +
                $"documents=[{string.Join(",", produced.Select(p => p.Path.Value))}]";
      }
      catch (Exception ex)
      {
         return $"threw {ex.GetType().Name}: {ex.Message}";
      }
   }
}
