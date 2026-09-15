namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// Catalog provider factory surface: resolves a named catalog by invariant/name and base URI.
/// Derived from <c>Edam.Data.CatalogModel.ICatalogs</c>. Pure interface — provider hidden behind DI/config.
/// </summary>
public interface ICatalogs
{
   string GetCurrentCatalogName();
   string GetDefaultCatalogName();
   ICatalogService? GetCatalog(string sessionId, string invariantName, string? baseUri = null);
}
