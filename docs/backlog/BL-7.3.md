# BL-7.3 — Establish the catalog interface/base-code platform

| Field | Value |
|---|---|
| **ID** | BL-7.3 |
| **Area** | Area W1.1 — Wave 1.1: Catalog decoupling |
| **Type** | Architecture / contracts |
| **Priority** | **High** |
| **Effort** | M |
| **Status** | **Done** — Extract: net10 no-EF `Edam.Data.Catalog.Contracts` platform **derived from the relocated real types** (2026-09-15). Target adoption + DI = BL-7.2/7.4 |

## Description
Consolidate the catalog's interface collection and shared base classes into a single **interface/base-code platform** — **`Edam.Data.Catalog.Contracts`** — that every target (FileSystem, **PostgreSQL/Npgsql data store**, future blob) implements uniformly. This follows the ADR-0006 mandate: **access via interfaces, never concrete classes**.
- Move/consolidate the interfaces: `ICatalogService`, `ICatalogClient:ICatalogService`, `ICatalogContainer`, `ICatalogItem`, `ICatalogItemData`, `ICatalogs`, `ICatalogBaseClient`, `IItemContent`, plus `ContainerType` (enum: `Unknown/DataContext/FileSystem`, extended as needed).
- Share the **base** code ("move any other code into the interface base platform"): `CatalogBaseClient`, base item/item-data, shared models/entities, and the new `ICatalogStore`/`IContentStore` (BL-7.2).
- Base platform is `net10`, dependency-light, UI-agnostic, **no EF**, DI-friendly (usable with `Microsoft.Extensions.DependencyInjection`).

## Dependencies
- BL-7.1 (extract), BL-7.2 (DB-independence; adds store/content interfaces onto the platform).

## Acceptance criteria
- [x] `Edam.Data.Catalog.Contracts` builds net10; exposes only interfaces + value records/enums (dependency-light, no EF, no packages) — **verified offline**.
- [ ] FileSystem and PostgreSQL (Npgsql) targets depend on the platform and implement `ICatalog*`/`ICatalogStore`/`IContentStore`; no target-specific types leak — **lands with BL-7.2 (EF-independence) / BL-7.4**.
- [ ] Everything is registerable/resolvable via DI (`AddCatalogServices()`), consistent with `AddWave1Services()` — **lands with BL-7.4 (per-Container resolution)**.

## Progress
- (2026-09-15) Draft `Edam.Data.Catalog.Contracts` scaffolded (`ContainerType`, `IContentStore`, `ContainerBinding`, `ICatalogProviderResolver`; net10, 0-error). **De-risk note:** *draft prototype only* — the real contracts are **derived from the relocated real types**; the draft seams are committed only where a real consumer validates them. NOT executed before BL-7.1/BL-7.5.
- (2026-09-15) **Extract executed.** Created `src\Edam.Data.Catalog\Edam.Data.Catalog.Contracts\` (added to `Edam.Data.Catalog.slnx`; old `Edam.Libraries` draft home removed). It holds the **validated seam contracts** (`ContainerType` extended `DataContext=1/FileSystem=2/PostgreSql=3/Service=4`, `ContainerBinding`, `ICatalogProviderResolver`, `IContentStore`) **plus the derived interface + value surface** from the relocated real types: `ContainerInfo`, `CatalogInfo`, `ItemInfo`, `ItemDataInfo`, `ContentTypeInfo` value records + `ItemType` enum, and `ICatalogService`, `ICatalogClient`, `ICatalogContainer`, `ICatalogItem`, `ICatalogItemData`, `ICatalogs`, `IItemContent` interfaces. **Verified offline: builds net10, 0-error, no EF/no packages** (self-contained; derived shapes from `Edam.Data.CatalogModel`).
- **Scoping decision (recorded, ADR-0006):** this is a **derived**, dependency-light platform rather than a **move** of the real types, because the relocated value POCOs are EF-annotated (`[Table]`, `[Key]`, `[Index]` needing `Microsoft.EntityFrameworkCore`) and live in the EF-coupled `Edam.Data.CatalogModel` — a literal move would drag EF into the contract platform and is the **BL-7.2 (EF-independence)** work. The full migration of `Model`/targets onto this platform (implement `ICatalog*`, `AddCatalogServices()`) is **BL-7.2/BL-7.4**. The derived contract is the clean, authoritative provider-agnostic surface (matches the de-risked "derive, validate/discard the draft" step).

## Why
A provider-agnostic contract + base platform is what makes targets interchangeable and Azure/blob later a drop-in (ADR-0007, ADR-0006).
