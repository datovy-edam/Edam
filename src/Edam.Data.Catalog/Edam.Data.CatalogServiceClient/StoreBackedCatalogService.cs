using Edam.Data.CatalogModel;
using Edam.DataObjects.Requests;
using Edam.DataObjects.Trees;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

// Contracts value records (the canonical wire/domain types). Aliased with a C- prefix
// because the Model (Edam.Data.CatalogModel) types share the same simple names.
using CICatalogStore = Edam.Data.Catalog.Contracts.ICatalogStore;
using CContainerInfo = Edam.Data.Catalog.Contracts.ContainerInfo;
using CItemInfo = Edam.Data.Catalog.Contracts.ItemInfo;
using CItemDataInfo = Edam.Data.Catalog.Contracts.ItemDataInfo;
using CContentTypeInfo = Edam.Data.Catalog.Contracts.ContentTypeInfo;
using CContainerType = Edam.Data.Catalog.Contracts.ContainerType;
using CItemType = Edam.Data.Catalog.Contracts.ItemType;

// -----------------------------------------------------------------------------
// BL-7.4: a store-backed Model catalog service. Adapts the provider-agnostic
// Contracts ICatalogStore (PostgreSQL/Npgsql today) onto the legacy Model
// ICatalogService surface the WinUI desktop path consumes, so the retired EF
// Edam.Data.CatalogDb local back-end is gone without regressing the desktop UI.
// The Model tree-builder / view models keep working; persistence is delegated to
// the ICatalogStore provider (ADR-0006/0007 — the back-end is a variable).
namespace Edam.Data.CatalogServiceClient;

/// <summary>
/// Adapts a <see cref="CICatalogStore"/> (metadata provider) to the Model service
/// surface a WinUI consumer expects.
/// </summary>
public sealed class StoreBackedCatalogService : Edam.Data.CatalogModel.ICatalogService
{
   private readonly CICatalogStore _store;
   private readonly StoreBackedContainer _container;
   private readonly StoreBackedItem _item;
   private readonly StoreBackedItemData _itemData;
   private readonly string _sessionId;
   private CatalogInfo? _catalog;
   private string _defaultContainerId = "default";
   private ContainerInfo? _defaultContainer;
   private ContainerInfo? _currentContainer;

   public StoreBackedCatalogService(CICatalogStore store, string? sessionId = null)
   {
      _store = store;
      _sessionId = sessionId ?? Guid.NewGuid().ToString();
      _container = new StoreBackedContainer(this);
      _item = new StoreBackedItem(this);
      _itemData = new StoreBackedItemData(this);
   }

   internal CICatalogStore Store => _store;
   internal string SessionId => _sessionId;

   public CatalogInfo? Catalog { get => _catalog; set => _catalog = value; }
   public object Instance { get; } = new object();

   public string DefaultContainerId
   {
      get => _defaultContainerId;
      set => _defaultContainerId = value;
   }

   public ContainerInfo DefaultContainer
   {
      get => _defaultContainer ?? ContainerMapper.ToModel(_store.GetContainer(string.Empty)) ?? new ContainerInfo { ContainerId = _defaultContainerId };
      set => _defaultContainer = value;
   }

   public ContainerInfo CurrentContainer
   {
      get => _currentContainer ?? DefaultContainer;
      set => _currentContainer = value;
   }

   public Edam.Data.CatalogModel.ICatalogContainer Container { get => _container; set { } }
   public Edam.Data.CatalogModel.ICatalogItem Item { get => _item; set { } }
   public Edam.Data.CatalogModel.ICatalogItemData ItemData { get => _itemData; set { } }
}

// ---------------------------------------------------------------------
// Model <-> Contracts value mapping (TreeItemType and ItemType/ContainerType
// numeric values align 1:1, so direct casts are correct for 0..2).
internal static class ContainerMapper
{
   internal static CContainerInfo ToContract(ContainerInfo m)
      => new(m.Id, m.ContainerId, m.Description ?? "", (CContainerType)m.ContainerType, m.ContainerURI ?? "", m.ContentType ?? "application/json");

   internal static ContainerInfo? ToModel(CContainerInfo? c)
   {
      if (c is null) return null;
      return new ContainerInfo
      {
         Id = c.Id,
         ContainerId = c.ContainerId,
         Description = c.Description ?? "",
         ContainerType = (ContainerType)c.ContainerType,
         ContainerURI = c.ContainerUri,
         ContentType = c.ContentType
      };
   }

   internal static CItemInfo ToContract(ItemInfo m)
      => new(m.Id, m.ContainerId, m.FullPath, m.Name, m.Description, (CItemType)m.ItemType, m.CreatedDate, m.UpdatedDate);

