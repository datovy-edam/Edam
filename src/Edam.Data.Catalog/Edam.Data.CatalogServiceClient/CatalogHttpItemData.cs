using Edam.Data.Catalog.Contracts;
using Edam.DataObjects.Requests;
using Edam.Text;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
// BL-7.2 sub-step 3b-2 / ADR-0008: Contracts-native REST item-data surface.
namespace Edam.Data.CatalogServiceClient;

/// <summary>
/// REST-backed item-data-leaf management implementing <see cref="ICatalogItemData"/>
/// over the <c>Contracts</c> wire shapes — no Model EF entities, no CatalogDb.
/// Resource addressing is path/URI-based; content values travel as <see cref="ItemDataInfo.Value"/>.
/// </summary>
public sealed class CatalogHttpItemData : ICatalogItemData
{
   public const string PARTITION_DEFAULT = "default";

   private readonly CatalogHttpClient _client;

   public CatalogHttpItemData(CatalogHttpClient client)
   {
      _client = client;
   }

   public ContentTypeInfo? GetContentType(string contentTypeId)
   {
      return _client.GetContentType(contentTypeId);
   }

   public ItemDataInfo? CreateDataLeaf(
      ItemInfo item, string name, Guid? dataId = null, byte[]? dataValue = null)
   {
      var value = dataValue == null ? null : Encoding.UTF8.GetString(dataValue);
      return CreateDataLeaf(item, name, dataId, value);
   }

   public ItemDataInfo? CreateDataLeaf(
      ItemInfo item, string name, Guid? dataId = null, string? dataValue = null)
   {
      return new ItemDataInfo(
         dataId ?? Guid.NewGuid(),
         item.Id,
         String.IsNullOrWhiteSpace(name) ? "default" : name,
         "text/plain",
         PARTITION_DEFAULT,
         dataValue);
   }

   public async Task<ItemDataInfo?> GetDataByNameAsync(
      Guid itemId, string name, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      ItemDataInfo? item = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_ITEM_ID, itemId.ToString());
      pars.Add(CatalogHttpClient.TAG_ITEM_NAME, name);
      var endpoint = CatalogHttpClient.URI_ITEM_DATA_ITEM_NAME + pars.ToString();
      try
      {
         item = await _client.Client!
            .GetDataFromJsonAsync<ItemDataInfo?>(endpoint).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return item;
   }

   public ItemDataInfo? GetDataByName(Guid itemId, string name)
   {
      return GetDataByNameAsync(itemId, name).Result;
   }

   public async Task<ItemDataInfo> AddItemAsync(
      ItemDataInfo item, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      ItemDataInfo? result = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      var endpoint = CatalogHttpClient.URI_ITEM_ADD + pars.ToString();
      try
      {
         result = await _client.Client!
            .PostAsync<ItemDataInfo?>(endpoint, item).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return result ?? item;
   }

   public ItemDataInfo AddItem(ItemDataInfo item)
   {
      return AddItemAsync(item).Result;
   }

   public async Task<ItemDataInfo?> GetDataAsync(Guid dataId, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      ItemDataInfo? item = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_DATA_ID, dataId.ToString());
      var endpoint = CatalogHttpClient.URI_DATA_ID + pars.ToString();
      try
      {
         item = await _client.Client!
            .GetDataFromJsonAsync<ItemDataInfo?>(endpoint).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return item;
   }

   public ItemDataInfo? GetData(Guid dataId)
   {
      return GetDataAsync(dataId).Result;
   }

   public async Task<IReadOnlyList<ItemDataInfo>> GetItemDataAsync(
      Guid itemId, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      IReadOnlyList<ItemDataInfo> items = Array.Empty<ItemDataInfo>();
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_ITEM_ID, itemId.ToString());
      var endpoint = CatalogHttpClient.URI_ITEM_DATA_ITEM_ID + pars.ToString();
      try
      {
         var data = await _client.Client!
            .GetDataFromJsonAsync<List<ItemDataInfo>?>(endpoint).ConfigureAwait(false);
         items = data != null ? (IReadOnlyList<ItemDataInfo>)data : Array.Empty<ItemDataInfo>();
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return items;
   }

   public IReadOnlyList<ItemDataInfo> GetItemData(Guid itemId)
   {
      return GetItemDataAsync(itemId).Result;
   }

   public async Task<bool> DeleteItemDataAsync(Guid itemId, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_ITEM_ID, itemId.ToString());
      var endpoint = CatalogHttpClient.URI_ITEM_DATA_ITEM_ID + pars.ToString();
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

   public bool DeleteItemData(Guid itemId)
   {
      return DeleteItemDataAsync(itemId).Result;
   }

   public async Task<bool> DeleteDataAsync(Guid dataId, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_DATA_ID, dataId.ToString());
      var endpoint = CatalogHttpClient.URI_DATA_ID + pars.ToString();
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

   public bool DeleteData(Guid dataId)
   {
      return DeleteDataAsync(dataId).Result;
   }
}
