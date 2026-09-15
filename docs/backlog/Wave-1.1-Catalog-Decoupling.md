# Wave 1.1 — Catalog decoupling: a UI/DB-independent `Edam.Data.Catalog` platform with a PostgreSQL-backed service

> **Status:** Planned (2026-09-15) — approved direction, **de-risked ordering** (relocate → prove → extract → abstract). ADR-0007.
> **Goal:** separate the reusable, interface-based catalog code from the WinUI solution, make it DB-independent, and deliver **a service that in the back uses a database (PostgreSQL, cost-free)** — with the storage provider hidden behind DI/instancing (end-user never sees the DB/EF). Azure/blob is deferred to **Wave 2**; **MS-SQL/EF is a deferred future option**.

## Why 1.1 (and not a numbered Wave 2)
Wave 1 (distributed-platform substrate) is essentially implemented and build-verified. The catalog is the **core business capability** the platform should surface, but today it is trapped in the `...WinUI` folder and tied to SQL/EF. Extracting it now is small, DB-neutral, high-value, and unblocks Wave-1's `ICatalogService`/`ICatalogStore` with **real, database-backed** data — the natural **next increment before** bigger items (Azure blob targets → Wave 2).

## Repository baseline (VCS governance)
- **Decision (2026-09-15):** a **private GitHub repo for `Edam.slnx`** is being set up as the baseline/rollback point, created **before** BL-7.1's relocation (fits the repository-hygiene area + handoff mandate).
- **Visibility:** **private**.
- **Internal package restore:** still from `c:\nugetlocalfeed` — the repo does **not** publish or vendor the internal `Edam.*` / `Edam.Mcp` packages. A fresh clone or CI must have that feed reachable (see BL-4.15 risk). Revisit publishing to GitHub Packages before any CI / multi-developer work.
- **Secrets guard:** verify no `appsettings`/connection strings/feed tokens get committed; `.gitignore` must cover them before the first push.

## De-risking order (why this sequence)
We move **real code first, then abstract** — not the reverse. A facade with nothing behind it is a trap.
1. **Relocate the real catalog projects as-is** (keep them building + referenced by real consumers) — de-risks everything.
2. **Land one working end-to-end slice fast** (relocated catalog surfaced through the Wave-1 boundary, FileSystem target) — proves the path before more work.
3. **Extract/derive the contracts from those relocated types** — no second, parallel model; the existing `Edam.Data.Catalog.Contracts` scaffold (draft `IContentStore`/`ContainerBinding`/`ICatalogProviderResolver`/`ContainerType`) is validated here against the real model, kept or discarded on evidence.
4. **EF-independence (Postgres)**, then the **full DI/provider-resolution seam**.
Only add a seam when a real consumer needs it (ADR-0006 purity guardrail).

## Scope (Area W1.1 — `BL-7.x`, in execution order)
| Order | ID | Item | Priority / Effort |
|---|---|---|---|
| 1 (Relocate) | [BL-7.1](BL-7.1.md) | Move catalog core projects to headless `Edam.Data.Catalog` **as-is** (UI-independent; out of the WinUI solution; keep building, repoint consumers) | **High** / L |
| 2 (Prove) | [BL-7.5](BL-7.5.md) | **Working end-to-end slice**: relocate catalog surfaced via the Wave-1 boundary (`ICatalogService`/`ICatalogStore`), FileSystem target first | **High** / M |
| 3 (Extract) | [BL-7.3](BL-7.3.md) | Derive the interface/base-code platform **from the relocated types** (`Edam.Data.Catalog.Contracts`); validate/discard the draft seams | **High** / M |
| 4 (Abstract) | [BL-7.2](BL-7.2.md) | EF-independence + **PostgreSQL (Npgsql)** back-end behind `ICatalogStore`/`IContentStore`; MS-SQL/EF **deferred** | **High** / L |
| 5 (Seam) | [BL-7.4](BL-7.4.md) | DI seam + per-**Container** provider resolution (`ContainerBinding` + `ICatalogProviderResolver`); drop factory `switch` | Medium / M |

## Out of scope / deferred
- **MS-SQL / EF — deferred future option (not in Wave 1.1).** No EF code now.
- Azure.Storage.Blobs and other storage targets (S3/GCP) — **Wave 2**; the path-addressable `IContentStore` seam is reserved but no Azure code (per ADR-0007).
- Monaco/Sizers/UI refactors — they stay in the WinUI/UI area; this wave only relocates the **catalog** out of that folder.

## Definition of 1.1-complete
- `Edam.Data.Catalog` lives under `Edam.Libraries`, **with no WinUI and no EF** in core.
- A **working end-to-end slice is live**: the real catalog surfaces via `ICatalogService`/`ICatalogStore` through WebApi/CLI (BL-7.5), proving the path before expansion.
- Contracts are **derived from the relocated real types** (BL-7.3); any remaining draft seams are committed only on evidence of a consumer.
- **Provider-conformance suite** (BL-7.2) passes against FileSystem (+ PostgreSQL) — the mechanism that *proves* back-ends swap without changing callers (ADR-0006).
- `Edam.slnx` (and the catalog graph) builds 0-error; README + HANDOFF + ADR-0007 current; handoff ready for a new session.
