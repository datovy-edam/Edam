using Edam.Data.Catalog.Contracts;
using Edam.Diagnostics;
using Edam.Net;
using Edam.Net.Web;
using Edam.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
// BL-7.2 sub-step 3b-2 / ADR-0008: a **Contracts-native** catalog REST client.
// This client implements the Edam.Data.Catalog.Contracts interfaces and
// deserializes the service's Contracts value-record wire shapes (ContainerInfo,
// ItemInfo, ItemDataInfo, ContentTypeInfo) — i.e. the *single canonical wire/
// domain contract* (ADR-0008). It has NO dependency on Edam.Data.CatalogModel's
// EF entity set and NO dependency on Edam.Data.CatalogDb. The HTTP routes mirror
// the relocated CatalogServiceMap. This is the transition client the WinUI /
// step-5 consumers bind to; the Model-based CatalogClient/ClientCatalog* path is
// retired in BL-7.2 step 4.
//
// Resource addressing is path/URI-based (ADR-0007): FullPath / ContainerUri select
// a container, item, or data leaf on the wire.
namespace Edam.Data.CatalogServiceClient;

/// <summary>
/// A connectable catalog REST client over the <c>Contracts</c> wire/domain shapes.
/// </summary>
public sealed class CatalogHttpClient : ICatalogClient
{
   // Wire constants — copied out of the retired Model CatalogBaseClient so this
   // HTTP path no longer depends on the Model tree-builder layer.
   public const string TAG_CONTAINER_ID = "containerId";
   public const string TAG_CONTAINER_GUID = "id";
   public const string TAG_ITEM_ID = "id";
   public const string TAG_ITEM_PATH = "path";
   public const string TAG_ITEM_NAME = "name";
   public const string TAG_DATA_ID = "id";
   public const string TAG_CONTENT_TYPE_ID = "contentTypeId";

   public const string URI_SESSION_INFO = "session/info";
   public const string URI_CONTAINER_ID = "container/id";
   public const string URI_CONTAINER_INFO = "container/info";
   public const string URI_CONTAINER_LIST = "container/list";
   public const string URI_CONTAINER_ENLIST = "container/enlist";
   public const string URI_CONTAINER_DELIST = "container/delist";
   public const string URI_CONTAINER_ROOT_ITEM_ID = "container/item/root/id";
   public const string URI_CONTAINER_ITEMS = "container/items/id";
   public const string URI_ITEM_ADD = "catalog/item";
   public const string URI_ITEM_ID = "catalog/item/id";
   public const string URI_ITEM_PATH = "catalog/item/path";
   public const string URI_BRANCH_ITEMS = "catalog/branch/items";
   public const string URI_ITEM_DATA_ITEM_ID = "catalog/data/item/id";
   public const string URI_ITEM_DATA_ITEM_NAME = "catalog/data/item/name";
   public const string URI_DATA_ID = "catalog/data/id";
   public const string URI_CONTENT_TYPE_ID = "catalog/content/type/id";

   private readonly HttpRequestInfo _httpRequestInfo;
   private string _sessionId;
   private string _defaultContainerId = "default";
   private IResultsLog _resultsLog = new ResultLog();

   private WebApiClient? _client;
   private CatalogInfo? _catalog;
   private ContainerInfo? _defaultContainer;
   private ContainerInfo? _currentContainer;

   private readonly ICatalogContainer _container;
   private readonly ICatalogItem _item;
   private readonly ICatalogItemData _itemData;

   /// <summary>A public, stable identifier for the catalog this client serves.</summary>
   public CatalogInfo? Catalog => _catalog;

   /// <summary>Default container id used when the server is asked for one.</summary>
   public string DefaultContainerId { get => _defaultContainerId; set => _defaultContainerId = value; }

   /// <summary>The default (initial) container returned by InitializeClient*.</summary>
   public ContainerInfo? DefaultContainer { get => _defaultContainer; set => _defaultContainer = value; }

