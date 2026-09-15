# EDAM — Doc-Health & Anti-Drift Checklist

> **Purpose:** The machine-checkable anti-drift gate (ADR-0003, L5). Run it whenever project state changes, before handoff, and on a hygiene cadence. A `[ ]` left unchecked is a **drift defect** to resolve, not ignore.

## A. State freshness

- [ ] `docs/HANDOFF.md` reflects the current state of **all** areas worked on (no stale counts, statuses, or artifact lists).
- [ ] Backlog item statuses (`docs/backlog/*`) are current; `docs/backlog/README.md` index is in sync.
- [ ] Every guidance doc (`docs/*.md`, `docs/adr/*`) carries **Status / Last-updated / Supersedes** and is in the current-adopted list where still adopted.
- [ ] No guidance doc is left describing a different product or an obsolete stack.

## B. Authority & pointers

- [ ] `docs/CONTEXT.md` (L0 kernel) is **terse** — principles/DoD/pointers only, no leaked detail.
- [ ] The kernel's pointer table names **only current-adopted** documents.
- [ ] Rules are **not duplicated** across files; duplicates were consolidated to one source with pointers.

## C. Traceability

- [ ] Open backlog items link requirement → ADR → code → test → validation evidence.
- [ ] ADRs reference supporting docs and are consistent with `docs/CONTEXT.md` and the standards baseline.

## D. Verification (executable engineering quality)

- [ ] Build produces **0 errors** (WinUI via VS18 MSBuild; libraries via `dotnet build`).
- [ ] Headless test suite is **green** (see `docs/backlog/BL-1.13.md` for the run command/count).
- [ ] Published packages followed SemVer + local-feed republish + consumer updates.

## E. Governance & autonomy

- [ ] AI-generated/assisted work was recorded at the correct readiness level (Draft → Reviewable → Machine-Valid → Autonomous-Ready) with human gates where required.
- [ ] AI usage metrics were recorded **or explicitly marked unavailable** (never silently estimated).

## Hygiene cadence
Run A–D on every state change and before handoff. Run E on any AI-assisted delivery. A full sweep is due at least once per sprint.
