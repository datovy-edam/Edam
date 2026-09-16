using Edam.Data.Catalog.Contracts;
using System.Text.Json;

namespace Edam.Data.Catalog.FileSystem;

/// <summary>
/// FileSystem-backed catalog <b>metadata</b> store (BL-7.2, ADR-0006/0007 — the back-end is a
/// variable). Persists the catalog (containers / items / item-data) as a JSON document under a
/// root directory, so a Container targets the real file system instead of a relational DB / EF.
/// Resource addressing is path-based (<see cref="ItemInfo.FullPath"/>). Binary content is handled
/// by the sibling <see cref="FileSystemContentStore"/>. Durable across store instances.
/// </summary>
public sealed class FileSystemCatalogStore : ICatalogStore
{
   private readonly string _root;
   private readonly string _statePath;
   private readonly object _lock = new();

   private readonly Dictionary<string, ContainerInfo> _containers = new();
   private readonly Dictionary<string, ItemInfo> _itemsByPath = new();
   private readonly Dictionary<Guid, ItemInfo> _itemsById = new();
   private readonly Dictionary<Guid, ItemDataInfo> _itemData = new();

   public FileSystemCatalogStore(string rootPath)
   {
      _root = Path.GetFullPath(rootPath);
      Directory.CreateDirectory(_root);
      _statePath = Path.Combine(_root, "catalog-state.json");
      Load();
   }

   public string RootPath => _root;
   public string DescribeStore() => "filesystem";

   // ---- persistence ---------------------------------------------------------

   private void Save()
   {
      lock (_lock)
      {
         var state = new FsState
         {
            Containers = _containers.Values.Select(Map).ToList(),
            Items = _itemsById.Values.Select(Map).ToList(),
            ItemData = _itemData.Values.Select(Map).ToList()
         };
         var json = JsonSerializer.Serialize(state);
         var tmp = _statePath + ".tmp";
         File.WriteAllText(tmp, json);
         File.Move(tmp, _statePath, overwrite: true);
      }
   }

   private void Load()
   {
      lock (_lock)
      {
         _containers.Clear();
         _itemsByPath.Clear();
         _itemsById.Clear();
         _itemData.Clear();
         if (!File.Exists(_statePath)) return;
         var state = JsonSerializer.Deserialize<FsState>(File.ReadAllText(_statePath));
         if (state is null) return;
         foreach (var c in state.Containers)
         {
            var container = ToContainer(c);
            _containers[container.ContainerId] = container;
         }
         foreach (var i in state.Items)
         {
            var item = ToItem(i);
            _itemsById[item.Id] = item;
            _itemsByPath[item.FullPath] = item;
         }
         foreach (var d in state.ItemData)
         {
            var data = ToItemData(d);
            _itemData[data.Id] = data;
         }
      }
   }

   private static FsContainer Map(ContainerInfo c)
      => new() { Id = c.Id, ContainerId = c.ContainerId, Description = c.Description, ContainerType = (int)c.ContainerType, ContainerUri = c.ContainerUri, ContentType = c.ContentType };

   private static FsItem Map(ItemInfo i)
      => new() { Id = i.Id, ContainerId = i.ContainerId, FullPath = i.FullPath, Name = i.Name, Description = i.Description, Type = (int)i.Type, CreatedDate = i.CreatedDate, UpdatedDate = i.UpdatedDate };

   private static FsItemData Map(ItemDataInfo d)
      => new() { Id = d.Id, ItemId = d.ItemId, Name = d.Name, ContentTypeId = d.ContentTypeId, PartitionId = d.PartitionId, Value = d.Value };

   private static ContainerInfo ToContainer(FsContainer c)
      => new(c.Id, c.ContainerId, c.Description, (ContainerType)c.ContainerType, c.ContainerUri, c.ContentType);

   private static ItemInfo ToItem(FsItem i)
      => new(i.Id, i.ContainerId, i.FullPath, i.Name, i.Description, (ItemType)i.Type, i.CreatedDate, i.UpdatedDate);

   private static ItemDataInfo ToItemData(FsItemData d)
      => new(d.Id, d.ItemId, d.Name, d.ContentTypeId, d.PartitionId, d.Value);

   // ---- containers ----------------------------------------------------------

