namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// A connectable catalog client: initializes a session against a default Container.
/// Derived from <c>Edam.Data.CatalogModel.ICatalogClient</c>. Pure interface — provider hidden.
/// </summary>
public interface ICatalogClient : ICatalogService
{
   /// <summary>The path-addressed content (blob/binary) surface for this catalog (ADR-0007).</summary>
   IContentStore Content { get; }

   Task<ContainerInfo> InitializeClientAsync(
      string sessionId, string? containerId = null, CancellationToken ct = default);
   ContainerInfo? InitializeClient(string sessionId, string? containerId = null);
}
