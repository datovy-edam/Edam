using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.FileSystem;
using Edam.Data.Catalog.PostgreSql;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>LM-2a / LM-2b (ADR-0011)</b> checks that a container is part of the <b>address</b> rather than part
/// of the path — for the <b>item index</b> (LM-2a) and for the <b>content namespace</b> (LM-2b) — so two
/// containers may hold the same path, each with its own item and its own bytes.
/// <para>
/// <b>LM-2a</b> proved the item side (the PE-3 defect: a second container was satisfied by the first
/// container's item). <b>LM-2b</b> proves the content side: the file-system store namespaces content per
/// container, and PostgreSQL keys it by <c>(container_id, resource_path)</c> — with an <b>additive,
/// lossless migration</b> whose application is verified by reading the primary-key columns (read-only, so
/// no data is touched).
/// </para>
/// <para>
/// <b>Residual:</b> handing per-container stores to the providers (LM-2b-ii) and dropping the
/// projects-side <c>/&lt;collectionId&gt;/</c> path prefix (LM-2c).
/// </para>
/// </summary>
public static class ScopingScenario
{
   public static async Task<List<ProjectScenario.Check>> RunAsync(
      string workRoot, string? dsn = null, CancellationToken ct = default)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      Directory.CreateDirectory(workRoot);

      // ---- LM-2a: the ITEM index is container-scoped ------------------------------------------
      var store = new FileSystemCatalogStore(Path.Combine(workRoot, "catalog"));
      store.EnlistContainer("collection.a", "LM-2a A", null, ContainerType.FileSystem);
      store.EnlistContainer("collection.b", "LM-2a B", null, ContainerType.FileSystem);

      var a = store.GetContainer("collection.a")!;
      var b = store.GetContainer("collection.b")!;
      const string shared = "/Projects/Shared.Name";

      var createdInA = await store.CreateBranchAsync(shared, "in A", a.Id, ct).ConfigureAwait(false);

      var bItemsBefore = store.GetContainerItems(b.Id);
      Check("A container does not inherit another container's items",
         bItemsBefore.Count == 0, $"{bItemsBefore.Count} item(s) in B");

      var createdInB = await store.CreateBranchAsync(shared, "in B", b.Id, ct).ConfigureAwait(false);
      var bItems = store.GetContainerItems(b.Id);

      Check("Two containers may hold the SAME path, each with its own item",
         createdInB.Id != createdInA.Id && createdInB.ContainerId == b.Id &&
         bItems.Any(i => i.Id == createdInB.Id),
         $"A={createdInA.Id.ToString()[..8]} B={createdInB.Id.ToString()[..8]}");

      Check("The item is matched INSIDE its container (the PE-3 defect)",
         createdInA.ContainerId == a.Id && createdInB.ContainerId == b.Id &&
         store.GetContainerItems(a.Id).Any(i => i.Id == createdInA.Id),
         $"A keeps {createdInA.Id.ToString()[..8]}, B keeps {createdInB.Id.ToString()[..8]}");

      var againInA = await store.CreateBranchAsync(shared, "in A", a.Id, ct).ConfigureAwait(false);
      Check("Creating the same branch twice in one container is idempotent",
         againInA.Id == createdInA.Id, againInA.Id.ToString()[..8]);

      Check("The legacy container-blind lookup still resolves (ambiguous, kept for compatibility)",
         store.GetItemByPath(shared) is not null,
         store.GetItemByPath(shared)?.ContainerId.ToString()[..8] ?? "<null>");

      // ---- LM-2b: the CONTENT namespace is container-scoped (file system) ----------------------
      var contentRoot = Path.Combine(workRoot, "content");
      var contentA = new FileSystemContentStore(contentRoot, "container.a");
      var contentB = new FileSystemContentStore(contentRoot, "container.b");
      var contentLegacy = new FileSystemContentStore(contentRoot);
      const string contentPath = "/Templates/Lm2b.Args.json";

      await contentA.WriteAsync(contentPath, Bytes("in A"), ct).ConfigureAwait(false);
      await contentB.WriteAsync(contentPath, Bytes("in B"), ct).ConfigureAwait(false);
      await contentLegacy.WriteAsync(contentPath, Bytes("unscoped"), ct).ConfigureAwait(false);

