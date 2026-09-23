# EDAM — Backlog

> **Status:** Active — initial backlog for the two focus areas.
> **Purpose:** Track work item by item. Each item is one file in this folder, named by its ID.
> **Authority:** Per `AGENTS.md`, the live repository state is the current project authority; this backlog is a planning aid and does not replace approved specifications.

---

## How to use this backlog

- **One file per item**, named by ID (e.g. `BL-1.1.md`). Each file holds the full detail: description, acceptance criteria, dependencies, status, and notes.
- **This `README.md` is the index** — a summary table of every item. Keep it in sync with the item files (when an item's status changes, update both the item file and this table).
- **Status values:** `New` → `In Progress` → `Done` | `Blocked` | `Deferred` | `Implemented — needs verification` (feature exists; work is to verify it with tests).
- **Effort values:** `S` (small), `M` (medium), `L` (large).
- **Triage (Areas P/E/R):** **Ready** = executable now · **Parked (needs D#)** = blocked by an open decision (see **`Open-Decisions.md`**) · **Idea** = sandbox, not committed. Make a gating decision to promote a cluster to **Ready**.

> **Handoff mandate (mandatory):** Per `AGENTS.md` §6, `docs/HANDOFF.md` is the single source of truth for transferring work between agents. Keep it current whenever project state changes, and verify against `docs/handoff-checklist.md` before ending a session or handing off. This backlog is the item-level detail; the handoff is the transfer mechanism.

## Areas

| Area | ID prefix | Focus |
|---|---|---|
| **Area 1** | `BL-1.x` | Notebook / Booklets for schema mapping (Source A → Target B) |
| **Area 2** | `BL-2.x` | Monaco code editor enhancement (Ctrl-S save, version, hardening) |
| **Area P** | `BL-3.x` | Platform / Runtime (cross-cutting) |
| **Area E** | `BL-4.x` | Enterprise / governance / AI / instrumentation (the i1 stack gaps + diagnostics) |
| **Area R** | `BL-5.x` | Repository hygiene / recommended fixes |
| **Area W1** | `BL-6.x` | **Wave 1** — move EDAM legacy resources into the distributed platform (Aspire; observability first-class; UI not in scope) |
| **Area W1.1** | `BL-7.x` | **Wave 1.1** — catalog decoupling: UI/EF-independent `Edam.Data.Catalog` platform |
| **Area PE** | `PE-x` | **Projects Enhancements** — EDAM Projects in the **Catalog** instead of a file system; interface-bound and replaceable (ADR-0009) |

## Backlog index

### Area 1 — Notebook / Booklets for Schema Mapping

| ID | Title | Type | Priority | Effort | Status |
|---|---|---|---|---|---|
| [BL-1.1](BL-1.1.md) | Fix `Notebooks` vs `Booklets` csproj path mismatch | Bug / build blocker | High | S | Done |
| [BL-1.2](BL-1.2.md) | Standardize "Notebook" vs "Booklet" naming | Refactor | Low | S | Deferred |
| [BL-1.3](BL-1.3.md) | Define the schema-mapping user workflow (spec) | Spec | High | M | Done |
| [BL-1.4](BL-1.4.md) | Verify Source schema (A) loading with tests | Testing / verification | High | M | Done |
| [BL-1.5](BL-1.5.md) | Verify Target schema (B) loading with tests | Testing / verification | High | M | Done |
| [BL-1.6](BL-1.6.md) | Verify text-cell add behavior with tests | Testing / verification | High | S | Done |
| [BL-1.7](BL-1.7.md) | Verify code-cell add behavior with tests | Testing / verification | High | M | Done |
| [BL-1.8](BL-1.8.md) | Verify code-cell execution with tests | Testing / verification | High | M | Done |
| [BL-1.9](BL-1.9.md) | Verify semantic similarity comparison with tests | Testing / verification | High | M | Implemented — needs verification |
| [BL-1.10](BL-1.10.md) | Verify mapping creation (Source→Target) with tests | Testing / verification | High | M | Done |
| [BL-1.11](BL-1.11.md) | Verify Booklet persist / save with tests | Testing / verification | High | M | Done |
| [BL-1.12](BL-1.12.md) | Verify cell reorder / delete with tests | Testing / verification | High | S | Done |
| [BL-1.13](BL-1.13.md) | Establish the Edam.Studio test project and Area 1 coverage | Testing / infrastructure | High | M | In Progress |

### Area 2 — Monaco Code Editor Enhancement

| ID | Title | Type | Priority | Effort | Status |
|---|---|---|---|---|---|
| [BL-2.1](BL-2.1.md) | Decide and document the Monaco integration strategy | Architecture | Medium | S | Done |
| [BL-2.2](BL-2.2.md) | Implement Ctrl-S → save in the WinUI.Monaco control (Catalog) | Feature | Medium | S | Implemented — needs verification |
| [BL-2.3](BL-2.3.md) | Add a WebMessage bridge to Edam.Studio's `code-editor.html` | Feature | Medium | S | Implemented — needs verification |
| [BL-2.4](BL-2.4.md) | Implement Ctrl-S → save in Edam.Studio's `CodeEditorControl` | Feature | Medium | S | Implemented — needs verification |
| [BL-2.5](BL-2.5.md) | Add dirty-state tracking (unsaved-changes indicator) | Feature | Medium | M | Implemented — needs verification |
| [BL-2.6](BL-2.6.md) | Update Monaco version in Edam.Studio (0.33.0 → current) | Dependency | Medium | M | Implemented — needs verification |
| [BL-2.7](BL-2.7.md) | Update Monaco version in the Catalog `Monaco` project (0.49.0 → current) | Dependency | Medium | M | Implemented — needs verification |
| [BL-2.8](BL-2.8.md) | Language detection via file extension | Feature | Medium | S | Implemented — needs verification |
| [BL-2.9](BL-2.9.md) | Theme support (light/dark/high-contrast) | Feature | Medium | S | Implemented — needs verification |
| [BL-2.10](BL-2.10.md) | Read-only mode | Feature | Medium | S | Implemented — needs verification |
| [BL-2.11](BL-2.11.md) | Editor performance / pooling | Performance | Medium | M | Implemented — needs verification |
| [BL-2.12](BL-2.12.md) | Editor tests | Testing | Medium | M | Implemented — needs verification |

### Area P — Platform / Runtime

| ID | Title | Type | Priority | Effort | Status | Triage |
|---|---|---|---|---|---|---|
| [BL-3.1](BL-3.1.md) | Upgrade runtime to .NET 10 (move off .NET 9) | Platform / dependency upgrade | **Critical** | L | **Complete — verified 2026-09-13** (build 0-err + headless suite green on net10) | **Ready** |

### Area E — Enterprise / governance / AI / instrumentation

| ID | Title | Type | Priority | Effort | Status | Triage |
|---|---|---|---|---|---|---|
| [BL-4.1](BL-4.1.md) | Diagnostics modernization: ResultLog → MEL bridge | Refactor / enterprise alignment | **High** | L | **Complete** — verified 2026-09-14 (26 pass / 0 fail / 2 UI) | **Ready** |
| [BL-4.2](BL-4.2.md) | Resolve `Edam.Diagnostics.ILogger` vs MEL `ILogger` collision (+ scan) | Bug / hygiene | **High** | M | **Complete** — deprecated + scanned 2026-09-14 | **Ready** |
| [BL-4.3](BL-4.3.md) | Governance/enforcement runtime + immutable audit log (**i1 #1**) | Architecture / capability | **High** | L | Implemented + verified live (engine decisions + audit chain) | **Ready** *(prototype-able)* |
| [BL-4.4](BL-4.4.md) | Codebase semantic index + AI coder-compliance telemetry (i1 #2) | Capability / infrastructure | Medium | L | New | Parked (needs D2/D4 — AI framework + vector/embedding) |
| [BL-4.5](BL-4.5.md) | Secrets & configuration management (i1 #3) | Infrastructure / security | **High** | M | New | **Ready** |
| [BL-4.6](BL-4.6.md) | Identity & access infrastructure — OAuth2/OIDC (i1 #4) | Infrastructure / security | **High** | M | New | Parked (needs D1 — web shell + IdP) |
| [BL-4.7](BL-4.7.md) | CI/CD + deployment + containerization (i1 #5) | Infrastructure | Medium | L | New | **Ready** *(foundation CI)* |
| [BL-4.8](BL-4.8.md) | Security tooling in-stack — SAST / dependency vuln / SBOM (i1 #6) | Security / tooling | Medium | M | New | **Ready** |
| [BL-4.9](BL-4.9.md) | Crash/telemetry + backup/DR for stores (i1 #7) | Reliability / ops | Medium | M | New | Parked (needs BL-4.1 + D3 persistence) |
| [BL-4.10](BL-4.10.md) | Import / connector layer (i1 #8) | Feature / capability | Medium | L | New | **Ready** |
| [BL-4.11](BL-4.11.md) | Schema-validation library + conformance registry (i1 #9) | Capability / tooling | Medium | M | New | Parked (needs D3/D2 — registry + tooling) |
| [BL-4.12](BL-4.12.md) | Caching / distributed state (i1 #10) | Infrastructure | Low | M | New | Parked (needs .NET 10 + service layer) |
| [BL-4.13](BL-4.13.md) | i18n / localization (i1 #11) | Feature / quality | Low | M | New | **Idea** |
| [BL-4.14](BL-4.14.md) | Contract tests (API/MCP) + UI automation (i1 #12) | Testing | Medium | M | New | Parked (needs D1/D5 — web shell + MCP contract) |
| [BL-4.15](BL-4.15.md) | Containerized testing: use Docker where it makes sense | Testing / infrastructure | Medium | L | New | Parked (needs .NET 10 + first service/web-shell) |

### Area R — Repository hygiene / recommended fixes

| ID | Title | Type | Priority | Effort | Status | Triage |
|---|---|---|---|---|---|---|
| [BL-5.1](BL-5.1.md) | Fix stale `HintPath` refs (Catalog → Edam.Libraries) | Bug / build hygiene | **High** | S | New | **Ready** |
| [BL-5.2](BL-5.2.md) | Add `nuget.config` / document local feed | Build / onboarding | **High** | S | New | **Ready** |
| [BL-5.3](BL-5.3.md) | Remove dead / duplicate code | Cleanup | Low | S | New | **Ready** |
| [BL-5.4](BL-5.4.md) | Replace `async void` / `.Wait()/.Result`; add session management | Refactor / quality | Medium | M | New | **Ready** |
| [BL-5.5](BL-5.5.md) | Align EF Core versions + add migrations | Dependency / data | Medium | L | **In Progress** — EF 6.0.25 → **9.0.2** aligned, republished **1.1.0** + consumed 2026-09-18; runtime validation + migrations pending | **Ready** |
| [BL-5.6](BL-5.6.md) | Add catalog REST client + local repository tests | Testing | Medium | M | New | **Ready** |
| [BL-5.7](BL-5.7.md) | Dependency vulnerability cleanup (NuGet Audit findings) | Dependency / security | **High** | M | **In Progress** — 5/6 advisories fixed 2026-09-18; SQLitePCLRaw blocked offline | **Ready** |

### Area W1 — Wave 1: move legacy resources into the distributed platform

| ID | Title | Type | Priority | Effort | Status | Triage |
|---|---|---|---|---|---|---|
| [BL-6.1](BL-6.1.md) | Aspire app-host + ServiceDefaults baseline (OTel, health, resilience) | Infrastructure / foundation | **High** | M | **Complete** — verified 2026-09-14 (builds 0-err net10; AppHost ran) | **Ready** (Wave 1) |
| [BL-6.2](BL-6.2.md) | Onboard legacy `Edam.Libraries` resources as components/services | Migration / capability | **High** | L | Implemented — contracts+core build 0-err; WebApi /wave1 verified live | **Ready** (Wave 1) |
| [BL-6.3](BL-6.3.md) | Service boundary: minimal API + MCP + CLI shells over the core | Capability / service layer | **High** | L | Complete — all 3 shells build 0-error (full Edam.slnx); CLI+API verified live; MCP stdio compiles | **Ready** (Wave 1) |
| [BL-6.7](BL-6.7.md) | Unified MEL/OTel diagnostics across Wave-1 services | Observability / diagnostics | **High** | L | Implemented — Core+WebApi built 0-err, verified live (correlation + trace + MEL) | **Ready** (Wave 1) |
| [BL-6.4](BL-6.4.md) | Environment & services health as first-class (probes, metrics, dashboard) | Observability / reliability | **High** | M | Implemented — ready/live/report endpoints verified live | **Ready** (Wave 1) |
| [BL-6.6](BL-6.6.md) | Persistence + assets into the mesh (PostgreSQL + blob) | Infrastructure / data | Medium | L | Store layer impl + build-verified; containers need Docker (BL-4.15) | **Ready** (Wave 1, after 6.1) |
| [BL-6.5](BL-6.5.md) | ~~Python scripting as a service~~ — **Parked/dropped** | Capability | — | — | **Parked (2026-09-15)** — Python removed; needed functionality via MCP (ADR-0004) | **Parked** |

> **Wave 1 overview + definition-of-complete:** `Wave-1-Migration.md`. **UI/deployed shells are NOT in Wave 1** — D1 (web-first UI) is parked until after Wave 1.

### Area W1.1 — Wave 1.1: Catalog decoupling

| ID | Title | Type | Priority | Effort | Status | Triage |
|---|---|---|---|---|---|---|
| [BL-7.1](BL-7.1.md) | Relocate catalog core to headless `Edam.Data.Catalog` **as-is** (1st — de-risk) | Migration / refactor | **High** | L | **Done/verified** (2026-09-15) | **Ready** (Wave 1.1) |
| [BL-7.5](BL-7.5.md) | Surface the real catalog behind the Wave-1 boundary (**FileSystem first**) | Capability / integration | **High** | M | **Done** (2026-09-15) | **Ready** (Wave 1.1) |
| [BL-7.3](BL-7.3.md) | Catalog interface/base-code platform — **derive from relocated types** (draft scaffold validated/discarded) | Architecture / contracts | **High** | M | **Done** (2026-09-15) | **Ready** (Wave 1.1) |
| [BL-7.2](BL-7.2.md) | EF-independence + **PostgreSQL (Npgsql)** back-end behind `ICatalogStore`/`IContentStore`; **MS-SQL/EF deferred** | Refactor / architecture | **High** | L | **Complete** (acceptance met; WinUI desktop runtime = user validation) — Npgsql provider + conformance **live ALL CONFORM 12/12**; Contracts-native REST client (3b-2); **`CatalogDb` fully retired** (ServiceClient, service host, Testing, WinUI desktop local now Npgsql store-backed via `StoreBackedCatalogService`); **FileSystem provider added — ALL CONFORM**; MSTest unit suite (`Edam.Data.Catalog.Tests`); remaining: user WinUI desktop build/run | **Ready** (Wave 1.1) |
| [BL-7.4](BL-7.4.md) | DI seam + per-**Container** provider resolution (`ContainerBinding`/`ICatalogProviderResolver`); drop factory `switch` | Refactor / infrastructure | Medium | M | **Complete** (composition root + per-Container resolver + service-host & WinUI local store resolved via DI — conformance-through-DI ALL CONFORM, rebuilds 0 error; PostgreSql `EnlistContainer` id fix; Model file/HTTP client retirement deferred) | **Ready** (Wave 1.1) |

> **Wave 1.1 overview + definition-of-complete:** `Wave-1.1-Catalog-Decoupling.md` (ADR-0007). Azure/blob content targets deferred to **Wave 2** (seam reserved in `IContentStore`).

### Area PE — Projects Enhancements (Projects in the Catalog)

| ID | Title | Type | Priority | Effort | Status |
|---|---|---|---|---|---|
| PE-0 | Scope, surface inventory + decisions for catalog-backed Projects | Spec / architecture | **High** | S | **Done** (2026-09-18) — `Projects-Enhancements.md` + ADR-0009 |
| PE-1 | `Edam.Data.Projects.Contracts` — value records + `IProjectCatalog`/`IProjectStore`/`IProjectResources`/`IProjectRunner` | Architecture / contracts | **High** | M | **Done** (2026-09-18) — builds 0-error, dependency-free, no consumer changed |
| PE-2 | File-system `IProjectResources` (behaviour-preserving) — the hinge that removes the file-system + CWD dependency | Refactor | **High** | M | **Done** (2026-09-18) — `Edam.Data.Projects.FileSystem` + `Edam.Data.Projects.Conformance` **15/15 ALL CONFORM** (incl. no CWD mutation) |
| PE-3 | **Catalog** implementation: project = branch, artifacts = items + `IContentStore`; collections via `ContainerBinding`; import/export (upload/download) | Capability / integration | **High** | L | **Done** (2026-09-18) — `Edam.Data.Projects.Catalog`; **ALL CONFORM** on file-system + catalog-local + catalog-remote HTTP + **postgres (local and remote)**; project paths collection-scoped |
| PE-4 | `AddProjectServices(config)` DI composition root; retire the static project surface (**retirement moved to PE-5**) | Infrastructure | Medium | M | **Done** (2026-09-18) — `Edam.Data.Projects.DependencyInjection`; **8 conformance targets ALL CONFORM** incl. 3 resolved through DI |
| PE-5 | Consumers + execution, in sub-steps: **5a** runner seam ✅ · **5b** asset-console adapter ✅ · **5c** real process (part 1 ✅) + Studio UI via DI (part 2 ✅) · **5d** retire the statics (**partial**) | Integration | Medium | L | **In Progress** (2026-09-18) — 5a–5c done; **5d partial**: 3 dead members deleted, 5 deprecated; **full deletion blocked by the pipeline** (see the PE-5d audit); Studio sln + all solutions 0 error |

> **Overview + decisions:** `Projects-Enhancements.md` (**ADR-0009**). Model agreed: **Collection = container, Project = branch**, artifacts = items + content in the Catalog; filesystem kept as a registered provider during the transition.

## Suggested sequencing

> **Area 1 is now a verification effort** — the features are implemented; the work is to test them. BL-1.2 (naming) is deferred as secondary.

> **Areas E, R, W1** capture the enterprise-alignment, repository-hygiene, and **Wave 1 (distributed-platform migration)** work discussed. Grouped for **Sprint S1** in `Sprint-S1-Next-Sprint.md`. **Decisions gate sequencing:** see **`Open-Decisions.md`** — D2/D4 (AI framework + vector), D3 (persistence), D5 (MCP) promote the Parked clusters to Ready; **D1 (web UI) is parked** (not in Wave 1); D7 (Aspire) governs Wave 1.

- **Wave 1 (current initiative):** `Wave-1-Migration.md` → **BL-6.1** (Aspire baseline) → **BL-6.2** (onboard resources) → **BL-6.3/BL-6.7** (shells + diagnostics) → **BL-6.4** (services health) → **BL-6.6** (persistence). **BL-6.5 (Python) dropped** — functionality via MCP (parked, MAF later; ADR-0004/0005). *UI not in scope.*
- **Wave 1.1 (catalog decoupling — next, de-risked):** `Wave-1.1-Catalog-Decoupling.md` → **1 Relocate**: **BL-7.1** (move real projects as-is) → **2 Prove**: **BL-7.5** (working end-to-end slice, FileSystem first) → **3 Extract**: **BL-7.3** (derive contracts from relocated types) → **4 Abstract**: **BL-7.2** (EF-independence + PostgreSQL) → **5 Seam**: **BL-7.4** (DI + per-**Container** resolution). Azure/blob → **Wave 2**.
- **Projects Enhancements (PE — current):** `Projects-Enhancements.md` (ADR-0009) → **PE-0…PE-4 DONE** (scope/inventory, contracts, file-system providers, Catalog-backed providers, **DI composition root — 8 conformance targets ALL CONFORM**) → **PE-5** consumers + `IProjectRunner` + retire the statics (pipeline, then Studio UI). Depends on Wave 1.1 (done).
- **Phase 0 (platform runtime — ASAP):** **BL-3.1** — upgrade to .NET 10 (current LTS), moving off .NET 9 (STS end-of-support).
- **Phase 1 (unblock build):** BL-1.1
- **Phase 2 (test foundation):** BL-1.13 (test project + fixtures), BL-1.3 (workflow → test scenarios)
- **Phase 3 (verify mapping core):** BL-1.4, BL-1.5, BL-1.6, BL-1.7, BL-1.8
- **Phase 4 (verify mapping intelligence):** BL-1.9, BL-1.10, BL-1.11, BL-1.12
- **Phase 5 (editor — Area 2):** BL-2.1, BL-2.2, BL-2.3, BL-2.4, BL-2.5, BL-2.6, BL-2.7, BL-2.8, BL-2.9, BL-2.10, BL-2.11, BL-2.12
