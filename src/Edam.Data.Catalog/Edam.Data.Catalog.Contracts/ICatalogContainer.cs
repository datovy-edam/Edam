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

   /// <summary>
   /// <b>Set a container's description</b> — the display name, and the <b>only</b> part of a container that is
   /// not identity (ADR-0014 decision 8).
   /// <para>
   /// This member exists because <b>enlist must not mean update</b>: enlistment is idempotent by contract (it
   /// returns the existing row), and an application enlists the container it works in on <b>every</b> browse —
   /// so if enlist rewrote the description, a person's edit would be silently reverted at the next panel
   /// refresh. The providers disagreed anyway: the database providers overwrote the description (and the kind
   /// and the base uri with it) on a re-enlist, while the file-system and in-memory providers ignored it — so
   /// editing a description worked on one back-end and did nothing on another. Both halves are fixed here.
   /// </para>
   /// <para>
   /// The name, the kind and the base uri are <b>never</b> touched: a binding is not edited in passing
   /// (ADR-0011).
   /// </para>
   /// </summary>
   /// <returns>The updated container, or null when the container does not exist.</returns>
   Task<ContainerInfo?> SetContainerDescriptionAsync(
      string containerId, string? description, CancellationToken ct = default);

   /// <summary>Blocking wrapper for <see cref="SetContainerDescriptionAsync"/>.</summary>
   ContainerInfo? SetContainerDescription(string containerId, string? description);

   ContainerInfo DelistContainer(string containerId);
   Task<IReadOnlyList<ContainerInfo>> GetContainersAsync(CancellationToken ct = default);
   IReadOnlyList<ContainerInfo> GetContainers();
}
