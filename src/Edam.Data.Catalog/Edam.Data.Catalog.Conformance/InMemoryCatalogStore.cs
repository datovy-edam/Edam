using Edam.Data.Catalog.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Edam.Data.Catalog.Conformance;

/// <summary>
/// In-memory catalog <see cref="ICatalogStore"/> (reference provider for the provider-conformance
/// suite, ADR-0006). Runs the same scenario as PostgreSQL and proves back-ends swap without caller
/// changes — this one is runnable offline in the sandbox.
/// </summary>
public sealed class InMemoryCatalogStore : ICatalogStore
{
    private readonly ConcurrentDictionary<Guid, ContainerInfo> _containers = new();
    private readonly ConcurrentDictionary<Guid, ItemInfo> _items = new();
    private readonly ConcurrentDictionary<Guid, ItemDataInfo> _itemData = new();
    private readonly ConcurrentDictionary<string, Guid> _containerByName = new();
    private readonly ConcurrentDictionary<string, Guid> _itemByPath = new();

    /// <summary>LM-2a: item index key — <b>container + path</b>, so two containers may share a path.</summary>
    private static string ItemKey(Guid containerId, string? path)
        => containerId.ToString("N") + "|" + (path ?? string.Empty);

    public string DescribeStore() => "in-memory";

    public Task<ContainerInfo?> GetContainerAsync(string? containerId, bool checkId = true, CancellationToken ct = default)
        => Task.FromResult(_containerByName.TryGetValue(containerId ?? string.Empty, out var id) && _containers.TryGetValue(id, out var c) ? c : null);

    public ContainerInfo? GetContainer(string? containerId, bool checkId = true) => GetContainerAsync(containerId, checkId).Result;

    public Task<ContainerInfo?> GetContainerAsync(Guid containerId, CancellationToken ct = default)
        => Task.FromResult(_containers.TryGetValue(containerId, out var c) ? c : null);

    public ContainerInfo? GetContainer(Guid containerId) => GetContainerAsync(containerId).Result;

    public Task<IReadOnlyList<ContainerInfo>> GetContainersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ContainerInfo>>(_containers.Values.OrderBy(c => c.ContainerId).ToList());

    public IReadOnlyList<ContainerInfo> GetContainers() => GetContainersAsync().Result;

    public async Task<ContainerInfo> EnlistContainerAsync(string containerId, string description, string? baseUri = null, ContainerType type = ContainerType.DataContext, CancellationToken ct = default)
    {
        if (_containerByName.TryGetValue(containerId, out var existingId) && _containers.TryGetValue(existingId, out var existing))
            return existing;
        var @new = new ContainerInfo(Guid.NewGuid(), containerId, description, type, baseUri ?? string.Empty);
        await Task.Yield();
        _containers[@new.Id] = @new;
        _containerByName[containerId] = @new.Id;
        return @new;
    }

    public ContainerInfo EnlistContainer(string containerId, string description, string? baseUri = null, ContainerType type = ContainerType.DataContext)
        => EnlistContainerAsync(containerId, description, baseUri, type).Result;

    public Task<ContainerInfo> SetContainerAsync(string sessionId, string containerId, CancellationToken ct = default)
        => Task.FromResult(GetContainer(containerId) ?? EnlistContainer(containerId, "Default", null));

    public ContainerInfo SetContainer(string sessionId, string containerId) => SetContainerAsync(sessionId, containerId).Result;

    public async Task<ContainerInfo> DelistContainerAsync(string containerId, CancellationToken ct = default)
    {
        if (_containerByName.TryRemove(containerId, out var id) && _containers.TryRemove(id, out var c)) return c;
        await Task.Yield();
        throw new InvalidOperationException($"Container '{containerId}' not found.");
    }

    public ContainerInfo DelistContainer(string containerId) => DelistContainerAsync(containerId).Result;

    public async Task<ItemInfo?> GetItemAsync(Guid itemId, CancellationToken ct = default)
    {
        await Task.Yield();
        return _items.TryGetValue(itemId, out var i) ? i : null;
    }

    public ItemInfo? GetItem(Guid itemId) => GetItemAsync(itemId).Result;

    public async Task<ItemInfo?> GetItemByPathAsync(string path, CancellationToken ct = default)
    {
        await Task.Yield();
        // LM-2a: legacy container-blind lookup (ambiguous once two containers share a path)
        return _items.Values.FirstOrDefault(i =>
            string.Equals(i.FullPath, path ?? string.Empty, StringComparison.OrdinalIgnoreCase));
    }

    public ItemInfo? GetItemByPath(string name) => GetItemByPathAsync(name).Result;

    public async Task<IReadOnlyList<ItemInfo>> GetContainerItemsAsync(Guid containerId, CancellationToken ct = default)
    {
        await Task.Yield();
        return _items.Values.Where(i => i.ContainerId == containerId).OrderBy(i => i.FullPath).ToList();
    }

    public IReadOnlyList<ItemInfo> GetContainerItems(Guid containerId) => GetContainerItemsAsync(containerId).Result;

    public async Task<ItemInfo?> GetContainerRootItemAsync(Guid id, CancellationToken ct = default)
    {
        var items = await GetContainerItemsAsync(id, ct);
        return items.FirstOrDefault(i => i.FullPath == "/") ?? items.FirstOrDefault();
    }

