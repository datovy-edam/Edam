# ADR-0002 — AI Specification-Driven Engineering & Governance (aligned to ALETHEIA ISL r3)

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-09-10 |
| **Area** | Governance / Enterprise Standards — AI Coding platform |
| **Backlog** | n/a (standards adoption; see `docs/EDAM-Engineering-Standards-and-Enterprise-Guidance.md` §5) |
| **Depends on** | `docs/AGENTS.md`, `docs/EDAM-Engineering-Standards-and-Enterprise-Guidance.md`, `docs/EDAM-v2.0-Introduction.md` |

## Context

EDAM is already **spec- and schema-first**: every change must trace to a governed requirement (AGENTS.md), and machine-checkable schemas/conformance artifacts are first-class. As the enterprise guidance corpus grows (`EDAM-Engineering-Standards-and-Enterprise-Guidance.md`, HANDOFF, ADRs, backlog) and as AI-assisted coding becomes central to the effort, two risks have become explicit:

1. **Governance drift** — a large, multi-document guidance corpus is no longer reliably held, prioritized, and non-contradicted by an AI coder whose context window is bounded.
2. **Un-governed autonomy** — AI coders/engines can generate work faster than the existing review loop can keep it anchored to requirements, schemas, and decisions.

The **ALETHEIA Specification Language (ISL) release 3** corpus (`specs/isl releases/isl release 3` in the Aletheia.Specifications repository) already defines a consolidated, machine-enforced answer to both: a specification/schema-first language, a canonical semantic model, and a **Readiness & Governance model** (readiness levels, risk tiers, approval gates, separation of duties, waivers/overrides/escalations, immutable audit, conformance).

## Decision

**Adopt, for EDAM's engineering governance and its AI-coding platform, the specification- and schema-first discipline and the Readiness & Governance model aligned to ISL r3**, applied incrementally:

1. **Spec/schema-first is the norm.** Requirements and conformance evidence are machine-checkable, versioned artifacts (schemas), not open-ended prose. Where a schema is feasible it is authoritative over prose.
2. **Governed readiness lifecycle.** AI-derived work (and human work) progresses Draft → Reviewable → Machine-Valid → Autonomous-Ready **only** by satisfying explicit, verifiable criteria per version. Autonomous generation is permitted only at **Autonomous-Ready**, and only for the exact authorized version.
3. **Governance is continuous and machine-enforced.** Approval gates, risk tiers, separation of duties, waivers vs. overrides, and escalations produce **runtime behavior** — a change or generation must pause at a required human gate rather than proceeding. No un-governed autonomous path by default.
4. **Immutable audit + traceability.** Every governance action is recorded (who, role, when, outcome, rationale, evidence, prior/new state); corrections are appended, never overwritten. Every spec-version and artifact stays traceable.
5. **AI usage metrics** are recorded when available and explicitly marked unavailable otherwise (never silently estimated) — per AGENTS.md §5.
6. **Adoption is incremental.** The first increment is: adopt the readiness/governance *concepts* into the engineering guidance (done in `EDAM-Engineering-Standards-and-Enterprise-Guidance.md` §5), plus a practical **transferable-coding-context management** mechanism (see the plan below / proposed ADR-0003). Full ISL-r3 tooling/schema automation is a later, separately budgeted increment.

## Options considered

- **(a) Status quo** — unstructured docs + largely autonomous agent work.
  - Cons: drift, contradictions, no gates, AI ineffectiveness on a large corpus, no auditability. *Rejected.*
- **(b) Adopt full ISL r3 as the normative spec language + full platform now.**
  - Pros: maximal governance and conformance.
  - Cons: heavy; premature before the platform/schema tooling exists; risks ceremony overload and slowing delivery. *Rejected now; target later.*
- **(c) Adopt ISL r3 as the governance reference + schema-first discipline incrementally (chosen)** — governance model and schema-first principle now, machine enforcement and tooling grown incrementally.

## Consequences

- `docs/EDAM-Engineering-Standards-and-Enterprise-Guidance.md` §5 (AI Coding pillar) is adopted direction, not merely stated intention.
- AI coders/engine treat governance as **continuous, enforced control** — human approval gates block autonomous progression where required.
- Requires a **transferable-coding-context management** mechanism so the large guidance stays effective for bounded-context AI coders (see the approach proposed for ADR-0003).
- Later increments: JSON-schema conformance artifacts for EDAM specs; readiness evaluator + governance engine tooling; integration with the v2.0 Aspire/API/CLI/MCP shells.

## Related documents

- `docs/EDAM-Engineering-Standards-and-Enterprise-Guidance.md` §5 (AI Coding pillar)
- `docs/EDAM-v2.0-Introduction.md` (Pillar C — enterprise-grade)
- `docs/AGENTS.md` (requirements-first, traceability, handoff mandate)
- *Aletheia.Specifications* — `specs/isl releases/isl release 3` (normative reference for the readiness & governance model)
