using Edam.Services.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Edam.WebApi;

/// <summary>
/// BL-6.4: a generic readiness/liveness health check backed by any <see cref="IWave1Service"/>.
/// Each Wave-1 service reports its own lifecycle/health descriptor; this surfaces it as a
/// per-service ASP.NET health check so the `/health` (ready) probe + Aspire/dashboard aggregate it.
/// Dependency health (DB, blob, vector) is reported through the service descriptor once those
/// land (BL-6.6); today they are healthy in-memory surfaces.
/// </summary>
public sealed class WaveOneServiceHealthCheck<T> : IHealthCheck where T : IWave1Service
{
    private readonly T _service;
    public WaveOneServiceHealthCheck(T service) => _service = service;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var d = _service.Describe();
        var ok = d.Health == "healthy";
        return Task.FromResult(ok
            ? HealthCheckResult.Healthy($"{d.Name} {d.Version}")
            : HealthCheckResult.Unhealthy(d.Health));
    }
}
