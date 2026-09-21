namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// A catalog item/branch addressed by <see cref="FullPath"/> within a Container. Pure value type.
/// Derived from the relocated <c>Edam.Data.CatalogModel.ItemInfo</c> (EF-annotated POCO) as a
/// dependency-light record — the contract shape, without EF/transport specifics (ADR-0006).
/// Resource addressing is <b>path-based</b>.
/// </summary>
/// <remarks>
/// Every parameter is <b>required on purpose</b>: <c>Microsoft.AspNetCore.OpenApi</c>'s schema
/// service cannot round-trip a <c>DateTimeOffset</c> parameter default, so
/// <c>CreatedDate</c>/<c>UpdatedDate</c> must not carry <c>= default</c> — doing so faults
/// <c>/openapi/v1.json</c> with <c>JsonException: The JSON value could not be converted to
/// System.DateTimeOffset</c> (C# then also forbids the optional <c>Description</c>/<c>Type</c>
/// defaults, as required parameters must precede optional ones). Timestamps are semantically
/// required for an item anyway.
/// </remarks>
public sealed record ItemInfo(
   Guid Id,
   Guid ContainerId,
   string FullPath,
   string Name,
   string? Description,
   ItemType Type,
   DateTimeOffset CreatedDate,
   DateTimeOffset UpdatedDate)
{   public static ItemInfo Empty => new(
      Guid.Empty, Guid.Empty, string.Empty, string.Empty,
      null, ItemType.Unknown, default, default);
}
