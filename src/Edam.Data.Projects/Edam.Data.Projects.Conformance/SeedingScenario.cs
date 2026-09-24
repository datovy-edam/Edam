using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.FileSystem;
using Edam.Data.Projects.Catalog;
using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.FileSystem;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>LM-5 (ADR-0011)</b> checks for seeding: <b>scaffolding stays structure-only</b>, and the starter
/// content is <b>seeded from an address</b> — restoring what the legacy
/// <c>Project.CreateProject</c> did (<c>&lt;project&gt;.&lt;template&gt;</c> in <c>Arguments/</c>)
/// without an app-settings path and without touching the process current directory.
/// <para>
/// It also proves the development story: a <b>container location is seeded from a folder</b> (the repo
/// <c>app-data/</c> is a <i>seed source</i>, never a configured location), and the same seeding then
/// reads one address and writes another.
/// </para>
/// </summary>
public static class SeedingScenario
{
   private const string TemplateName = "Starter.Args.json";
   private const string TemplateContent = "{ \"Process\": { \"Name\": \"Seeded\" } }";

   public static async Task<List<ProjectScenario.Check>> RunAsync(
      string workRoot, CancellationToken ct = default)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      Directory.CreateDirectory(workRoot);

      // ---- file-system provider ---------------------------------------------------------------
      {
         var root = Path.Combine(workRoot, "fs");
         Directory.CreateDirectory(Path.Combine(root, "Templates"));
         File.WriteAllText(Path.Combine(root, "Templates", TemplateName), TemplateContent);

         var catalog = new FileSystemProjectCatalog(root);
         var store = new FileSystemProjectStore(catalog);
         var resources = new FileSystemProjectResources(root, catalog);
         var seeder = new FileSystemProjectSeeder(root, resources);

         var collection = (await catalog.GetCollectionsAsync(ct).ConfigureAwait(false)).First();
         var project = await store.CreateAsync(collection.CollectionId, "Seed.P", ct: ct)
            .ConfigureAwait(false);

         var children = await resources.ListAsync(project, ProjectPath.Root, ct: ct).ConfigureAwait(false);
         var hasFolders = ProjectFolders.All.All(f => children.Any(c => c.IsFolder && c.Name == f));
         var noFilesYet = !children.Any(c => !c.IsFolder);

         Check("Scaffolding is structure-only: the standard folders, no seeded file yet",
            hasFolders && noFilesYet,
            string.Join(",", children.Select(c => c.Name)));

         var template = CatalogAddress.Parse(
            $"catalog://{collection.CollectionId}/Templates/{TemplateName}");
         var written = await seeder.SeedArgumentsAsync(project, template, ct: ct).ConfigureAwait(false);

         var expected = ProjectPath.Parse("/Arguments/Seed.P." + TemplateName);
         var text = await ReadAsync(resources, project, expected, ct).ConfigureAwait(false);

         Check("Seeding restores the legacy naming: <project>.<template> inside Arguments",
            written == expected && text == TemplateContent,
            $"{written} => '{text}'");

         var absent = await seeder.SeedArgumentsAsync(project,
            CatalogAddress.Parse($"catalog://{collection.CollectionId}/Templates/Absent.Args.json"), ct: ct)
            .ConfigureAwait(false);
         Check("An absent template seeds nothing (and does not throw)", absent is null, "null");
      }

      // ---- catalog provider -------------------------------------------------------------------
      {
         var root = Path.Combine(workRoot, "cat");
         Directory.CreateDirectory(root);

         var seedSource = Path.Combine(workRoot, "app-data-seed", "Templates");
         Directory.CreateDirectory(seedSource);
         File.WriteAllText(Path.Combine(seedSource, TemplateName), TemplateContent);

         var store = new FileSystemCatalogStore(root);
         var content = new FileSystemContentStore(root);
         store.EnlistContainer("seed", "LM-5 seeding conformance", null, ContainerType.FileSystem);

         // the development story: a container location is seeded FROM A FOLDER (the repo app-data)
         var seeded = await CatalogFolderSeeder
            .SeedAsync(store, store, content, "seed", seedSource, "/Templates", ct)
            .ConfigureAwait(false);
         Check("A container location is seeded from a folder (app-data is a seed source)",
            seeded == 1, $"{seeded} file(s) into /Templates");

         var catalog = new CatalogProjectCatalog(store, store, "seed");
         var projectStore = new CatalogProjectStore(catalog, store, store, content);
         var resources = new CatalogProjectResources(store, store, content);
         var seeder = new CatalogProjectSeeder(content, resources);

         var project = await projectStore.CreateAsync("seed", "Seed.P", ct: ct).ConfigureAwait(false);

         var template = CatalogAddress.Parse($"catalog://seed/Templates/{TemplateName}");
         var written = await seeder.SeedArgumentsAsync(project, template, ct: ct).ConfigureAwait(false);

         var expected = ProjectPath.Parse("/Arguments/Seed.P." + TemplateName);
         var text = await ReadAsync(resources, project, expected, ct).ConfigureAwait(false);

         Check("The catalog seeds by reading one address and writing another",
            written == expected && text == TemplateContent,
            $"{written} => '{text}'");

         var listed = await resources
            .ListAsync(project, ProjectFolders.Path(ProjectFolders.Arguments), ct: ct)
            .ConfigureAwait(false);
         Check("The seeded file is a real artifact of the project (listed)",
            listed.Any(x => !x.IsFolder && x.Path == expected),
            string.Join(",", listed.Select(x => x.Path.Value)));
      }

      return checks;
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
