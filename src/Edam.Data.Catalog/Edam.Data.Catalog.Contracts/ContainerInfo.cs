namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// A catalog <b>Container</b> (a named, addressed catalog partition). Pure value type.
/// Derived from the relocated <c>Edam.Data.CatalogModel.ContainerInfo</c> (EF-annotated POCO)
/// as a dependency-light record — the contract shape, without EF/transport specifics (ADR-0006).
/// <see cref="ContainerUri"/> is the container's base (file-system root, connection/base URI).
/// </summary>
public sealed record ContainerInfo(
   Guid Id,
   string ContainerId,
   string Description,
   ContainerType ContainerType,
   string ContainerUri = "",
   string ContentType = "application/json");
