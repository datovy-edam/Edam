# EDAM v2.0 — Introduction & Direction

> **Status:** Vision / framing (draft for review — **not** yet an approved requirements baseline).
> **Purpose:** Updated introduction that consolidates the EDAM v2.0 direction so we can move forward on a shared foundation. It states *direction and principles*, not an implementation plan.
> **Authority:** Per `AGENTS.md`, live approved requirements remain authoritative; this document is the agreed framing to be developed into requirements + ADRs.

---

## 1. Purpose & Positioning

**EDAM v2.0 is an enterprise-grade environment for Data Assets Management, delivered as a collection of distributed applications and services.**

It is not a rewrite of the current desktop tool for its own sake. It is the same product vision — schema mapping, booklets, data-asset management — re-platformed and service-ized so **functionality is delivered as reusable, modular components** that platforms and shells compose, instantiate, and reach as needed, and so it is **distributed, replaceable, and governed by applicable standards** as first-class objectives.

## 2. Core Principle: Reusable, Modular Components

The organizing idea behind everything that follows:

> **Functionality is delivered as reusable, modular components — each an interface with plug-in implementations — that are separate from one another and instantiated exactly as needed. There is no single monolithic "core"; there is a catalog of component families that platforms and shells compose and wire.**

This reflects how EDAM already works: **not all functionality lives in one portable core.** `Edam.Libraries` is one portable, UI-free component family, and **`Edam.Data.Catalog` is a separate code set** (consumed by its own distinct `Edam.Data.Catalog.WinUI` solution). v2.0 generalizes that reality rather than contradicting it:

- **Reusable components, not one core.** Each functional area (data-assets mapping/booklets, data catalog, persistence, …) is a component — or component family — with an interface and plug-in implementation(s). Components are independent, reusable, and separately consumable.
- **Separate and pluggable.** Components do not depend on each other's details; they compose against interfaces and are **instantiated as needed** by each platform or shell.
- **Many shells reach the components.** WinUI desktop, a WebAssembly/web UI, a **REST API**, a small-footprint **CLI**, and an **MCP server** are each a **composition root** that wires the components it needs.
- **Parity by construction.** CLI, API, MCP, and UI behave consistently because they reuse the same components and bind the same interfaces.

## 3. Requirements & the Direction That Addresses Each

| Requirement | Direction |
|---|---|
| Distributed environment health & monitoring | .NET **Aspire** app-host scaffolding with service-defaults (OpenTelemetry, health checks, resilience) + a production observability story (OTLP collector / dashboards) defined separately. |
| Pluggable — all components are plugins | **Program to interfaces**; dependency injection at **composition roots**; components are replaceable by swapping an implementation **without** touching consumers. |
| Platform-independent components, apps & services | Portable .NET component libraries; platform-specific code kept to the edges (UI shells, native/storage adapters). Windows / macOS / Linux; a WebAssembly UI shell is under consideration. |
| Functionality exposed through an API | ASP.NET Core API (OpenAPI) as the **contract**; all shells are API/capable clients. |
| Small-footprint CLI + separate code library reused by MCP + API | **Component libraries are the product.** A thin CLI (e.g. System.CommandLine) wraps the same reusable components the API and MCP server use — guaranteed behavioral parity, without requiring one monolithic core. |
| MCP server to expose functionality to others | A .NET **MCP server** wrapping the same reusable components; domain operations map to MCP **tools**; MCP boundary governed like the API (versioning, auth, capabilities). |
| Common persistent layer for artifacts/assets (documents, scripts, code) | A **blob store + metadata index + versioning + provenance** (content-addressable), exposed as a persistence/asset service — one of the most important service boundaries. |

## 4. The Three Foundational Pillars

Three themes from the design discussion anchor the direction and carry the most risk decisions. Each is worth an ADR as it is validated.

### Pillar A — Distributed scaffold via .NET Aspire

- Use **Aspire** as the distributed-app **scaffold + developer observable environment**: it orchestrates the API, MCP server, and backing-store resources, and its **service-defaults** deliver cross-cutting **OpenTelemetry metrics/tracing, health checks, and resilience** with minimal cost.
- **Start modular-monolith-shaped, split later.** Aspire lets the topology be declared in the app host rather than baked into each service, so boundaries can grow without rewrites.
- **Aspire is a dev/CI-time scaffold, not a production runtime.** Production deployment (containers / K8s / cloud) and production observability ingestion are defined separately.
- Compatibility of an Aspire version with the target .NET SDK must be verified on the build machine before anything is pinned.

