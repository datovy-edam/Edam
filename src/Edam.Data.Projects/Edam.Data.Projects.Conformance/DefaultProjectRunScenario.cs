using System.Text.Json;
using Edam.Data.Projects.Catalog;
using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>DP-5 (ADR-0012 + ADR-0011 decision 6)</b> runs the <b>gated default project over the real targets</b>
/// — the file-system providers and the Catalog-backed providers — through the <b>same seams the Studio
/// uses</b> (<see cref="IProjectCatalog"/>, <see cref="IProjectStore"/>, <see cref="IProjectResources"/>,
/// <see cref="IProjectSeeder"/>), instead of the substituted host of the <c>DP-3</c> group. That is the
/// point: the gates, the created project and the seeded content are observed with <b>real I/O</b> on each
/// back-end.
/// <para>
/// The two checks worth the group are the last two: the created project is an <b>ordinary project</b>
/// addressable as <c>/Projects/&lt;name&gt;</c> with no disk path, and <b>the seeded arguments' own input
/// reference resolves inside it</b> — which is what proves the sample input really landed where the
/// template looks for it (the DP-4 input half).
/// </para>
/// </summary>
public static class DefaultProjectRunScenario
{
   /// <summary>
   /// The seed content a target is prepared with: the shipped starter template and the sample input, plus
   /// the folder they are seeded <i>from</i> (a seed <b>source</b>, never a configured location — ADR-0011).
   /// </summary>
   public sealed record SeedContent(
      string SourceRoot,
      string TemplateFileName,
      string TemplateContent,
      string InputFileName,
      string InputPath,
      string Origin);

   /// <summary>The name the person accepts in the first-run surface (ADR-0012: organization `Edam`).</summary>
   public const string TestName = "Edam.Sample";

   /// <summary>A host over the real seams — the conformance peer of the Studio's own default-project host.</summary>
   private sealed class SeamHost : IDefaultProjectHost
   {
      private readonly IProjectCatalog _catalog;
      private readonly IProjectStore _store;
      private readonly IProjectResources _resources;
      private readonly IProjectSeeder _seeder;
      private readonly string _collectionId;
      private readonly SeedContent _seed;

      public SeamHost(
         IProjectCatalog catalog, IProjectStore store, IProjectResources resources, IProjectSeeder seeder,
         string collectionId, SeedContent seed)
      {
         _catalog = catalog;
         _store = store;
         _resources = resources;
         _seeder = seeder;
         _collectionId = collectionId;
         _seed = seed;
      }

      public async Task<bool> IsContainerEmptyAsync(CancellationToken ct)
         => (await _catalog.GetProjectsAsync(_collectionId, ct).ConfigureAwait(false)).Count == 0;

      public async Task<bool> CreateProjectAsync(string name, CancellationToken ct)
         => await _store.CreateAsync(_collectionId, name, "Created by the first-run setup.", ct)
            .ConfigureAwait(false) is not null;

      public async Task<bool> SeedAsync(string projectName, string templateAddress, CancellationToken ct)
      {
         var project = await _catalog.GetProjectAsync(_collectionId, projectName, ct).ConfigureAwait(false);
         if (project is null) return false;

         // What the service hands over is the registry's template LOCATION (a bare
         // "Templates/ToAssets.Args.json"), not a catalog:// address, so it is resolved the way a host
         // resolves it: through the well-known `app:templates` alias against the project's scope
         // (ADR-0011) — the same resolution the Studio performs. A full address is honoured as given.
         var scope = new CatalogAddress(_collectionId, project.Path);
         if (!CatalogAddress.TryParse(templateAddress, out var template, out _))
         {
            var fileName = Path.GetFileName(templateAddress);
            if (!ProjectLocations.TryResolve(
                   ProjectLocations.AppScheme + ":" + ProjectLocations.Templates + "/" + fileName,
                   scope, out template, out _))
            {
               return false;
            }
         }

         var seeded = await _seeder.SeedArgumentsAsync(project, template, ct: ct).ConfigureAwait(false);
         if (seeded is null) return false;

         // DP-4 (input half), mirroring the Studio: the starter arguments name their input precisely, so the
         // sample input is written into the project's Files/ — through the resources seam, never behind the
         // platform's back. The seed itself is file-system content (ADR-0010: content to import).
         await using var content = File.OpenRead(_seed.InputPath);
         await _resources.WriteAsync(
            project, ProjectFolders.Path(ProjectFolders.Files).Combine(_seed.InputFileName), content, ct)
            .ConfigureAwait(false);

         return true;
      }
   }

   // ---------------------------------------------------------------------------------------------
   // the seed
   // ---------------------------------------------------------------------------------------------

