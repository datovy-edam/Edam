using Edam.Data.Catalog.Contracts;
using Edam.Data.Projects.Contracts;
using Edam.Data.Projects.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>LM-4 (ADR-0011)</b> checks for <b>bindings</b> — the statement of <i>where a container's storage
/// actually is</i>, kept separate from the locations inside it.
/// <para>
/// The payoff test is the last group: the <b>same project settings</b> (same collection id, same
/// project, same resource path) drive <b>two different bindings</b> — a folder and a catalog — and each
/// backend returns <b>its own</b> content for the identical consumer-facing address. That is what makes
/// "switch the storage by changing the binding" a fact rather than a claim.
/// </para>
/// </summary>
public static class BindingScenario
{
   private static IConfiguration Config(params (string Key, string Value)[] values)
      => new ConfigurationBuilder()
         .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
         .Build();

   public static async Task<List<ProjectScenario.Check>> RunAsync(
      string workRoot, CancellationToken ct = default)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      // ---- the binding derives from today's configuration (nothing regresses) ----------------
      var derived = ProjectSettings.Read(Config(
         (ProjectSettings.TARGET_KEY, "filesystem"),
         (ProjectSettings.ROOT_KEY, "/data/edam")));

      Check("A binding is derived from today's configuration (target + location)",
         derived.DefaultBinding is { Target: "filesystem", Location: "/data/edam", IsDefault: true },
         $"{derived.DefaultBinding?.Target} {derived.DefaultBinding?.Location}");

      // ---- a declared binding says WHERE the storage is ---------------------------------------
      var declared = ProjectSettings.Read(Config(
         (ProjectSettings.ROOT_KEY, "/data/edam"),
         (ProjectSettings.BINDINGS_SECTION + ":(default):Target", "postgres"),
         (ProjectSettings.BINDINGS_SECTION + ":(default):Credential", "ConnectionStrings:catalog")));

      Check("A declared binding carries target + credential NAME (never the secret)",
         declared.DefaultBinding is { Target: "postgres", Credential: "ConnectionStrings:catalog" } &&
         !ProjectSettings.IsCredentialReference(declared.DefaultBinding.Credential),
         $"{declared.DefaultBinding?.Target} {declared.DefaultBinding?.Credential}");

      Check("A vault credential is kept as a REFERENCE (unresolved by design)",
         ProjectSettings.IsCredentialReference("vault://edam.catalog") &&
         ProjectSettings.Read(Config(
            (ProjectSettings.ROOT_KEY, "/x"),
            (ProjectSettings.BINDINGS_SECTION + ":(default):Credential", "vault://edam.catalog")))
            .DefaultBinding?.Credential == "vault://edam.catalog",
         "vault://edam.catalog");

      // ---- environment overrides win ----------------------------------------------------------
      var overridden = ProjectSettings.Read(
         Config((ProjectSettings.ROOT_KEY, "/data/edam")),
         new Dictionary<string, string?> { [ProjectSettings.ENV_ROOT] = "/env/edam" });

      Check("EDAM_ROOT overrides the configured location (environment wins) and is reported",
         overridden.Root == "/env/edam" &&
         overridden.DefaultBinding?.Location == "/env/edam" &&
         overridden.EnvironmentOverrides.Contains(ProjectSettings.ENV_ROOT),
         overridden.Root ?? "<none>");

      Check("A collection id maps to its environment segment (edam.studio -> EDAM_STUDIO)",
         ProjectSettings.EnvironmentSegment("edam.studio") == "EDAM_STUDIO",
         ProjectSettings.EnvironmentSegment("edam.studio"));

      var collectionOverride = ProjectSettings.Read(
         Config(
            (ProjectSettings.ROOT_KEY, "/data/edam"),
            (ProjectSettings.COLLECTIONS_SECTION + ":other", "/data/other"),
            (ProjectSettings.BINDINGS_SECTION + ":other:Target", "postgres")),
         new Dictionary<string, string?>
         {
            [ProjectSettings.ENV_COLLECTION_PREFIX + "OTHER" + ProjectSettings.ENV_SUFFIX_ROOT] = "/env/other",
            [ProjectSettings.ENV_COLLECTION_PREFIX + "OTHER" + ProjectSettings.ENV_SUFFIX_TARGET] = "fs",
         });

      Check("EDAM_COLLECTION_OTHER__ROOT/__TARGET override a named collection's binding",
         collectionOverride.BindingFor("other") is { Target: "filesystem", Location: "/env/other" } &&
         collectionOverride.EnvironmentOverrides.Count == 2,
         $"{collectionOverride.BindingFor("other")?.Target} {collectionOverride.BindingFor("other")?.Location}");

