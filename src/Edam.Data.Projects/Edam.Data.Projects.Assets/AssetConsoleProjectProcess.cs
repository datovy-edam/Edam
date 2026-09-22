using Edam.Data.AssetConsole;
using Edam.Data.AssetConsole.Services;
using Edam.Data.Projects.Contracts;
using Edam.Diagnostics;

namespace Edam.Data.Projects.Assets;

/// <summary>
/// <b>The asset-console adapter (PE-5b)</b> — the one place that knows the legacy EDAM pipeline.
/// The runner has already materialized the project's inputs into <see cref="ProjectProcessContext.WorkingFolder"/>,
/// so this process only has to execute.
/// <para>
/// <b>Contained legacy behaviour:</b> the asset console resolves its relative paths
/// (<c>./Archive</c>, <c>./Files</c>, <c>./Documents</c>) against the process <b>current
/// directory</b>. That is a property of the console, not of projects, so it is confined to this
/// adapter: the current directory is switched to the working folder for the duration of the call and
/// <b>restored immediately afterwards</b> (<c>finally</c>). The project surface
/// (<see cref="IProjectResources"/>, <see cref="IProjectRunner"/>) never relies on it.
/// </para>
/// <para>
/// The console invocation itself is injectable (<see cref="ExecuteHandler"/>) — the default calls
/// <see cref="AssetServiceHelper.Execute(AssetConsoleArgumentsInfo, string)"/>; tests can substitute
/// it so the adapter's own contract is verifiable without the asset machinery.
/// </para>
/// </summary>
public sealed class AssetConsoleProjectProcess : IProjectProcess
{
   /// <summary>The console invocation: (arguments, arguments file path) → results.</summary>
   public delegate IResultsLog? ExecuteHandler(
      AssetConsoleArgumentsInfo arguments, string argumentsFilePath);

   private readonly ExecuteHandler _execute;

   /// <param name="execute">Console invocation; defaults to the real asset console.</param>
   public AssetConsoleProjectProcess(ExecuteHandler? execute = null)
   {
      // idempotent: the console's procedure registry must be populated before a dispatch
      AssetServiceHelper.PrepareProceduresRegistry();
      _execute = execute ?? new ExecuteHandler((arguments, argumentsFilePath) =>
         AssetServiceHelper.Execute(arguments, argumentsFilePath));
   }

   public Task<ProjectRunResult> RunAsync(
      ProjectProcessContext context, CancellationToken ct = default)
   {
      ArgumentNullException.ThrowIfNull(context);

      var argumentsFilePath = Path.Combine(context.WorkingFolder, context.ArgumentsFile.Name);
      if (!File.Exists(argumentsFilePath))
         return Task.FromResult(new ProjectRunResult(false,
            $"The arguments file was not materialized in the working folder: {argumentsFilePath}"));

      AssetConsoleArgumentsInfo? arguments;
      try
      {
         arguments = AssetConsoleArgumentsInfo.FromJsonFilePath(argumentsFilePath);
      }
      catch (Exception ex)
      {
         return Task.FromResult(new ProjectRunResult(false,
            $"The arguments document could not be loaded: {ex.Message}"));
      }

      var previous = Directory.GetCurrentDirectory();
      try
      {
         // contained legacy behaviour — see the class remarks
         Directory.SetCurrentDirectory(context.WorkingFolder);

         var results = _execute(arguments, argumentsFilePath);
         var success = results?.Success == true;

         return Task.FromResult(new ProjectRunResult(
            success, success ? null : Describe(results)));
      }
      catch (Exception ex)
      {
         return Task.FromResult(new ProjectRunResult(false, ex.Message));
      }
      finally
      {
         try { Directory.SetCurrentDirectory(previous); } catch { /* best effort */ }
      }
   }

   private static string Describe(IResultsLog? results)
   {
      if (results is null) return "the asset console returned no results.";

      var message = results.MessageText;
      return string.IsNullOrWhiteSpace(message)
         ? "the process failed (see the asset console log)."
         : message;
   }
}
