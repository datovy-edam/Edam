namespace Edam.Data.Projects.Contracts;

/// <summary>Outcome of running a project's process.</summary>
/// <param name="Success">True when the process completed.</param>
/// <param name="Message">Failure detail / diagnostic message, when available.</param>
/// <param name="Artifacts">Project-relative paths of the artifacts that were produced.</param>
public sealed record ProjectRunResult(
   bool Success,
   string? Message = null,
   IReadOnlyList<ProjectPath>? Artifacts = null);

/// <summary>
/// <b>Processing</b>: execute a project's <c>*.Args.json</c> definition. The runner reads the
/// arguments and its inputs through <see cref="IProjectResources"/> and writes its outputs the
/// same way, so the process is storage-agnostic (file system today, catalog next).
/// <para>
/// Today's equivalent is <c>ProjectConsole.Execute/ProcessItem</c> (static, current-directory
/// dependent) driving the asset console.
/// </para>
/// </summary>
public interface IProjectRunner
{
   /// <summary>Run the process defined by <paramref name="argumentsFile"/> (e.g. <c>/Arguments/x.Args.json</c>).</summary>
   Task<ProjectRunResult> RunAsync(
      ProjectInfo project, ProjectPath argumentsFile, CancellationToken ct = default);

   /// <summary>
   /// Run the process writing its output to <paramref name="outputFile"/> — the Studio "save option"
   /// flow (export the produced assets to a chosen artifact). When <c>null</c>, the output declared
   /// by the arguments document is used.
   /// </summary>
   Task<ProjectRunResult> RunAsync(
      ProjectInfo project, ProjectPath argumentsFile, ProjectPath? outputFile,
      CancellationToken ct = default);
}
