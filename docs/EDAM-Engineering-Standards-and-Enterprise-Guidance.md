# EDAM Engineering Standards & Enterprise Guidance

> **Status:** Enterprise guidance baseline (living document). Applies to the current EDAM codebase and sets the standards direction for EDAM v2.0 (see `docs/EDAM-v2.0-Introduction.md`).
> **Last updated:** 2026-09-10
> **Authority:** Per `AGENTS.md`, live approved requirements and `AGENTS.md` are the current project authority; this document is the **standards baseline** that turns best practices into **mechanically enforced** operating guidance. Standards are adopted through ADRs (`docs/adr/`), never silently.
> **Supersedes:** the prior SDLC compliance statement (which described a different product — Legislature.TrackingSystem/WaTech — not EDAM).

---

## 1. Identity, Scope & Applicability

**EDAM is an enterprise-grade environment for Data Assets Management** built on WinUI 3, `Edam.Libraries` (native .NET), and `Edam.Data.Catalog`. Its design principle is **reusable, flexible functionality that is independent of any UI platform** — the mapping/booklet/persistence functionality is portable and applies to any target UI (WinUI today; API/CLI/MCP/a web shell in v2.0).

### 1.1 Applicability

- **Current (v1):** applies to the code we ship today (WinUI 3 + `Edam.Libraries`, .NET 9, VS18 MSBuild, local NuGet feed, 26-test headless suite).
- **v2.0 (forward):** this baseline, extended by the AI Coding / specification-schema-first platform and the reusable-components direction in `docs/EDAM-v2.0-Introduction.md`.

### 1.2 Product characteristics this guidance protects

These are the durable EDAM characteristics that every practice below serves:

- **UI-agnostic functionality** — core behavior must never depend on a specific UI platform; the UI is one shell among many.
- **Reusable, modular components** — interface-driven, replaceable, separately consumable (program-to-interfaces + DI at composition roots).
- **Spec-and-schema-first** — every change traces to a governed requirement; schemas/conformance are first-class artifacts.
- **Governed, audit-able, enterprise-grade** — the boring, repeatable, mechanically-enforced discipline.

---

## 2. Governing Authority & Document Hierarchy

| Document | Role in this baseline |
| --- | --- |
| `AGENTS.md` | The **operational contract** — how work may proceed (mandatory reading; handoff mandate; requirements-first; AI-metrics honesty). |
| `docs/HANDOFF.md` | Single source of truth for transferring work; must stay current on every state change. |
| `docs/EDAM-v2.0-Introduction.md` | Vision/framing for v2.0 (reusable components, Aspire, standards, AI Coding). |
| `docs/adr/*` | Decision records — how a standard/pattern is formally adopted. |
| `docs/backlog/*` | Authorized work items + traceability to tests/verification. |
| **This document** | The **standards & enterprise-guidance baseline** — policies, enforcement, and what "enterprise-ready" means. |

**Adoption rule:** no new standard, practice, or deviation is binding until captured in an ADR. An unpinned practice is an aspiration, not a rule.

---

## 3. Guiding Principles

1. **Functionality is reusable and platform-independent** — never couple core behavior to a UI, storage, or transport.
2. **Program to interfaces; inject at composition roots** — components are replaceable without touching consumers ("all components are plugins" = dependency inversion).
3. **Spec-first and schema-first** — requirements and schemas drive construction; schemas are conformance artifacts, not prose.
4. **Traceability is a first-class artifact** — every change links requirement → ADR → backlog → code → test → verification → validation record.
5. **Enterprise-grade = mechanically enforced standards** — a practice that is not enforced by a tool/gate is not a rule.
6. **Honesty over aspiration** — validation status is stated precisely (validated now vs. needs live confirmation vs. needs external certification), never overclaimed.

---

## 4. SDLC Process & Gates (executable)

The loop: `spec → normalize → plan → execute → verify → repair → promote → trace → report`.

