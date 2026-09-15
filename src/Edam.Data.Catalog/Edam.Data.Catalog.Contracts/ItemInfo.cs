namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// A catalog item/branch addressed by <see cref="FullPath"/> within a Container. Pure value type.
/// Derived from the relocated <c>Edam.Data.CatalogModel.ItemInfo</c> (EF-annotated POCO) as a
/// dependency-light record — the contract shape, without EF/transport specifics (ADR-0006).
/// Resource addressing is <b>path-based</b>.
/// </summary>
public sealed record ItemInfo(
   Guid Id,
   Guid ContainerId,
   string FullPath,
   string Name,
   string? Description = null,
   ItemType Type = ItemType.Branch,
   DateTimeOffset CreatedDate = default,
   DateTimeOffset UpdatedDate = default)
{
   public static ItemInfo Empty => new(
      Guid.Empty, Guid.Empty, string.Empty, string.Empty,
      null, ItemType.Unknown, default, default);
}
