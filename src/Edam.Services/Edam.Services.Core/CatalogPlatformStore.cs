using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Edam.Services.Contracts;
using Microsoft.Extensions.Logging;
using platform = Edam.Data.Catalog.Contracts;

namespace Edam.Services.Core;

/// <summary>
/// BL-7.5 seam swap: adapts the <b>real catalog platform</b> store
/// (<c>Edam.Data.Catalog</c> — PostgreSQL/FileSystem behind DI, BL-7.2/BL-7.4) onto the Wave-1
/// catalog/asset persistence boundary (<see cref="ICatalogStore"/>), so WebApi
/// <c>/catalog/items</c>, the CLI, and the health surfaces reflect the real catalog instead of a
/// self-contained stand-in — with callers unchanged.
/// <para>
/// Resource addressing stays <b>path-based</b>: a catalog item's Id is its
/// <c>ItemInfo.FullPath</c>. Degrades to an empty result (logged) when the backing store is
/// unreachable, so the mesh stays up — the same safe pattern the previous stand-ins used.
/// </para>
/// </summary>
public sealed class CatalogPlatformStore : ICatalogStore
{
    private readonly platform.ICatalogStore _store;
    private readonly ILogger<CatalogPlatformStore>? _log;
    private readonly string _identity;

    public CatalogPlatformStore(platform.ICatalogStore store, ILogger<CatalogPlatformStore>? log = null)
    {
        _store = store;
        _log = log;
        // The provider reports its own identity; the adapter never names (or knows) it.
        _identity = $"catalog:{store.DescribeStore()}";
    }

    public string DescribeStore() => _identity;

    public Task<IReadOnlyList<CatalogAsset>> GetAssetsAsync(int top = 50, CancellationToken ct = default)
    {
        if (top <= 0)
            return Task.FromResult<IReadOnlyList<CatalogAsset>>(Array.Empty<CatalogAsset>());

        try
        {
            var assets = new List<CatalogAsset>();
            foreach (var container in _store.GetContainers())
            {
                ct.ThrowIfCancellationRequested();
                foreach (var item in _store.GetContainerItems(container.Id))
                {
                    assets.Add(new CatalogAsset(
                        item.FullPath, item.Name, item.Type.ToString(), item.UpdatedDate));
                }
            }

            return Task.FromResult<IReadOnlyList<CatalogAsset>>(
                assets.OrderByDescending(a => a.UpdatedAt).Take(top).ToArray());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "Catalog platform store enumeration failed ({Store})", DescribeStore());
            return Task.FromResult<IReadOnlyList<CatalogAsset>>(Array.Empty<CatalogAsset>());
        }
    }
}
