# ADR-0003 — AI Transferable Coder Context Management

| Field | Value |
|---|---|
| **Status** | Proposed (adopt via review) |
| **Date** | 2026-09-10 |
| **Area** | Governance / Enterprise Standards — AI Coding platform |
| **Backlog** | n/a (standards adoption; complements ADR-0002 and `docs/EDAM-Engineering-Standards-and-Enterprise-Guidance.md`) |
| **Depends on** | ADR-0002, `docs/AGENTS.md` |

## Context

EDAM is adopting a specification-and-schema-first governance discipline (ADR-0002). As the guidance corpus grows (`docs/EDAM-Engineering-Standards-and-Enterprise-Guidance.md`, `docs/HANDOFF.md`, ADRs, backlog, schemas), AI coders/engines — with **bounded context windows and no persistent memory across sessions** — begin to drift:

- the corpus exceeds any single coder's context, forcing unchecked prioritization;
- authority ambiguity (which doc is current / superseded / highest-ranked) leads to silent rule selection;
- documentation goes stale relative to code and to itself (e.g., stale test counts; a standards doc describing the wrong product);
- handoff/checkpoint compression loses nuance at every agent-to-agent transfer;
- there is no feedback loop telling us whether a coder actually complied.

These are failures of **context transfer**, not of individual code quality.

## Decision

**Adopt a layered, lazy-loaded, pointer-based context-management regime** for how AI coders receive and preserve EDAM guidance:

1. **A small authoritative "operating kernel" (L0)** — `docs/CONTEXT.md` — deliberately terse and token-efficient so it fits any coder's context window and is **always loaded**. It holds only: the authority hierarchy, the immutable principles + Definition of Done, and **pointers** to where each topic lives. **This is what is transferred coder-to-coder.**
2. **On-demand deep detail** — area/sprint summaries (L1) and full standards/ADRs/schemas (L2) are loaded *by pointer* only when a task touches them. Deep detail is **never shipped wholesale** to every coder.
3. **Pointers, not duplication.** Each rule lives once; everything else references it. Duplication is the engine of drift.
4. **Explicit Status / Last-updated / Supersedes** on every guidance doc, and a kernel that lists **only current-adopted** documents, to remove authority ambiguity.
5. **One machine-checkable anti-drift control** — a compact invariants/checklist (`docs/doc-health-checklist.md`) enforced as a gate, not merely read: HANDOFF current, build 0 errors, headless suite green, DoD met, no stale counts/links.
6. **Traceability as the anti-drift anchor** for generated work: requirement → ADR → backlog → code → test → validation, with links **verified**.
7. **Readiness gates for AI work** (from ADR-0002/ISL): Draft → Reviewable → Machine-Valid → Autonomous-Ready, with explicit human authority gates — no un-governed autonomy by default.
8. **Doc-health hygiene cadence + coder-compliance telemetry**, so staleness and non-compliance become visible instead of silent.

This applies to EDAM now (CONTEXT.md + doc-health checklist) and is the generalized **human–AI collaboration protocol** for any co-development effort.

## Options considered

- **(a) Status quo** — large single HANDOFF/standards; coder reads all or nothing.
  - Cons: context exhaustion, authority ambiguity, stale-doc trust, transfer loss. *Rejected.*
- **(b) One mega-document** containing all guidance.
  - Cons: exceeds context window; forces unchecked prioritization; contradicts "pointer, not duplication." *Rejected.*
- **(c) Layered pointer-based kernel (chosen)** — small always-loaded kernel + on-demand detail + machine-enforced anti-drift.
  - Pros: fits bounded context, survives handoff, kills staleness/ambiguity, governed autonomy.
  - Cons: requires discipline to keep the kernel terse and pointers accurate.

## Consequences

- `docs/CONTEXT.md` created and maintained as the L0 kernel; `docs/doc-health-checklist.md` added to the baseline and run as a gate.
- Improved agent-to-agent transfer and reduced governance drift; AI autonomy is gated by readiness + human authority.
- Requires ongoing **kernel discipline** — the kernel must be aggressively kept small, and deep detail must move behind pointers rather than leaking in.

## Related documents

- `docs/CONTEXT.md` (L0 kernel), `docs/doc-health-checklist.md` (anti-drift gate)
- `docs/EDAM-Engineering-Standards-and-Enterprise-Guidance.md` (§6 engineering baseline, incl. doc-health/anti-drift row)
- ADR-0002 (spec/schema-first governance), `docs/AGENTS.md` (operating contract)
