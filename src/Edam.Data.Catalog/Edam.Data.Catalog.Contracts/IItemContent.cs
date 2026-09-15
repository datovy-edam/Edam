namespace Edam.Data.Catalog.Contracts;

/// <summary>Holds an item's in-memory content value. Derived from <c>Edam.Data.CatalogModel.IItemContent</c>.</summary>
public interface IItemContent
{
   object? ItemInstance { get; set; }
   string Content { get; set; }
}
