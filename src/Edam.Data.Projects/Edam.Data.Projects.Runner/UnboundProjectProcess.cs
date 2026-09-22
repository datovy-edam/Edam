using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Runner;

/// <summary>
/// The process used when none is bound (PE-5): running fails with a clear, actionable message rather
/// than an obscure DI error. Bind a real one — e.g. the asset-console adapter (PE-5b) — with
/// <c>services.AddSingleton&lt;IProjectProcess, …&gt;()</c> before resolving
/// <see cref="IProjectRunner"/>.
/// </summary>
public sealed class UnboundProjectProcess : IProjectProcess
{
   public Task<ProjectRunResult> RunAsync(
      ProjectProcessContext context, CancellationToken ct = default)
      => throw new NotSupportedException(
         "No IProjectProcess is bound, so no project can be executed. Register one " +
         "(services.AddSingleton<IProjectProcess, \u2026>()) \u2014 for example the asset-console " +
         "adapter, which is the piece that knows how to run an EDAM procedure.");
}
