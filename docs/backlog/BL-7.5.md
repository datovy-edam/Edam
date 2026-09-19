# BL-7.5 — Surface the real catalog behind the Wave-1 service boundary

| Field | Value |
|---|---|
| **ID** | BL-7.5 |
| **Area** | Area W1.1 — Wave 1.1: Catalog decoupling |
| **Type** | Capability / integration |
| **Priority** | **High** (de-risk proof gate) |
| **Effort** | M |
| **Status** | **Done/verified** (2026-09-15); **seam swap landed (2026-09-18)** — the Wave-1 boundary resolves the **real catalog platform** store (adapter over `Edam.Data.Catalog.Contracts.ICatalogStore`), the in-memory store is only the fallback, and the AppHost feeds the WebApi the same catalog config as the catalog service |

## Description
Wire the real, decoupled catalog (`Edam.Data.Catalog` platform) into the **Wave-1 service boundary**, replacing the in-memory stand-in, so WebApi/CLI surface real catalog data through `ICatalogService`/`ICatalogStore`.
- `Edam.Services.Core` (`AddWave1Services`) resolves the catalog surface from the catalog platform (target per `ContainerType`/config, BL-7.4) instead of the temporary in-memory catalog/booklet/vocabulary services.
- WebApi `/catalog/items` and CLI `edam wave1` reflect the **real** catalog with the `Wave1ServiceInfo` descriptor boundary intact — **FileSystem target first** (fast working proof), PostgreSQL-backed after BL-7.2. Provider hidden behind DI/instancing.
- Ensure governance/health checks still wrap the catalog surface (BL-4.3/BL-6.4).

## Dependencies
- BL-7.1 (relocate). Wave-1 shells already exist (BL-6.3). PostgreSQL wiring follows BL-7.2/7.4.

## Acceptance criteria
- [x] `Edam.slnx` builds 0-error; WebApi routes the real catalog; `/catalog/items` returns **real catalog data — FileSystem first** (a real root folder is enumerated into catalog assets). PostgreSQL follows after BL-7.2; the provider is hidden behind DI.
- [x] The temporary in-memory catalog stand-ins in `Edam.Services.Core` remain only as the **registered fallback** (active when no FileSystem root / no Postgres connection is configured).
- [x] Health/`/wave1` descriptors still report the catalog `running/healthy` (boundary untouched).

## Progress
- (2026-09-18) **Seam swap landed — the boundary now resolves the real catalog platform (verified live).** Added `Edam.Services.Core/CatalogPlatformStore` (Wave-1 `ICatalogStore` adapter over the platform `ICatalogStore`: containers→items, path-based Ids, `catalog:<provider>` identity, empty+logged on failure) and a new `AddWave1Services(IConfiguration?)` overload that maps the Wave-1 catalog keys onto the BL-7.4 composition root and calls `AddCatalogServices` — the platform provider (PostgreSQL/FileSystem) is now the store, with **in-memory only as the registered fallback**. Deleted the superseded Services.Core stand-ins (`FileSystemCatalogStore` System.IO enumeration, `PostgresCatalogStore` reading a non-existent `edam.catalog_assets` table) and dropped `Npgsql` from `Edam.Services.Core`. `Edam.WebApi` passes `builder.Configuration`, and `AppHost.cs` now feeds `edam-web-api` the same `ConnectionStrings__catalog`/`Edam__Catalog__Target=postgres` as the catalog service, so the default stack serves one real catalog. **Verified:** `Edam.slnx` 0 error; `/catalog/items` → `Store='catalog:postgres'` with 7 real items (live DB); unconfigured → `Store='in-memory'` + 3 seed assets; `/wave1` and `Edam.Cli wave1` unchanged. **Note:** the platform reference drags `Edam.System` into Services.Core, whose `Edam.Convert` shadows the BCL `System.Convert` for `Edam.*` code (BL-4.2 class) — fixed by fully qualifying in `Governance.cs`.
- (2026-09-15) Positioned as the **second (`Prove`) step** — a fast working end-to-end slice: land the relocated catalog surfaced through the Wave-1 boundary (FileSystem) before expanding (see `Wave-1.1-Catalog-Decoupling.md`).
- (2026-09-15) **Verified working slice.** Added `Edam.Services.Core/FileSystemCatalogStore.cs` (`ICatalogStore`, FileSystem target) and a `filesystem`/root-config mode in `AddWave1Services` (store chosen by DI/config; in-memory stays the fallback). `Edam.Services.Core`, `Edam.WebApi`, `Edam.Cli` all build 0-error offline; **live** run of `Edam.WebApi` with `Edam:CatalogRoot` pointed at `docs` returned `Store: filesystem` on `/catalog/items` with 50 real recursive file assets (path-based Ids + real last-write timestamps). WebApi `/catalog/items`, CLI `edam wave1`, and the `Wave1ServiceInfo`/health boundaries needed **zero** changes.
- **Scoping note (honest):** this first slice's `FileSystemCatalogStore` is **self-contained (`System.IO`)** rather than referencing the relocated catalog's FileSystem `FolderCatalogClient`/`CatalogFileSystem` (which transitively drag EF into `Edam.Services.Core`). That coupling is the BL-7.2 (EF-independence) / BL-7.4 (DI per-**Container**) work: the seam swap — pointing `ICatalogStore` at the catalog's FileSystem/PostgreSQL provider behind DI with the same caller surface — is exercised by the provider-conformance test. For now the FileSystem store is the cleanest EF-free stand-in that proves the boundary.

## Why
This is the payoff of 1.1 — the platform’s core business capability, not a stand-in, exposed through the Wave-1 boundary (ADR-0007).
