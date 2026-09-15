namespace Edam.Data.Catalog.Contracts;

/// <summary>Catalog content-type descriptor (a named, addressed content type). Pure value type.</summary>
public sealed record ContentTypeInfo(string TypeId, string? Description = null);