| Stage | What must happen | Primary control |
| --- | --- | --- |
| **Spec** | Start from a versioned requirement or a governed change (AGENTS.md); nothing free-author. | Sprint/backlog authority |
| **Normalize** | Reflect requirements into an ADR and backlog item (BL-x.y); decide impact + risk tier. | ADR + backlog index |
| **Plan** | Design to reusable components; identify interfaces/seams and affected standards. | This baseline §6–§7 |
| **Execute** | Implement against interface contracts; keep platform-specific code at the edges. | Code review |
| **Verify** | Build (0 errors), run the headless test suite, run the targeted new tests. | CI/verify gate |
| **Repair** | Bound repair loops; escalate beyond a configured limit rather than loop. | Repair limit |
| **Promote / Trace** | Update HANDOFF; link outcome to the ADR/backlog/test evidence. | Handoff mandate |
| **Report** | Record validation evidence + AI metrics (marked unavailable, not estimated). | AGENTS.md §5 |

### 4.1 Non-negotiable quality gates

- Build must be **0 warnings/errors** for the applicable stack (WinUI via VS18 MSBuild; libraries via `dotnet build`).
- The **headless unit suite must stay green**; new headless coverage is added rather than skipped.
- Changes touching a published package require a **SemVer bump + republish to the local feed + updating package consumers** (binary-compatible additive → patch).
- Every backlog item records its **test evidence and status** in the index (`docs/backlog/README.md`).

---

## 5. AI Coding — Specification- & Schema-First Platform

EDAM **is and will remain spec- and schema-first**, and will support AI coding efforts by **hosting, normalizing, machine-validating, and governing specifications and producing practical, governable enterprise resources**. The normative reference for this pillar is the **ALETHEIA Specification Language (ISL) release 3** corpus (`specs/isl releases/isl release 3` in the Aletheia.Specifications repository), which defines the specification language, canonical semantic model, JSON-schema conformance artifacts, and the readiness & governance model EDAM aligns to.

### 5.1 Specification & conformance (schema-first)

- Specifications and conformance evidence are **machine-checkable artifacts** (JSON schemas), not prose: common conventions, language schema, canonical model, enterprise records.
- EDAM produces **governable resources**: canonical models, schemas, evidence packages, readiness reports, audit records — each traceable to its specification version.

### 5.2 Readiness lifecycle (ISL-style gates)

Forward progression is ordered and gate-controlled:

| Level | Meaning | Autonomous construction |
| --- | --- | --- |
| Draft | Authored/incomplete; advisory checks only | NO |
| Reviewable | Coherent; human reviewer sign-off | NO |
| Machine-Valid | Structurally + semantically valid (canonical normalized) | NO |
| Autonomous-Ready | Validated, risk-approved, authorized | YES |

- Readiness is **version-specific**, recorded per version, and **immutable per transition** (corrections appended).
- **Regression is mandatory** when a change invalidates a requirement for the current level (e.g., a changed security policy → Machine-Valid; a changed must-have requirement → Reviewable).
- Autonomous construction occurs **only** for the exact authorized version.

### 5.3 Governance model (machine-enforced, not advisory)

- **Roles + separation of duties:** Reviewer, Security/Architecture Reviewer, Authorizing Official, Governance Admin, Override Authority, Auditor, Operations Approver, Data Steward. Authorizing Official ≠ Reviewer/Security Reviewer; Auditor must not approve readiness/deployment.
- **Risk tiers** (`low`/`standard`/`high`/`critical`) determine control strength (reviewers, evidence retention, approvals, deployment).
- **Approval gates** (human authority where required) pause runtime until authorized — e.g., reviewable-transition, machine-valid-security/architecture, autonomous-ready, deployment-authorization, override/waiver, post-change-reauthorization.
- **Waivers vs. overrides:** waivers allow a documented, time-bound exception with compensating controls; overrides are stronger, exceptional, Override-Authority-approved, and never used to bypass audit/authorization/legal constraints. Both expire.
- **Escalation** is a controlled pause (approval/waiver/override/repair/replan) when the platform cannot safely proceed — not a failure by itself.
- **Immutable audit** and traceability: every governance action is recorded (who, role, when, outcome, rationale, evidence, prior/new state); corrections are new records referencing the originals.