   internal static ItemInfo? ToModel(CItemInfo? c)
   {
      if (c is null) return null;
      return new ItemInfo
      {
         Id = c.Id,
         ContainerId = c.ContainerId,
         FullPath = c.FullPath,
         Name = c.Name,
         Description = c.Description ?? "",
         ItemType = (TreeItemType)c.Type,
         CreatedDate = c.CreatedDate,
         UpdatedDate = c.UpdatedDate
      };
   }

   internal static CItemDataInfo ToContract(ItemDataInfo m)
   {
      var value = m.Data == null ? null : Encoding.UTF8.GetString(m.Data);
      return new CItemDataInfo(m.Id, m.ItemId, m.Name, m.ContentTypeId, m.PartitionId ?? "default", value);
   }

   internal static ItemDataInfo? ToModel(CItemDataInfo? c)
   {
      if (c is null) return null;
      var m = new ItemDataInfo
      {
         Id = c.Id,
         ItemId = c.ItemId,
         Name = c.Name,
         ContentTypeId = c.ContentTypeId,
         PartitionId = c.PartitionId ?? "default"
      };
      if (c.Value != null)
      {
         m.DataText = c.Value;
      }
      return m;
   }

   internal static CContentTypeInfo ToContract(ContentTypeInfo m)
      => new(m.TypeId, m.Description);

   internal static ContentTypeInfo? ToModel(CContentTypeInfo? c)
   {
      if (c is null) return null;
      return new ContentTypeInfo(c.TypeId, c.Description);
   }
}

/// <summary>Model <see cref="ICatalogContainer"/> backed by an <see cref="CICatalogStore"/>.</summary>
internal sealed class StoreBackedContainer : Edam.Data.CatalogModel.ICatalogContainer
{
   private readonly StoreBackedCatalogService _svc;

   public StoreBackedContainer(StoreBackedCatalogService svc) => _svc = svc;

   public async Task<ContainerInfo> GetContainerAsync(string? containerId, bool checkId = true)
      => ContainerMapper.ToModel(await _svc.Store.GetContainerAsync(containerId, checkId)) ?? new ContainerInfo { ContainerId = containerId ?? "default" };

   public ContainerInfo GetContainer(string? containerId, bool checkId = true)
      => ContainerMapper.ToModel(_svc.Store.GetContainer(containerId, checkId)) ?? new ContainerInfo { ContainerId = containerId ?? "default" };

   public ContainerInfo GetContainer(Guid containerId)
      => ContainerMapper.ToModel(_svc.Store.GetContainer(containerId)) ?? new ContainerInfo();

   public ContainerInfo SetContainer(string sessionId, string containerId)
   {
      var container = ContainerMapper.ToModel(_svc.Store.SetContainer(sessionId, containerId));
      _svc.CurrentContainer = container;
      _svc.DefaultContainer = container;
      return container;
   }

   public ContainerInfo EnlistContainer(string containerId, string description, string? baseURI = null, ContainerType type = ContainerType.DataContext)
      => ContainerMapper.ToModel(_svc.Store.EnlistContainer(containerId, description, baseURI, (CContainerType)type)) ?? new ContainerInfo { ContainerId = containerId };

   public ContainerInfo DelistContainer(string containerId)
      => ContainerMapper.ToModel(_svc.Store.DelistContainer(containerId)) ?? new ContainerInfo { ContainerId = containerId };

   public async Task<List<ContainerInfo>> GetContainersAsync()
   {
      var list = new List<ContainerInfo>();
      foreach (var c in await _svc.Store.GetContainersAsync())
      {
         var map = ContainerMapper.ToModel(c);
         if (map != null) list.Add(map);
      }
      return list;
   }

   public List<ContainerInfo> GetContainers()
   {
      var list = new List<ContainerInfo>();
      foreach (var c in _svc.Store.GetContainers())
      {
         var map = ContainerMapper.ToModel(c);
         if (map != null) list.Add(map);
      }
      return list;
   }
}

/// <summary>Model <see cref="ICatalogItem"/> backed by an <see cref="CICatalogStore"/>.</summary>
internal sealed class StoreBackedItem : Edam.Data.CatalogModel.ICatalogItem
{
   private readonly StoreBackedCatalogService _svc;

   public StoreBackedItem(StoreBackedCatalogService svc) => _svc = svc;

   public async Task<ItemInfo> CreateBranchAsync(string path, string? description = null, Guid? containerId = null)
   {
      var container = containerId ?? _svc.CurrentContainer.Id;
      return ContainerMapper.ToModel(await _svc.Store.CreateBranchAsync(path, description, container)) ?? new ItemInfo { FullPath = path };
   }

   public ItemInfo CreateBranch(string path, string? description = null, Guid? containerId = null)
      => CreateBranchAsync(path, description, containerId).GetAwaiter().GetResult();

   public ItemInfo CreateRootItem(Guid? containerId = null)
      => ContainerMapper.ToModel(_svc.Store.CreateRootItem(containerId)) ?? new ItemInfo { FullPath = "/" };

