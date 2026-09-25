using Edam.Data.Catalog.Contracts;
using Edam.Data.Projects.Catalog;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>
/// The composition root's adapter from the shared <b>container-scoping policy</b>
/// (<see cref="CatalogScopedContent.Factory"/>) to the projects layer (LM-2b-ii / ADR-0011): the content
/// seam stays path-only, so the providers are handed a store created <i>for</i> a container.
/// <para>
/// One policy therefore serves the service and the projects layer, which is what keeps
/// <b>local and remote access to a container's content in the same namespace</b>.
/// </para>
/// </summary>
public sealed class ProjectContentStoreResolver : IProjectContentStoreResolver
{
   private readonly Func<string, IContentStore?> _factory;

   public ProjectContentStoreResolver(Func<string, IContentStore?> factory)
      => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

   public IContentStore? ForContainer(ContainerInfo container)
      => container is null ? null : _factory(container.ContainerId);
}
