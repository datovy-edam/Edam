using Edam.Data.Catalog.Contracts;
using Edam.DataObjects.Requests;
using Edam.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
// BL-7.2 sub-step 3b-2 / ADR-0008: Contracts-native REST item/branch surface.
namespace Edam.Data.CatalogServiceClient;

/// <summary>
/// REST-backed item/branch management implementing <see cref="ICatalogItem"/>
/// over the <c>Contracts</c> wire shapes — no Model EF entities, no CatalogDb.
/// </summary>
public sealed class CatalogHttpItem : ICatalogItem
{
   private readonly CatalogHttpClient _client;

   public CatalogHttpItem(CatalogHttpClient client)
   {
      _client = client;
   }

   public async Task<ItemInfo> CreateBranchAsync(
      string path, string? description = null, Guid? containerId = null,
      CancellationToken ct = default)
   {
      var container = containerId ?? _client.CurrentContainer?.Id ?? Guid.Empty;
      var item = new ItemInfo(Guid.NewGuid(), container, path, path,
         description, ItemType.Branch, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
      return await AddItemAsync(item, ct).ConfigureAwait(false);
   }

   public ItemInfo CreateBranch(
      string path, string? description = null, Guid? containerId = null)
   {
      return CreateBranchAsync(path, description, containerId).Result;
   }

   public ItemInfo? CreateRootItem(Guid? containerId = null)
   {
      throw new NotSupportedException(
         "CatalogHttpItem.CreateRootItem is a store-side operation (root item is seeded by the provider).");
   }

   public async Task<ItemInfo?> GetContainerRootItemAsync(
      Guid id, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      ItemInfo? item = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_CONTAINER_GUID, id.ToString());
      var endpoint = CatalogHttpClient.URI_CONTAINER_ROOT_ITEM_ID + pars.ToString();
      try
      {
         item = await _client.Client!
            .GetDataFromJsonAsync<ItemInfo?>(endpoint).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return item;
   }

   public ItemInfo? GetContainerRootItem(Guid containerId)
   {
      return GetContainerRootItemAsync(containerId).Result;
   }

   public async Task<IReadOnlyList<ItemInfo>> GetContainerItemsAsync(
      Guid id, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      IReadOnlyList<ItemInfo> items = Array.Empty<ItemInfo>();
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_CONTAINER_GUID, id.ToString());
      var endpoint = CatalogHttpClient.URI_CONTAINER_ITEMS + pars.ToString();
      try
      {
         var data = await _client.Client!
            .GetDataFromJsonAsync<List<ItemInfo>?>(endpoint).ConfigureAwait(false);
         items = data != null ? (IReadOnlyList<ItemInfo>)data : Array.Empty<ItemInfo>();
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return items;
   }

   public IReadOnlyList<ItemInfo> GetContainerItems(Guid containerId)
   {
      return GetContainerItemsAsync(containerId).Result;
   }

   public async Task<ItemInfo?> GetItemAsync(Guid itemId, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      ItemInfo? item = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_ITEM_ID, itemId.ToString());
      var endpoint = CatalogHttpClient.URI_ITEM_ID + pars.ToString();
      try
      {
         item = await _client.Client!
            .GetDataFromJsonAsync<ItemInfo?>(endpoint).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return item;
   }

   public ItemInfo? GetItem(Guid itemId)
   {
      return GetItemAsync(itemId).Result;
   }

   public async Task<ItemInfo?> GetItemByPathAsync(
      string path, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      ItemInfo? item = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_ITEM_PATH, path);
      var endpoint = CatalogHttpClient.URI_ITEM_PATH + pars.ToString();
      try
      {
         item = await _client.Client!
            .GetDataFromJsonAsync<ItemInfo?>(endpoint).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return item;
   }

   public ItemInfo? GetItemByPath(string name)
   {
      return GetItemByPathAsync(name).Result;
   }

   public async Task<bool> DeleteItemAsync(Guid itemId, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_ITEM_ID, itemId.ToString());
      var endpoint = CatalogHttpClient.URI_ITEM_ID + pars.ToString();
      try
      {
         var response = await _client.Client!
            .DeleteAsync<RequestResponseInfo?>(endpoint).ConfigureAwait(false);
         return response != null && response.Success;
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
         return false;
      }
   }

   public bool DeleteItem(Guid itemId)
   {
      return DeleteItemAsync(itemId).Result;
   }

   public async Task<IReadOnlyList<ItemInfo>> GetBranchAsync(
      string? path = null, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      IReadOnlyList<ItemInfo> items = Array.Empty<ItemInfo>();
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_ITEM_PATH, path);
      var endpoint = CatalogHttpClient.URI_BRANCH_ITEMS + pars.ToString();
      try
      {
         var data = await _client.Client!
            .GetDataFromJsonAsync<List<ItemInfo>?>(endpoint).ConfigureAwait(false);
         items = data != null ? (IReadOnlyList<ItemInfo>)data : Array.Empty<ItemInfo>();
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return items;
   }

   public IReadOnlyList<ItemInfo> GetBranch(string? path = null)
   {
      return GetBranchAsync(path).Result;
   }

   public async Task<ItemInfo> AddItemAsync(ItemInfo item, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      ItemInfo? result = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      var endpoint = CatalogHttpClient.URI_ITEM_ADD + pars.ToString();
      try
      {
         result = await _client.Client!
            .PostAsync<ItemInfo?>(endpoint, item).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return result ?? ItemInfo.Empty;
   }

   public ItemInfo AddItem(ItemInfo item)
   {
      return AddItemAsync(item).Result;
   }
}
