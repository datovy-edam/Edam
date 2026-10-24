using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.DependencyInjection;
using Edam.Data.Projects.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>S3-5: the text-map seam over a REAL collection</b> — the project's own map wins over the application's, a
/// kind the project lacks is served from the application seed <b>and copied into the project</b> (which is what the
/// application does), a kind nobody serves resolves to <b>nothing</b> rather than an invented path, and a component
/// supplied through DI <b>is</b> the one resolved. Offline: a temporary file-system collection, no database.
/// </summary>
public static class TextMapResolutionScenario
{
   private const string ProjectMap = "{ \"project\": \"XsdTextMap.json\" }";
   private const string SeedMap = "{ \"seed\": \"XsdTextMap.json\" }";

   public static async Task<List<ProjectScenario.Check>> Run(string workRoot, CancellationToken ct = default)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      var root = Path.Combine(workRoot, "textmaps");
      var seedFolder = Path.Combine(workRoot, "app-textmaps");
      Directory.CreateDirectory(root);
      Directory.CreateDirectory(seedFolder);

      // the application's shipped maps
      File.WriteAllText(Path.Combine(seedFolder, "XsdTextMap.json"), SeedMap);
      File.WriteAllText(Path.Combine(seedFolder, "SqlTextMap.json"), "{ \"seed\": \"SqlTextMap.json\" }");
      // a file the convention must ignore, and say why
      File.WriteAllText(Path.Combine(seedFolder, "MyMapping.json"), "{}");

      var catalog = new FileSystemProjectCatalog(root);
      var store = new FileSystemProjectStore(catalog);
      var resources = new FileSystemProjectResources(root, catalog);

      var collection = (await catalog.GetCollectionsAsync(ct).ConfigureAwait(false)).First();
      var project = await store.CreateAsync(collection.CollectionId, "TextMap.P", ct: ct)
         .ConfigureAwait(false);

      // the PROJECT's own map, with different content than the seed's of the same name
      var projectMap = ProjectPath.Parse("/TextMaps/XsdTextMap.json");
      await resources.CreateFolderAsync(project, ProjectPath.Parse("/TextMaps"), ct).ConfigureAwait(false);
      using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ProjectMap)))
      {
         await resources.WriteAsync(project, projectMap, stream, ct).ConfigureAwait(false);
      }

      var reported = new List<String>();
      var resolver = new DefaultTextMapCatalog(resources, new[] { seedFolder }, reported.Add);

      var options = await resolver.ListAsync(project, ct).ConfigureAwait(false);

      Check("The PROJECT's own map is found FIRST; the application's follows",
         options.Count >= 3 &&
         options[0].Name == "XsdTextMap.json" && options[0].Address.StartsWith("/TextMaps/"),
         string.Join(" | ", options.Select(o => o.Kind + ":" + o.Name + "@" + o.Address)));

      Check("A kind the project has resolves to the PROJECT's copy, not the seed's",
         (await resolver.ResolveAsync(project, TextMapKind.Xsd, ct).ConfigureAwait(false))
            ?.Address.StartsWith("/TextMaps/") == true,
         (await resolver.ResolveAsync(project, TextMapKind.Xsd, ct).ConfigureAwait(false))?.Address ?? "(none)");

      // the application does this: read the shipped map, write it into the project, reference it relatively
      var sql = await resolver.ResolveAsync(project, TextMapKind.Sql, ct).ConfigureAwait(false);
      var copied = false;
      if (sql is not null)
      {
         var content = File.ReadAllText(sql.Address);
         using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)))
         {
            await resources.WriteAsync(project, ProjectPath.Parse("/TextMaps/SqlTextMap.json"), stream, ct)
               .ConfigureAwait(false);
         }

         copied = await resources.ExistsAsync(
            project, ProjectPath.Parse("/TextMaps/SqlTextMap.json"), ct).ConfigureAwait(false);
      }

      Check("A map served from the application is COPIED into the project (./TextMaps/<name>)",
         copied,
         sql is null ? "no SQL map was found" : "copied '" + sql.Name + "' from " + sql.Address);

      Check("After the copy the PROJECT answers for that kind too (one seam, project content)",
         (await resolver.ResolveAsync(project, TextMapKind.Sql, ct).ConfigureAwait(false))
            ?.Address.StartsWith("/TextMaps/") == true,
         (await resolver.ResolveAsync(project, TextMapKind.Sql, ct).ConfigureAwait(false))?.Address ?? "(none)");

      var nothing = await resolver.ResolveAsync(project, TextMapKind.Gql, ct).ConfigureAwait(false);
      Check("A kind nobody serves resolves to NOTHING — no path is invented",
         nothing is null, nothing is null ? "(none)" : "invented " + nothing.Address);

      Check("A non-conforming file is IGNORED WITH ITS REASON (never silently)",
         reported.Any(m => m.IndexOf("MyMapping.json", StringComparison.OrdinalIgnoreCase) >= 0 &&
                           m.IndexOf("TextMap", StringComparison.OrdinalIgnoreCase) >= 0),
         reported.Count == 0 ? "(nothing reported)" : reported[0]);

      // ---- the seam is honoured: whatever the container returns is what the application uses ----------------
      ITextMapCatalog asInterface = resolver;
      var replacement = new ReplacementCatalog();
      var services = new ServiceCollection();
      services.AddSingleton<ITextMapCatalog>(asInterface);
      services.AddSingleton<ITextMapCatalog>(replacement);

      using var provider = services.BuildServiceProvider();
      var resolved = provider.GetRequiredService<ITextMapCatalog>();

      Check("A component supplied at initialization IS the catalog resolved (the seam, not a hardcoded type)",
         ReferenceEquals(resolved, replacement) && resolved.SupportedKinds.Count == 1,
         resolved.GetType().Name + " with " + resolved.SupportedKinds.Count + " kind(s)");

      await Task.CompletedTask;
      return checks;
   }

   /// <summary>A supplied implementation: proof that nothing in the application needs the default one.</summary>
   private sealed class ReplacementCatalog : ITextMapCatalog
   {
      public IReadOnlyCollection<TextMapKind> SupportedKinds { get; } = new[] { TextMapKind.Xsd };

      public Task<IReadOnlyList<TextMapOption>> ListAsync(
         ProjectInfo project, CancellationToken ct = default)
         => Task.FromResult<IReadOnlyList<TextMapOption>>(new List<TextMapOption>());

      public Task<TextMapOption?> ResolveAsync(
         ProjectInfo project, TextMapKind kind, CancellationToken ct = default)
         => Task.FromResult<TextMapOption?>(null);
   }
}
