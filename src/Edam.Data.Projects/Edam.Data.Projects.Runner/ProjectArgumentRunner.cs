using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Runner;

/// <summary>
/// The default <see cref="IProjectRunner"/> (PE-5): reads a project's <c>*.Args.json</c> through
/// <see cref="IProjectResources"/>, <b>materializes</b> the referenced inputs into a private working
/// folder, hands that folder to an <see cref="IProjectProcess"/>, then <b>captures</b> what the
/// process produced back into the project.
/// <para>
/// Storage-agnostic — file system, catalog, or a remote catalog client all behave identically — and
/// it <b>never</b> calls <c>Directory.SetCurrentDirectory</c>: the working folder is passed to the
/// process explicitly, which keeps the legacy pipeline's file-system behaviour contained in one
/// adapter (the <see cref="IProjectProcess"/> implementation) instead of leaking into callers.
/// </para>
/// </summary>
public sealed class ProjectArgumentRunner : IProjectRunner
{
   private readonly IProjectResources _resources;
   private readonly IProjectProcess _process;
   private readonly string _workingRoot;

   public ProjectArgumentRunner(
      IProjectResources resources, IProjectProcess process, string? workingRoot = null)
   {
      _resources = resources ?? throw new ArgumentNullException(nameof(resources));
      _process = process ?? throw new ArgumentNullException(nameof(process));
      _workingRoot = string.IsNullOrWhiteSpace(workingRoot)
         ? Path.GetTempPath()
         : Path.GetFullPath(workingRoot!);
   }

   public Task<ProjectRunResult> RunAsync(
      ProjectInfo project, ProjectPath argumentsFile, CancellationToken ct = default)
      => RunAsync(project, argumentsFile, outputFile: null, ct);

   public async Task<ProjectRunResult> RunAsync(
      ProjectInfo project, ProjectPath argumentsFile, ProjectPath? outputFile,
      CancellationToken ct = default)
   {
      ArgumentNullException.ThrowIfNull(project);

      var json = await ReadTextAsync(project, argumentsFile, ct).ConfigureAwait(false);
      if (json is null)
         return new ProjectRunResult(false,
            $"Arguments file '{argumentsFile}' was not found in project '{project.Name}'.");

      ProjectArguments arguments;
      try
      {
         arguments = ProjectArguments.Parse(json);
      }
      catch (Exception ex)
      {
         return new ProjectRunResult(false,
            $"Arguments file '{argumentsFile}' is not valid JSON: {ex.Message}");
      }

      var working = Path.Combine(_workingRoot, "edam-project-run-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(working);

      try
      {
         var materialized = new List<ProjectPath>();

         foreach (var input in arguments.Inputs)
            await MaterializeAsync(project, input, working, materialized, ct).ConfigureAwait(false);

         await MaterializeAsync(project, argumentsFile, working, materialized, ct).ConfigureAwait(false);
         if (arguments.TextMapFile is { } textMap)
            await MaterializeAsync(project, textMap, working, materialized, ct).ConfigureAwait(false);

         var before = Snapshot(working);

         var context = new ProjectProcessContext(
            project, argumentsFile, working, materialized,
            outputFile ?? arguments.OutputFile, arguments.OutputFolder);

         var result = await _process.RunAsync(context, ct).ConfigureAwait(false);

         var artifacts = await CaptureAsync(
            project, working, before, arguments, outputFile, ct).ConfigureAwait(false);

         return new ProjectRunResult(result.Success, result.Message, artifacts);
      }
      finally
      {
         try { Directory.Delete(working, true); } catch { /* best effort */ }
      }
   }

   // ---------------------------------------------------------------------
   // materialize (project resources -> working folder)
   // ---------------------------------------------------------------------

   /// <summary>A referenced path is a folder (expand its files) or a file (copy it).</summary>
   private async Task MaterializeAsync(
      ProjectInfo project, ProjectPath path, string working,
      List<ProjectPath> materialized, CancellationToken ct)
   {
      var children = await _resources.ListAsync(project, path, ct: ct).ConfigureAwait(false);
      if (children.Count > 0)
      {
         foreach (var child in children)
         {
            if (child.IsFolder) continue;
            await CopyAsync(project, child.Path, working, materialized, ct).ConfigureAwait(false);
         }
         return;
      }

      await CopyAsync(project, path, working, materialized, ct).ConfigureAwait(false);
   }

   private async Task CopyAsync(
      ProjectInfo project, ProjectPath path, string working,
      List<ProjectPath> materialized, CancellationToken ct)
   {
      using var stream = await _resources.OpenReadAsync(project, path, ct).ConfigureAwait(false);
      if (stream is null) return;

      var target = Physical(working, path);
      var parent = Path.GetDirectoryName(target);
      if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

      using var file = File.Create(target);
      await stream.CopyToAsync(file, ct).ConfigureAwait(false);
      materialized.Add(path);
   }

   // ---------------------------------------------------------------------
   // capture (working folder -> project resources)
   // ---------------------------------------------------------------------

   private async Task<IReadOnlyList<ProjectPath>> CaptureAsync(
      ProjectInfo project, string working, HashSet<string> before,
      ProjectArguments arguments, ProjectPath? outputFile, CancellationToken ct)
   {
      var current = Snapshot(working);

      // everything newly created, plus anything under a declared output (a process may overwrite)
      var capture = new HashSet<string>(
         current.Where(file => !before.Contains(file)), StringComparer.OrdinalIgnoreCase);

      foreach (var file in current)
      {
         var relative = Relative(working, file);
         if (Under(relative, outputFile) ||
             Under(relative, arguments.OutputFile) ||
             Under(relative, arguments.OutputFolder))
         {
            capture.Add(file);
         }
      }

      var artifacts = new List<ProjectPath>();
      foreach (var file in capture)
      {
         ct.ThrowIfCancellationRequested();
         var relative = Relative(working, file);

         using var stream = File.OpenRead(file);
         await _resources.WriteAsync(project, relative, stream, ct).ConfigureAwait(false);
         artifacts.Add(relative);
      }

      artifacts.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
      return artifacts;
   }

   private static bool Under(ProjectPath candidate, ProjectPath? scope)
      => scope is { IsRoot: false } target &&
         (candidate.Value.Equals(target.Value, StringComparison.OrdinalIgnoreCase) ||
          candidate.Value.StartsWith(target.Value + "/", StringComparison.OrdinalIgnoreCase));

   // ---------------------------------------------------------------------

   private async Task<string?> ReadTextAsync(
      ProjectInfo project, ProjectPath path, CancellationToken ct)
   {
      using var stream = await _resources.OpenReadAsync(project, path, ct).ConfigureAwait(false);
      if (stream is null) return null;

      using var reader = new StreamReader(stream);
      return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
   }

   private static HashSet<string> Snapshot(string folder)
      => new(Directory.GetFiles(folder, "*", SearchOption.AllDirectories), StringComparer.OrdinalIgnoreCase);

   private static string Physical(string working, ProjectPath path)
      => Path.Combine(working, path.Value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

   private static ProjectPath Relative(string working, string file)
      => ProjectPath.Parse("/" + Path.GetRelativePath(working, file).Replace('\\', '/'));
}
