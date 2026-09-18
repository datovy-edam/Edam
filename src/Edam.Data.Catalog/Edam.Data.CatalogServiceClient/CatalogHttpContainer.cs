using Edam.Data.Catalog.Contracts;
using Edam.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
// BL-7.2 sub-step 3b-2 / ADR-0008: Contracts-native REST container surface.
namespace Edam.Data.CatalogServiceClient;

/// <summary>
/// REST-backed container management implementing <see cref="ICatalogContainer"/>
/// over the <c>Contracts</c> wire shapes — no Model EF entities, no CatalogDb.
/// </summary>
public sealed class CatalogHttpContainer : ICatalogContainer
{
   private readonly CatalogHttpClient _client;

   public CatalogHttpContainer(CatalogHttpClient client)
   {
      _client = client;
   }

   public async Task<ContainerInfo?> GetContainerAsync(
      string? containerId, bool checkId = true, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      ContainerInfo? container = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_CONTAINER_ID, containerId);
      var endpoint = CatalogHttpClient.URI_CONTAINER_INFO + pars.ToString();
      try
      {
         container = await _client.Client!
            .GetDataFromJsonAsync<ContainerInfo?>(endpoint).ConfigureAwait(false);
         _client.DefaultContainer = _client.CurrentContainer = container;
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return container;
   }

   public ContainerInfo? GetContainer(string? containerId, bool checkId = true)
   {
      return GetContainerAsync(containerId, checkId).Result;
   }

       public ContainerInfo? GetContainer(Guid containerId)
    {
       _client.ResultsLog.Clear();
       ContainerInfo? container = null;
       var pars = new QueryStringBuilder();
       pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
       pars.Add(CatalogHttpClient.TAG_CONTAINER_GUID, containerId.ToString());
       var endpoint = CatalogHttpClient.URI_CONTAINER_ID + pars.ToString();
       try
       {
          container = _client.Client!
             .GetDataFromJsonAsync<ContainerInfo?>(endpoint).ConfigureAwait(false).GetAwaiter().GetResult();
       }
       catch (Exception ex)
       {
          _client.ResultsLog.Failed(ex);
       }
       return container;
    }

   public ContainerInfo SetContainer(string sessionId, string containerId)
   {
      return GetContainer(containerId) ?? new ContainerInfo(
         Guid.Empty, containerId, "Default", ContainerType.DataContext);
   }

   public async Task<ContainerInfo> EnlistContainerAsync(
      string containerId, string description, string? baseUri = null,
      ContainerType type = ContainerType.DataContext, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      ContainerInfo? container = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_CONTAINER_ID, containerId);
      pars.Add(QueryStringTag.Description, description);
      pars.Add("baseUri", baseUri);
      pars.Add("type", (int)type);
      var endpoint = CatalogHttpClient.URI_CONTAINER_ENLIST + pars.ToString();
      try
      {
         container = await _client.Client!
            .GetDataFromJsonAsync<ContainerInfo?>(endpoint).ConfigureAwait(false);
         _client.DefaultContainer = _client.CurrentContainer = container;
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return container ?? new ContainerInfo(
         Guid.Empty, containerId, description, type, baseUri ?? "");
   }

   public async Task<ContainerInfo> DelistContainerAsync(
      string containerId, string? description = null, string? status = null,
      CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      ContainerInfo? container = null;
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_CONTAINER_ID, containerId);
      var endpoint = CatalogHttpClient.URI_CONTAINER_DELIST + pars.ToString();
      try
      {
         container = await _client.Client!
            .GetDataFromJsonAsync<ContainerInfo?>(endpoint).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return container ?? new ContainerInfo(
         Guid.Empty, containerId, String.Empty, ContainerType.Unknown);
   }

   public ContainerInfo EnlistContainer(
      string containerId, string description, string? baseUri = null,
      ContainerType type = ContainerType.DataContext)
   {
      return EnlistContainerAsync(containerId, description, baseUri, type).Result;
   }

   public ContainerInfo DelistContainer(string containerId)
   {
      return DelistContainerAsync(containerId).Result;
   }

   public async Task<IReadOnlyList<ContainerInfo>> GetContainersAsync(
      CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      IReadOnlyList<ContainerInfo> list = Array.Empty<ContainerInfo>();
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      var endpoint = CatalogHttpClient.URI_CONTAINER_LIST + pars.ToString();
      try
      {
         var data = await _client.Client!
            .GetDataFromJsonAsync<List<ContainerInfo>?>(endpoint).ConfigureAwait(false);
         list = data != null ? (IReadOnlyList<ContainerInfo>)data : Array.Empty<ContainerInfo>();
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
      return list;
   }

   public IReadOnlyList<ContainerInfo> GetContainers()
   {
      return GetContainersAsync().Result;
   }
}