   public async Task<ItemInfo> GetContainerRootItemAsync(Guid id)
      => ContainerMapper.ToModel(await _svc.Store.GetContainerRootItemAsync(id)) ?? new ItemInfo { ContainerId = id, FullPath = "/" };

   public ItemInfo GetContainerRootItem(Guid containerId)
      => GetContainerRootItemAsync(containerId).GetAwaiter().GetResult();

   public List<ItemInfo> GetContainerItems(Guid containerId)
   {
      var list = new List<ItemInfo>();
      foreach (var i in _svc.Store.GetContainerItems(containerId))
      {
         var map = ContainerMapper.ToModel(i);
         if (map != null) list.Add(map);
      }
      return list;
   }

   public ItemInfo? GetItem(Guid itemId)
      => ContainerMapper.ToModel(_svc.Store.GetItem(itemId));

   public async Task<ItemInfo> GetItemByPathAsync(string path)
      => ContainerMapper.ToModel(await _svc.Store.GetItemByPathAsync(path)) ?? new ItemInfo { FullPath = path };

   public ItemInfo GetItemByPath(string name)
      => GetItemByPathAsync(name).GetAwaiter().GetResult();

   public RequestStatus DeleteItem(Guid itemId)
      => _svc.Store.DeleteItem(itemId) ? RequestStatus.Completed : RequestStatus.Failed;

   public async Task<List<ItemInfo?>> GetBranchAsync(string? path = null)
   {
      var list = new List<ItemInfo?>();
      foreach (var i in await _svc.Store.GetBranchAsync(path))
      {
         list.Add(ContainerMapper.ToModel(i));
      }
      return list;
   }

   public List<ItemInfo?> GetBranch(string? path = null)
      => GetBranchAsync(path).GetAwaiter().GetResult();

   public async Task<ItemInfo> AddItemAsync(ItemInfo item)
      => ContainerMapper.ToModel(await _svc.Store.AddItemAsync(ContainerMapper.ToContract(item))) ?? item;

   public ItemInfo AddItem(ItemInfo item)
      => AddItemAsync(item).GetAwaiter().GetResult();
}

/// <summary>Model <see cref="ICatalogItemData"/> backed by an <see cref="CICatalogStore"/>.</summary>
internal sealed class StoreBackedItemData : Edam.Data.CatalogModel.ICatalogItemData
{
   private readonly StoreBackedCatalogService _svc;

   public StoreBackedItemData(StoreBackedCatalogService svc) => _svc = svc;

   public ContentTypeInfo GetContentType(string contentTypeId)
      => ContainerMapper.ToModel(_svc.Store.GetContentType(contentTypeId)) ?? new ContentTypeInfo(contentTypeId, null);

   public ItemDataInfo CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, byte[] dataValue = null)
      => new ItemDataInfo { Id = dataId ?? Guid.NewGuid(), ItemId = item.Id, Name = name, Data = dataValue };

   public ItemDataInfo CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, string dataValue = null)
      => new ItemDataInfo { Id = dataId ?? Guid.NewGuid(), ItemId = item.Id, Name = name, DataText = dataValue };

   public ItemDataInfo GetDataByName(Guid itemId, string name)
      => ContainerMapper.ToModel(_svc.Store.GetDataByName(itemId, name)) ?? new ItemDataInfo { ItemId = itemId, Name = name };

   public async Task<ItemDataInfo> AddItemAsync(ItemDataInfo item)
      => ContainerMapper.ToModel(await _svc.Store.AddItemAsync(ContainerMapper.ToContract(item))) ?? item;

   public ItemDataInfo AddItem(ItemDataInfo item) => AddItemAsync(item).GetAwaiter().GetResult();

   public ItemDataInfo GetData(Guid dataId)
      => ContainerMapper.ToModel(_svc.Store.GetData(dataId)) ?? new ItemDataInfo();

   public async Task<List<ItemDataInfo>> GetItemDataAsync(Guid itemId)
   {
      var list = new List<ItemDataInfo>();
      foreach (var d in await _svc.Store.GetItemDataAsync(itemId))
      {
         var map = ContainerMapper.ToModel(d);
         if (map != null) list.Add(map);
      }
      return list;
   }

   public List<ItemDataInfo> GetItemData(Guid itemId)
   {
      var list = new List<ItemDataInfo>();
      foreach (var d in _svc.Store.GetItemData(itemId))
      {
         var map = ContainerMapper.ToModel(d);
         if (map != null) list.Add(map);
      }
      return list;
   }

   public RequestStatus DeleteItemData(Guid itemId)
      => _svc.Store.DeleteItemData(itemId) ? RequestStatus.Completed : RequestStatus.Failed;

   public RequestStatus DeleteData(Guid dataId)
      => _svc.Store.DeleteData(dataId) ? RequestStatus.Completed : RequestStatus.Failed;
}
