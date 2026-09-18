using System.Threading;
using System.Threading.Tasks;
using Edam.Services.Contracts;

namespace Edam.Services.Core;

/// <summary>
/// BL-6.6: in-memory catalog store — the Wave-1 baseline / dev stand-in, and since the BL-7.5 seam
/// swap the <b>registered fallback</b> (active only when no catalog target/connection/root is
/// configured). The real back-end is the catalog platform behind DI
/// (see <see cref="CatalogPlatformStore"/>).
/// </summary>
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
