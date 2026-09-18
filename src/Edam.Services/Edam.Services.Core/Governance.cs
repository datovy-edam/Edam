using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Edam.Services.Contracts;

namespace Edam.Services.Core;

/// <summary>
/// BL-4.3 / ADR-0002+0003: deterministic governance engine — the runtime that makes the
/// governed, gated AI-coding model real rather than a policy document. Pure C#, DI-bound,
/// replaceable behind <see cref="IGovernanceEngine"/>.
/// </summary>
public sealed class GovernanceEngine : IGovernanceEngine
{
    public GovernanceResult GetDecision(GovernanceRequest request)
    {
        // Draft is never governable — no un-governed autonomous path by default.
        if (request.Readiness == ReadinessTier.Draft)
        {
            return new GovernanceResult(
                GovernanceDecision.Block, "resource is Draft and not yet governable",
                request.Readiness, request.Risk, request.RequestedBy);
        }

        return request.Risk switch
        {
            RiskTier.Low =>
                new GovernanceResult(GovernanceDecision.Allow, "low risk", request.Readiness, request.Risk, request.RequestedBy),
            RiskTier.Standard =>
                request.Readiness >= ReadinessTier.AutonomousReady
                    ? new GovernanceResult(GovernanceDecision.Allow, "standard risk, autonomous-ready", request.Readiness, request.Risk, request.RequestedBy)
                    : new GovernanceResult(GovernanceDecision.Warn, "standard risk, not yet autonomous-ready", request.Readiness, request.Risk, request.RequestedBy),
            RiskTier.High =>
                request.Readiness switch
                {
                    ReadinessTier.AutonomousReady => new GovernanceResult(GovernanceDecision.Allow, "high risk, autonomous-ready", request.Readiness, request.Risk, request.RequestedBy),
                    ReadinessTier.MachineValid => new GovernanceResult(GovernanceDecision.ApprovalRequired, "high risk, machine-valid — human approval gate required", request.Readiness, request.Risk, request.RequestedBy),
                    ReadinessTier.Reviewable => new GovernanceResult(GovernanceDecision.Escalate, "high risk, reviewable — escalate for review", request.Readiness, request.Risk, request.RequestedBy),
                    _ => new GovernanceResult(GovernanceDecision.Block, "high risk, not governable", request.Readiness, request.Risk, request.RequestedBy),
                },
            RiskTier.Critical =>
                request.Readiness >= ReadinessTier.AutonomousReady
                    ? new GovernanceResult(GovernanceDecision.OverrideRequired, "critical risk requires an explicit human override", request.Readiness, request.Risk, request.RequestedBy)
                    : new GovernanceResult(GovernanceDecision.ApprovalRequired, "critical risk — human approval gate required", request.Readiness, request.Risk, request.RequestedBy),
            _ => new GovernanceResult(GovernanceDecision.Block, "unknown risk", request.Readiness, request.Risk, request.RequestedBy),
        };
    }
}

/// <summary>Deterministic fake approval gate (tests): a role of <c>approver</c>/<c>owner</c> resolves.</summary>
public sealed class InMemoryApprovalGate : IApprovalGate
{
    private static readonly string[] ResolvingRoles = { "approver", "owner" };
    public bool TryResolve(string resource, string role, out GovernanceResult result)
    {
        if (!string.IsNullOrWhiteSpace(role) && ResolvingRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            result = new GovernanceResult(GovernanceDecision.Allow, $"approval granted by role '{role}'", ReadinessTier.AutonomousReady, RiskTier.High, role);
            return true;
        }
        result = new GovernanceResult(GovernanceDecision.ApprovalRequired, "no approving role present", ReadinessTier.Reviewable, RiskTier.High, role);
        return false;
    }
}

/// <summary>
/// Append-only, event-sourced audit log with an SHA-256 chained integrity hash. Corrections are
/// appended as new records (never overwritten); <see cref="VerifyIntegrity"/> proves no tampering.
/// </summary>
public sealed class InMemoryAuditLog : IAuditLog
{
    private readonly List<AuditEntry> _entries = new();
    private readonly object _gate = new();
    private string _tailHash = "";

    public AuditEntry Append(string principal, string role, string action, string outcome,
        string rationale, string resource, string priorState, string newState, string? evidence = null)
    {
        lock (_gate)
        {
            var ts = DateTimeOffset.UtcNow;
            var id = $"aud-{ts.ToUnixTimeMilliseconds()}-{_entries.Count}";
            var hash = Compute(_tailHash, ts, principal, role, action, outcome, rationale, resource, priorState, newState, evidence);
            var entry = new AuditEntry(id, ts, principal, role, action, outcome, rationale, resource, priorState, newState, hash, evidence);
            _entries.Add(entry);
            _tailHash = hash;
            return entry;
        }
    }

    public IReadOnlyList<AuditEntry> Read()
    {
        lock (_gate) { return _entries.ToArray(); }
    }

    public bool VerifyIntegrity()
    {
        lock (_gate)
        {
            var prevHash = "";
            foreach (var e in _entries)
            {
                var recomputed = Compute(prevHash, e.Timestamp, e.Principal, e.Role, e.Action, e.Outcome,
                    e.Rationale, e.Resource, e.PriorState, e.NewState, e.Evidence);
                if (recomputed != e.IntegrityHash) return false;
                prevHash = e.IntegrityHash;
            }
            return true;
        }
    }

    private static string Compute(string prevHash, DateTimeOffset ts, string principal, string role,
        string action, string outcome, string rationale, string resource, string priorState, string newState, string? evidence)
    {
        var raw = $"{prevHash}|{ts:o}|{principal}|{role}|{action}|{outcome}|{rationale}|{resource}|{priorState}|{newState}|{evidence}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        // NOTE: fully qualified — the catalog platform drags in Edam.System, which declares
        // Edam.Convert; for code in an Edam.* namespace that shadows the BCL System.Convert.
        return System.Convert.ToHexString(bytes);
    }
}

/// <summary>Versioned conformance/schema registry (concurrent, in-memory for the Wave baseline).</summary>
public sealed class InMemoryConformanceRegistry : IConformanceRegistry
{
    private readonly ConcurrentDictionary<string, string> _schemas = new(StringComparer.OrdinalIgnoreCase);
    private static string Key(string name, string version) => $"{name}##{version}";

    public void StoreSchema(string name, string version, string schema) => _schemas[Key(name, version)] = schema;
    public string? GetSchema(string name, string version) => _schemas.TryGetValue(Key(name, version), out var s) ? s : null;
    public IReadOnlyList<string> Versions(string name)
        => _schemas.Keys.Where(k => k.StartsWith(name + "##", StringComparison.OrdinalIgnoreCase))
                        .Select(k => k[(name.Length + 2)..]).OrderBy(v => v).ToArray();
}
