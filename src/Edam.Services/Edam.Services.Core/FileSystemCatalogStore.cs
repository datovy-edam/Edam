using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Edam.Services.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Edam.Services.Core;

/// <summary>
/// BL-7.5: catalog assets surfaced from a real file system (FileSystem target first) behind the
/// <see cref="ICatalogStore"/> persistence boundary. Self-contained (System.IO) — no EF, no DB, no
/// infrastructure leak — so it is the decoupled/FileSystem-first store the Wave-1 surface can switch
/// to by config while callers (WebApi <c>/catalog/items</c>, CLI, health) stay unchanged.
/// Resource addressing is **path-based** (an asset's Id is its file path), matching the catalog's
/// path/URI content model. Degrades to empty (logged) when unconfigured/error so the mesh stays up,
/// the same safe pattern as <see cref="PostgresCatalogStore"/>.
/// </summary>
public sealed class FileSystemCatalogStore : ICatalogStore
{
    private readonly string? _root;
    private readonly ILogger<FileSystemCatalogStore> _log;

    public FileSystemCatalogStore(IConfiguration config, ILogger<FileSystemCatalogStore> log)
    {
        _root = config["Edam:CatalogRoot"] ?? config["DefaultRootFileFolder"];
        _log = log;
    }

    public string DescribeStore() =>
        string.IsNullOrWhiteSpace(_root) ? "filesystem(unconfigured)" : "filesystem";

    public Task<IReadOnlyList<CatalogAsset>> GetAssetsAsync(int top = 50, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_root) || !Directory.Exists(_root))
        {
            _log.LogWarning("File system catalog store root missing/unconfigured ({Root}); returning empty", _root);
            return Task.FromResult<IReadOnlyList<CatalogAsset>>(Array.Empty<CatalogAsset>());
        }

        try
        {
            var assets = new List<CatalogAsset>();
            CollectFiles(_root, assets, top, ct);
            return Task.FromResult<IReadOnlyList<CatalogAsset>>(assets);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "File system catalog store enumeration failed ({Root})", _root);
            return Task.FromResult<IReadOnlyList<CatalogAsset>>(Array.Empty<CatalogAsset>());
        }
    }

    private static void CollectFiles(string dir, List<CatalogAsset> assets, int top, CancellationToken ct)
    {
        if (assets.Count >= top) return;

        ct.ThrowIfCancellationRequested();
        foreach (var file in Directory.EnumerateFiles(dir))
        {
            ct.ThrowIfCancellationRequested();
            assets.Add(new CatalogAsset(
                file,
                Path.GetFileName(file),
                "file",
                new DateTimeOffset(File.GetLastWriteTimeUtc(file))));
            if (assets.Count >= top) return;
        }

        foreach (var subDir in Directory.EnumerateDirectories(dir))
        {
            CollectFiles(subDir, assets, top, ct);
        }
    }
}
