namespace Edam.Data.Projects.Contracts;

/// <summary>
/// What a process implementation is handed when a project runs (PE-5).
/// <para>
/// The runner has already read the arguments document through <see cref="IProjectResources"/> and
/// <b>materialized the referenced inputs</b> into <see cref="WorkingFolder"/>, so the process only
/// has to execute — it never resolves a project path, never touches a catalog, and never changes the
/// process current directory (the working folder is given to it explicitly).
/// </para>
/// </summary>
/// <param name="Project">The project being run.</param>
/// <param name="ArgumentsFile">The <c>*.Args.json</c> that defines the process.</param>
/// <param name="WorkingFolder">A private folder holding the materialized inputs.</param>
/// <param name="Inputs">The project-relative paths that were materialized.</param>
/// <param name="OutputFile">The declared output file, when the arguments specify one.</param>
/// <param name="OutputFolder">The declared output folder (e.g. <c>/Documents</c>), when specified.</param>
public sealed record ProjectProcessContext(
   ProjectInfo Project,
   ProjectPath ArgumentsFile,
   string WorkingFolder,
   IReadOnlyList<ProjectPath> Inputs,
   ProjectPath? OutputFile = null,
   ProjectPath? OutputFolder = null);

/// <summary>
/// <b>The execution seam</b> (PE-5): whatever actually runs a project's process — today the legacy
/// asset console (<c>AssetServiceHelper</c>/<c>ProjectConsole</c>), later anything else. Keeping it
/// behind an interface is what lets <see cref="IProjectRunner"/> stay storage-agnostic and lets the
/// legacy pipeline's file-system/current-directory behaviour be contained in one adapter instead of
/// leaking into every caller.
/// </summary>
public interface IProjectProcess
{
   /// <summary>Execute the process over the materialized working folder.</summary>
   Task<ProjectRunResult> RunAsync(ProjectProcessContext context, CancellationToken ct = default);
}
