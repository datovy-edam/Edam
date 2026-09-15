namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// A catalog item's data leaf (a named, addressed content value on an <see cref="ItemInfo"/>).
/// Pure value type, derived from the relocated <c>Edam.Data.CatalogModel.ItemDataInfo</c>
/// (EF-annotated POCO) as a dependency-light record (ADR-0006).
/// </summary>
public sealed record ItemDataInfo(
   Guid Id,
   Guid ItemId,
   string Name,
   string ContentTypeId,
   string PartitionId = "default",
   string? Value = null);