   public Task<ContainerInfo?> GetContainerAsync(string? containerId, bool checkId = true, CancellationToken ct = default)
      => Task.FromResult(_containers.TryGetValue(containerId ?? string.Empty, out var c) ? c : null);

   public ContainerInfo? GetContainer(string? containerId, bool checkId = true)
      => GetContainerAsync(containerId, checkId).GetAwaiter().GetResult();

   public ContainerInfo? GetContainer(Guid containerId)
      => _containers.Values.FirstOrDefault(c => c.Id == containerId);

   public ContainerInfo EnlistContainer(string containerId, string description, string? baseUri = null, ContainerType type = ContainerType.DataContext)
   {
      if (_containers.TryGetValue(containerId, out var existing)) return existing;
      var @new = new ContainerInfo(Guid.NewGuid(), containerId, description, type, baseUri ?? string.Empty);
      _containers[containerId] = @new;
      Save();
      return @new;
   }

   public ContainerInfo SetContainer(string sessionId, string containerId)
      => GetContainer(containerId) ?? EnlistContainer(containerId, "Default", null);

   public ContainerInfo DelistContainer(string containerId)
   {
      if (!_containers.TryGetValue(containerId, out var c))
         throw new InvalidOperationException($"Container '{containerId}' not found.");
      _containers.Remove(containerId);
      Save();
      return c;
   }

   public Task<IReadOnlyList<ContainerInfo>> GetContainersAsync(CancellationToken ct = default)
      => Task.FromResult<IReadOnlyList<ContainerInfo>>(_containers.Values.OrderBy(c => c.ContainerId).ToList());

   public IReadOnlyList<ContainerInfo> GetContainers()
      => _containers.Values.OrderBy(c => c.ContainerId).ToList();

   // ---- items ---------------------------------------------------------------

   public ItemInfo? GetItem(Guid itemId)
      => _itemsById.TryGetValue(itemId, out var i) ? i : null;

   public Task<ItemInfo?> GetItemByPathAsync(string path, CancellationToken ct = default)
      => Task.FromResult(GetItemByPath(path));

   public ItemInfo? GetItemByPath(string name)
      => _itemsByPath.TryGetValue(name ?? string.Empty, out var i) ? i : null;

   public Task<IReadOnlyList<ItemInfo>> GetContainerItemsAsync(Guid containerId, CancellationToken ct = default)
      => Task.FromResult<IReadOnlyList<ItemInfo>>(_itemsById.Values.Where(i => i.ContainerId == containerId).OrderBy(i => i.FullPath).ToList());

   public IReadOnlyList<ItemInfo> GetContainerItems(Guid containerId)
      => _itemsById.Values.Where(i => i.ContainerId == containerId).OrderBy(i => i.FullPath).ToList();

   public Task<ItemInfo?> GetContainerRootItemAsync(Guid id, CancellationToken ct = default)
      => Task.FromResult(GetContainerRootItem(id));

   public ItemInfo? GetContainerRootItem(Guid containerId)
   {
      var items = GetContainerItems(containerId);
      return items.FirstOrDefault(i => i.FullPath == "/") ?? items.FirstOrDefault();
   }

   public Task<IReadOnlyList<ItemInfo>> GetBranchAsync(string? path = null, CancellationToken ct = default)
      => Task.FromResult(GetBranch(path));

   public IReadOnlyList<ItemInfo> GetBranch(string? path = null)
   {
      if (string.IsNullOrWhiteSpace(path)) return _itemsById.Values.OrderBy(i => i.FullPath).ToList();
      var prefix = path.EndsWith("/") ? path : path + "/";
      return _itemsById.Values.Where(i => i.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).OrderBy(i => i.FullPath).ToList();
   }

   public async Task<ItemInfo> AddItemAsync(ItemInfo item, CancellationToken ct = default)
   {
      _itemsById[item.Id] = item;
      _itemsByPath[item.FullPath] = item;
      Save();
      return item;
   }

   public ItemInfo AddItem(ItemInfo item) => AddItemAsync(item).GetAwaiter().GetResult();

