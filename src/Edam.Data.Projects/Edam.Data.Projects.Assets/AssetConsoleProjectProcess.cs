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

   private static readonly object InitializeGate = new();
   private static bool _initialized;

   /// <summary>
   /// The console's own initialization failed (session, <c>appsettings.json</c>, the type registry,
   /// the procedure registry). Surfaced with a failed run rather than thrown, so a host that
   /// substitutes the console call is unaffected.
   /// </summary>
   public static string? InitializationError { get; private set; }

   /// <param name="execute">Console invocation; defaults to the real asset console.</param>
   public AssetConsoleProjectProcess(ExecuteHandler? execute = null)
   {
      // the console needs its FULL initialization (not only the procedure registry): session, an
      // optional appsettings.json, the type registry (e.g. the OpenXML row builder) and the
      // procedures. Done once, best effort.
      EnsureInitialized();

      _execute = execute ?? new ExecuteHandler((arguments, argumentsFilePath) =>
         AssetServiceHelper.Execute(arguments, argumentsFilePath));
   }

   private static void EnsureInitialized()
   {
      if (_initialized) return;

      lock (InitializeGate)
      {
         if (_initialized) return;

         try
         {
            AssetServiceHelper.Initialize();
            InitializationError = null;
            _initialized = true;
         }
         catch (Exception ex)
         {
            InitializationError = ex.Message;

            // the procedure registry is still worth preparing on its own
            try { AssetServiceHelper.PrepareProceduresRegistry(); } catch { /* best effort */ }
         }
      }
   }

   public Task<ProjectRunResult> RunAsync(
      ProjectProcessContext context, CancellationToken ct = default)
   {
      ArgumentNullException.ThrowIfNull(context);

      // the runner materializes every resource at its PROJECT-RELATIVE path, so the arguments
      // document sits at its own relative path inside the working folder (e.g. Arguments/x.Args.json)
      var argumentsFilePath = Path.Combine(context.WorkingFolder,
         context.ArgumentsFile.Value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

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
      var message = results is null
         ? "the asset console returned no results."
         : results.MessageText;

      if (string.IsNullOrWhiteSpace(message))
         message = "the process failed (see the asset console log).";

      return InitializationError is null
         ? message
         : $"{message} (console initialization: {InitializationError})";
   }
}
