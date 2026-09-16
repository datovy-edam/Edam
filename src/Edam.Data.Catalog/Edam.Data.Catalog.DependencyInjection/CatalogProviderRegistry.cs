using Edam.Data.Catalog.Contracts;

namespace Edam.Data.Catalog.DependencyInjection;

/// <summary>
/// Per-<b>Container</b> provider registry (BL-7.4 / ADR-0007). Holds the configured back-ends keyed
/// by <see cref="ContainerType"/> and resolves a <see cref="ContainerBinding"/> to its implementation —
/// one Container can target the file-system store, another PostgreSQL — with a fall-back to the default
/// target. Implements <see cref="ICatalogProviderResolver{TProvider}"/> for both the metadata
/// (<see cref="ICatalogStore"/>) and content (<see cref="IContentStore"/>) seams, so callers depend
/// only on the contract and never name a provider (ADR-0006).
/// </summary>
public sealed class CatalogProviderRegistry :
   ICatalogProviderResolver<ICatalogStore>,
   ICatalogProviderResolver<IContentStore>
{
   private readonly IReadOnlyDictionary<ContainerType, ICatalogStore?> _stores;
   private readonly IReadOnlyDictionary<ContainerType, IContentStore?> _contents;
   private readonly ContainerType _defaultTarget;

   public CatalogProviderRegistry(
      IReadOnlyDictionary<ContainerType, ICatalogStore?> stores,
      IReadOnlyDictionary<ContainerType, IContentStore?> contents,
      ContainerType defaultTarget)
   {
      _stores = stores;
      _contents = contents;
      _defaultTarget = defaultTarget;
   }

   /// <summary>The metadata store bound to the default target (hidden back-end), if configured.</summary>
   public ICatalogStore? DefaultStore => ResolveStore(ContainerBinding.Default());

   /// <summary>The content store bound to the default target (hidden back-end), if configured.</summary>
   public IContentStore? DefaultContent => ResolveContent(ContainerBinding.Default());

   /// <summary>Resolve a Container's metadata store by its <see cref="ContainerType"/> target.</summary>
   public ICatalogStore? ResolveStore(ContainerBinding binding)
      => Lookup(_stores, binding.Target, _defaultTarget);

   /// <summary>Resolve a Container's content store by its <see cref="ContainerType"/> target.</summary>
   public IContentStore? ResolveContent(ContainerBinding binding)
      => Lookup(_contents, binding.Target, _defaultTarget);

   ICatalogStore? ICatalogProviderResolver<ICatalogStore>.Resolve(ContainerBinding binding)
      => ResolveStore(binding);

   IContentStore? ICatalogProviderResolver<IContentStore>.Resolve(ContainerBinding binding)
      => ResolveContent(binding);

   private static T? Lookup<T>(IReadOnlyDictionary<ContainerType, T?> map, ContainerType target, ContainerType @default)
   {
      if (map.TryGetValue(target, out var t)) return t;
      return map.TryGetValue(@default, out var d) ? d : default;
   }
}
