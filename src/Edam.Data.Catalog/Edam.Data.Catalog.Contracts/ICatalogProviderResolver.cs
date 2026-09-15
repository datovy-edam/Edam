namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// Resolves a <b>Container</b>'s target into the matching catalog provider for
/// <typeparamref name="TProvider"/> (e.g. <c>ICatalogProviderResolver&lt;IContentStore&gt;</c>).
/// Centralizes the "Container → implementation" mapping that today is scattered across UI/service
/// switches (BL-7.4) so a Container targets a file system, another a database — declared in DI/config.
/// Concrete providers are DI-/config-registered; callers depend only on this contract (ADR-0006 purity).
/// </summary>
public interface ICatalogProviderResolver<out TProvider>
{
   TProvider? Resolve(ContainerBinding binding);
}
