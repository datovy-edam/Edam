# BL-7.5 — Surface the real catalog behind the Wave-1 service boundary

| Field | Value |
|---|---|
| **ID** | BL-7.5 |
| **Area** | Area W1.1 — Wave 1.1: Catalog decoupling |
| **Type** | Capability / integration |
| **Priority** | **High** (de-risk proof gate) |
| **Effort** | M |
| **Status** | New |

## Description
Wire the real, decoupled catalog (`Edam.Data.Catalog` platform) into the **Wave-1 service boundary**, replacing the in-memory stand-in, so WebApi/CLI surface real catalog data through `ICatalogService`/`ICatalogStore`.
- `Edam.Services.Core` (`AddWave1Services`) resolves the catalog surface from the catalog platform (target per `ContainerType`/config, BL-7.4) instead of the temporary in-memory catalog/booklet/vocabulary services.
- WebApi `/catalog/items` and CLI `edam wave1` reflect the **real** catalog with the `Wave1ServiceInfo` descriptor boundary intact — **FileSystem target first** (fast working proof), PostgreSQL-backed after BL-7.2. Provider hidden behind DI/instancing.
- Ensure governance/health checks still wrap the catalog surface (BL-4.3/BL-6.4).

## Dependencies
- BL-7.1 (relocate). Wave-1 shells already exist (BL-6.3). PostgreSQL wiring follows BL-7.2/7.4.

## Acceptance criteria
- [ ] `Edam.slnx` builds 0-error; WebApi routes the real catalog; `/catalog/items` returns **real catalog data — FileSystem first**, then PostgreSQL after BL-7.2 (provider hidden behind DI).
- [ ] The temporary in-memory catalog stand-ins in `Edam.Services.Core` are replaced or become a registered fallback target.
- [ ] Health/`/wave1` descriptors still report the catalog `running/healthy`.

## Progress
- (2026-09-15) Positioned as the **second (`Prove`) step** — a fast working end-to-end slice: land the relocated catalog surfaced through the Wave-1 boundary (FileSystem) before expanding (see `Wave-1.1-Catalog-Decoupling.md`).

## Why
This is the payoff of 1.1 — the platform’s core business capability, not a stand-in, exposed through the Wave-1 boundary (ADR-0007).
