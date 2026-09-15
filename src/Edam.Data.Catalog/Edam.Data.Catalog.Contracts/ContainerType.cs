namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// Kind of catalog back-end (a "replaceable target"). Chosen by config/DI —
/// never baked into callers. ADR-0007: back-ends are a variable.
/// Derived from the relocated <c>Edam.Data.CatalogModel.ContainerType</c> and extended for
/// Wave 1.1/1.2 relational + service targets (BL-7.3/BL-7.4).
/// </summary>
public enum ContainerType
{
   Unknown = 0,
   DataContext = 1,   // a relational data context (metadata store)
   FileSystem = 2,    // file-system target
   PostgreSql = 3,    // PostgreSQL (Npgsql) target — Wave 1.1 default relational store
   Service = 4        // remote catalog service (HTTP/REST)
}
