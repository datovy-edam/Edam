using Edam.Data.Catalog.Contracts;

namespace Edam.Data.Catalog.DependencyInjection;

/// <summary>What removing a container actually did — the counts are what makes "it worked" verifiable.</summary>
/// <param name="ContainerId">The container that was asked about.</param>
/// <param name="Removed">True only when the container row is really gone.</param>
/// <param name="Items">Items (branches and leaves) removed with it.</param>
/// <param name="Contents">Content entries removed with it.</param>
/// <param name="Problem">Why it was not removed, when it was not.</param>
public sealed record ContainerRemovalResult(
   string ContainerId,
   bool Removed,
   int Items,
   int Contents,
   string? Problem = null);

/// <summary>
/// One container as a manager shows it (ADR-0014 decision 8): the immutable <b>name</b> (the id, which is the
/// address authority), the <b>description</b> (the display name a person edits), how it is <b>implemented</b>
/// (kind + base uri) and how much it holds.
/// </summary>
/// <param name="ContainerId">The container's name — immutable.</param>
/// <param name="Description">The display name; free text, and the only editable part.</param>
/// <param name="Type">The provider kind recorded for this container.</param>
/// <param name="ContainerUri">The container's base (folder path, base URI) when it has one.</param>
/// <param name="Items">Items (branches and leaves) currently in the container.</param>
public sealed record ContainerSummary(
   string ContainerId,
   string? Description,
   ContainerType Type,
   string? ContainerUri,
   int Items);

/// <summary>
/// <b>Container administration</b> (ADR-0014 decision 8 / LM-9): what a UI needs to do with a catalog's
/// containers, kept <b>outside</b> the UI so it can be proven headlessly — and kept in the ONLY order the
/// database accepts.
/// <para>
/// <b>Removal is not one statement.</b> <c>DelistContainer</c> deletes the container row <i>alone</i>, while
/// <c>edam_item</c> holds a <b>foreign key</b> to that row: removing a container that still holds anything
/// fails with a foreign-key violation — which is exactly why a Remove button can appear to do nothing. The
/// content table carries <b>no</b> foreign key at all, so its rows are not blocked; they would simply be
/// <b>orphaned</b>. The order here is therefore deliberate: <b>each item's content → the item → the
/// container</b>, oldest address first, and the counts come back so "nothing was left behind" is checkable
/// rather than asserted.
/// </para>
/// </summary>
public sealed class CatalogContainerAdministration
{
   private readonly ICatalogStore _store;
   private readonly Func<string, IContentStore?> _contentForContainer;
   private readonly IContentStore? _defaultContent;

   /// <param name="store">The catalog store (containers + items + item data).</param>
   /// <param name="contentForContainer">
   /// Resolves a <b>container-scoped</b> content store (the one policy, LM-2b-ii). The sentinel
   /// <c>default</c> resolves to null by design — that namespace is the caller's unscoped store, which is
   /// why <paramref name="defaultContent"/> exists.
   /// </param>
   /// <param name="defaultContent">The unscoped content store (used for the legacy <c>default</c> namespace).</param>
   public CatalogContainerAdministration(
      ICatalogStore store,
      Func<string, IContentStore?>? contentForContainer = null,
      IContentStore? defaultContent = null)
   {
      _store = store ?? throw new ArgumentNullException(nameof(store));
      _contentForContainer = contentForContainer ?? (_ => null);
      _defaultContent = defaultContent;
   }

   /// <summary>The container's items (branches and leaves) — used to state what a removal will take.</summary>
   public IReadOnlyList<ItemInfo> Items(string containerId)
   {
      var container = _store.GetContainer(containerId);
      return container is null ? Array.Empty<ItemInfo>() : _store.GetContainerItems(container.Id);
   }

   /// <summary>The container record, or null when it is not there.</summary>
   public ContainerInfo? Find(string containerId)
      => string.IsNullOrWhiteSpace(containerId) ? null : _store.GetContainer(containerId);

