namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// Identity/descriptor of a catalog (the named catalog instance). Pure value type.
/// Derived from <c>Edam.Data.CatalogModel.CatalogInfo</c>, kept dependency-light here (ADR-0006).
/// </summary>
public sealed record CatalogInfo(string Name, string? DefaultContainerId = null);
