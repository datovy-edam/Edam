using System.Threading;
using System.Threading.Tasks;

namespace Edam.Services.Contracts;

/// <summary>BL-6.6: a catalog asset row surfaced by the store (the persistence boundary).</summary>
public sealed record CatalogAsset(
    string Id, string Name, string Type, DateTimeOffset UpdatedAt);

/// <summary>
/// BL-6.6: catalog/asset persistence boundary. In-memory today (BL-6.2 baseline);
/// BL-7.5: backed by the <b>real catalog platform</b> (<c>Edam.Data.Catalog</c> behind DI) when a target is configured; the in-memory store is the registered fallback.
/// The store name is exposed so the environment overview (/health/report, BL-6.4) can show
/// which backing store is live.
/// </summary>
public interface ICatalogStore
{
    Task<IReadOnlyList<CatalogAsset>> GetAssetsAsync(int top = 50, CancellationToken ct = default);
    string DescribeStore();
}
