# EDAM — Operating Kernel (L0 context manifest)

> **Purpose:** The small, token-efficient kernel every AI coder / contributor holds at all times. It is the **only** guidance guaranteed to fit a bounded context window and to be transferred coder-to-coder without loss. **Do not let detail leak into this file** — add pointers, not prose.
> **Read first:** `docs/AGENTS.md` (operating contract). **Detail lives behind the pointers below, loaded on demand.**

## 1. What EDAM is

Enterprise-grade **Data Assets Management** built to be **UI-agnostic** — functionality is reusable, flexible, portable components that any target UI/`API`/`CLI`/`MCP` can reach. WinUI 3 shell today; v2.0 direction: `docs/EDAM-v2.0-Introduction.md`.

## 2. Authority hierarchy (high → low)

1. **`docs/AGENTS.md`** — the operating contract (must-read; handoff mandate).
2. **Approved requirements / current sprint authority.**
3. **ADRs** (`docs/adr/`) — adopted decisions (only current/`Accepted` apply).
4. **This kernel (`docs/CONTEXT.md`)** + `docs/HANDOFF.md` (live state; single source of truth for handoff).
5. **Standards baseline** (`docs/EDAM-Engineering-Standards-and-Enterprise-Guidance.md`).
6. Working notes / workflow specs — **informative**, lower authority than all of the above.

When docs conflict, follow this order; when in doubt, ask.

## 3. Immutable principles (Do not violate)

1. **Functionality is reusable & UI-agnostic** — never couple core behavior to a UI/transport/storage.
2. **Program to interfaces; inject at composition roots** — replaceable components ("all components are plugins" = dependency inversion).
3. **Spec-first / schema-first** — requirements + schemas drive construction; schemas are conformance artifacts.
4. **Traceability is first-class** — requirement → ADR → backlog → code → test → validation, links verified.
5. **Enterprise-grade = mechanically enforced standards** — a practice not enforced by a gate/analyzer is not a rule.
6. **Honesty over aspiration** — state validated vs. needs-confirmation vs. needs-certification; mark AI metrics unavailable rather than estimating.

## 4. Definition of Done (all 8)

Authorized · Enterprise quality · Verified (build 0 errors, tests green) · Traceable · Governed (risk/approvals where triggered) · Documented (HANDOFF/backlog current) · AI metrics recorded/unavailable · No deprecated terminology.

## 5. Anti-drift invariants (machine-checkable — run `docs/doc-health-checklist.md`)

- `HANDOFF.md` reflects current state; backlog index current.
- Build 0 errors; headless suite green.
- Every guidance doc carries Status / Last-updated / Supersedes.
- Guidance lists `docs/CONTEXT.md` pointers only for **current-adopted** docs.
- No rule duplicated across the corpus (duplication = drift).

## 6. Pointers (load on demand — do not read all up front)

| Topic | Pointer |
| --- | --- |
| Operating contract / how to work | `AGENTS.md` |
| Live state + handoff | `docs/HANDOFF.md` |
| Full standards & enterprise guidance | `docs/EDAM-Engineering-Standards-and-Enterprise-Guidance.md` |
| v2.0 vision / reusable components | `docs/EDAM-v2.0-Introduction.md` |
| Adopted decisions | `docs/adr/` (0001, 0002, 0003) |
| Authorized work + traceability | `docs/backlog/` → `README.md` index |
| Area 1 workflow spec | `docs/area1-schema-mapping-workflow.md` |
| Anti-drift gate/checklist | `docs/doc-health-checklist.md` |
| AI governance / ISL r3 reference | ALETHEIA `specs/isl releases/isl release 3` |

## 7. Autonomy & governance (from ADR-0002)

AI work progress is **gated**: Draft → Reviewable → Machine-Valid → Autonomous-Ready. **Human authority is explicit** where required; no un-governed autonomous path by default. When unsure, treat the state as *Draft / needs review*, not *Autonomous-Ready*.
