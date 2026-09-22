namespace Edam.Data.Projects.Contracts;

/// <summary>Outcome of importing a project into a collection.</summary>
/// <param name="Project">The imported project.</param>
/// <param name="FolderCount">Folders created (project folders such as Archive/Arguments/…).</param>
/// <param name="FileCount">Files/artifacts whose content was stored.</param>
/// <param name="Issues">Non-fatal problems encountered while importing (skipped files, etc.).</param>
public sealed record ProjectImportResult(
   ProjectInfo Project,
   int FolderCount = 0,
   int FileCount = 0,
   IReadOnlyList<string>? Issues = null);

/// <summary>
/// <b>Lifecycle and transfer</b> of projects. <see cref="ImportAsync"/> is the "upload" side —
/// read a project from a file-system folder (or any source location) and store it in the
/// collection; <see cref="ExportAsync"/> is the "download" side — materialize a project back out
/// to a folder. Neither assumes the collection itself is a file system.
/// </summary>
public interface IProjectStore
{
   /// <summary>Create a project with the standard folder structure (see <see cref="ProjectFolders"/>).</summary>
   Task<ProjectInfo> CreateAsync(
      string collectionId, string name, string? description = null, CancellationToken ct = default);

   /// <summary>
   /// Import ("upload") a project from <paramref name="sourceLocation"/> (a folder path today)
   /// into the collection. The project name defaults to the source folder name.
   /// </summary>
   Task<ProjectImportResult> ImportAsync(
      string collectionId, string sourceLocation, string? projectName = null,
      CancellationToken ct = default);

   /// <summary>Export ("download") a project out to <paramref name="targetLocation"/>.</summary>
   Task ExportAsync(
      string collectionId, string projectName, string targetLocation, CancellationToken ct = default);

   /// <summary>Delete a project (and its artifacts) from the collection.</summary>
   Task<bool> DeleteAsync(
      string collectionId, string projectName, CancellationToken ct = default);
}
