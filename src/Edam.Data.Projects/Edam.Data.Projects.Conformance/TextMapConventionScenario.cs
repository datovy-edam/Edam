using System;
using System.Collections.Generic;
using System.Linq;
using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>S3-3: the map-file identity convention (ADR-0015)</b> — proven mechanically rather than described.
/// <para>
/// The rule lives in exactly one parser, <see cref="TextMapConvention"/>, so these checks are the enforcement: every
/// name it must ACCEPT (including the dotted purpose and a canonical spelling), every name it must REFUSE **and the
/// reason it gives**, and the capability list a surface is allowed to offer.
/// </para>
/// </summary>
public static class TextMapConventionScenario
{
   public static List<ProjectScenario.Check> Run()
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      // ---- accepted: the shape the specification of this ADR settles ----------------------------------------
      void Accept(string file, TextMapKind kind, string canonical, string? purpose)
      {
         var option = TextMapConvention.TryParse(file, "/TextMaps/" + file, out var problem);

         Check("Accept '" + file + "' as " + kind + (purpose is null ? "" : " (" + purpose + ")"),
            option is not null && option!.Kind == kind &&
            String.Equals(option.TypeId, canonical, StringComparison.Ordinal) &&
            String.Equals(option.Purpose, purpose, StringComparison.Ordinal),
            option is null
               ? "refused: " + problem
               : $"{option.TypeId}/{option.Kind}/purpose='{option.Purpose}' address={option.Address}");
      }

      Accept("XsdTextMap.json", TextMapKind.Xsd, "Xsd", null);
      Accept("SqlTextMap.json", TextMapKind.Sql, "Sql", null);
      Accept("GqlTextMap.json", TextMapKind.Gql, "Gql", null);
      Accept("JsDTextMap.json", TextMapKind.Json, "Jsd", null);        // ids are case-insensitive...
      Accept("XsdTextMap.B2B.json", TextMapKind.Xsd, "Xsd", "B2B");
      Accept("JsdTextMap.HL7.Fhir.json", TextMapKind.Json, "Jsd", "HL7.Fhir");

      // ---- the dotted purpose is the WHOLE remainder, and each segment matches as a tag ---------------------
      var hl7 = TextMapConvention.TryParse("JsdTextMap.HL7.Fhir.json", null, out _);
      Check("A dotted purpose is kept verbatim ('HL7.Fhir') and every segment matches as a tag",
         hl7 is not null &&
         String.Equals(hl7!.Purpose, "HL7.Fhir", StringComparison.Ordinal) &&
         TextMapConvention.HasTag(hl7, "fhir") && TextMapConvention.HasTag(hl7, "HL7") &&
         !TextMapConvention.HasTag(hl7, "B2B"),
         hl7 is null ? "refused" : "'" + hl7.Purpose + "' tags: " +
            String.Join(",", new[] { "fhir", "HL7", "B2B" }.Where(t => TextMapConvention.HasTag(hl7, t))));

      // ---- refused, WITH THE REASON (a rejected file must never look like a missing one) --------------------
      void Refuse(string file, string expectedFragment)
      {
         var option = TextMapConvention.TryParse(file, null, out var problem);

         Check("Refuse '" + file + "' and say why",
            option is null && !String.IsNullOrWhiteSpace(problem) &&
            problem!.IndexOf(expectedFragment, StringComparison.OrdinalIgnoreCase) >= 0,
            option is not null ? "ACCEPTED (wrong)" : problem ?? "(no reason given!)");
      }

      Refuse("MyMapping.json", "TextMap");                 // no marker at all
      Refuse("TextMapXsd.json", "START");                  // marker where the identifier belongs
      Refuse("FooTextMap.json", "not a known type");       // unknown identifier
      Refuse("XsdTextMap.txt", ".json");                   // wrong extension
      Refuse("XsdTextMap..json", "empty");                 // a dotted purpose that is not there
      Refuse("XsdTextMapB2B.json", "dotted purpose");      // text after the marker without the dot

      // ---- capability: every known identifier maps to a kind, so the offered list is derived, not written -----
      var kinds = TextMapConvention.KnownTypeIds
         .Select(id => TextMapConvention.ToKind(id))
         .ToList();

      Check("Every known type identifier maps to a kind (SupportedKinds is derived from them)",
         kinds.All(k => k.HasValue) &&
         TextMapConvention.KnownTypeIds.Count == 6 &&
         TextMapConvention.KnownTypeIds.Contains("Gql"),
         String.Join(",", TextMapConvention.KnownTypeIds) + " -> " +
         String.Join(",", kinds.Select(k => k?.ToString() ?? "?")));

      return checks;
   }
}
