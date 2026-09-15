# BL-7.1 — Extract catalog core to a headless `Edam.Data.Catalog`

| Field | Value |
|---|---|
| **ID** | BL-7.1 |
| **Area** | Area W1.1 — Wave 1.1: Catalog decoupling |
| **Type** | Migration / refactor |
| **Priority** | **High** |
| **Effort** | L |
| **Status** | In Progress — moved as-is + all references resolved; **compile pending user online build** (EF/OpenApi packages not offline-cached) |

## Description
Move the reusable catalog code out of the **`src\Edam.Data.Catalog.WinUI\`** solution folder (which also hosts WinUI apps — `Edam.CatalogExplorer`, `Edam.UI.CatalogExplorer`, `CommunityToolkit.WinUI.Controls.Sizers`, `Monaco`) into a **headless, net10 `Edam.Data.Catalog`** platform under `Edam.Libraries`, alongside the other headless libraries. The catalog core is **UI-independent** and must not depend on (or live beside) UI projects.

**Approach — relocate as-is first** (keep existing namespaces/projects) into `Edam.Libraries\Data\Edam.Data.Catalog\`: the highest-value, lowest-risk move. Do **not** re-abstract during the move; the project/namespace split into `Edam.Data.Catalog.Contracts` / core / stores happens in later steps (BL-7.3 contracts, BL-7.2 stores).

## Dependencies
- ADR-0007 (catalog decoupling direction). BL-7.3 defines the contracts; BL-7.2 EF-independence pairs with this move.
- Existing catalog projects to relocate: `Edam.Data.CatalogModel`, `Edam.Data.CatalogDb` (adapter), `Edam.Data.CatalogService`, `Edam.Data.CatalogServiceClient` (client parts).

## Acceptance criteria
- [ ] Catalog core builds **net10** under `Edam.Libraries`, referenced by no UI/AppSDK project.
- [ ] No `Edam.Data.Catalog*` project references a WinUI/AppSDK package.
- [ ] WinUI apps (Explorer/Sizers/Monaco) remain in the UI area; the catalog portion is gone from the `Edam.Data.Catalog.WinUI` folder (only UI + testing remains).
- [x] **Relocate as-is in one move** (namespaces preserved); no re-abstraction during relocation — this is the foundational de-risk step (`Relocate`).
- [ ] `Edam.slnx` (or the Wave-1 solution + a catalog sln) builds 0-error — **pending user online build** (offline restore blocked on EF/OpenApi/System.Private.Uri).

## Progress
- (2026-09-15) Moved the 4 catalog projects **as-is** (namespaces preserved) from `src\Edam.Data.Catalog.WinUI\` → `src\Edam.Data.Catalog\` — `CatalogModel`, `CatalogDb`, `CatalogService` (host), `CatalogServiceClient`.
- Fixed `Edam.Libraries\System\...` reference depth (now `..\..\`), repointed consumers (`Edam.UI.CatalogExplorer`, `Edam.Test.TestBuilder`, `Edam.Test.TestCatalogLibrary`) to the new area, repointed the `Edam.Data.CatalogExplorer.sln` project paths, and added a standalone `src\Edam.Data.Catalog\Edam.Data.Catalog.slnx`.
- **Validation:** every ProjectReference (catalog graph, consumers, both sln files) resolved via absolute `GetFullPath` — **ALL RESOLVE**. (Caught + fixed a path-depth bug and an `Edam.Data.Catalog\` area-segment bug during this check.)
- **Pending (user online shell, not offline-cacheable):** compile the catalog graph (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.OpenApi`, `System.Private.Uri` not in sandbox cache) and the repointed WinUI solution. No EF removal yet (that is BL-7.2, still deferred).

## Why
The `...WinUI` name/location mis-communicates that the catalog is UI-bound and blocks reuse — the catalog is the core business capability Wave-1 must surface (BL-7.5).