   public Task<ItemInfo> CreateBranchAsync(string path, string? description = null, Guid? containerId = null, CancellationToken ct = default)
   {
      var existing = GetItemByPath(path);
      if (existing is not null) return Task.FromResult(existing);
      var name = Path.GetFileName(path.TrimEnd('/'));
      var item = new ItemInfo(Guid.NewGuid(), containerId ?? Guid.Empty, path, string.IsNullOrEmpty(name) ? path : name, description, ItemType.Branch, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
      return Task.FromResult(AddItem(item));
   }

   public ItemInfo CreateBranch(string path, string? description = null, Guid? containerId = null)
      => CreateBranchAsync(path, description, containerId).GetAwaiter().GetResult();

   public ItemInfo? CreateRootItem(Guid? containerId = null)
      => CreateBranch("/", "root", containerId);

   public bool DeleteItem(Guid itemId)
   {
      if (!_itemsById.TryGetValue(itemId, out var it)) return false;
      _itemsById.Remove(itemId);
      _itemsByPath.Remove(it.FullPath);
      Save();
      return true;
   }

   // ---- item data -----------------------------------------------------------

   public ContentTypeInfo? GetContentType(string contentTypeId)
      => new(contentTypeId);

   public ItemDataInfo? CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, byte[]? dataValue = null)
      => CreateDataLeaf(item, name, dataId, dataValue is null ? null : Convert.ToBase64String(dataValue));

   public ItemDataInfo? CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, string? dataValue = null)
      => AddItem(new ItemDataInfo(dataId ?? Guid.NewGuid(), item.Id, name, string.Empty, "default", dataValue));

   public Task<ItemDataInfo> AddItemAsync(ItemDataInfo item, CancellationToken ct = default)
   {
      _itemData[item.Id] = item;
      Save();
      return Task.FromResult(item);
   }

   public ItemDataInfo AddItem(ItemDataInfo item) => AddItemAsync(item).GetAwaiter().GetResult();

   public ItemDataInfo? GetData(Guid dataId)
      => _itemData.TryGetValue(dataId, out var d) ? d : null;

   public ItemDataInfo? GetDataByName(Guid itemId, string name)
      => _itemData.Values.FirstOrDefault(d => d.ItemId == itemId && d.Name == name);

   public Task<IReadOnlyList<ItemDataInfo>> GetItemDataAsync(Guid itemId, CancellationToken ct = default)
      => Task.FromResult<IReadOnlyList<ItemDataInfo>>(_itemData.Values.Where(d => d.ItemId == itemId).OrderBy(d => d.Name).ToList());

   public IReadOnlyList<ItemDataInfo> GetItemData(Guid itemId)
      => _itemData.Values.Where(d => d.ItemId == itemId).OrderBy(d => d.Name).ToList();

   public bool DeleteData(Guid dataId)
   {
      var removed = _itemData.Remove(dataId);
      if (removed) Save();
      return removed;
   }

   public bool DeleteItemData(Guid itemId)
   {
      var changed = false;
      foreach (var kvp in _itemData.ToArray())
      {
         if (kvp.Value.ItemId == itemId) { _itemData.Remove(kvp.Key); changed = true; }
      }
      if (changed) Save();
      return true;
   }

   // ---- persistence DTOs (mutable, JSON-friendly) ---------------------------

   internal sealed class FsContainer { public Guid Id { get; set; } public string ContainerId { get; set; } = ""; public string Description { get; set; } = ""; public int ContainerType { get; set; } public string ContainerUri { get; set; } = ""; public string ContentType { get; set; } = "application/json"; }
   internal sealed class FsItem { public Guid Id { get; set; } public Guid ContainerId { get; set; } public string FullPath { get; set; } = ""; public string Name { get; set; } = ""; public string? Description { get; set; } public int Type { get; set; } public DateTimeOffset CreatedDate { get; set; } public DateTimeOffset UpdatedDate { get; set; } }
   internal sealed class FsItemData { public Guid Id { get; set; } public Guid ItemId { get; set; } public string Name { get; set; } = ""; public string ContentTypeId { get; set; } = ""; public string PartitionId { get; set; } = "default"; public string? Value { get; set; } }
   internal sealed class FsState { public List<FsContainer> Containers { get; set; } = new(); public List<FsItem> Items { get; set; } = new(); public List<FsItemData> ItemData { get; set; } = new(); }
}
