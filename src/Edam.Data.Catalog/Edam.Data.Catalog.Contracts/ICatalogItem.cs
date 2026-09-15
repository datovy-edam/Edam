namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// Item/branch surface: path-addressed creation/traversal/trees of catalog items.
/// Derived from the relocated <c>Edam.Data.CatalogModel.ICatalogItem</c> — the
/// provider-agnostic contract shape. Pure interface (ADR-0006).
/// </summary>
public interface ICatalogItem
{
   Task<ItemInfo> CreateBranchAsync(
      string path, string? description = null, Guid? containerId = null, CancellationToken ct = default);
   ItemInfo CreateBranch(string path, string? description = null, Guid? containerId = null);
   ItemInfo? CreateRootItem(Guid? containerId = null);
   Task<ItemInfo?> GetContainerRootItemAsync(Guid id, CancellationToken ct = default);
   ItemInfo? GetContainerRootItem(Guid containerId);
   IReadOnlyList<ItemInfo> GetContainerItems(Guid containerId);
   ItemInfo? GetItem(Guid itemId);
   Task<ItemInfo?> GetItemByPathAsync(string path, CancellationToken ct = default);
   ItemInfo? GetItemByPath(string name);
   bool DeleteItem(Guid itemId);
   Task<IReadOnlyList<ItemInfo>> GetBranchAsync(string? path = null, CancellationToken ct = default);
   IReadOnlyList<ItemInfo> GetBranch(string? path = null);
   Task<ItemInfo> AddItemAsync(ItemInfo item, CancellationToken ct = default);
   ItemInfo AddItem(ItemInfo item);
}
