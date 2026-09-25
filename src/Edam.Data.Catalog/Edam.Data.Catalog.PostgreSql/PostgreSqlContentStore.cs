using Edam.Data.Catalog.Contracts;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Edam.Data.Catalog.PostgreSql;

/// <summary>
/// PostgreSQL (Npgsql) content back-end for the <see cref="IContentStore"/> seam (BL-7.2 / LM-2b).
/// Binary content is stored in a <c>bytea</c> column, addressed by <b>container + resource path</b> so
/// that <b>two containers may hold the same path</b>; callers still address content by a resource path,
/// because the container is the <b>instance's</b> scope (ADR-0011 — the seam stays pure).
/// </summary>
public sealed class PostgreSqlContentStore : IContentStore
{
    /// <summary>
    /// LM-2b: the content table is keyed by <b>(container_id, resource_path)</b>. The DDL is
    /// <b>additive, idempotent and lossless</b>: a fresh database gets the composite key, and a legacy
    /// table (<c>resource_path</c> primary key, no container column) is migrated in place — the column
    /// is added with a default, the old primary key is replaced by the composite one, and existing rows
    /// keep their bytes, becoming the <b>unscoped</b> container (<c>''</c>) that a store created without
    /// a scope still reads.
    /// </summary>
    private static readonly string ContentSchemaSql = """
        CREATE TABLE IF NOT EXISTS edam_content (
          container_id text NOT NULL DEFAULT '',
          resource_path text NOT NULL,
          body bytea NULL,
          PRIMARY KEY (container_id, resource_path)
        );
        ALTER TABLE edam_content ADD COLUMN IF NOT EXISTS container_id text NOT NULL DEFAULT '';
        ALTER TABLE edam_content DROP CONSTRAINT IF EXISTS edam_content_pkey;
        ALTER TABLE edam_content ADD PRIMARY KEY (container_id, resource_path);
        """;

    private readonly string _connection;
    private int _initialized;

    /// <param name="connectionString">The catalog PostgreSQL connection string.</param>
    /// <param name="containerScope">The container this store namespaces content for; <c>null</c>/empty
    /// keeps the legacy unscoped namespace (so single-container content still resolves).</param>
    public PostgreSqlContentStore(string connectionString, string? containerScope = null)
    {
        _connection = connectionString;
        ContainerScope = containerScope ?? string.Empty;
    }

    public PostgreSqlContentStore(IConfiguration config, string? containerScope = null)
        : this(config["ConnectionStrings:catalog"] ?? config["Edam:Catalog:ConnectionString"]
               ?? throw new InvalidOperationException("Catalog PostgreSQL connection string is not configured."),
               containerScope)
    { }

    /// <summary>The container whose content this instance addresses (empty = unscoped/legacy).</summary>
    public string ContainerScope { get; }

    private async Task EnsureSchemaAsync(CancellationToken ct)
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1) return;
        using var conn = new NpgsqlConnection(_connection);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = ContentSchemaSql;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<Stream?> OpenReadAsync(string resourcePath, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        using var conn = new NpgsqlConnection(_connection);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT body FROM edam_content WHERE container_id = @c AND resource_path = @p";
        cmd.Parameters.AddWithValue("c", ContainerScope);
        cmd.Parameters.AddWithValue("p", resourcePath);
        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is null || result is DBNull) return null;
        return new MemoryStream((byte[])result);
    }

    public async Task WriteAsync(string resourcePath, Stream content, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        byte[] bytes;
        if (content is MemoryStream ms)
        {
            bytes = ms.ToArray();
        }
        else
        {
            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, ct);
            bytes = copy.ToArray();
        }
        using var conn = new NpgsqlConnection(_connection);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO edam_content (container_id, resource_path, body) VALUES (@c, @p, @body)
            ON CONFLICT (container_id, resource_path) DO UPDATE SET body = @body;
            """;
        cmd.Parameters.AddWithValue("c", ContainerScope);
        cmd.Parameters.AddWithValue("p", resourcePath);
        cmd.Parameters.AddWithValue("body", bytes);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> DeleteAsync(string resourcePath, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        using var conn = new NpgsqlConnection(_connection);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM edam_content WHERE container_id = @c AND resource_path = @p";
        cmd.Parameters.AddWithValue("c", ContainerScope);
        cmd.Parameters.AddWithValue("p", resourcePath);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> ExistsAsync(string resourcePath, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        using var conn = new NpgsqlConnection(_connection);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT EXISTS(SELECT 1 FROM edam_content WHERE container_id = @c AND resource_path = @p)";
        cmd.Parameters.AddWithValue("c", ContainerScope);
        cmd.Parameters.AddWithValue("p", resourcePath);
        return (bool)await cmd.ExecuteScalarAsync(ct);
    }
}
