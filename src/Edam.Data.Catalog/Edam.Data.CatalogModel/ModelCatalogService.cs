using Edam.DataObjects.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
// BL-7.2 step 4: a pure-Model in-memory catalog service. EF-free, no CatalogDb.
// Used as the default container/catalog provider for consumers that must not
// depend on the EF CatalogDb layer (e.g. Edam.Data.CatalogServiceClient's
// file-system client and CatalogInfo). Enlisted Containers are held in memory;
// this default does NOT persist. It is a transitional stand-in only — the
// durable/default store is the Npgsql ICatalogStore provider (BL-7.2/BL-7.4).
namespace Edam.Data.CatalogModel;

/// <summary>
/// A pure-Model, in-memory <see cref="ICatalogService"/> used as a fallback
/// default catalog so consumers can enumerate/enlist Containers without the
/// EF <c>Edam.Data.CatalogDb</c> layer.
/// </summary>
public sealed class ModelCatalogService : ICatalogService
{
   private static readonly Lazy<ModelCatalogService> _default =
      new(() => new ModelCatalogService());

   /// <summary>Shared default in-memory catalog service.</summary>
   public static ModelCatalogService Default => _default.Value;

   private readonly ModelCatalogContainer _container = new();
   private readonly ModelCatalogItem _item = new();
   private readonly ModelCatalogItemData _itemData = new();
   private ContainerInfo? _defaultContainer;

   public CatalogInfo? Catalog { get; set; }
   public object Instance { get; } = new object();
   public string DefaultContainerId { get; set; } = ContainerInfo.CONTAINER_ID_DEFAULT;

   public ContainerInfo DefaultContainer
   {
      get => _defaultContainer ??=
         _container.EnlistContainer(DefaultContainerId, "Default");
      set => _defaultContainer = value;
   }

   public ContainerInfo CurrentContainer
   {
      get => _defaultContainer ??=
         _container.EnlistContainer(DefaultContainerId, "Default");
      set => _defaultContainer = value;
   }

   public ICatalogContainer Container { get => _container; set { } }
   public ICatalogItem Item { get => _item; set { } }
   public ICatalogItemData ItemData { get => _itemData; set { } }
}

/// <summary>In-memory <see cref="IEnumerable{T}"/> container store.</summary>
internal sealed class ModelCatalogContainer : ICatalogContainer
{
   private readonly Dictionary<string, ContainerInfo> _containers = new();
   private readonly object _sync = new();

   public Task<ContainerInfo> GetContainerAsync(string? containerId, bool checkId = true)
   {
      lock (_sync)
      {
         return Task.FromResult(GetContainer(containerId, checkId));
      }
   }

   public ContainerInfo GetContainer(string? containerId, bool checkId = true)
   {
      lock (_sync)
      {
         if (String.IsNullOrEmpty(containerId))
         {
            var first = _containers.Values.FirstOrDefault();
            return first ?? new ContainerInfo
            { ContainerId = ContainerInfo.CONTAINER_ID_DEFAULT };
         }
         return _containers.TryGetValue(containerId, out var c)
            ? c
            : new ContainerInfo { ContainerId = containerId };
      }
   }

   public ContainerInfo GetContainer(Guid containerId)
   {
      lock (_sync)
      {
         return _containers.Values.FirstOrDefault(c => c.Id == containerId)
            ?? new ContainerInfo();
      }
   }

   public ContainerInfo SetContainer(string sessionId, string containerId)
      => GetContainer(containerId);

   public ContainerInfo EnlistContainer(
      string containerId, string description, string? baseURI = null,
      ContainerType type = ContainerType.DataContext)
   {
      lock (_sync)
      {
         if (!_containers.TryGetValue(containerId, out var c))
         {
            c = new ContainerInfo
            {
               Id = Guid.NewGuid(),
               ContainerId = containerId,
               Description = description,
               ContainerURI = baseURI ?? "",
               ContainerType = type
            };
            _containers.Add(containerId, c);
         }
         return c;
      }
   }

   public ContainerInfo DelistContainer(string containerId)
   {
      lock (_sync)
      {
         _containers.Remove(containerId);
         return new ContainerInfo { ContainerId = containerId };
      }
   }

   public Task<List<ContainerInfo>> GetContainersAsync()
   {
      lock (_sync)
      {
         return Task.FromResult(_containers.Values.ToList());
      }
   }

   public List<ContainerInfo> GetContainers()
   {
      lock (_sync)
      {
         return _containers.Values.ToList();
      }
   }
}

/// <summary>In-memory <see cref="ICatalogItem"/> store (minimal).</summary>
internal sealed class ModelCatalogItem : ICatalogItem
{
   private readonly List<ItemInfo> _items = new();
   private readonly object _sync = new();

   public async Task<ItemInfo> CreateBranchAsync(
      string path, string? description = null, Guid? containerId = null)
   {
      var item = await AddItemAsync(new ItemInfo
      {
         FullPath = path,
         Description = description,
         ContainerId = containerId ?? Guid.Empty,
         ItemType = Edam.DataObjects.Trees.TreeItemType.Branch
      });
      return item;
   }

   public ItemInfo CreateBranch(string path, string? description = null, Guid? containerId = null)
      => CreateBranchAsync(path, description, containerId).Result;