    public ItemInfo? GetContainerRootItem(Guid containerId) => GetContainerRootItemAsync(containerId).Result;

    public async Task<IReadOnlyList<ItemInfo>> GetBranchAsync(string? path = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path)) return _items.Values.OrderBy(i => i.FullPath).ToList();
        var prefix = path.EndsWith("/") ? path : path + "/";
        return _items.Values.Where(i => i.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).OrderBy(i => i.FullPath).ToList();
    }

    public IReadOnlyList<ItemInfo> GetBranch(string? path = null) => GetBranchAsync(path).Result;

    public async Task<ItemInfo> AddItemAsync(ItemInfo item, CancellationToken ct = default)
    {
        await Task.Yield();
        _items[item.Id] = item;
        _itemByPath[ItemKey(item.ContainerId, item.FullPath)] = item.Id;
        return item;
    }

    public ItemInfo AddItem(ItemInfo item) => AddItemAsync(item).Result;

    public Task<ItemInfo> CreateBranchAsync(string path, string? description = null, Guid? containerId = null, CancellationToken ct = default)
    {
        // LM-2a: match INSIDE the container — never return another container's branch for this path
        var existing = containerId is null
            ? GetItemByPath(path)
            : (_itemByPath.TryGetValue(ItemKey(containerId.Value, path), out var id) &&
               _items.TryGetValue(id, out var found) ? found : null);
        if (existing is not null) return Task.FromResult(existing);
        var name = System.IO.Path.GetFileName(path.TrimEnd('/'));
        var item = new ItemInfo(Guid.NewGuid(), containerId ?? Guid.Empty, path, string.IsNullOrEmpty(name) ? path : name, description, ItemType.Branch, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        return Task.FromResult(AddItem(item));
    }

    public ItemInfo CreateBranch(string path, string? description = null, Guid? containerId = null) => CreateBranchAsync(path, description, containerId).Result;

    public ItemInfo? CreateRootItem(Guid? containerId = null) => CreateBranch("/", "root", containerId);

    public async Task<bool> DeleteItemAsync(Guid itemId, CancellationToken ct = default)
    {
        await Task.Yield();
        var removed = _items.TryRemove(itemId, out var it);
        if (removed) { _itemByPath.TryRemove(ItemKey(it.ContainerId, it.FullPath), out _); return true; }
        return false;
    }

    public bool DeleteItem(Guid itemId) => DeleteItemAsync(itemId).Result;

    public async Task<IReadOnlyList<ItemDataInfo>> GetItemDataAsync(Guid itemId, CancellationToken ct = default)
    {
        await Task.Yield();
        return _itemData.Values.Where(d => d.ItemId == itemId).OrderBy(d => d.Name).ToList();
    }

    public IReadOnlyList<ItemDataInfo> GetItemData(Guid itemId) => GetItemDataAsync(itemId).Result;

    public async Task<ItemDataInfo> AddItemAsync(ItemDataInfo item, CancellationToken ct = default)
    {
        await Task.Yield();
        _itemData[item.Id] = item;
        return item;
    }

    public ItemDataInfo AddItem(ItemDataInfo item) => AddItemAsync(item).Result;

    public async Task<ItemDataInfo?> GetDataAsync(Guid dataId, CancellationToken ct = default)
    {
        await Task.Yield();
        return _itemData.TryGetValue(dataId, out var d) ? d : null;
    }

    public ItemDataInfo? GetData(Guid dataId) => GetDataAsync(dataId).Result;

    public async Task<ItemDataInfo?> GetDataByNameAsync(Guid itemId, string name, CancellationToken ct = default)
    {
        await Task.Yield();
        return _itemData.Values.FirstOrDefault(d => d.ItemId == itemId && d.Name == name);
    }

    public ItemDataInfo? GetDataByName(Guid itemId, string name) => GetDataByNameAsync(itemId, name).Result;

    public async Task<ContentTypeInfo?> GetContentTypeAsync(string contentTypeId, CancellationToken ct = default)
    {
        await Task.Yield();
        return new ContentTypeInfo(contentTypeId);
    }

    public ContentTypeInfo? GetContentType(string contentTypeId) => GetContentTypeAsync(contentTypeId).Result;

    public ItemDataInfo? CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, byte[]? dataValue = null)
        => CreateDataLeaf(item, name, dataId, dataValue is null ? null : Convert.ToBase64String(dataValue));

    public ItemDataInfo? CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, string? dataValue = null)
        => AddItem(new ItemDataInfo(dataId ?? Guid.NewGuid(), item.Id, name, string.Empty, "default", dataValue));

    public Task<bool> DeleteDataAsync(Guid dataId, CancellationToken ct = default)
    {
        return Task.FromResult(_itemData.TryRemove(dataId, out _));
    }

    public bool DeleteData(Guid dataId) => DeleteDataAsync(dataId).Result;

    public Task<bool> DeleteItemDataAsync(Guid itemId, CancellationToken ct = default)
    {
        foreach (var kvp in _itemData.ToArray())
            if (kvp.Value.ItemId == itemId) _itemData.TryRemove(kvp.Key, out _);
        return Task.FromResult(true);
    }

    public bool DeleteItemData(Guid itemId) => DeleteItemDataAsync(itemId).Result;
}
