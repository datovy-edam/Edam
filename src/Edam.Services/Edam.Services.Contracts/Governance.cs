using System.Collections.Generic;

namespace Edam.Services.Contracts;

/// <summary>BL-4.3 / ADR-0002: readiness tiers of the governed, gated AI-coding model.</summary>
public enum ReadinessTier { Draft, Reviewable, MachineValid, AutonomousReady }

/// <summary>BL-4.3: risk classification applied to a governed resource/action.</summary>
public enum RiskTier { Low, Standard, High, Critical }

/// <summary>BL-4.3: typed governance decision emitted by the engine.</summary>
public enum GovernanceDecision
{
    Allow, Warn, Block, Escalate,
    ApprovalRequired, WaiverRequired, OverrideRequired
}

public sealed record GovernanceRequest(
    string Resource, ReadinessTier Readiness, RiskTier Risk, string? RequestedBy = null);

public sealed record GovernanceResult(
    GovernanceDecision Decision,
    string Reason,
    ReadinessTier Readiness,
    RiskTier Risk,
    string? ResolvedBy = null);

/// <summary>One append-only audit record (event-sourced; corrections append, never overwrite).</summary>
public sealed record AuditEntry(
    string Id, DateTimeOffset Timestamp, string Principal, string Role,
    string Action, string Outcome, string Rationale, string Resource,
    string PriorState, string NewState, string IntegrityHash, string? Evidence);

public interface IAuditLog
{
    /// <summary>Appends a record to the immutably-hashed, append-only log and returns it.</summary>
    AuditEntry Append(string principal, string role, string action, string outcome,
        string rationale, string resource, string priorState, string newState, string? evidence = null);
    IReadOnlyList<AuditEntry> Read();
    /// <summary>Recomputes the SHA-256 chain hash to confirm the log was not tampered with.</summary>
    bool VerifyIntegrity();
}

public interface IGovernanceEngine
{
    GovernanceResult GetDecision(GovernanceRequest request);
}

public interface IApprovalGate
{
    /// <summary>Pauses/passes an approval-required action; deterministic fake for now (tests).</summary>
    bool TryResolve(string resource, string role, out GovernanceResult result);
}

public interface IConformanceRegistry
{
    void StoreSchema(string name, string version, string schema);
    string? GetSchema(string name, string version);
    IReadOnlyList<string> Versions(string name);
}
