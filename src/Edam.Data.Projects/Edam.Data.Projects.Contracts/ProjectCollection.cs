namespace Edam.Data.Projects.Contracts;

/// <summary>
/// What kind of location a <see cref="ProjectCollectionInfo"/> points at. Mirrors the existing
/// <c>UriType</c> value used by <c>Edam.Settings.json</c> (<c>1 = Console Path</c>), so today's
/// configuration keeps working unchanged.
/// </summary>
public enum ProjectCollectionType
{
   Unknown = 0,

   /// <summary>A file-system folder path (the classic EDAM app-data / collection root).</summary>
   ConsolePath = 1,

   /// <summary>A Catalog-backed collection (projects/artifacts stored in the catalog).</summary>
   Catalog = 2,
}

/// <summary>
/// A registered <b>Project Collection</b> — a location that contains Projects. Mirrors an entry in
/// the <c>UriList</c> of <c>Edam.Settings.json</c> (<c>Name</c> / <c>Type</c> / <c>UriText</c>), and
/// is the unit a consumer selects from before browsing projects.
/// </summary>
/// <param name="CollectionId">Stable id (the catalog container id, or the collection name).</param>
/// <param name="Name">Display name; for file-system collections it matches the folder name.</param>
/// <param name="Uri">Where the collection lives (folder path today; catalog/URI later).</param>
/// <param name="Type">How <paramref name="Uri"/> should be interpreted.</param>
/// <param name="IsDefault">True for the default collection (required for the app to operate).</param>
public sealed record ProjectCollectionInfo(
   string CollectionId,
   string Name,
   string Uri,
   ProjectCollectionType Type = ProjectCollectionType.ConsolePath,
   bool IsDefault = false);

/// <summary>
/// The folder names found directly under a collection / the app-data root
/// (EDAM Studio "Understanding Projects" §2.1).
/// </summary>
public static class ProjectCollectionFolders
{
   public const string Arguments = "Arguments";
   public const string Documents = "Documents";
   public const string Files = "Files";
   public const string Projects = "Projects";
   public const string Samples = "Samples";
   public const string Temp = "Temp";
   public const string Templates = "Templates";
   public const string TextMaps = "TextMaps";
}
