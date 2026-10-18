using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Edam.Data.Projects.Contracts;

namespace Edam.WinUI.Controls.Projects;

/// <summary>
/// <b>S3-4: what a document created from a KIND looks like.</b> The person says what kind of schema they process —
/// XSD, JSON schema, JSON-LD, GraphQL, SQL — and the skeleton derives the extension, a procedure the runner really
/// knows, and a text map <b>found by the convention</b> (ADR-0015) rather than a path invented here.
/// </summary>
public static class ArgumentsScaffold
{
   /// <summary>A kind, as offered: the label the person chooses and the kind it stands for.</summary>
   public sealed record KindChoice(TextMapKind Kind, String Label);

   /// <summary>
   /// The kinds this surface may offer: exactly the ones the convention knows, so nothing is offered that the
   /// resolution cannot answer (the resolver's <c>SupportedKinds</c> is derived from the same list).
   /// </summary>
   public static IReadOnlyList<KindChoice> Choices { get; } = new[]
   {
      new KindChoice(TextMapKind.Xsd, "XSD — an XML schema (.xsd)"),
      new KindChoice(TextMapKind.Json, "JSON schema (.json)"),
      new KindChoice(TextMapKind.JsonLd, "JSON-LD (.jsonld)"),
      new KindChoice(TextMapKind.Gql, "GraphQL (.graphql)"),
      new KindChoice(TextMapKind.Sql, "SQL / DDL (.sql)"),
   };

   /// <summary>The input file's extension for a kind (no leading dot).</summary>
   public static String Extension(TextMapKind kind) => kind switch
   {
      TextMapKind.Xsd => "xsd",
      TextMapKind.Json => "json",
      TextMapKind.JsonLd => "jsonld",
      TextMapKind.Gql => "graphql",
      TextMapKind.Sql => "sql",
      _ => "xsd",
   };

   /// <summary>
   /// A procedure the runner really knows for a kind (<c>Edam.Data.AssetConsole.AssetConsoleProcedure</c>). Only
   /// procedures that exist are named here — a skeleton must never assert a process that cannot run.
   /// </summary>
   public static String Procedure(TextMapKind kind) => kind switch
   {
      TextMapKind.Xsd => "XsdToAssets",
      TextMapKind.Json => "JsonToAssets",
      TextMapKind.JsonLd => "JsonToAssets",     // JSON-LD is JSON
      TextMapKind.Gql => "JsonToAssets",        // a GraphQL document travels as JSON
      TextMapKind.Sql => "DdlImportToAssets",
      _ => "XsdToAssets",
   };

   /// <summary>One text map found in a folder, by the convention's own rule.</summary>
   public sealed record FoundMap(String Name, String FullPath, TextMapOption Option);

   /// <summary>
   /// Find the maps a folder holds, keeping only what the convention accepts — and reporting every file it
   /// ignored, with the reason (ADR-0015: a rejected file must never look like a missing one).
   /// </summary>
   public static IReadOnlyList<FoundMap> FindIn(
      String? folder, Action<String>? report = null)
   {
      var found = new List<FoundMap>();
      if (String.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return found;

      try
      {
         foreach (var file in Directory.EnumerateFiles(folder!, "*.json"))
         {
            var option = TextMapConvention.TryParse(Path.GetFileName(file), file, out var problem);
            if (option is null)
            {
               report?.Invoke("Ignored the text map '" + Path.GetFileName(file) + "': " + problem);
               continue;
            }

            found.Add(new FoundMap(option.Name, file, option));
         }
      }
      catch (Exception ex)
      {
         report?.Invoke("The text maps folder '" + folder + "' could not be read: " + ex.Message);
      }

      return found;
   }

   /// <summary>The map that serves <paramref name="kind"/> (null when the folder has none for it).</summary>
   public static FoundMap? ForKind(IReadOnlyList<FoundMap> maps, TextMapKind kind)
      => maps?.FirstOrDefault(map => map.Option.Kind == kind);
}
