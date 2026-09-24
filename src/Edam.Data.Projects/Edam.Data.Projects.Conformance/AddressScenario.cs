using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>LM-1 (ADR-0011)</b> checks for the address core: <c>catalog://&lt;container&gt;/&lt;path&gt;</c>
/// parse/format, resolution of a <c>*.Args.json</c> reference against its scope (staying in the
/// container), alias expansion for the frequently-referenced locations, and the rejection of
/// non-addresses — drive paths, other schemes, empty containers, unknown aliases and cross-container
/// jumps.
/// </summary>
public static class AddressScenario
{
   public static List<ProjectScenario.Check> Run()
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      var project = CatalogAddress.Parse("catalog://edam.studio/Projects/Datovy.HC.CD");

      // ---- parse / format -------------------------------------------------------------------
      var roundTrip = CatalogAddress.TryParse(
         "catalog://edam.studio/Projects/Datovy.HC.CD/Documents/x.jsd", out var parsed, out var e1);
      Check("Address parses and round-trips",
         roundTrip && parsed.ToString() == "catalog://edam.studio/Projects/Datovy.HC.CD/Documents/x.jsd",
         roundTrip ? parsed.ToString() : e1 ?? "<failed>");

      var root = CatalogAddress.Parse("catalog://edam.studio");
      Check("A container root addresses the container",
         root.IsRoot && root.ToString() == "catalog://edam.studio", root.ToString());

      var normalised = CatalogAddress.Parse("catalog://edam.studio/./Projects//Datovy.HC.CD/");
      Check("An address is normalised ('.', doubled separators, trailing slash)",
         normalised.ToString() == "catalog://edam.studio/Projects/Datovy.HC.CD", normalised.ToString());

      var escaped = CatalogAddress.Parse("catalog://edam.studio/../../x.txt");
      Check("A '..' cannot escape the container",
         escaped.ToString() == "catalog://edam.studio/x.txt", escaped.ToString());

      // ---- rejection ------------------------------------------------------------------------
      Check("A drive path is not an address",
         !CatalogAddress.TryParse(@"C:\prjs\Datovy.Edam\Edam.App.Data", out _, out _),
         @"C:\prjs\… rejected");

      Check("Another scheme is not a catalog address",
         !CatalogAddress.TryParse("file:///D:/x", out _, out _), "file:// rejected");

      Check("An address must name a container",
         !CatalogAddress.TryParse("catalog:///x", out _, out _), "catalog:///x rejected");

      Check("An empty address is rejected",
         !CatalogAddress.TryParse("   ", out _, out _), "blank rejected");

      // ---- a *.Args.json reference resolves inside the project (same container) -------------
      var resolved = project.TryResolve("./Archive/x.xsd", out var input, out var e2)
         && input.ToString() == "catalog://edam.studio/Projects/Datovy.HC.CD/Archive/x.xsd";
      Check("A './Archive' reference resolves inside the project, same container",
         resolved, resolved ? input.ToString() : e2 ?? "<failed>");

      var output = project.TryResolve("./Documents/dictionary.jsd", out var document, out var e3)
         && document.ToString() == "catalog://edam.studio/Projects/Datovy.HC.CD/Documents/dictionary.jsd";
      Check("A './Documents' reference resolves inside the project",
         output, output ? document.ToString() : e3 ?? "<failed>");

      var up = project.TryResolve("../Files/a.txt", out var sibling, out var e4)
         && sibling.ToString() == "catalog://edam.studio/Projects/Files/a.txt";
      Check("A '..' reference resolves relative to the scope root (cannot leave the container)",
         up, up ? sibling.ToString() : e4 ?? "<failed>");

      Check("A full reference to the SAME container is allowed",
         project.TryResolve("catalog://EDAM.STUDIO/Templates/ToAssets.Args.json", out var same, out _)
         && same.ToString() == "catalog://EDAM.STUDIO/Templates/ToAssets.Args.json",
         "same container accepted");

      Check("A reference may not change container",
         !project.TryResolve("catalog://other/Templates/ToAssets.Args.json", out _, out _),
         "cross-container rejected");

      // ---- aliases over the frequently-referenced locations ---------------------------------
      var docs = ProjectLocations.TryResolve("project:documents", project, out var docsAddress, out var e5)
         && docsAddress.ToString() == "catalog://edam.studio/Projects/Datovy.HC.CD/Documents";
      Check("'project:documents' expands to the project's Documents location",
         docs, docs ? docsAddress.ToString() : e5 ?? "<failed>");

      var args = ProjectLocations.TryResolve(
         "project:arguments/0001.HC.CD.ToAssets.Args.json", project, out var argsAddress, out var e6)
         && argsAddress.ToString() ==
            "catalog://edam.studio/Projects/Datovy.HC.CD/Arguments/0001.HC.CD.ToAssets.Args.json";
      Check("'project:arguments/<file>' expands to a file inside the project",
         args, args ? argsAddress.ToString() : e6 ?? "<failed>");

      var template = ProjectLocations.TryResolve("app:templates", project, out var appAddress, out var e7)
         && appAddress.ToString() == "catalog://edam.studio/Templates";
      Check("'app:templates' expands to the container root (not the project)",
         template, template ? appAddress.ToString() : e7 ?? "<failed>");

      Check("An unknown alias scheme is rejected",
         !ProjectLocations.TryResolve("bogus:documents", project, out _, out _), "bogus: rejected");

      Check("An unknown project folder is rejected",
         !ProjectLocations.TryResolve("project:nope", project, out _, out _), "project:nope rejected");

      // ---- the scaffolder/seeding shape from ADR-0011 (read an address, write an address) ----
      var seedSource = ProjectLocations.TryResolve("app:templates", project, out var from, out _);
      var seedTarget = ProjectLocations.TryResolve(
         "project:arguments/Datovy.HC.CD.ToAssets.Args.json", project, out var to, out _);
      Check("Seeding is 'read an address, write an address' (template -> project Arguments)",
         seedSource && seedTarget &&
         from.ToString() == "catalog://edam.studio/Templates" &&
         to.ToString() == "catalog://edam.studio/Projects/Datovy.HC.CD/Arguments/Datovy.HC.CD.ToAssets.Args.json",
         $"{from} -> {to}");

      return checks;
   }
}