   public ItemInfo CreateRootItem(Guid? containerId = null)
      => throw new NotSupportedException(
         "ModelCatalogItem.CreateRootItem is provider-side.");

   public Task<ItemInfo> GetContainerRootItemAsync(Guid id)
   {
      lock (_sync)
      {
         return Task.FromResult(_items.FirstOrDefault(i => i.ContainerId == id)
            ?? new ItemInfo { ContainerId = id, FullPath = "/" });
      }
   }

   public ItemInfo GetContainerRootItem(Guid containerId)
      => GetContainerRootItemAsync(containerId).Result;

   public List<ItemInfo> GetContainerItems(Guid containerId)
   {
      lock (_sync) { return _items.Where(i => i.ContainerId == containerId).ToList(); }
   }

   public ItemInfo? GetItem(Guid itemId)
   {
      lock (_sync) { return _items.FirstOrDefault(i => i.Id == itemId); }
   }

   public Task<ItemInfo> GetItemByPathAsync(string path)
   {
      lock (_sync)
      {
         return Task.FromResult(_items.FirstOrDefault(i => i.FullPath == path)
            ?? new ItemInfo { FullPath = path });
      }
   }

   public ItemInfo GetItemByPath(string name) => GetItemByPathAsync(name).Result;

   public RequestStatus DeleteItem(Guid itemId)
   {
      lock (_sync)
      {
         var removed = _items.RemoveAll(i => i.Id == itemId) > 0;
         return removed ? RequestStatus.Completed : RequestStatus.Failed;
      }
   }

   public Task<List<ItemInfo?>> GetBranchAsync(string? path = null)
   {
      lock (_sync)
      {
         var spath = path?.ToLower() ?? "/";
         return Task.FromResult(_items.Where(i =>
            i.FullPath.ToLower().StartsWith(spath)).Cast<ItemInfo?>().ToList());
      }
   }

   public List<ItemInfo?> GetBranch(string? path = null) => GetBranchAsync(path).Result;

   public async Task<ItemInfo> AddItemAsync(ItemInfo item)
   {
      await Task.CompletedTask;
      lock (_sync)
      {
         var existing = _items.FirstOrDefault(i => i.Id == item.Id);
         if (existing != null)
         {
            existing.FullPath = item.FullPath;
            existing.Description = item.Description;
            existing.ItemType = item.ItemType;
            return existing;
         }
         _items.Add(item);
         return item;
      }
   }

   public ItemInfo AddItem(ItemInfo item) => AddItemAsync(item).Result;
}

/// <summary>In-memory <see cref="ICatalogItemData"/> store (minimal).</summary>
internal sealed class ModelCatalogItemData : ICatalogItemData
{
   private readonly List<ItemDataInfo> _data = new();
   private readonly object _sync = new();

   public ContentTypeInfo GetContentType(string contentTypeId)
      => throw new NotSupportedException(
         "ModelCatalogItemData.GetContentType is provider-side.");

   public ItemDataInfo CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, byte[] dataValue = null)
   {
      var d = new ItemDataInfo { ItemId = item.Id, Name = name, Id = dataId ?? Guid.NewGuid(), Data = dataValue };
      return d;
   }

   public ItemDataInfo CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, string dataValue = null)
   {
      var d = new ItemDataInfo { ItemId = item.Id, Name = name, Id = dataId ?? Guid.NewGuid(), DataText = dataValue };
      return d;
   }

   public Task<ItemDataInfo> AddItemAsync(ItemDataInfo item)
   {
      return Task.FromResult(AddItem(item));
   }

   public ItemDataInfo AddItem(ItemDataInfo item)
   {
      lock (_sync)
      {
         var existing = _data.FirstOrDefault(d => d.Id == item.Id);
         if (existing != null)
         {
            existing.Name = item.Name;
            existing.Data = item.Data;
         }
         else
         {
            _data.Add(item);
         }
         return item;
      }
   }

   public ItemDataInfo GetDataByName(Guid itemId, string name)
   {
      lock (_sync)
      {
         return _data.FirstOrDefault(d => d.ItemId == itemId && d.Name == name)
            ?? new ItemDataInfo { ItemId = itemId, Name = name };
      }
   }

   public ItemDataInfo GetData(Guid dataId)
   {
      lock (_sync)
      {
         return _data.FirstOrDefault(d => d.Id == dataId) ?? new ItemDataInfo();
      }
   }

   public Task<List<ItemDataInfo>> GetItemDataAsync(Guid itemId)
   {
      lock (_sync)
      {
         return Task.FromResult(_data.Where(d => d.ItemId == itemId).ToList());
      }
   }

   public List<ItemDataInfo> GetItemData(Guid itemId) => GetItemDataAsync(itemId).Result;

   public RequestStatus DeleteItemData(Guid itemId)
   {
      lock (_sync)
      {
         var removed = _data.RemoveAll(d => d.ItemId == itemId) > 0;
         return removed ? RequestStatus.Completed : RequestStatus.Failed;
      }
   }

   public RequestStatus DeleteData(Guid dataId)
   {
      lock (_sync)
      {
         var removed = _data.RemoveAll(d => d.Id == dataId) > 0;
         return removed ? RequestStatus.Completed : RequestStatus.Failed;
      }
   }
}
