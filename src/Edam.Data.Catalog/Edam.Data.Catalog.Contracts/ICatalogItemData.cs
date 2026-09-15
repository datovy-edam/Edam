namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// Item-data leaf surface: named, addressed content on catalog items.
/// Derived from the relocated <c>Edam.Data.CatalogModel.ICatalogItemData</c>. Pure interface (ADR-0006).
/// </summary>
public interface ICatalogItemData
{
   ContentTypeInfo? GetContentType(string contentTypeId);
   ItemDataInfo? CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, byte[]? dataValue = null);
   ItemDataInfo? CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, string? dataValue = null);
   ItemDataInfo? GetDataByName(Guid itemId, string name);
   Task<ItemDataInfo> AddItemAsync(ItemDataInfo item, CancellationToken ct = default);
   ItemDataInfo AddItem(ItemDataInfo item);
   ItemDataInfo? GetData(Guid dataId);
   Task<IReadOnlyList<ItemDataInfo>> GetItemDataAsync(Guid itemId, CancellationToken ct = default);
   IReadOnlyList<ItemDataInfo> GetItemData(Guid itemId);
   bool DeleteItemData(Guid itemId);
   bool DeleteData(Guid dataId);
}
