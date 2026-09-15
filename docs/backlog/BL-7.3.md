# BL-7.3 — Establish the catalog interface/base-code platform

| Field | Value |
|---|---|
| **ID** | BL-7.3 |
| **Area** | Area W1.1 — Wave 1.1: Catalog decoupling |
| **Type** | Architecture / contracts |
| **Priority** | **High** |
| **Effort** | M |
| **Status** | New for execution (step **3: Extract**) — draft `Edam.Data.Catalog.Contracts` scaffold exists; to be **derived/validated against the relocated real types** and kept or discarded (de-risk order) |

## Description
Consolidate the catalog's interface collection and shared base classes into a single **interface/base-code platform** — **`Edam.Data.Catalog.Contracts`** — that every target (FileSystem, **PostgreSQL/Npgsql data store**, future blob) implements uniformly. This follows the ADR-0006 mandate: **access via interfaces, never concrete classes**.
- Move/consolidate the interfaces: `ICatalogService`, `ICatalogClient:ICatalogService`, `ICatalogContainer`, `ICatalogItem`, `ICatalogItemData`, `ICatalogs`, `ICatalogBaseClient`, `IItemContent`, plus `ContainerType` (enum: `Unknown/DataContext/FileSystem`, extended as needed).
- Share the **base** code ("move any other code into the interface base platform"): `CatalogBaseClient`, base item/item-data, shared models/entities, and the new `ICatalogStore`/`IContentStore` (BL-7.2).
- Base platform is `net10`, dependency-light, UI-agnostic, **no EF**, DI-friendly (usable with `Microsoft.Extensions.DependencyInjection`).

## Dependencies
- BL-7.1 (extract), BL-7.2 (DB-independence; adds store/content interfaces onto the platform).

## Acceptance criteria
- [ ] `Edam.Data.Catalog.Contracts` builds net10; exposes only interfaces + value records/enums + base classes.
- [ ] FileSystem and PostgreSQL (Npgsql) targets depend on the platform and implement `ICatalog*`/`ICatalogStore`/`IContentStore`; no target-specific types leak into consumers.
- [ ] Everything is registerable/resolvable via DI (`AddCatalogServices()` extension), consistent with `AddWave1Services()`.

## Progress
- (2026-09-15) Draft `Edam.Data.Catalog.Contracts` scaffolded (`ContainerType`, `IContentStore`, `ContainerBinding`, `ICatalogProviderResolver`; net10, 0-error). **De-risk note:** this is a *draft prototype* only — the real contracts are **derived from the relocated real types** in this step; the draft seams are committed only where a real consumer validates them. NOT executed before BL-7.1/BL-7.5 (see reordered `Wave-1.1-Catalog-Decoupling.md`).

## Why
A provider-agnostic contract + base platform is what makes targets interchangeable and Azure/blob later a drop-in (ADR-0007, ADR-0006).
