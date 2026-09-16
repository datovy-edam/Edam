using Edam.Data.Catalog.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

// -----------------------------------------------------------------------------
// BL-7.4 seed (supports BL-7.2 remaining step 5): an ICatalogProviderResolver
// that maps a Container's Target to the matching catalog provider registered in DI,
// centralizing the "Container -> implementation" mapping that the UI/service once
// scattered across switches (ADR-0006 purity / ADR-0007 back-end-as-a-variable).
// The PostgreSQL provider is DI-registered here; unknown / not-registered targets
// resolve to null (callers stay up). A later step backs the WinUI "local" instance
// with this resolver so the retired Edam.Data.CatalogDb EF back-end is gone.
namespace Edam.Data.Catalog.PostgreSql;

/// <summary>
/// Resolves a <see cref="ContainerBinding"/>'s target to the matching catalog
/// provider (metadata + content) registered in DI.
/// </summary>
public sealed class CatalogProviderResolver :
   ICatalogProviderResolver<ICatalogStore>,
   ICatalogProviderResolver<IContentStore>
{
   private readonly IServiceProvider _services;

   public CatalogProviderResolver(IServiceProvider services)
   {
      _services = services;
   }

   ICatalogStore? ICatalogProviderResolver<ICatalogStore>.Resolve(ContainerBinding binding)
   {
      return binding.Target switch
      {
         ContainerType.PostgreSql => _services.GetService<ICatalogStore>(),
         _ => null
      };
   }

   IContentStore? ICatalogProviderResolver<IContentStore>.Resolve(ContainerBinding binding)
   {
      return binding.Target switch
      {
         ContainerType.PostgreSql => _services.GetService<IContentStore>(),
         _ => null
      };
   }
}
