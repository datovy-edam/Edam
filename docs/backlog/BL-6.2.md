# BL-6.2 — Onboard existing Edam.Libraries "legacy resources" as components/services

| Field | Value |
|---|---|
| **ID** | BL-6.2 |
| **Area** | Area W1 — Wave 1: distributed platform migration |
| **Type** | Migration / capability |
| **Priority** | **High (Wave 1)** |
| **Effort** | L |
| **Status** | **Implemented — needs verification** (contracts+core build 0-err; WebApi end-to-end needs online build) — 2026-09-14 |

## Description
**Move EDAM legacy resources into the distributed platform** — package and expose existing `Edam.Libraries` capabilities (data catalog, mapping/booklets, vocabulary/lexicon, reference/data codes, assets) as distributable **components and services** behind interfaces, consumable by the Wave-1 shells (API/MCP/CLI) rather than living only inside a desktop WinUI app.

## Scope
- Inventory the legacy resources/capabilities in `Edam.Libraries` and the Catalog; classify each as **hosted service**, **in-process library component**, or **data store**.
- Wrap service-worthy capabilities behind interfaces (DI-composed), exposing them to the Aspire mesh.
- Instrument each onboarded resource with the MEL/OTel diagnostics (metrics + health) so **every service is observable**.
- The WinUI UI is **out of scope for Wave 1** (it is a reference model only; UI not yet migrated).

## Dependencies
- BL-6.1 (Aspire substrate), BL-4.1 (MEL diagnostics), BL-3.1 (.NET 10).

## Acceptance criteria
- [x] Inventory of legacy resources is documented (service vs component vs store). → **`Wave-1-Inventory.md`** (catalog=hosted; mapping/booklets + vocabulary=components; catalog DB/blob=data stores).
- [ ] At least the catalog + mapping/booklets + vocabulary surfaces are exposed behind interfaces and run inside the mesh with health + metrics. → **Implemented**: `Edam.Services.Contracts` (`ICatalogService`, `IBookletMappingService`, `IVocabularyService`, `IWave1Service`) + `Edam.Services.Core` (`InMemory*` + `AddWave1Services()`), both net10, **build 0-error in-sandbox**; wired into the WebApi (`/wave1`, `AddWave1Services()`) + health/metrics from ServiceDefaults. **Verification pending**: the WebApi is `Microsoft.NET.Sdk.Web`, which my sandbox cannot restore offline (broken SDK workload installer — the BL-3.1 machine defect), so the end-to-end build/run needs the user's online `dotnet build Edam.slnx`.
- [x] No UI migration in this wave. (WinUI untouched.)

## Progress (2026-09-14)
- `Wave-1-Inventory.md` — full legacy-resource classification.
- New projects added to `Edam.slnx`: `Edam.Services.Contracts`, `Edam.Services.Core`. WebApi now references Core (DI-composed `AddWave1Services()`), exposes `/wave1` listing onboarded surfaces + their `Wave1ServiceInfo` (lifecycle/health), with ServiceDefaults OTel/health (BL-6.1).
- Namespace note: chose `Wave1ServiceInfo` (not `ServiceDescriptor`) to avoid the DI `ServiceDescriptor` collision — same BL-4.2 collision class.

## Why Wave 1
This is the literal "move EDAM legacy resources into the distributed platform" step — UI deferred, platform + observability first.
