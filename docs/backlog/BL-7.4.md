# BL-7.4 — DI seam + target provider selection

| Field | Value |
|---|---|
| **ID** | BL-7.4 |
| **Area** | Area W1.1 — Wave 1.1: Catalog decoupling |
| **Type** | Refactor / infrastructure |
| **Priority** | Medium |
| **Effort** | M |
| **Status** | **In Progress** — DI composition root + per-<b>Container</b> resolver delivered & verified; consumer migration (WinUI desktop → DI) pending |

## Description
Replace today's **factory/static selection** (`CatalogInstance.GetCatalog` `switch` on an invariant name; `CatalogFileSystemClient.GetClient/GetClientAsync` statics) with a **DI seam**: register the catalog surface (`ICatalogClient`, `ICatalogStore`/`IContentStore`, container/item/item-data) via a single `AddCatalogServices()` extension and select the target by **`ContainerType` + configuration** — consistent with Wave-1's `AddWave1Services()`/`ICatalogStore` config switch.
- Define the provider-selection config key/section (e.g. `Edam:Catalog:Target = filesystem | postgres | service` and/or `ContainerType`).
- Wire `Microsoft.Extensions.DependencyInjection` + `Microsoft.Extensions.Logging.Abstractions` for the catalog services.
- Remove the concrete `switch`/static factory from consumer paths.

## Dependencies
- BL-7.1–7.3 (core + contracts + stores).

## Acceptance criteria
- [x] `AddCatalogServices()` registers catalog interfaces + base classes; the registered `ICatalogClient`/`ICatalogStore` is chosen via `ContainerType`/config.
- [ ] No consumer calls `new CatalogFileSystemClient(...)`/`CatalogInstance.GetCatalog` direct. *(Service host now resolves `ICatalogStore` via DI — done; the WinUI desktop app still constructs its local providers directly in `GetLocalInstance` — the remaining consumer-migration step.)*
- [x] Consumers resolve an instance via DI and never reference the provider (hidden back-end — Postgres/FileSystem); a target swap is a config/DI change only (matches Wave-1 seam).
- [x] **Per-Container resolution:** `ICatalogProviderResolver<TProvider>` resolves a `ContainerBinding` (`{ ContainerId, ContainerType Target, BaseUri }`) to its provider — e.g. an `ICatalogProviderResolver<IContentStore>` returns the FileSystem store for one Container and the PostgreSQL store for another, via a DI/config registry (no UI/service `switch`).
- [x] Provider-conformance suite (from BL-7.2) passes against FileSystem + PostgreSQL through the DI-registered surface.

## Progress
- (2026-09-15) **DI seam delivered + verified (offline build 0 error; conformance-through-DI ALL CONFORM).** Added **`Edam.Data.Catalog.DependencyInjection`**: `CatalogServices.AddCatalogServices(config)` — the single composition root that registers the catalog interfaces by `ContainerType`/config (`Edam:Catalog:Target` = filesystem|postgres|service; provider config via `ConnectionStrings:catalog`, `Edam:Catalog:FileSystemRoot`, `Edam:Catalog:ServiceBaseUri`), registers the hidden default `ICatalogStore`/`IContentStore` (only when the default target resolves) and the remote `ICatalogClient` when a service base URI is set. Added **`CatalogProviderRegistry`** implementing `ICatalogProviderResolver<ICatalogStore>` + `<IContentStore>` — per-**Container** resolution with a default-target fallback. `Edam.Data.CatalogService` host now uses the consolidated `AddCatalogServices` (resolves `ICatalogStore` by contract, never names a provider). **Verified:** a throwaway DI probe ran the provider-conformance suite **through the DI surface — ALL CONFORM** (live Postgres), and resolved per-Container providers: a `FileSystem` Container → the FileSystem store, a `PostgreSql` Container → the Postgres store, an unconfigured `Service` Container → default fallback. MSTest (`Edam.Data.Catalog.Tests/CatalogResolutionTests`) covers the registry's per-Container mapping + `ParseTarget`. Remaining: migrate the WinUI desktop app to resolve stores via this DI seam (it still constructs its local providers directly).
- (2026-09-15) **BL-7.2 store correctness fix (surfaced during re-validation).** `PostgreSqlCatalogStore.EnlistContainerAsync` used `ON CONFLICT (container_id) DO UPDATE` but returned a fresh `Guid.NewGuid()`, so re-enlisting an existing container returned an id that did not match the persisted row and `GetContainer(byId)` came back null (only a fresh volume passed). Fixed to `... RETURNING id, ...` so the returned `Id` is always the actual row. Standalone Postgres conformance is again **ALL CONFORM 12/12** (verified with leftover rows present).

## Why
Removes the coupling of consumers to concrete targets and aligns the catalog with Wave-1's DI/provider patterns (ADR-0007; BL-6.2/6.3 precedent).