      var readA = await ReadAsync(contentA, contentPath, ct).ConfigureAwait(false);
      var readB = await ReadAsync(contentB, contentPath, ct).ConfigureAwait(false);
      var readLegacy = await ReadAsync(contentLegacy, contentPath, ct).ConfigureAwait(false);

      Check("File system: two containers hold the SAME content path, each its own bytes",
         readA == "in A" && readB == "in B", $"a='{readA}' b='{readB}'");

      Check("File system: an unscoped store keeps the legacy namespace (existing content resolves)",
         readLegacy == "unscoped", readLegacy ?? "<none>");

      // ---- LM-2b: the CONTENT key is (container, path) in PostgreSQL ---------------------------
      if (string.IsNullOrWhiteSpace(dsn))
      {
         checks.Add(new ProjectScenario.Check(
            "PostgreSQL: two containers hold the same content path", false,
            "no DSN argument — the postgres half could not be exercised"));
         return checks;
      }

      var pgA = new PostgreSqlContentStore(dsn, "container.a");
      var pgB = new PostgreSqlContentStore(dsn, "container.b");
      var pgLegacy = new PostgreSqlContentStore(dsn);
      const string pgPath = "/Templates/Lm2b.Args.json";

      await pgA.WriteAsync(pgPath, Bytes("pg A"), ct).ConfigureAwait(false);
      await pgB.WriteAsync(pgPath, Bytes("pg B"), ct).ConfigureAwait(false);
      await pgLegacy.WriteAsync(pgPath, Bytes("pg unscoped"), ct).ConfigureAwait(false);

      var pgReadA = await ReadAsync(pgA, pgPath, ct).ConfigureAwait(false);
      var pgReadB = await ReadAsync(pgB, pgPath, ct).ConfigureAwait(false);
      var pgReadLegacy = await ReadAsync(pgLegacy, pgPath, ct).ConfigureAwait(false);

      Check("PostgreSQL: two containers hold the SAME content path, each its own bytes",
         pgReadA == "pg A" && pgReadB == "pg B", $"a='{pgReadA}' b='{pgReadB}'");

      Check("PostgreSQL: the unscoped namespace still resolves (legacy rows keep their bytes)",
         pgReadLegacy == "pg unscoped", pgReadLegacy ?? "<none>");

      var keyColumns = await PrimaryKeyColumnsAsync(dsn, ct).ConfigureAwait(false);
      Check("PostgreSQL: the primary key IS (container_id, resource_path) — the migration applied",
         keyColumns.Length == 2 && keyColumns.Contains("container_id") &&
         keyColumns.Contains("resource_path"),
         string.Join(",", keyColumns));

      // leave the database as we found it (our own rows only)
      await pgA.DeleteAsync(pgPath, ct).ConfigureAwait(false);
      await pgB.DeleteAsync(pgPath, ct).ConfigureAwait(false);
      await pgLegacy.DeleteAsync(pgPath, ct).ConfigureAwait(false);

      return checks;
   }

   /// <summary>The primary-key columns of <c>edam_content</c> (read-only; touches no data).</summary>
   private static async Task<string[]> PrimaryKeyColumnsAsync(string dsn, CancellationToken ct)
   {
      await using var conn = new Npgsql.NpgsqlConnection(dsn);
      await conn.OpenAsync(ct);
      await using var cmd = conn.CreateCommand();
      cmd.CommandText = """
         SELECT a.attname
         FROM pg_index i
         JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)
         WHERE i.indrelid = 'edam_content'::regclass AND i.indisprimary
         ORDER BY a.attname
         """;
      var columns = new List<string>();
      await using var reader = await cmd.ExecuteReaderAsync(ct);
      while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(0));
      return columns.ToArray();
   }

   private static MemoryStream Bytes(string text)
      => new(System.Text.Encoding.UTF8.GetBytes(text));

   private static async Task<string?> ReadAsync(
      IContentStore content, string path, CancellationToken ct)
   {
      using var stream = await content.OpenReadAsync(path, ct).ConfigureAwait(false);
      if (stream is null) return null;

      using var reader = new StreamReader(stream);
      return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
   }
}
