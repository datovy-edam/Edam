namespace Edam.Data.Catalog.Contracts;

/// <summary>
/// A named <b>Container</b> bound to a target provider — the model's differentiator.
/// A container's <see cref="Target"/> selects its implementation (FileSystem, PostgreSql, ...),
/// and <see cref="BaseUri"/> is the container's base (file-system root, connection/base URI).
/// Pure value type (ADR-0006): no provider/transport specifics leak in.
/// </summary>
public sealed record ContainerBinding(
   string ContainerId,
   ContainerType Target,
   string? BaseUri = null)
{
   /// <summary>A default container bound to a plain data context.</summary>
   public static ContainerBinding Default(string? baseUri = null)
      => new("default", ContainerType.DataContext, baseUri);
}