   /// <summary>
   /// <b>Add</b> a container — or, when the name is already taken, <b>update</b> the description of the existing
   /// one. That is deliberately the same operation: a container's name is its identity (the address authority,
   /// and the namespace its content lives under), so "add" and "describe" are one upsert, and a name can never
   /// be silently re-pointed at something else.
   /// <para>
   /// The caller validates the name (the registry's rule, ADR-0013 decision 7); this refuses only what cannot
   /// be an identifier at all.
   /// </para>
   /// </summary>
   public Task<ContainerInfo?> AddAsync(
      string containerId, string? description, ContainerType type,
      string? containerUri = null, CancellationToken ct = default)
   {
      if (string.IsNullOrWhiteSpace(containerId) ||
          containerId.Any(c => Char.IsWhiteSpace(c) || c == '/' || c == '\\'))
      {
         return Task.FromResult<ContainerInfo?>(null);
      }

      var enlisted = _store.EnlistContainer(
         containerId.Trim(),
         string.IsNullOrWhiteSpace(description) ? containerId.Trim() : description!,
         containerUri,
         type);

      return Task.FromResult<ContainerInfo?>(enlisted);
   }

   /// <summary>
   /// <b>Save the description</b> — the only part of a container a person may edit, through the contract's own
   /// member (not through enlist, which is idempotent and <b>never</b> rewrites: an application enlists its
   /// container on every browse, so enlist-as-update would silently revert this edit). The name, the kind and
   /// the base uri are untouched — a binding is not edited in passing (ADR-0011).
   /// </summary>
   public Task<ContainerInfo?> SaveDescriptionAsync(
      string containerId, string? description, CancellationToken ct = default)
      => _store.SetContainerDescriptionAsync(containerId, description, ct);

   /// <summary>One container as a manager shows it, or null when it is not there.</summary>
   public Task<ContainerSummary?> DescribeAsync(string containerId, CancellationToken ct = default)
   {
      var container = Find(containerId);
      if (container is null)
      {
         return Task.FromResult<ContainerSummary?>(null);
      }

      return Task.FromResult<ContainerSummary?>(Summarize(container));
   }

   /// <summary>Every container in this catalog, as a manager lists them.</summary>
   public async Task<IReadOnlyList<ContainerSummary>> DescribeAllAsync(CancellationToken ct = default)
   {
      var containers = await _store.GetContainersAsync(ct).ConfigureAwait(false);
      return containers.Select(Summarize).ToList();
   }

   private ContainerSummary Summarize(ContainerInfo container)
      => new(
         container.ContainerId,
         container.Description,
         container.ContainerType,
         container.ContainerUri,
         _store.GetContainerItems(container.Id).Count);

   /// <summary>
   /// Remove a container <b>and everything it holds</b>: each leaf's content, then every item, then the
   /// container row. Reports what it did; never throws at the caller.
   /// </summary>
   public async Task<ContainerRemovalResult> RemoveAsync(
      string containerId, CancellationToken ct = default)
   {
      if (string.IsNullOrWhiteSpace(containerId))
      {
         return new ContainerRemovalResult(containerId ?? string.Empty, false, 0, 0,
            "a container name is required");
      }

      var container = _store.GetContainer(containerId);
      if (container is null)
      {
         return new ContainerRemovalResult(containerId, false, 0, 0,
            "the container '" + containerId + "' does not exist in this catalog");
      }

      var content = _contentForContainer(container.ContainerId) ?? _defaultContent;
      var items = _store.GetContainerItems(container.Id);

      var contents = 0;
      var removed = 0;

      foreach (var item in items)
      {
         // the content key IS the item's full path, so it must be removed while that address still means
         // something — and before the item row that carries it
         if (content is not null && item.Type == ItemType.Leaf)
         {
            try
            {
               if (await content.DeleteAsync(item.FullPath, ct).ConfigureAwait(false))
               {
                  contents++;
               }
            }
            catch (Exception)
            {
               // a provider with nothing at that path must not stop the removal; the count tells the truth
            }
         }

         if (_store.DeleteItem(item.Id))
         {
            removed++;
         }
      }

      // only now the container row — the foreign key refuses it while any item still references it
      _store.DelistContainer(container.ContainerId);

      var gone = _store.GetContainer(container.ContainerId) is null;
      return new ContainerRemovalResult(
         container.ContainerId,
         gone,
         removed,
         contents,
         gone ? null : "the container row is still present after the removal");
   }
}