   /// <summary>The shipped starter template (must name its input under <c>./Files</c>).</summary>
   public const string TEMPLATE_FILE = "ToAssets.Args.json";

   /// <summary>The shipped sample input the starter template names.</summary>
   public const string INPUT_FILE = "schema.local.v1r0.xsd";

   /// <summary>
   /// The seed to run with: <b>the shipped repo seed</b> when it can be found (the strongest claim — the
   /// real <c>app-data</c> template and the real sample input), else a minimal inline fixture of the same
   /// shape, so the mechanism is still proven when the source tree is not reachable from the build output.
   /// </summary>
   public static SeedContent Seed(string? repoSeedRoot, string fixtureRoot)
   {
      if (repoSeedRoot is not null)
      {
         var template = Path.Combine(repoSeedRoot, "Templates", TEMPLATE_FILE);
         var input = Path.Combine(repoSeedRoot, "Samples", INPUT_FILE);
         if (File.Exists(template) && File.Exists(input))
         {
            return new SeedContent(
               repoSeedRoot, TEMPLATE_FILE, File.ReadAllText(template), INPUT_FILE, input,
               "the shipped repo seed at " + repoSeedRoot);
         }
      }

      Directory.CreateDirectory(Path.Combine(fixtureRoot, "Templates"));
      Directory.CreateDirectory(Path.Combine(fixtureRoot, "Samples"));

      var inlineTemplate = Path.Combine(fixtureRoot, "Templates", TEMPLATE_FILE);
      var inlineInput = Path.Combine(fixtureRoot, "Samples", INPUT_FILE);
      File.WriteAllText(inlineTemplate, INLINE_TEMPLATE);
      File.WriteAllText(inlineInput, INLINE_INPUT);

      return new SeedContent(
         fixtureRoot, TEMPLATE_FILE, INLINE_TEMPLATE, INPUT_FILE, inlineInput,
         "an inline fixture (the repo app-data seed was not found from here)");
   }

   /// <summary>The shipped template's shape, trimmed to what the DP-5 checks read.</summary>
   private const string INLINE_TEMPLATE = """
   {
      "Process": { "Name": "ReferenceData", "ProcedureName": "XsdToAssets", "SchemaType": 1 },
      "InputFile": { "Extension": "xsd", "Name": "schema.local.v1r0.xsd", "Path": "./Files", "Full": null },
      "OutputFile": { "Extension": "xlsx", "Name": "referenceData", "Path": "./Documents", "Full": null },
      "UriList": [ "./Files" ],
      "TextMapFilePath": "./TextMaps/XsdTextMap.json"
   }
   """;

   /// <summary>A minimal sample input of the shape the template declares.</summary>
   private const string INLINE_INPUT = """
   <?xml version="1.0" encoding="utf-8"?>
   <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              targetNamespace="http://www.datovy.com/et/entity"
              xmlns:et="http://www.datovy.com/et/entity"
              elementFormDefault="qualified">
     <xs:element name="Reference.Entity" type="et:EntityType" />
     <xs:complexType name="EntityType">
       <xs:sequence>
         <xs:element name="ReferenceId" type="xs:string" />
       </xs:sequence>
     </xs:complexType>
   </xs:schema>
   """;

