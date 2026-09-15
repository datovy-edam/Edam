namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// Container management surface: enlist/delist/select Containers and enumerate them.
/// Derived from the relocated <c>Edam.Data.CatalogModel.ICatalogContainer</c>. Pure interface (ADR-0006).
/// </summary>
public interface ICatalogContainer
{
   Task<ContainerInfo?> GetContainerAsync(
      string? containerId, bool checkId = true, CancellationToken ct = default);
   ContainerInfo? GetContainer(string? containerId, bool checkId = true);
   ContainerInfo? GetContainer(Guid containerId);
   ContainerInfo SetContainer(string sessionId, string containerId);
   ContainerInfo EnlistContainer(
      string containerId, string description, string? baseUri = null,
      ContainerType type = ContainerType.DataContext);
   ContainerInfo DelistContainer(string containerId);
   Task<IReadOnlyList<ContainerInfo>> GetContainersAsync(CancellationToken ct = default);
   IReadOnlyList<ContainerInfo> GetContainers();
}
