namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// Kind of catalog item/branch in the path-addressed tree.
/// Derived from the relocated <c>Edam.DataObjects.Trees.TreeItemType</c> used by
/// <c>ItemInfo</c>, kept dependency-light here (ADR-0006).
/// </summary>
public enum ItemType
{
   Unknown = 0,
   Branch = 1,   // a folder/container item
   Leaf = 2      // a leaf/file item
}
