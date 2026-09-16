using Edam.Data.Catalog.Contracts;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Edam.Data.Catalog.PostgreSql;

/// <summary>
/// PostgreSQL (Npgsql) content back-end for the <see cref="IContentStore"/> seam (BL-7.2).
/// Binary content is stored path/URI-keyed in a <c>bytea</c> column; callers address content by a
/// resource path (file/json/xml/blob). Provider-specifics stay behind the seam (ADR-0006/0007).
/// </summary>
public sealed class PostgreSqlContentStore : IContentStore
{
    private static readonly string ContentSchemaSql = """
        CREATE TABLE IF NOT EXISTS edam_content (
          resource_path text PRIMARY KEY,
          body bytea NULL
        );
        """;

    private readonly string _connection;
    private int _initialized;

    public PostgreSqlContentStore(string connectionString) => _connection = connectionString;

    public PostgreSqlContentStore(IConfiguration config)
        : this(config["ConnectionStrings:catalog"] ?? config["Edam:Catalog:ConnectionString"]
               ?? throw new InvalidOperationException("Catalog PostgreSQL connection string is not configured."))
    { }

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
        cmd.CommandText = "SELECT body FROM edam_content WHERE resource_path = @p";
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
            INSERT INTO edam_content (resource_path, body) VALUES (@p, @body)
            ON CONFLICT (resource_path) DO UPDATE SET body = @body;
            """;
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
        cmd.CommandText = "DELETE FROM edam_content WHERE resource_path = @p";
        cmd.Parameters.AddWithValue("p", resourcePath);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> ExistsAsync(string resourcePath, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        using var conn = new NpgsqlConnection(_connection);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM edam_content WHERE resource_path = @p)";
        cmd.Parameters.AddWithValue("p", resourcePath);
        return (bool)await cmd.ExecuteScalarAsync(ct);
    }
}
