using Edam.Data.Catalog.Contracts;
using Edam.DataObjects.Requests;
using Edam.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// -----------------------------------------------------------------------------
// BL-7.x / ADR-0007/0008: the content (blob/binary) surface of the Contracts-native
// catalog REST client. Content is addressed path/URI-style and travels base64-encoded
// in ContentInfo so the JSON wire stays a single canonical contract — the
// stream-based IContentStore seam is preserved for callers.
namespace Edam.Data.CatalogServiceClient;

/// <summary>
/// REST-backed <see cref="IContentStore"/>: opens/writes/deletes/exists-checks path-addressed
/// content through the catalog service. Binary-safe (base64 on the wire); provider details
/// (file-system, PostgreSQL, future blob) stay server-side behind the seam.
/// </summary>
public sealed class CatalogHttpContent : IContentStore
{
   private readonly CatalogHttpClient _client;

   public CatalogHttpContent(CatalogHttpClient client)
   {
      _client = client;
   }

   public async Task<bool> ExistsAsync(string resourcePath, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_RESOURCE_PATH, resourcePath);
      var endpoint = CatalogHttpClient.URI_CONTENT_INFO + pars.ToString();
      try
      {
         var info = await _client.Client!
            .GetDataFromJsonAsync<ContentInfo?>(endpoint).ConfigureAwait(false);
         return info?.Exists == true;
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
         return false;
      }
   }

   public async Task<Stream?> OpenReadAsync(string resourcePath, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_RESOURCE_PATH, resourcePath);
      var endpoint = CatalogHttpClient.URI_CONTENT_ITEM + pars.ToString();
      try
      {
         var info = await _client.Client!
            .GetDataFromJsonAsync<ContentInfo?>(endpoint).ConfigureAwait(false);
         if (info?.ContentBase64 is null)
            return null;
         return new MemoryStream(System.Convert.FromBase64String(info.ContentBase64));
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
         return null;
      }
   }

   public async Task WriteAsync(
      string resourcePath, Stream content, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      using var ms = new MemoryStream();
      await content.CopyToAsync(ms, ct).ConfigureAwait(false);
      var payload = new ContentInfo(
         resourcePath, true, ms.Length, "application/octet-stream",
         System.Convert.ToBase64String(ms.ToArray()));

      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      var endpoint = CatalogHttpClient.URI_CONTENT_ITEM + pars.ToString();
      try
      {
         await _client.Client!
            .PostAsync<ContentInfo?>(endpoint, payload).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
         _client.ResultsLog.Failed(ex);
      }
   }

   public async Task<bool> DeleteAsync(string resourcePath, CancellationToken ct = default)
   {
      _client.ResultsLog.Clear();
      var pars = new QueryStringBuilder();
      pars.Add(QueryStringTag.SessionId, _client.LastSessionId);
      pars.Add(CatalogHttpClient.TAG_RESOURCE_PATH, resourcePath);
      var endpoint = CatalogHttpClient.URI_CONTENT_ITEM + pars.ToString();
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
}
