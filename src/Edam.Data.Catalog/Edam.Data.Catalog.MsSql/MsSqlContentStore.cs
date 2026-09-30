using Edam.Data.Catalog.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Catalog.MsSql;

/// <summary>
/// MS-SQL content back-end for the <see cref="IContentStore"/> seam (LM-8) — the T-SQL peer of
/// <c>PostgreSqlContentStore</c>: binary content in a <c>varbinary(max)</c> column, addressed by
/// <b>container + resource path</b> so that <b>two containers may hold the same path</b>, while callers
/// still address content by a resource path because the container is the <b>instance's</b> scope
/// (ADR-0011 — the seam stays pure).
/// <para>
/// <b>No migration here:</b> the composite key is part of the create script. LM-2b's
/// <c>ALTER TABLE … ADD PRIMARY KEY</c> upgraded an existing <i>PostgreSQL</i> database, and this storage
/// is new. The <b>unscoped</b> namespace is the empty container id <c>''</c>, so a store created without a
/// scope still resolves single-container content.
/// </para>
/// </summary>
public sealed class MsSqlContentStore : IContentStore
{
   private readonly string _connection;
   private int _initialized;

   /// <param name="connectionString">The catalog MS-SQL connection string.</param>
   /// <param name="containerScope">The container this store namespaces content for; <c>null</c>/empty
   /// keeps the legacy unscoped namespace, so single-container content still resolves.</param>
   public MsSqlContentStore(string connectionString, string? containerScope = null)
   {
      _connection = connectionString
         ?? throw new ArgumentNullException(nameof(connectionString));
      ContainerScope = containerScope ?? string.Empty;
   }

   public MsSqlContentStore(IConfiguration config, string? containerScope = null)
      : this(config["ConnectionStrings:catalog"] ?? config["Edam:Catalog:ConnectionString"]
             ?? throw new InvalidOperationException(
                "Catalog MS-SQL connection string is not configured."),
             containerScope)
   { }

   /// <summary>The container whose content this instance addresses (empty = unscoped/legacy).</summary>
   public string ContainerScope { get; }

   private async Task EnsureSchemaAsync(CancellationToken ct)
   {
      // create once, lazily — the same idiom as the PostgreSQL peer
      if (Interlocked.Exchange(ref _initialized, 1) == 1)
      {
         return;
      }

      await MsSqlSchema.EnsureAsync(_connection, ct).ConfigureAwait(false);
   }

   public async Task<Stream?> OpenReadAsync(string resourcePath, CancellationToken ct = default)
   {
      await EnsureSchemaAsync(ct).ConfigureAwait(false);

      await using var connection = new SqlConnection(_connection);
      await connection.OpenAsync(ct).ConfigureAwait(false);
      await using var command = new SqlCommand(
         "SELECT body FROM dbo.edam_content WHERE container_id = @c AND resource_path = @p",
         connection);

      command.Parameters.AddWithValue("@c", ContainerScope);
      command.Parameters.AddWithValue("@p", resourcePath);

      var result = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
      if (result is null || result is DBNull)
      {
         return null;
      }

      return new MemoryStream((byte[])result);
   }

   public async Task WriteAsync(string resourcePath, Stream content, CancellationToken ct = default)
   {
      await EnsureSchemaAsync(ct).ConfigureAwait(false);

      byte[] bytes;
      if (content is MemoryStream memory)
      {
         bytes = memory.ToArray();
      }
      else
      {
         using var copy = new MemoryStream();
         await content.CopyToAsync(copy, ct).ConfigureAwait(false);
         bytes = copy.ToArray();
      }

      await using var connection = new SqlConnection(_connection);
      await connection.OpenAsync(ct).ConfigureAwait(false);

      // MS-SQL has no ON CONFLICT: MERGE is the upsert, on LM-2b's composite key
      await using var command = new SqlCommand("""
         MERGE dbo.edam_content AS target
         USING (SELECT @c AS container_id, @p AS resource_path) AS source
            ON target.container_id = source.container_id
           AND target.resource_path = source.resource_path
         WHEN MATCHED THEN UPDATE SET body = @body
         WHEN NOT MATCHED THEN
            INSERT (container_id, resource_path, body) VALUES (@c, @p, @body);
         """, connection);

      command.Parameters.AddWithValue("@c", ContainerScope);
      command.Parameters.AddWithValue("@p", resourcePath);
      command.Parameters.AddWithValue("@body", bytes);

      await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
   }

   public async Task<bool> DeleteAsync(string resourcePath, CancellationToken ct = default)
   {
      await EnsureSchemaAsync(ct).ConfigureAwait(false);

      await using var connection = new SqlConnection(_connection);
      await connection.OpenAsync(ct).ConfigureAwait(false);
      await using var command = new SqlCommand(
         "DELETE FROM dbo.edam_content WHERE container_id = @c AND resource_path = @p",
         connection);

      command.Parameters.AddWithValue("@c", ContainerScope);
      command.Parameters.AddWithValue("@p", resourcePath);

      return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) > 0;
   }

   public async Task<bool> ExistsAsync(string resourcePath, CancellationToken ct = default)
   {
      await EnsureSchemaAsync(ct).ConfigureAwait(false);

      await using var connection = new SqlConnection(_connection);
      await connection.OpenAsync(ct).ConfigureAwait(false);

      // MS-SQL has no bare SELECT EXISTS(...): count, and ask whether it found anything
      await using var command = new SqlCommand(
         "SELECT COUNT(1) FROM dbo.edam_content WHERE container_id = @c AND resource_path = @p",
         connection);

      command.Parameters.AddWithValue("@c", ContainerScope);
      command.Parameters.AddWithValue("@p", resourcePath);

      var count = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
      return count is int found && found > 0;
   }
}