   /// <summary>
   /// Prepare a <b>file-system</b> collection root with the seed's <c>Templates/</c> and <c>Samples/</c> —
   /// the installed shape, where the app-data root <i>is</i> the default collection.
   /// </summary>
   public static void PrepareFileSystemSeed(string collectionRoot, SeedContent seed)
   {
      Directory.CreateDirectory(collectionRoot);

      foreach (var family in new[] { "Templates", "Samples" })
      {
         var source = Path.Combine(seed.SourceRoot, family);
         if (!Directory.Exists(source)) continue;

         foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
         {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(collectionRoot, family, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
         }
      }
   }

   /// <summary>
   /// Prepare a <b>catalog</b> container with the seed's <c>Templates/</c> and <c>Samples/</c> — LM-5's
   /// folder seeder, i.e. the development story "point a container at the repo app-data", reused rather
   /// than reimplemented.
   /// </summary>
   public static async Task PrepareCatalogSeedAsync(
      Edam.Data.Catalog.Contracts.ICatalogContainer containers,
      Edam.Data.Catalog.Contracts.ICatalogItem items,
      Edam.Data.Catalog.Contracts.IContentStore content,
      string containerId, SeedContent seed, CancellationToken ct = default)
   {
      await CatalogFolderSeeder.SeedAsync(
         containers, items, content, containerId, Path.Combine(seed.SourceRoot, "Templates"), "/Templates", ct)
         .ConfigureAwait(false);

      await CatalogFolderSeeder.SeedAsync(
         containers, items, content, containerId, Path.Combine(seed.SourceRoot, "Samples"), "/Samples", ct)
         .ConfigureAwait(false);
   }

   // ---------------------------------------------------------------------------------------------
   // the scenario
   // ---------------------------------------------------------------------------------------------

   public static async Task<List<ProjectScenario.Check>> RunAsync(
      string workRoot, string collectionId,
      IProjectCatalog catalog, IProjectStore store, IProjectResources resources, IProjectSeeder seeder,
      SeedContent seed, Func<CancellationToken, Task> prepareSeed, CancellationToken ct = default)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      Directory.CreateDirectory(workRoot);

      // the seed SOURCE for this target (never a configured location — ADR-0011)
      await prepareSeed(ct).ConfigureAwait(false);

      var nameKey = ConfigurableItems.DEFAULT_PROJECT_NAME_KEY;
      var templateAddress = CatalogAddress.Parse(
         $"catalog://{collectionId}/Templates/{seed.TemplateFileName}");

      // ---- first run: every gate holds, so exactly one project is created -----------------------
      var user = Build(workRoot, "first-run", (nameKey, TestName));
      var host = new SeamHost(catalog, store, resources, seeder, collectionId, seed);
      var service = new DefaultProjectService(user, host, isLocalTarget: true);

      var outcome = await service.TryCreateAsync(ct).ConfigureAwait(false);
      var projects = await catalog.GetProjectsAsync(collectionId, ct).ConfigureAwait(false);
      var project = projects.FirstOrDefault(p =>
         string.Equals(p.Name, TestName, StringComparison.OrdinalIgnoreCase));

      Check("First run (a local target, an empty collection, the switch on): exactly ONE default project is created",
         outcome.Created && projects.Count == 1 && project is not null,
         $"created={outcome.Created} name={outcome.Name} projects=[{string.Join(", ", projects.Select(p => p.Name))}]" +
         (string.IsNullOrWhiteSpace(outcome.Problem) ? string.Empty : $" problem='{outcome.Problem}'"));

      if (project is null)
      {
         // nothing further can be observed honestly: the creation itself did not happen
         return checks;
      }

      // ---- it is a complete project, not an empty folder -----------------------------------------
      var children = await resources.ListAsync(project, ProjectPath.Root, ct: ct).ConfigureAwait(false);
      var missing = ProjectFolders.All
         .Where(f => !children.Any(c =>
            c.IsFolder && string.Equals(c.Name, f, StringComparison.OrdinalIgnoreCase)))
         .ToArray();

      Check("The created project has the 8 standard folders (scaffolding is complete)",
         missing.Length == 0,
         missing.Length == 0
            ? string.Join(",", ProjectFolders.All)
            : "missing: " + string.Join(",", missing));

      // ---- it is SEEDED, so it is not inert (DP-4) ----------------------------------------------
      var arguments = ProjectSeeding.TargetPath(project, templateAddress);
      var argumentsText = await ReadAsync(resources, project, arguments, ct).ConfigureAwait(false);

      Check("The created project is SEEDED: /Arguments/<project>.<template> holds the template's content",
         argumentsText is not null && argumentsText == seed.TemplateContent,
         $"{arguments} ({argumentsText?.Length ?? 0} chars) from {seed.Origin}" +
         (string.IsNullOrWhiteSpace(outcome.Problem) ? string.Empty : $" — the service reported: {outcome.Problem}"));

      // ---- THE POINT (DP-5): the arguments' own input reference resolves INSIDE the project,
      //      and the sample input is where that reference looks for it.
      var reference = InputReference(argumentsText);
      var expectedInput = ProjectFolders.Path(ProjectFolders.Files).Combine(seed.InputFileName);

      var scope = new CatalogAddress(collectionId, project.Path);
      var address = default(CatalogAddress);
      var resolved = reference is not null && scope.TryResolve(reference, out address, out _);

      var insideProject = resolved &&
         (address.Path.Value == project.Path.Value ||
          address.Path.Value.StartsWith(project.Path.Value + "/", StringComparison.OrdinalIgnoreCase));

      ProjectPath? relative = resolved && insideProject
         ? ProjectPath.Parse(address.Path.Value[project.Path.Value.Length..])
         : null;

      var inputThere = relative is not null &&
         await resources.ExistsAsync(project, relative.Value, ct).ConfigureAwait(false);

      Check("The seeded arguments' ./Files/… input resolves INSIDE the project, and the sample input is where it looks",
         reference is not null && resolved && insideProject && relative == expectedInput && inputThere,
         $"reference='{reference}' -> {(relative is null ? "(unresolved)" : relative.Value.Value)} " +
         $"(expected {expectedInput.Value}) exists={inputThere}; input seeded from {seed.Origin}");

      // ---- an ordinary project: no disk path, no current directory (ADR-0009/0011) --------------
      Check("The project is an ordinary project addressable as /Projects/<name> — no drive path, no CWD",
         project.Path.Value == "/Projects/" + TestName &&
         !project.Path.Value.Contains(':') && !project.Path.Value.Contains('\\'),
         $"{project.CollectionId} :: {project.Path.Value}");

      // ---- the marker ends the asking: the answer is complete because the ACTION happened ------
      var again = new DefaultProjectService(user, host, isLocalTarget: true);
      var againOutcome = await again.TryCreateAsync(ct).ConfigureAwait(false);
      var afterAgain = await catalog.GetProjectsAsync(collectionId, ct).ConfigureAwait(false);

      Check("The action COMPLETES the answer: the ask stops and a second attempt creates nothing",
         user.ToAsk().Count == 0 && again.WasAlreadyHandled && !again.IsApplicable &&
         !againOutcome.Created && afterAgain.Count == 1,
         $"toAsk={user.ToAsk().Count} handled={again.WasAlreadyHandled} " +
         $"applicable={again.IsApplicable} created={againOutcome.Created}");

      // ---- it never resurrects (ADR-0012 decision 4) --------------------------------------------
      var deleted = await store.DeleteAsync(collectionId, TestName, ct).ConfigureAwait(false);
      var afterDelete = await catalog.GetProjectsAsync(collectionId, ct).ConfigureAwait(false);
      var resurrected = await new DefaultProjectService(user, host, isLocalTarget: true)
         .TryCreateAsync(ct).ConfigureAwait(false);
      var afterResurrect = await catalog.GetProjectsAsync(collectionId, ct).ConfigureAwait(false);

      Check("It NEVER resurrects: after a delete the marker keeps it deleted, even though the collection is empty again",
         deleted && afterDelete.Count == 0 && !resurrected.Created && afterResurrect.Count == 0,
         $"deleted={deleted} afterDelete={afterDelete.Count} created={resurrected.Created} " +
         $"after={afterResurrect.Count}");

      // ---- a collection that is not empty is refused, with its OWN reason ----------------------
      // a FRESH answer state, so the marker cannot be the reason it is refused
      await store.CreateAsync(collectionId, "Already.Here", null, ct).ConfigureAwait(false);

      var fresh = new DefaultProjectService(
         Build(workRoot, "second-install", (nameKey, TestName)), host, isLocalTarget: true);
      var refused = await fresh.TryCreateAsync(ct).ConfigureAwait(false);

      Check("A collection that already holds a project is refused, with its OWN reason (not the marker)",
         !refused.Created && refused.Applicable && refused.Problem == "the collection already holds projects",
         $"created={refused.Created} applicable={refused.Applicable} problem='{refused.Problem}'");

      await store.DeleteAsync(collectionId, "Already.Here", ct).ConfigureAwait(false);

      return checks;
   }

