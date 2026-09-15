# AGENTS

> Operational instructions for AI-Coder agents and contributors.
> Read this file in full before starting any work. It serves as the working contract between the project and every agent.

---

# 1. Mission & Repository Context

Every decision must support a single goal:

**Deliver a specification-grounded, enterprise-quality solution whose artifacts are traceable to approved requirements and current project authority.**

You are not free-authoring a solution; you are executing the project's documented requirements, architecture, and design intent.

---

# 2. Authoritative Source of Truth

The project's documentation repository is the authoritative source for requirements, architecture, business rules, backlog items, sprint definitions, and implementation guidance.

## Version / Release Policy

- The live project authority is the current repository state.
- Original source documents remain authoritative.
- Generated or derived documents are planning aids and do not replace original source artifacts.
- When documents conflict, follow the project's established authority hierarchy.
- Historical notes, handoffs, and archived materials have lower authority than the current approved specifications.

## Reading Order

Before implementing work:

1. Project Charter
2. Project Work Plan
3. Current Sprint or Active Iteration
4. Resource Management Guidance
5. Architecture and Technology Guidance
6. Sprint Planning Documentation
7. Timelines and Milestones
8. Handoff Guides
9. Relevant Specifications
10. Relevant Backlog and Planning Documents

## Terminology Grounding

- Use only approved project terminology.
- Follow terminology defined by current specifications and sprint documentation.
- Avoid inventing alternate names for systems, modules, services, processes, or business concepts.

---

# 3. Deprecated Terminology

- Do not introduce unsupported or obsolete terminology.
- Treat outdated terminology as a defect.
- Replace obsolete references with approved terminology when modifying affected content.
- Frame all work using current project vocabulary.

---

# 4. Repository Guidance

- Follow the repository's documented governance and project structure.
- The active sprint or iteration is the implementation authority.
- Work is authorized only when explicitly described in active planning documentation.
- If project governance documents do not exist, establishing them becomes the first authorized task.
- Place code, services, modules, tests, and documentation in their approved locations.
- Until a project-specific structure exists:
  - Application source should reside under `src/`
  - Documentation should reside under `docs/`

---

# 5. Documentation Maintenance (Mandatory)

- Maintain project documentation as work progresses.
- Update sprint, roadmap, handoff, log, and status artifacts whenever project state changes.
- Never leave documentation describing a stale state.
- Maintain onboarding and handoff documentation for future contributors.
- After completing validated work, prepare a validation report summarizing:
  - Completed scope
  - Requirements coverage
  - Validation evidence
  - Delivered artifacts
  - Risks and deferred items
  - Validation outcome
- Record AI usage metrics when available.
- If exact metrics are unavailable, explicitly mark them as unavailable rather than estimating silently.

---

# 6. Handoff Mandate (Mandatory)

The handoff document (`docs/HANDOFF.md`) is the **single source of truth for transferring work between agents and contributors**. Keeping it current is **mandatory** — it is the mechanism that allows work to be handed off to another agent without losing context.

## Rules

- **Always current.** Update `docs/HANDOFF.md` whenever project state changes — not just at milestones or session ends. Never leave it describing a stale state.
- **Completion gate.** No work is considered complete until the handoff reflects the new state. A change that is not recorded in the handoff is not done.
- **Transfer-ready.** The handoff must be sufficient for another agent to continue the work **without access to the original conversation or context**. If a new agent cannot pick up the work from the handoff alone, the handoff is incomplete.
- **Traceability.** Every backlog item, decision (ADR), and delivered artifact must be reachable from the handoff.
- **Validation report.** After completing validated work, update the validation report in the handoff (completed scope, requirements coverage, validation evidence, delivered artifacts, risks/deferred, validation outcome, AI usage metrics).

## Handoff checklist

Before ending a work session or transferring work, verify against `docs/handoff-checklist.md`:

- [ ] `docs/HANDOFF.md` reflects the current state of all areas worked on.
- [ ] Backlog item statuses (`docs/backlog/`) are current and the index is in sync.
- [ ] New decisions are recorded as ADRs in `docs/adr/`.
- [ ] Build/verification evidence is recorded (what builds, what was verified, what is pending).
- [ ] Blockers, risks, and deferred items are listed.
- [ ] A new agent can continue the work from the handoff alone.