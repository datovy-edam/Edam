namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// Pure contract for a catalog's <b>content</b> back-end (the blob/binary seam).
/// A catalog resource is addressed by a <b>path/URI-style</b> identifier that may
/// resolve to a file, JSON, XML, or any blob/binary. Providers keep transport and
/// storage details behind this seam (ADR-0006 purity) and are swappable by DI/config —
/// PostgreSQL/file-system today, Azure blob (Wave 2) next — with zero caller changes.
/// </summary>
public interface IContentStore
{
   /// <summary>Open the content at <paramref name="resourcePath"/>, or <c>null</c> if absent.</summary>
   Task<Stream?> OpenReadAsync(string resourcePath, CancellationToken ct = default);

   /// <summary>Write/replace the content at <paramref name="resourcePath"/>.</summary>
   Task WriteAsync(string resourcePath, Stream content, CancellationToken ct = default);

   /// <summary>Delete the content at <paramref name="resourcePath"/>; true if it existed.</summary>
   Task<bool> DeleteAsync(string resourcePath, CancellationToken ct = default);

   /// <summary>True if content exists at <paramref name="resourcePath"/>.</summary>
   Task<bool> ExistsAsync(string resourcePath, CancellationToken ct = default);
}