   /// <summary>The currently selected container.</summary>
   public ContainerInfo? CurrentContainer { get => _currentContainer; set => _currentContainer = value; }

   /// <summary>Container management surface (REST-backed).</summary>
   public ICatalogContainer Container => _container;

   /// <summary>Item/branch management surface (REST-backed).</summary>
   public ICatalogItem Item => _item;

   /// <summary>Item-data-leaf management surface (REST-backed).</summary>
   public ICatalogItemData ItemData => _itemData;

   /// <summary>Underlying Web API client (exposed for the sibling REST surfaces).</summary>
   public WebApiClient? Client => _client;

   /// <summary>Current session id.</summary>
   public string LastSessionId => _sessionId;

   /// <summary>Diagnostics log collected while processing requests.</summary>
   public IResultsLog ResultsLog => _resultsLog;

   public CatalogHttpClient(string sessionId, string? baseUri = null)
   {
      _sessionId = sessionId;
      var req = new HttpRequestInfo();
      req.BaseUri = String.IsNullOrWhiteSpace(baseUri)
         ? "https://localhost:7069/catalogservice/"
         : baseUri;
      req.ContentType = WebApiContentType.ApplicationJson;
      _httpRequestInfo = req;
      _container = new CatalogHttpContainer(this);
      _item = new CatalogHttpItem(this);
      _itemData = new CatalogHttpItemData(this);
   }

   public CatalogHttpClient(string sessionId, HttpRequestInfo connectionInfo)
   {
      _sessionId = sessionId;
      _httpRequestInfo = connectionInfo;
      _container = new CatalogHttpContainer(this);
      _item = new CatalogHttpItem(this);
      _itemData = new CatalogHttpItemData(this);
   }

   /// <summary>Get the default container for a session (session/info).</summary>
   public async Task<ContainerInfo> InitializeClientAsync(
      string sessionId, string? containerId = null, CancellationToken ct = default)
   {
      if (_client == null)
      {
         _resultsLog.Clear();
         if (String.IsNullOrWhiteSpace(_sessionId))
            _sessionId = sessionId;
         _client = new WebApiClient(_httpRequestInfo);
      }

      ContainerInfo? container = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _sessionId);
      pars.Add(TAG_CONTAINER_ID, containerId);
      var endpoint = URI_SESSION_INFO + pars.ToString();
      try
      {
         container = await _client.GetDataFromJsonAsync<ContainerInfo?>(endpoint)
            .ConfigureAwait(false);
         _defaultContainer = _currentContainer = container;
      }
      catch (Exception ex)
      {
         _resultsLog.Failed(ex);
      }
      return container ?? new ContainerInfo(
         Guid.Empty, String.Empty, "Default", ContainerType.DataContext);
   }

   /// <summary>Initialize this client against a default container (blocking wrapper).</summary>
   public ContainerInfo? InitializeClient(string sessionId, string? containerId = null)
   {
      var result = InitializeClientAsync(sessionId, containerId).Result;
      return result;
   }

   /// <summary>Get a content type descriptor by id.</summary>
   public async Task<ContentTypeInfo?> GetContentTypeAsync(
      string contentTypeId, CancellationToken ct = default)
   {
      _resultsLog.Clear();
      ContentTypeInfo? item = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _sessionId);
      pars.Add(TAG_CONTENT_TYPE_ID, contentTypeId);
      var endpoint = URI_CONTENT_TYPE_ID + pars.ToString();
      try
      {
         item = await _client!.GetDataFromJsonAsync<ContentTypeInfo?>(endpoint)
            .ConfigureAwait(false);
      }
      catch (Exception ex)
      {
         _resultsLog.Failed(ex);
      }
      return item;
   }

   /// <summary>Get a content type descriptor by id (blocking wrapper).</summary>
   public ContentTypeInfo? GetContentType(string contentTypeId)
   {
      return GetContentTypeAsync(contentTypeId).Result;
   }
}
