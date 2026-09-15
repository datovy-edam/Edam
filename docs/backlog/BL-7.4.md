# BL-7.4 — DI seam + target provider selection

| Field | Value |
|---|---|
| **ID** | BL-7.4 |
| **Area** | Area W1.1 — Wave 1.1: Catalog decoupling |
| **Type** | Refactor / infrastructure |
| **Priority** | Medium |
| **Effort** | M |
| **Status** | New |

## Description
Replace today's **factory/static selection** (`CatalogInstance.GetCatalog` `switch` on an invariant name; `CatalogFileSystemClient.GetClient/GetClientAsync` statics) with a **DI seam**: register the catalog surface (`ICatalogClient`, `ICatalogStore`/`IContentStore`, container/item/item-data) via a single `AddCatalogServices()` extension and select the target by **`ContainerType` + configuration** — consistent with Wave-1's `AddWave1Services()`/`ICatalogStore` config switch.
- Define the provider-selection config key/section (e.g. `Edam:Catalog:Target = filesystem | postgres | service` and/or `ContainerType`).
- Wire `Microsoft.Extensions.DependencyInjection` + `Microsoft.Extensions.Logging.Abstractions` for the catalog services.
- Remove the concrete `switch`/static factory from consumer paths.

## Dependencies
- BL-7.1–7.3 (core + contracts + stores).

## Acceptance criteria
- [ ] `AddCatalogServices()` registers catalog interfaces + base classes; the registered `ICatalogClient`/`ICatalogStore` is chosen via `ContainerType`/config.
- [ ] No consumer calls `new CatalogFileSystemClient(...)`/`CatalogInstance.GetCatalog` direct.
- [ ] Consumers resolve an instance via DI and never reference the provider (hidden back-end — Postgres/FileSystem); a target swap is a config/DI change only (matches Wave-1 seam).
- [ ] **Per-Container resolution:** `ICatalogProviderResolver<TProvider>` resolves a `ContainerBinding` (`{ ContainerId, ContainerType Target, BaseUri }`) to its provider — e.g. an `ICatalogProviderResolver<IContentStore>` returns the FileSystem store for one Container and the PostgreSQL store for another, via a DI/config registry (no UI/service `switch`).
- [ ] Provider-conformance suite (from BL-7.2) passes against FileSystem + PostgreSQL through the DI-registered surface.

## Progress
(planned)

## Why
Removes the coupling of consumers to concrete targets and aligns the catalog with Wave-1's DI/provider patterns (ADR-0007; BL-6.2/6.3 precedent).
