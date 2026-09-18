using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

// -----------------------------------------------------------------------------
using Edam.Data.Catalog.Contracts;
using Edam.DataObjects.Requests;
using szer = Edam.Serialization;

namespace Edam.Data.CatalogServiceClient;

/// <summary>
/// Catalog service minimal-API map (BL-7.2 / ADR-0008). Endpoints now back onto the
/// PostgreSQL (Npgsql) <see cref="ICatalogStore"/> and emit <c>Contracts</c> value-record
/// shapes (replacing the EF-backed <c>CatalogDb</c> instances and Model entity shapes).
/// The HTTP routes are unchanged; the response record types are the <c>Contracts</c> set.
/// </summary>
public class CatalogServiceMap
{
   private readonly ICatalogStore store;

   public CatalogServiceMap(WebApplication app, ICatalogStore store)
   {
      this.store = store;

      #region -- 1.50 - Initialization and Session Management

      // this should be called first...
      app.MapGet("/catalogservice/session/info", (
         string sessionId, string containerId) =>
      {
         return store.SetContainer(sessionId, containerId);
      });

      #endregion
      #region -- 4.00 - Container Support

      // get container info
      app.MapGet("/catalogservice/container/info", (
         string sessionId, string containerId) =>
      {
         return store.SetContainer(sessionId, containerId);
      });

      // get container info by id
      app.MapGet("/catalogservice/container/id", (
         string sessionId, string id) =>
      {
         return Guid.TryParse(id, out var guid) ? store.GetContainer(guid) : store.GetContainer(id);
      });

      // get container items
      app.MapGet("/catalogservice/container/items/id", (
         string sessionId, Guid id) =>
      {
         return store.GetContainerItems(id);
      });

      // get container root item
      app.MapGet("/catalogservice/container/item/root/id", (
         string sessionId, Guid id) =>
      {
         return store.GetContainerRootItem(id);
      });

      // get container list
      app.MapGet("/catalogservice/container/list", (string sessionId) =>
      {
         return store.GetContainers();
      });

      // enlist a container
      app.MapGet("/catalogservice/container/enlist", (
         string sessionId, string containerId, string description, string baseUri,
         ContainerType type) =>
      {
         return store.EnlistContainer(containerId, description, baseUri, type);
      });

      // delist a container
      app.MapGet("/catalogservice/container/delist", (
         string sessionId, string containerId, string description,
         string? status = null) =>
      {
         return store.DelistContainer(containerId);
      });

      #endregion
      #region -- 4.00 - Catalog Item Support

      // post catalog item
      app.MapPost("/catalogservice/catalog/item", (
         string sessionId, ItemInfo item) =>
      {
         var aitem = store.AddItem(item);
         return szer.JsonSerializer.Serialize<ItemInfo>(aitem);
      });

      // get catalog item by id
      app.MapGet("/catalogservice/catalog/item/id", (
         string sessionId, Guid id) =>
      {
         return store.GetItem(id);
      });

      // get catalog item by path
      app.MapGet("/catalogservice/catalog/item/path", (
         string sessionId, string path) =>
      {
         return store.GetItemByPath(path);
      });

      // delete catalog item
      app.MapDelete("/catalogservice/catalog/item/id", (
         string sessionId, Guid id) =>
      {
         RequestResponseInfo response = new RequestResponseInfo();
         try
         {
            store.DeleteItem(id);
            response.Success = true;
            response.Status = RequestStatus.Completed;
         }
         catch (Exception)
         {
            response.Success = false;
            response.SessionId = sessionId;
            response.Status = RequestStatus.Failed;
         }
         return response;
      });

      #endregion
      #region -- 4.00 - Manage Branches and Leafs

      // get branch items
      app.MapGet("/catalogservice/catalog/branch/items", (
         string sessionId, string path) =>
      {
         return store.GetBranch(path);
      });

      #endregion
      #region -- 4.00 - Catalog Data Item Support

      // post catalog data item
      app.MapPost("/catalogservice/catalog/data/item", (
         string sessionId, ItemDataInfo itemData) =>
      {
         var aitem = store.AddItem(itemData);
         return szer.JsonSerializer.Serialize<ItemDataInfo>(aitem);
      });

      // get catalog data items for item id
      app.MapGet("/catalogservice/catalog/data/items/id", (
         string sessionId, Guid id) =>
      {
         return store.GetItemData(id);
      });

      // get catalog data item by name
      app.MapGet("/catalogservice/catalog/data/item/name", (
         string sessionId, Guid id, string name) =>
      {
         return store.GetDataByName(id, name);
      });

      // get catalog data items by id
      app.MapGet("/catalogservice/catalog/data/item/id", (
         string sessionId, Guid id) =>
      {
         return store.GetItemData(id);
      });

      // delete catalog data item
      app.MapDelete("/catalogservice/catalog/data/item/id", (
         string sessionId, Guid id) =>
      {
         RequestResponseInfo response = new RequestResponseInfo();
         try
         {
            store.DeleteItemData(id);
            response.Success = true;
            response.Status = RequestStatus.Completed;
         }
         catch (Exception)
         {
            response.Success = false;
            response.SessionId = sessionId;
            response.Status = RequestStatus.Failed;
         }
         return response;
      });

      // delete catalog data by id
      app.MapDelete("/catalogservice/catalog/data/id", (
         string sessionId, Guid id) =>
      {
         RequestResponseInfo response = new RequestResponseInfo();
         try
         {
            store.DeleteData(id);
            response.Success = true;
            response.Status = RequestStatus.Completed;
         }
         catch (Exception)
         {
            response.Success = false;
            response.SessionId = sessionId;
            response.Status = RequestStatus.Failed;
         }
         return response;
      });

      #endregion
      #region -- 4.00 - Manage Other Requests...

      // content type by id
      app.MapGet("/catalogservice/catalog/content/type/id", (
         string sessionId, string contentTypeId) =>
      {
         return store.GetContentType(contentTypeId);
      });

      #endregion
   }

}
