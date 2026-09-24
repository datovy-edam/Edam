using System;
using System.Linq;

namespace Edam.Data.Projects.Contracts;

/// <summary>
/// The <b>frequently-referenced locations</b> of a project and of the application, expressed as
/// aliases over <see cref="CatalogAddress"/> (ADR-0011).
/// <para>
/// These names exist because they are referenced often — they are <b>conveniences, not the model</b>.
/// Any artifact may live at any path in any container, projects are one location family among many,
/// and <c>Projects/</c> is not a privileged root: a new artifact kind needs no new configuration key,
/// only a path.
/// </para>
/// <list type="bullet">
/// <item><c>project:&lt;folder&gt;[/&lt;rest&gt;]</c> — inside the addressed project
/// (<see cref="ProjectFolders"/>), e.g. <c>project:documents</c>,
/// <c>project:arguments/0001.HC.CD.ToAssets.Args.json</c></item>
/// <item><c>app:&lt;folder&gt;[/&lt;rest&gt;]</c> — at the <b>container root</b>
/// (<see cref="AppFolders"/>), e.g. <c>app:templates</c></item>
/// </list>
/// </summary>
public static class ProjectLocations
{
   /// <summary>The alias scheme for a project's own folders.</summary>
   public const string ProjectScheme = "project";

   /// <summary>The alias scheme for application-level folders.</summary>
   public const string AppScheme = "app";

   /// <summary>The folder that holds a collection's projects (the project family's root segment).</summary>
   public const string Projects = "Projects";

   /// <summary>Application-level folder: shared templates (args definitions, DDL, …).</summary>
   public const string Templates = "Templates";

   /// <summary>Application-level folder: shared text maps.</summary>
   public const string TextMaps = "TextMaps";

   /// <summary>Application-level folder: shared samples.</summary>
   public const string Samples = "Samples";

   /// <summary>Application-level folder: scratch space (never a catalog container).</summary>
   public const string Temp = "Temp";

   /// <summary>The application-level folder names (frequently referenced, at a container root).</summary>
   public static readonly string[] AppFolders = { Templates, TextMaps, Samples, Temp };

   /// <summary>
   /// Expand an alias into an address. <paramref name="scope"/> is the addressed project for
   /// <c>project:</c> aliases, and supplies the container for <c>app:</c> aliases.
   /// </summary>
   public static bool TryResolve(
      string? alias, CatalogAddress scope, out CatalogAddress address, out string? error)
   {
      address = default;
      error = null;

      if (string.IsNullOrWhiteSpace(alias))
      {
         error = "A location alias is required.";
         return false;
      }

      var text = alias!.Trim();
      var colon = text.IndexOf(':');
      if (colon <= 0)
      {
         error = $"'{text}' is not a location alias (expected 'project:<folder>' or 'app:<folder>').";
         return false;
      }

      var scheme = text[..colon].ToLowerInvariant();
      var remainder = text[(colon + 1)..].Trim('/');
      if (remainder.Length == 0)
      {
         error = $"'{text}' must name a folder.";
         return false;
      }

      var slash = remainder.IndexOf('/');
      var folder = slash < 0 ? remainder : remainder[..slash];
      var rest = slash < 0 ? null : remainder[(slash + 1)..];

      switch (scheme)
      {
         case ProjectScheme:
            // the alias is case-insensitive, but the address must use the CANONICAL folder name
            var projectFolder = ProjectFolders.All.FirstOrDefault(
               f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
            if (projectFolder is null)
            {
               error = $"'{folder}' is not a project folder ({string.Join(", ", ProjectFolders.All)}).";
               return false;
            }
            address = scope.Combine(projectFolder);
            break;

         case AppScheme:
            var appFolder = AppFolders.FirstOrDefault(
               f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
            if (appFolder is null)
            {
               error = $"'{folder}' is not an application folder ({string.Join(", ", AppFolders)}).";
               return false;
            }
            address = CatalogAddress.ForContainer(scope.Container).Combine(appFolder);
            break;

         default:
            error = $"Unsupported location scheme '{scheme}'; expected '{ProjectScheme}' or " +
                    $"'{AppScheme}'.";
            return false;
      }

      if (!string.IsNullOrWhiteSpace(rest)) address = address.Combine(rest);
      return true;
   }
}
