using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
using Edam.Data.Catalog.Folder;
using Edam.Data.CatalogModel;
using Edam.Data.CatalogServiceClient;
using Edam.UI.Catalog.Models;
using Edam.UI.CatalogExplorer;

namespace Edam.UI.Catalog.Controls;

public class CatalogViewModel
{

   public const string CATALOG_INITIALIZED = "CATALOG-INITIALIZED";

   public AppModelState State = null;
   public bool HasCatalog = false;

   /// <summary>
   /// Base Catalog...
   /// </summary>
   public CatalogInfo? Catalog = null;

   /// <summary>
   /// Root Item...
   /// </summary>
   public CatalogItemModel RootItem = null;

   /// <summary>
   /// Current Container Item...
   /// </summary>
   public ContainerItem CurrentContainerItem { get; set; } = null;

   /// <summary>
   /// Notify Events
   /// </summary>
   public NotificationEventHandler NotifyEvent { get; set; }

   /// <summary>
   /// Initialize Catalog
   /// </summary>
   public async Task GetCatalogAsync(AppModelState state)
   {
      string connectionUri = state.GetConnectionUri();

      Catalog = await CatalogServiceHelper.GetCatalogAsync(connectionUri);
   }

   /// <summary>
   /// Post Item.
   /// </summary>
   /// <param name="path"></param>
   /// <param name="payload"></param>
   /// <returns></returns>
   public async Task<ItemDataInfo> PostItemAsync(string path, byte[] payload)
   {
      ItemDataInfo? itemData = null;
      CatalogTreeBuilder builder =
          new CatalogTreeBuilder(Catalog.CatalogService, Catalog);
      var item = await builder.GetItemAsync(path);
      if (payload != null && payload.Length > 0)
      {
         itemData = item.ToItemData(payload);
         var rItem =
            await Catalog.CatalogService.ItemData.AddItemAsync(itemData);
      }
      return itemData;
   }

   /// <summary>
   /// Post Item.
   /// </summary>
   /// <param name="path"></param>
   /// <param name="payload"></param>
   /// <returns></returns>
   public async Task<ItemDataInfo> PostItemAsync(string path, string payload)
   {
      byte[] bytes = Encoding.ASCII.GetBytes(payload);
      return await PostItemAsync(path, bytes);
   }

   /// <summary>
   /// Get Item Data.
   /// </summary>
   /// <param name="item">item instance of CatalogItem</param>
   /// <returns>ItemDataInfo instance is returned</returns>
   public async Task<List<ItemDataInfo>> GetItemDataAsync(CatalogItemModel item)
   {
      CatalogPathItem pitem = item.Item.Tag as CatalogPathItem;

      var catalog = CurrentContainerItem.Catalog;

      //var idata =
      //   await Catalog.CatalogService.ItemData.GetItemDataAsync(pitem.Item.Id);

      var idata =
         await catalog.CatalogService.ItemData.GetItemDataAsync(pitem.Item.Id);
      return idata;
   }

   #region -- 4.00 - Support to Setup Client based on Container type

   /// <summary>
   /// Create an instance of the Folder-File System provided.
   /// </summary>
   /// <param name="container">container</param>
   /// <returns>instance (client) of ICatalogService is returned</returns>
   public async Task<ICatalogService> GetFileSystemProviderAsync(
      ContainerInfo container)
   {
      // BL-7.x: a FileSystem container's URI is a real folder that is INGESTED into the catalog
      // through the Contracts seams (FolderCatalogIndexer) — provider-agnostic and DI-resolved, so
      // it targets the same catalog the desktop already uses, and the identical call works against
      // a remote catalog client. This replaces the bespoke Model FolderCatalogClient: the folder
      // becomes ordinary catalog items/content, browsable through the standard surface.
      var (store, content) = CatalogServiceHelper.GetLocalStore();
      await FolderCatalogIndexer.IndexAsync(
         store, content, container.ContainerId, container.ContainerURI ?? String.Empty);

      return new StoreBackedCatalogService(store, CatalogBaseClient.SessionId);
   }

   /// <summary>
   /// Given a container return required Catalog Service provider to support
   /// it based on the Container Type.
   /// </summary>
   /// <param name="container">container</param>
   /// <returns>instance of client is returned</returns>
   public async Task<ICatalogService> GetClientAsync(ContainerInfo container)
   {
      ICatalogService client = null;
      switch (container.ContainerType)
      {
         case ContainerType.DataContext:
            client = Catalog.DefaultCatalogService;
            break;
         case ContainerType.FileSystem:
            client = await GetFileSystemProviderAsync(container);
            break;
         default:
            client = Catalog.DefaultCatalogService;
            break;
      }
      return client;
   }

   #endregion

}
