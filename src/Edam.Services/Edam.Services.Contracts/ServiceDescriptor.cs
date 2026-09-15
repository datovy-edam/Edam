namespace Edam.Services.Contracts;

/// <summary>BL-6.2: how an onboarded Wave-1 resource is classified in the distributed platform.</summary>
public enum ServiceKind
{
    /// <summary>A resource exposed over the mesh (API / MCP / CLI surface).</summary>
    HostedService,

    /// <summary>A library component, DI-composed and run in-process.</summary>
    InProcessComponent,

    /// <summary>A persistence store (database / blob); wired in BL-6.6.</summary>
    DataStore
}

/// <summary>
/// BL-6.2/6.4: lifecycle + health descriptor for an onboarded Wave-1 surface.
/// Exposing this (rather than direct legacy calls) is the observable contract the mesh sees.
/// </summary>
public sealed record Wave1ServiceInfo(
    string Name,
    string Version,
    ServiceKind Kind,
    string Status,     // e.g. "running"
    string Health,     // e.g. "healthy" — aligned with BL-6.4 readiness/liveness
    string? Endpoint = null);