### 5.4 What EDAM guarantees under this pillar

- Functionality is exposed so AI tooling can **consume and drive the core via the same interfaces** as every other shell.
- Generated/assisted work follows the **same readiness, governance, and traceability controls** as human work — no un-governed autonomous path by default.
- AI usage metrics are **recorded when available, and explicitly marked unavailable when not** (never estimated silently).

---

## 6. Engineering & Toolchain Standards Baseline (enforcement-mapped)

Each standard is stated as **the rule → how it is enforced → what verification looks like** for the actual EDAM stack.

| # | Standard | How enforced | Verification |
| --- | --- | --- | --- |
| E1 | Nullable reference types enabled; no nullability warnings | Compiler (`<Nullable>enable</Nullable>`), warning-as-error in Release | `dotnet build -c Release` clean |
| E2 | Consistent formatting & style | `.editorconfig` + Roslyn analyzers; `dotnet format --verify-no-changes` | CI gate |
| E3 | Central, consistent package versions | Central Package Management (`Directory.Packages.props`) | Review + CI |
| E4 | Common build props across projects | `Directory.Build.props` | Review |
| E5 | Headless-testable core / UI stays thin | Headless test project (`Edam.Test.Studio`) + test DoD | `vstest` 26-pass baseline |
| E6 | Package hygiene (local feed) | `GeneratePackageOnBuild` + feed `c:\nugetlocalfeed`; SemVer bumps | Republish + consumer updates |
| E7 | Handoff current on every change | AGENTS.md §6 mandate | `docs/handoff-checklist.md` |
| E8 | No deprecated terminology | Review (AGENTS.md §3) | Review |
| E9 | Doc-health / anti-drift (ADR-0003) | `docs/CONTEXT.md` L0 kernel + `docs/doc-health-checklist.md` gate | Checklist A–E; kernel stays terse |
| E9 | Doc-health / anti-drift (ADR-0003) | `docs/CONTEXT.md` L0 kernel + `docs/doc-health-checklist.md` gate | Checklist A–E; kernel stays terse |

---

## 7. Applicable Standards Matrix (actionable)

Status uses the EDAM honesty discipline: **Adopted/implemented** (verified now) · **Target/needs confirmation** (designed, needs live env) · **Requires external certification**.

| # | Standard / concern | EDAM posture | Validation status |
| --- | --- | --- | --- |
| S1 | WCAG 2.2 AA + ACR/VPAT (UI/UX) | Accessibility-conscious design | Target — requires external evaluation/certification |
| S2 | Security: OWASP ASVS, TLS 1.2+, AES-256, secrets management | Design posture; secrets out of code; TLS/AES at rest/in transit | Target/needs live-environment confirmation |
| S3 | Identity & authorization: OAuth 2.0 / OIDC, least-privilege RBAC, separation of duties | RBAC + SoD modeled in governance (ISL §5.3) | Implemented at model level; live tenant confirmation pending |
| S4 | API contract: OpenAPI, versioning, error envelope | `/api` contract as stability boundary (v2.0 direction) | Target (v2.0) |
| S5 | Observability: OpenTelemetry, structured logging, health checks | Aspire service-defaults + OTLP planned (v2.0); audit logging in governance | Target/needs live-env confirmation |
| S6 | Data/asset governance: metadata, provenance, content-addressable versioning, FAIR | Design posture for asset store (v2.0) | Target/design |
| S7 | Portability / no lock-in: open standards, portable extract | `Edam.Libraries` portable; open formats | Aligned/implemented; SBOM/deferred |
| S8 | AI/agent boundary: MCP spec version, tool contract, governance of AI work | ISL r3 alignment (§5); MCP server wraps shared components (v2.0) | Target (v2.0) |

