namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// The catalog <b>metadata</b> store seam — the relational/durable back-end behind the
/// catalog surface (BL-7.2). A provider (PostgreSQL/Npgsql today, FileSystem, future blob)
/// implements the container/item/item-data CRUD behind this interface plus
/// <see cref="IContentStore"/> for binary content, and is registered via DI so consumers
/// depend only on the contract (ADR-0006 / ADR-0007 — the back-end is a variable).
/// Resource addressing is <b>path/URI-based</b> (see <see cref="ItemInfo.FullPath"/>).
/// </summary>
public interface ICatalogStore : ICatalogContainer, ICatalogItem, ICatalogItemData
{
   /// <summary>Human-readable identity of the backing store (e.g. "postgres", "filesystem").</summary>
   string DescribeStore();
}
