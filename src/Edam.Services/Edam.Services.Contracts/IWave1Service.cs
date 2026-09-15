namespace Edam.Services.Contracts;

/// <summary>
/// BL-6.2: a Wave-1 onboarded surface (service or component) behind an interface,
/// DI-composed and observable through the Aspire mesh. Deep data operations build on this
/// in BL-6.6 (persistence); the baseline contract surfaces the descriptor for health/metrics.
/// </summary>
public interface IWave1Service
{
    /// <summary>Current lifecycle + health descriptor (name, version, kind, status, health).</summary>
    Wave1ServiceInfo Describe();
}
