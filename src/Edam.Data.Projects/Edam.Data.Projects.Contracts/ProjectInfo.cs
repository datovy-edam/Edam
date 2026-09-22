namespace Edam.Data.Projects.Contracts;

/// <summary>
/// A Project — a named, self-contained set of processing resources (argument definitions, input
/// files, output documents, archived artifacts and Use Case definitions).
/// <para>
/// Identity mirrors the <c>Project</c> section of a <c>*.Args.json</c> file
/// (<c>Name</c> + <c>VersionId</c>), extended with the collection it belongs to and its address.
/// The spec's rule holds: <see cref="Name"/> must match the project folder name.
/// </para>
/// </summary>
/// <param name="ProjectId">Stable id (provider-assigned; the item/container id in a catalog).</param>
/// <param name="Name">Project name — matches the project folder name.</param>
/// <param name="VersionId">Project version (e.g. <c>v1r0</c>), stamped onto produced assets.</param>
/// <param name="CollectionId">The collection this project belongs to.</param>
/// <param name="Path">The project's address within its collection.</param>
/// <param name="Description">Optional description.</param>
public sealed record ProjectInfo(
   string ProjectId,
   string Name,
   string VersionId,
   string CollectionId,
   ProjectPath Path,
   string? Description = null);

/// <summary>
/// The fixed folder vocabulary of a Project (EDAM Studio "Understanding Projects" §2.2).
/// Use these instead of magic strings; <see cref="Path"/> builds the project-relative address.
/// </summary>
public static class ProjectFolders
{
   public const string Archive = "Archive";
   public const string Arguments = "Arguments";
   public const string Documents = "Documents";
   public const string Files = "Files";
   public const string Samples = "Samples";
   public const string UseCases = "UseCases";
   public const string Libraries = "Libraries";
   public const string TextMaps = "TextMaps";

   /// <summary>All standard project folder names (the structure created for a new project).</summary>
   public static readonly string[] All =
   {
      Archive, Arguments, Documents, Files, Samples, UseCases, Libraries, TextMaps,
   };

   /// <summary>The project-relative <see cref="ProjectPath"/> of a folder (e.g. <c>/Archive</c>).</summary>
   public static ProjectPath Path(string folder) => ProjectPath.Parse("/" + folder);
}
