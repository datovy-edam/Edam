using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Edam.Services.Contracts;

namespace Edam.Services.Core;

/// <summary>BL-6.6: in-memory catalog store (Wave-1 baseline, before the DB is up).</summary>
public sealed class InMemoryCatalogStore : ICatalogStore
{
    private static readonly CatalogAsset[] Seed =
    {
        new("edam://asset/0001", "EDAM Core Schema", "schema", DateTimeOffset.UtcNow.AddDays(-1)),
        new("edam://asset/0002", "FIBO Reference Data", "reference", DateTimeOffset.UtcNow.AddHours(-2)),
        new("edam://asset/0003", "Booklet Mapping Sample", "booklet", DateTimeOffset.UtcNow.AddMinutes(-30)),
    };

    public Task<IReadOnlyList<CatalogAsset>> GetAssetsAsync(int top, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CatalogAsset>>(Seed.Take(top).ToArray());

    public string DescribeStore() => "in-memory";
}

/// <summary>
/// BL-6.6: PostgreSQL-backed catalog store (Npgsql). Activated when the "catalog" connection
/// string is configured and reachable. Runtime verification needs the Postgres container (Docker);
/// the code/DI wiring is build-verified offline. Degrades to an empty result (clearly logged)
/// when unconfigured so the mesh stays up until the DB lands.
/// </summary>
public sealed class PostgresCatalogStore : ICatalogStore
{
    private readonly string? _connection;
    private readonly ILogger<PostgresCatalogStore> _log;

    public PostgresCatalogStore(IConfiguration config, ILogger<PostgresCatalogStore> log)
    {
        _connection = config["ConnectionStrings:catalog"];
        _log = log;
    }

    public string DescribeStore() =>
        string.IsNullOrWhiteSpace(_connection) ? "postgres(unconfigured)" : "postgres";

    public async Task<IReadOnlyList<CatalogAsset>> GetAssetsAsync(int top, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_connection))
        {
            _log.LogWarning("Postgres catalog store is unconfigured (no 'catalog' connection string); returning empty");
            return Array.Empty<CatalogAsset>();
        }
        try
        {
            await using var conn = new NpgsqlConnection(_connection);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT id, name, type, updated_at FROM edam.catalog_assets " +
                "ORDER BY updated_at DESC LIMIT @top", conn);
            cmd.Parameters.AddWithValue("top", top);

            var list = new List<CatalogAsset>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new CatalogAsset(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    reader.GetFieldValue<DateTimeOffset>(3)));
            }
            return list;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Postgres catalog store query failed ({Store})", DescribeStore());
            return Array.Empty<CatalogAsset>();
        }
    }
}
