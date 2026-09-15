# Handoff Checklist

> Mandatory per AGENTS.md §6 (Handoff Mandate). Before ending a work session or transferring work to another agent, verify every item below. The handoff must be sufficient for a new agent to continue the work **without access to the original conversation or context**.

## 1. Handoff document

- [ ] `docs/HANDOFF.md` reflects the **current** state of all areas worked on (not a stale snapshot).
- [ ] Every change made this session is recorded (what changed, where, and why).
- [ ] The document is organized so a new agent can find the relevant section quickly.

## 2. Backlog

- [ ] Each worked backlog item (`docs/backlog/BL-*.md`) has an accurate status (New / In Progress / Done / Blocked / Deferred / Implemented — needs verification).
- [ ] The backlog index (`docs/backlog/README.md`) is in sync with the item files (no missing/orphaned links).
- [ ] Acceptance criteria reflect what was actually delivered.

## 3. Decisions (ADRs)

- [ ] New architectural/design decisions are recorded as ADRs in `docs/adr/`.
- [ ] Each ADR has a status, date, context, decision, options considered, and consequences.

## 4. Build & verification evidence

- [ ] Recorded what builds and with what result (e.g., "Edam.Studio `Edam.WinUI.Controls` builds 0 errors").
- [ ] Recorded what was verified and what is still pending (e.g., runtime behavior in the running app).
- [ ] Recorded any environment prerequisites (e.g., local NuGet feed, Visual Studio MSIX tooling).

## 5. Blockers, risks, and deferred items

- [ ] Current blockers are listed with the concrete condition that blocks them.
- [ ] Known risks and deferred items are listed (or reference the relevant HANDOFF §4.x section).

## 6. Transfer-readiness

- [ ] A new agent can pick up the work from the handoff alone (no original conversation needed).
- [ ] The next recommended step is stated explicitly.

## 7. Validation report

- [ ] The validation report in `docs/HANDOFF.md` is updated: completed scope, requirements coverage, validation evidence, delivered artifacts, risks/deferred, validation outcome.
- [ ] AI usage metrics are recorded, or explicitly marked as unavailable.
