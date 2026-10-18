using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Edam.Data.Projects.Contracts;

/// <summary>
/// The kind of schema a process reads. A person knows THIS — that they are processing an XSD, a JSON schema,
/// JSON-LD, GraphQL or SQL — and does not know what a text map is called, so resolution is by kind and never by
/// file name (S3-2).
/// </summary>
public enum TextMapKind
{
   Other = 0,
   Xsd = 1,
   Json = 2,
   JsonLd = 3,
   Gql = 4,
   Sql = 5
}

/// <summary>One text map that was found and understood.</summary>
/// <param name="TypeId">The canonical type identifier from the file name (e.g. <c>Xsd</c>).</param>
/// <param name="Kind">The kind it serves.</param>
/// <param name="Purpose">The optional dotted purpose, verbatim (e.g. <c>HL7.Fhir</c>).</param>
/// <param name="Name">The file's name.</param>
/// <param name="Address">Where it lives, as an address.</param>
/// <param name="Description">What it does, in words a person can choose from.</param>
public sealed record TextMapOption(
   string TypeId, TextMapKind Kind, string? Purpose,
   string Name, string Address, string Description);

/// <summary>
/// <b>Finds the text maps a project may use</b> (S3-2). It is an interface so that a component presented at
/// project initialization simply <i>is</i> the one the application uses — nothing is hardcoded — and so that
/// <see cref="SupportedKinds"/> can drive exactly what the UI offers: a surface never offers a kind the resolver
/// cannot honour.
/// </summary>
public interface ITextMapCatalog
{
   /// <summary>The kinds this resolver can actually serve — the capability the UI is allowed to offer.</summary>
   IReadOnlyCollection<TextMapKind> SupportedKinds { get; }

   /// <summary>Every text map found for the project, in preference order (the project's own first).</summary>
   Task<IReadOnlyList<TextMapOption>> ListAsync(ProjectInfo project, CancellationToken ct = default);

   /// <summary>The map that serves <paramref name="kind"/>, or null when this resolver has none.</summary>
   Task<TextMapOption?> ResolveAsync(
      ProjectInfo project, TextMapKind kind, CancellationToken ct = default);
}

/// <summary>
/// <b>The one place that owns a text map's identity</b> (S3-3): <c>&lt;TypeId&gt;TextMap[.&lt;Purpose&gt;].json</c>.
/// <para>
/// <c>TypeId</c> is a known identifier (case-insensitive, canonical spelling returned), the literal
/// <c>TextMap</c> sits in the middle, the optional dotted <b>purpose is the whole remainder</b>
/// (<c>XsdTextMap.HL7.Fhir.json</c> → <c>HL7.Fhir</c>) and every dotted segment is also matched as a tag, and the
/// extension is <c>.json</c>. Anything else is <b>refused with a reason</b> — never skipped in silence, because a
/// rejected file and a missing file must not look the same.
/// </para>
/// </summary>
public static class TextMapConvention
{
   /// <summary>The marker that says what the file is.</summary>
   public const string Marker = "TextMap";

   /// <summary>The known type identifiers; the set grows by adding one entry.</summary>
   public static IReadOnlyList<String> KnownTypeIds { get; } = new[]
   {
      "Xsd", "Jsd", "Sql", "Json", "JsonLd", "Gql"
   };

   /// <summary>Map a type identifier to its kind (null when the identifier is not known).</summary>
   public static TextMapKind? ToKind(String? typeId)
   {
      if (String.IsNullOrWhiteSpace(typeId)) return null;

      switch (typeId!.Trim().ToLowerInvariant())
      {
         case "xsd": return TextMapKind.Xsd;
         case "jsd": return TextMapKind.Json;
         case "json": return TextMapKind.Json;
         case "jsonld": return TextMapKind.JsonLd;
         case "gql": return TextMapKind.Gql;
         case "sql": return TextMapKind.Sql;
         default: return null;
      }
   }

   /// <summary>
   /// Read a file name against the convention. Returns the option when it conforms, else <c>null</c> with the
   /// reason in <paramref name="problem"/> — a caller never has to guess why a file was ignored.
   /// </summary>
   /// <param name="name">The file's name (a path is accepted; only the file name is read).</param>
   /// <param name="address">Where the file lives, carried into the option.</param>
   /// <param name="problem">Why the name does not conform (null when it does).</param>
   public static TextMapOption? TryParse(String? name, String? address, out String? problem)
   {
      problem = null;

      if (String.IsNullOrWhiteSpace(name))
      {
         problem = "the file has no name";
         return null;
      }

      var fileName = name!.Replace('\\', '/');
      var slash = fileName.LastIndexOf('/');
      if (slash >= 0) fileName = fileName.Substring(slash + 1);

      var dot = fileName.LastIndexOf('.');
      var extension = dot < 0 ? String.Empty : fileName.Substring(dot);
      if (!String.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
      {
         problem = "a text map must be a .json document (this is '" + extension + "')";
         return null;
      }

      var stem = fileName.Substring(0, dot);
      var marker = stem.IndexOf(Marker, StringComparison.OrdinalIgnoreCase);

      if (marker < 0)
      {
         problem = "the name has no '" + Marker + "' marker";
         return null;
      }
      if (marker == 0)
      {
         problem = "the name must START with the type identifier, before '" + Marker + "'";
         return null;
      }

      var typeId = stem.Substring(0, marker);
      var kind = ToKind(typeId);
      if (kind is null)
      {
         problem = "'" + typeId + "' is not a known type identifier (" +
                   String.Join(", ", KnownTypeIds) + ")";
         return null;
      }

      var remainder = stem.Substring(marker + Marker.Length);
      String? purpose = null;
      if (remainder.Length > 0)
      {
         if (remainder[0] != '.')
         {
            problem = "the text after '" + Marker + "' must be a dotted purpose";
            return null;
         }

         purpose = remainder.Substring(1);
         if (String.IsNullOrWhiteSpace(purpose))
         {
            problem = "a dotted purpose was stated but is empty";
            return null;
         }
      }

      var canonical = KnownTypeIds.First(id =>
         String.Equals(id, typeId, StringComparison.OrdinalIgnoreCase));

      var description = canonical + " types" + (purpose is null ? String.Empty : " · " + purpose);

      return new TextMapOption(
         canonical, kind.Value, purpose, fileName, address ?? fileName, description);
   }

   /// <summary>True when the option's purpose carries <paramref name="tag"/> (a dotted segment counts).</summary>
   public static Boolean HasTag(TextMapOption option, String? tag)
   {
      if (option is null || String.IsNullOrWhiteSpace(tag)) return false;

      if (String.Equals(option.Purpose, tag, StringComparison.OrdinalIgnoreCase)) return true;

      if (String.IsNullOrWhiteSpace(option.Purpose)) return false;

      return option.Purpose!
         .Split('.', StringSplitOptions.RemoveEmptyEntries)
         .Any(segment => String.Equals(segment, tag!.Trim(), StringComparison.OrdinalIgnoreCase));
   }
}