### Pillar B — Pluggability = Program-to-Interfaces + Dependency Injection

- **"All components are plugins" means:** components depend on **interfaces they don't own**; DI at the **composition root** selects the concrete implementation; replaceability is achieved without coupling consumers to details.
- Introduce interfaces **where there is a genuine seam** (a type you can realistically swap: a persistence backend, an asset store, a similarity/scoring service). Avoid interface-everywhere ceremony.
- **Own the interface on the consumer side** (dependency inversion), so the consumer defines the contract and implementations plug in.
- **Keep DI wiring at the composition roots** — the API host, CLI, MCP server, and each UI shell are separate composition roots binding the same core interfaces. Avoid container/service-locator anti-patterns.
- **Scope is decidable:** internal/organizational replaceability is fully covered by interface + DI. **External third-party plugin *hosting*** (runtime loading, isolation, discovery, security) is a separate, larger capability — **deferred** and independently budgeted.

### Pillar C — Enterprise-grade = Applicable Standards & Best Practices

- **Enterprise-grade is the governed, repeatable, auditable stuff — not the architecture.** Best practices are a **primary objective**, and they are made real by **mechanical enforcement**, not aspiration.
- **"Applicable" is deliberate:** a short, curated, enforced baseline beats a long, aspirational, ignored one.
- Candidate focus areas (each selected against real risk, documented, and tool-enforced):
  - **Engineering/tooling:** nullable enable, `.editorconfig` + Roslyn analyzers, Central Package Management, `Directory.Build.props`, warning-as-error in release, CI gates.
  - **API contract:** OpenAPI as source of truth, API versioning, consistent error envelope, stability boundary protected.
  - **Security:** OWASP ASVS checklist, secrets management, OAuth 2.0 / OIDC identity for API/CLI/MCP/web, least privilege, SBOM + signed packages, supply-chain scanning.
  - **Data/asset governance:** metadata & provenance standards, content-addressable versioning, FAIR/governance principles applied to the asset store.
  - **Observability:** OpenTelemetry naming/structure so every service emits consistently.
  - **MCP / agent integration:** pinned MCP spec version; tool/resource contract and naming.

## 5. Relationship to the Current EDAM (v1)

- `Edam.Libraries` and **`Edam.Data.Catalog`** are **separate portable .NET component families** in the current EDAM; they map forward to v2.0 components. Platform-specific code sits in thin shells (`Edam.WinUI.Controls`, `Edam.Data.Catalog.WinUI`).
- The two WinUI shells (`Edam.WinUI.Controls`, `Edam.Data.Catalog.WinUI`) become **one shell among many**.
- The Area 1 model work (mapping context, booklets, JSONata execution, persistence round-trip) is portable and **not wasted** — it is the seed of the v2.0 data-assets domain.
- Migration carry-over and what is retired are explicit open questions to be resolved with requirements.

## 6. Open Decisions to Validate (ADR-tracked next steps)

These are the decisions that must be validated before v2.0 implementation begins — not for implementation now:

1. **Aspire version** pinned against the target .NET SDK / build machine; production deployment target (containers / K8s / cloud) chosen.
2. **Web UI shell:** Uno (WinUI-on-WASM) vs Blazor; whether the WinUI desktop client is retained permanently.
3. **Persistence model:** blob store + metadata index + versioning + provenance; storage provider (e.g., MinIO / object storage + metadata DB).
4. **Plugin scope confirmation:** internal interface+DI only vs later external plugin hosting.
5. **API / MCP contract stabilization:** first stable API version; MCP spec version and tool contract.
6. **Standards baseline set + enforcement mechanism** (the curated, tool-enforced list above).
7. **Identity & security baseline** across all shells (OAuth 2.0 / OIDC).

## 7. Forward Path

This introduction is the agreed **framing** to allow forward motion. The next milestone is turning it into:

- A concise **standards baseline** (selected, applicable standards with an enforcement mechanism).
- **Individual ADRs** for Pillars A/B/C and the open decisions as they are validated.
- **Approved requirements** (per `AGENTS.md` requirements-first discipline) before any implementation.

**No code, scaffolding, or implementation is authorized from this document alone.** It is the shared direction; approved requirements and ADRs authorize the work.

---

### Companion artifacts
- `docs/HANDOFF.md` — current-state handoff (EDAM v1 and v2.0 notes indexed here).
- `docs/backlog/`, `docs/adr/` — backlog items and decision records (v2.0 decisions will be recorded here).