---

## 8. Definition of Done (enterprise)

A change is **done** only when all of:

1. **Authorized** — traced to a versioned requirement/backlog item (AGENTS.md).
2. **Quality** — meets the engineering baseline (§6) and relevant standards (§7).
3. **Verified** — build 0 errors + tests green; validation evidence recorded.
4. **Traceable** — requirement → ADR → backlog → code → test → validation link intact.
5. **Governed** — risk tier, approvals, separation of duties honored where the change triggers them (§5).
6. **Documented** — HANDOFF and backlog current; no stale docs.
7. **AI metrics** — recorded, or explicitly marked unavailable.
8. **Terminology** — no deprecated terms (AGENTS.md §3).

---

## 9. Roles & Responsibilities (light RACI)

| Role | Responsible for | Approves |
| --- | --- | --- |
| Developer/Agent | Authoring, implementation against this baseline | — |
| Reviewer | Coherence, completeness, standard conformance | Reviewable → Machine-Valid |
| Security Reviewer | Security posture (§7 S2/S3) | Machine-Valid security sign-off |
| Architecture Reviewer | Reusable-component boundaries; no UI-leak into core | Machine-Valid architecture sign-off |
| Authorizing Official | Authorizing autonomous/construction/deployment progression | Autonomous-Ready, deployment |
| Governance Admin | Standards/config baseline; this document | Governance configuration |
| Override Authority | Exceptional overrides (never bypass audit/legal) | Override approval |
| Auditor | Traceability, evidence, audit log integrity | Audit (no construction authority) |

---

## 10. Practical Appendices

### Appendix A — "Is this change enterprise-ready?" checklist

- [ ] Traced to a versioned requirement/backlog item + ADR where it changes standards.
- [ ] Core change is UI-agnostic (no UI/platform dependency leaked into `Edam.Libraries`/components).
- [ ] Programmed to an interface seam (if replaceable) with DI at a composition root.
- [ ] Engineering baseline met (nullable, format, packages, build 0 errors, headless tests green).
- [ ] Applicable standards (§7) assessed; risk tier assigned where governance applies.
- [ ] HANDOFF + backlog updated; AI metrics recorded or marked unavailable.

### Appendix B — Adopting a new standard or deviation

1. Open an **ADR** (pattern in `docs/adr/`) stating the standard, rationale, scope, and enforcement mechanism.
2. If it has a tool-enforceable rule, add the gate/analyzer/CI step (do not keep it aspirational).
3. Record adoption + traceability in this document and the HANDOFF.

### Appendix C — AI / ISL governance quick reference

- Readiness gate = **Draft → Reviewable → Machine-Valid → Autonomous-Ready**, version-specific and gated.
- **Higher risk tier → stronger controls** (reviewers, evidence, approvals, deployment).
- **Human authority is explicit**: where approval is required, the platform pauses until an authorized role acts.
- **Waivers expire and require compensating controls; overrides are exceptional and require an active Override Authority.**
- **Every governance action is audited immutably** and traceable to its specification version.

---

## Bottom line

The SDLC process, engineering baseline, and verification gates are **validated and reproducible** for EDAM as it actually is (WinUI + `Edam.Libraries`, .NET 9, VS18 MSBuild, headless test suite). The **external compliance certifications** (WCAG accessibility, hosting/security certification, live identity) and the **v2.0 direction** (distributed Aspire scaffold, web/API/CLI/MCP shells, and the AI Coding specification-schema-first platform aligned to ISL r3) are **targets that require live-environment confirmation or external certification** and are recorded as such, never overclaimed.

*This is enterprise guidance — it changes how we work, not the running code, by itself. Implemented only via ADR-adopted standards and authorized work items.*
