namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// A path-addressed <b>content</b> descriptor/payload on the catalog wire (ADR-0007/0008).
/// <see cref="IContentStore"/> is the provider/client seam (stream-based); this is its
/// <b>wire shape</b>. Binary content travels <see cref="ContentBase64"/>-encoded so the JSON
/// wire stays the single canonical contract, and any blob/file/JSON/XML payload round-trips
/// binary-safely. <see cref="ContentBase64"/> is null when only the descriptor is requested
/// (or when the resource is absent).
/// </summary>
public sealed record ContentInfo(
   string ResourcePath,
   bool Exists = false,
   long Length = 0,
   string ContentType = "application/octet-stream",
   string? ContentBase64 = null);
