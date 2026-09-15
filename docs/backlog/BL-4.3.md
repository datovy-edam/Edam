# BL-4.3 — Governance / enforcement runtime + immutable audit log (**i1 #1**)

| Field | Value |
|---|---|
| **ID** | BL-4.3 |
| **Area** | Area E — Enterprise / governance |
| **Type** | Architecture / capability |
| **Priority** | **High (i1 #1)** |
| **Effort** | L |
| **Status** | Implemented + verified live (engine decisions + immutable audit chain) — 2026-09-14 |

## Description
The **top tech-stack gap (i1 #1)**: the component that makes the governed, gated AI-coding model (ADR-0002/0003) real at runtime — rather than remaining a policy document. A **governance engine** that evaluates readiness, issues decisions, and records everything immutably.

## Scope
- **Governance engine (behind an interface, DI-bound, replaceable):** evaluates readiness (Draft → Reviewable → Machine-Valid → Autonomous-Ready), applies risk tiers (`low/standard/high/critical`), and emits decisions (`allow/warn/block/escalate/approval-required/waiver-required/override-required`).
- **Human approval gates:** pause runtime where a role must act; resume only on valid approval/waiver/override.
- **Immutable audit log:** append-only/event-sourced record of every governance action (who, role, when, outcome, rationale, evidence, prior/new state); corrections append, never overwrite.
- **Schema / conformance registry:** hosts canonical schemas + conformance artifacts (the "governable resources" of the AI pillar).
- Wire into the readiness pipeline (ADR-0002) and anti-drift gate (ADR-0003/doc-health).

## Suggested interfaces (prototype cheap, behind DI)
`IGovernanceEngine.GetDecision(...)`, `IApprovalGate`, `IAuditLog.Append(...)`, `IConformanceRegistry`.

## Acceptance criteria
- [x] Engine issues typed decisions per risk tier; approval gates pause execution. → `GovernanceEngine.GetDecision` (readiness×risk matrix), `InMemoryApprovalGate` (deterministic fake), both DI-bound. **Verified live**: `Draft/Low`→`Block`, `AutonomousReady/Critical`→`OverrideRequired`, `MachineValid/High`→`ApprovalRequired`.
- [x] Audit log append-only + traceable; no un-governed autonomous path by default. → `InMemoryAuditLog.Append` (SHA-256 chained hash, corrections append never overwrite); `VerifyIntegrity()` recomputes chain. **Verified live**: `/audit` → `verified:true`. Draft always resolves `Block` (no un-governed autonomous path by default).
- [x] Conformance registry can store/retrieve versioned schemas. → `InMemoryConformanceRegistry` (`StoreSchema`/`GetSchema`/`Versions`), registered.
- [x] Integration points stubbed behind interfaces with deterministic fakes for tests. → All four interfaces DI-registered with in-memory/deterministic implementations; testable.

## Progress (2026-09-14)
- `Contracts/Governance.cs` — enums (`ReadinessTier`/`RiskTier`/`GovernanceDecision`), records, `IGovernanceEngine`/`IApprovalGate`/`IAuditLog`/`IConformanceRegistry`.
- `Core/Governance.cs` — `GovernanceEngine` (decision matrix), `InMemoryApprovalGate`, `InMemoryAuditLog` (SHA-256 chain), `InMemoryConformanceRegistry`.
- `Wave1Services.cs` — all four registered.
- WebApi `/governance/decision` (typed decision + audited) + `/audit` (append-only log + integrity).
- Verified live: full endpoint battery in one run (see acceptance). Builds 0-error.

## Related
ADR-0002 (governance model), ADR-0003 (anti-drift), `docs/CONTEXT.md` §7. Reference: Aletheia ISL r3 Readiness & Governance model.