   // ---------------------------------------------------------------------------------------------

   private static UserConfiguration Build(
      string root, string name, params (string Key, string Value)[] values)
   {
      var config = new ConfigurationBuilder()
         .AddInMemoryCollection(values.Select(v =>
            new KeyValuePair<string, string?>(v.Key, v.Value)))
         .Build();

      return new UserConfiguration(
         config,
         new FileStateStore(Path.Combine(root, name)),
         new InMemoryStateStore("(packaged)"));
   }

   /// <summary>
   /// The input reference the seeded arguments state — <c>InputFile.Path</c> + <c>InputFile.Name</c> (the
   /// shipped template states them separately, e.g. <c>./Files</c> + <c>schema.local.v1r0.xsd</c>) — read out
   /// of the <b>seeded artifact</b> rather than hardcoded, so the check follows the content it created.
   /// </summary>
   private static string? InputReference(string? argumentsJson)
   {
      if (string.IsNullOrWhiteSpace(argumentsJson)) return null;

      try
      {
         using var document = JsonDocument.Parse(argumentsJson);
         if (!document.RootElement.TryGetProperty("InputFile", out var input)) return null;

         var path = input.TryGetProperty("Path", out var p) ? p.GetString() : null;
         var name = input.TryGetProperty("Name", out var n) ? n.GetString() : null;
         if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(name)) return null;

         return path!.TrimEnd('/') + "/" + name;
      }
      catch (JsonException)
      {
         return null;
      }
   }

   private static async Task<string?> ReadAsync(
      IProjectResources resources, ProjectInfo project, ProjectPath path, CancellationToken ct)
   {
      using var stream = await resources.OpenReadAsync(project, path, ct).ConfigureAwait(false);
      if (stream is null) return null;

      using var reader = new StreamReader(stream);
      return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
   }
}
