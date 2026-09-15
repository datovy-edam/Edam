namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// The catalog service surface (a catalog client operating on a set of Containers).
/// Derived from the relocated <c>Edam.Data.CatalogModel.ICatalogService</c> — the
/// provider-agnostic contract shape, using the dependency-light value records in this
/// platform (ADR-0006). Target/Container selection is by DI/config (BL-7.4).
/// </summary>
public interface ICatalogService
{
   CatalogInfo? Catalog { get; }
   string DefaultContainerId { get; set; }
   ContainerInfo? DefaultContainer { get; set; }
   ContainerInfo? CurrentContainer { get; set; }
   ICatalogContainer Container { get; }
   ICatalogItem Item { get; }
   ICatalogItemData ItemData { get; }
}