      // ---- the composition root refuses what it cannot honour, clearly ------------------------
      Check("A 'service' binding is refused with guidance",
         Throws<NotSupportedException>(() => Build(new Dictionary<string, string>
         {
            [ProjectSettings.TARGET_KEY] = "catalog",
            [ProjectSettings.DEFAULT_COLLECTION_KEY] = "c1",
            [ProjectSettings.BINDINGS_SECTION + ":c1:Target"] = "service",
            [ProjectSettings.BINDINGS_SECTION + ":c1:Location"] = "https://catalog.example/",
         })),
         "NotSupportedException");

      Check("A vault credential is refused with guidance (the host must resolve it)",
         Throws<NotSupportedException>(() => Build(new Dictionary<string, string>
         {
            [ProjectSettings.TARGET_KEY] = "catalog",
            [ProjectSettings.DEFAULT_COLLECTION_KEY] = "c1",
            [ProjectSettings.BINDINGS_SECTION + ":c1:Target"] = "postgres",
            [ProjectSettings.BINDINGS_SECTION + ":c1:Credential"] = "vault://catalog",
         })),
         "NotSupportedException");

      // ---- THE PAYOFF: same settings, two bindings, identical consumer address ----------------
      Directory.CreateDirectory(workRoot);
      var fileSystemRoot = Path.Combine(workRoot, "bound-filesystem");
      var catalogRoot = Path.Combine(workRoot, "bound-catalog");
      Directory.CreateDirectory(fileSystemRoot);
      Directory.CreateDirectory(catalogRoot);

      const string projectName = "Bound.Project";
      var resourcePath = ProjectPath.Parse("/Documents/bound.txt");

      // binding A — a folder
      using var fileSystem = Build(new Dictionary<string, string>
      {
         [ProjectSettings.TARGET_KEY] = "filesystem",
         [ProjectSettings.BINDINGS_SECTION + ":(default):Target"] = "filesystem",
         [ProjectSettings.BINDINGS_SECTION + ":(default):Location"] = fileSystemRoot,
      });

      // binding B — the catalog (same collection id, same project, same resource path)
      using var catalog = Build(new Dictionary<string, string>
      {
         [ProjectSettings.TARGET_KEY] = "catalog",
         [ProjectSettings.DEFAULT_COLLECTION_KEY] = "bound",
         [ProjectSettings.BINDINGS_SECTION + ":bound:Target"] = "filesystem",
         [ProjectSettings.BINDINGS_SECTION + ":bound:Location"] = catalogRoot,
      });

      // the platform never invents containers: the host registers the collection's container
      catalog.GetRequiredService<ICatalogStore>()
         .EnlistContainer("bound", "LM-4 binding conformance", null, ContainerType.FileSystem);

      var fileSystemResult = await RoundTripAsync(fileSystem, projectName, resourcePath, "from the folder", ct);
      var catalogResult = await RoundTripAsync(catalog, projectName, resourcePath, "from the catalog", ct);

      Check("Same settings, two bindings: each backend keeps its own content at the SAME address",
         fileSystemResult == "from the folder" && catalogResult == "from the catalog",
         $"folder='{fileSystemResult}' catalog='{catalogResult}'");

      return checks;
   }

   private static ServiceProvider Build(IReadOnlyDictionary<string, string> config)
      => new ServiceCollection().AddProjectServices(config).BuildServiceProvider();

   private static bool Throws<TException>(Action action) where TException : Exception
   {
      try { action(); return false; }
      catch (TException) { return true; }
      catch { return false; }
   }

   private static async Task<string?> RoundTripAsync(
      IServiceProvider provider, string projectName, ProjectPath resource, string content,
      CancellationToken ct)
   {
      var store = provider.GetRequiredService<IProjectStore>();
      var resources = provider.GetRequiredService<IProjectResources>();
      var catalog = provider.GetRequiredService<IProjectCatalog>();

      var collection = (await catalog.GetCollectionsAsync(ct).ConfigureAwait(false)).First();
      var project = await store.CreateAsync(collection.CollectionId, projectName, ct: ct)
         .ConfigureAwait(false);

      var bytes = System.Text.Encoding.UTF8.GetBytes(content);
      await resources.WriteAsync(project, resource, new MemoryStream(bytes), ct).ConfigureAwait(false);

      using var stream = await resources.OpenReadAsync(project, resource, ct).ConfigureAwait(false);
      if (stream is null) return "<not found>";

      using var reader = new StreamReader(stream);
      return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
   }
}
