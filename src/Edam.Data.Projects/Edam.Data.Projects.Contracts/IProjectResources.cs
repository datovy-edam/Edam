namespace Edam.Data.Projects.Contracts;

/// <summary>
/// <b>The hinge.</b> Access to a project's resources by <see cref="ProjectPath"/> — open, write,
/// list, create folder, delete — with no knowledge of the underlying storage and <b>no process
/// current-directory</b>. This is the interface that replaces the file-system calls plus the
/// <c>Directory.SetCurrentDirectory</c> dance the asset pipeline relies on today.
/// <para>
/// A file-system implementation keeps today's behaviour; a Catalog implementation resolves the
/// same paths to catalog items and <c>IContentStore</c> content (binary-safe), so inputs
/// (<c>./Archive/*.xlsx</c>) and outputs (<c>./Documents/*</c>) can live in the catalog.
/// </para>
/// </summary>
public interface IProjectResources
{
   /// <summary>
   /// List the resources directly or recursively under <paramref name="folder"/>. When
   /// <paramref name="extension"/> is supplied (with or without the leading dot, case-insensitive)
   /// only files with that extension are returned.
   /// </summary>
   Task<IReadOnlyList<ProjectResourceInfo>> ListAsync(
      ProjectInfo project, ProjectPath folder, string? extension = null,
      CancellationToken ct = default);

   /// <summary>True when a resource exists at <paramref name="path"/>.</summary>
   Task<bool> ExistsAsync(
      ProjectInfo project, ProjectPath path, CancellationToken ct = default);

   /// <summary>Open a resource for reading, or <c>null</c> when it does not exist.</summary>
   Task<Stream?> OpenReadAsync(
      ProjectInfo project, ProjectPath path, CancellationToken ct = default);

   /// <summary>Write (create or replace) a resource's content.</summary>
   Task WriteAsync(
      ProjectInfo project, ProjectPath path, Stream content, CancellationToken ct = default);

   /// <summary>Create a folder (idempotent); returns the created folder path.</summary>
   Task<ProjectPath> CreateFolderAsync(
      ProjectInfo project, ProjectPath folder, CancellationToken ct = default);

   /// <summary>Delete a resource (folder or file); true when it existed.</summary>
   Task<bool> DeleteAsync(
      ProjectInfo project, ProjectPath path, CancellationToken ct = default);
}
