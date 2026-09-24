namespace Edam.Data.Projects.Contracts;

/// <summary>
/// The address of a location or artifact in the Catalog: <b>container + path</b>, written as one URI —
/// <c>catalog://&lt;container&gt;/&lt;path&gt;</c> (ADR-0011).
/// <para>
/// The <b>container is the authority</b> and the path is the "file-system-like" catalog path, so the
/// same address is valid whether the container is backed by a disk, PostgreSQL or a service — and two
/// containers may hold identical paths without colliding, because the container is part of the
/// <b>address</b> rather than part of the path.
/// </para>
/// <para>
/// A relative reference (e.g. <c>./Archive/x.xsd</c> in a <c>*.Args.json</c>) resolves against the
/// <b>scope</b> address — the project root — and stays in the same container
/// (<see cref="TryResolve"/>). Nothing here touches the process current directory.
/// </para>
/// </summary>
/// <param name="Container">The container (the collection) that holds the artifact.</param>
/// <param name="Path">The container-relative catalog path.</param>
public readonly record struct CatalogAddress(string Container, ProjectPath Path)
{
   /// <summary>The address scheme.</summary>
   public const string Scheme = "catalog";

   /// <summary>The address of a container root: <c>catalog://&lt;container&gt;</c>.</summary>
   public static CatalogAddress ForContainer(string container)
      => new(container ?? string.Empty, ProjectPath.Root);

   /// <summary>True when this addresses the container root (no path).</summary>
   public bool IsRoot => Path.IsRoot;

   /// <summary>Append a relative segment inside the same container.</summary>
   public CatalogAddress Combine(string? relative) => new(Container, Path.Combine(relative));

   /// <summary>The parent location (same container).</summary>
   public CatalogAddress Parent => new(Container, Path.Parent);

   /// <summary>The address as text — <c>catalog://&lt;container&gt;/&lt;path&gt;</c>.</summary>
   public override string ToString()
      => Path.IsRoot
         ? $"{Scheme}://{Container}"
         : $"{Scheme}://{Container}{Path.Value}";

   /// <summary>Parse an address; throws <see cref="FormatException"/> with the reason when invalid.</summary>
   public static CatalogAddress Parse(string? value)
      => TryParse(value, out var address, out var error) ? address : throw new FormatException(error);

   /// <summary>
   /// Parse <c>catalog://&lt;container&gt;/&lt;path&gt;</c>. A plain path or a drive path
   /// (<c>C:\…</c>) is <b>not</b> an address and is rejected (ADR-0011).
   /// </summary>
   public static bool TryParse(string? value, out CatalogAddress address, out string? error)
   {
      address = default;
      error = null;

      if (string.IsNullOrWhiteSpace(value))
      {
         error = "An address is required.";
         return false;
      }

      var text = value!.Trim();
      var separator = text.IndexOf("://", StringComparison.Ordinal);
      if (separator <= 0)
      {
         error = "An address is written as catalog://<container>/<path> — a plain or drive path is " +
                 "not an address.";
         return false;
      }

      var scheme = text[..separator];
      if (!string.Equals(scheme, Scheme, StringComparison.OrdinalIgnoreCase))
      {
         error = $"Unsupported address scheme '{scheme}'; expected '{Scheme}'.";
         return false;
      }

      var remainder = text[(separator + 3)..];
      var slash = remainder.IndexOf('/');
      var container = slash < 0 ? remainder : remainder[..slash];
      var path = slash < 0 ? "/" : remainder[slash..];

      if (string.IsNullOrWhiteSpace(container))
      {
         error = "An address must name a container.";
         return false;
      }

      if (!IsValidContainer(container))
      {
         error = $"'{container}' is not a valid container name (letters, digits, '.', '-' and '_' only).";
         return false;
      }

      address = new CatalogAddress(container, ProjectPath.Parse(path));
      return true;
   }

   /// <summary>
   /// Resolve a reference <b>against this address as the scope root</b> (for a <c>*.Args.json</c> that
   /// scope is the project root).
   /// <para>
   /// A relative reference (<c>./Archive/x.xsd</c>, <c>Files</c>) combines with the scope and stays in
   /// the <b>same container</b>; a full <c>catalog://</c> reference is accepted only when it names the
   /// same container (no silent cross-container jump); <c>..</c> cannot escape the container root.
   /// </para>
   /// </summary>
   public bool TryResolve(string? reference, out CatalogAddress address, out string? error)
   {
      address = default;
      error = null;

      if (string.IsNullOrWhiteSpace(reference))
      {
         address = this;
         return true;
      }

      var text = reference!.Trim();

      if (text.Contains("://", StringComparison.Ordinal))
      {
         if (!TryParse(text, out var absolute, out error)) return false;

         if (!string.Equals(absolute.Container, Container, StringComparison.OrdinalIgnoreCase))
         {
            error = $"Reference '{text}' names container '{absolute.Container}' but the scope is " +
                    $"'{Container}'; a reference may not change container.";
            return false;
         }

         address = absolute;
         return true;
      }

      address = Combine(text);
      return true;
   }

   private static bool IsValidContainer(string container)
   {
      foreach (var c in container)
      {
         if (!char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_') return false;
      }
      return true;
   }
}
