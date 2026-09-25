using Edam.Data.Catalog.Contracts;

namespace Edam.Data.Projects.Catalog;

/// <summary>
/// Resolves the <b>content store for a container</b> (LM-2b-ii / ADR-0011). The content seam keeps its
/// pure shape — <see cref="IContentStore"/> addresses a <b>path</b> only — so the <b>container is the
/// instance's scope</b>: the composition root (which knows the storage) hands the providers a store
/// created <i>for</i> a container, and the providers ask for it per operation.
/// <para>
/// A provider built without a resolver keeps the single store it was given, which is the behaviour
/// before container scoping existed — so this is additive for every caller.
/// </para>
/// </summary>
public interface IProjectContentStoreResolver
{
   /// <summary>
   /// The content store scoped to <paramref name="container"/>, or <c>null</c> to fall back to the store
   /// the provider was constructed with (e.g. the legacy/unscoped namespace).
   /// </summary>
   IContentStore? ForContainer(ContainerInfo container);
}
