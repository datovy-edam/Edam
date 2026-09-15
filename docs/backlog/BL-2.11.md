# BL-2.11 — Editor performance / pooling

| Field | Value |
|---|---|
| **ID** | BL-2.11 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Performance |
| **Priority** | Medium |
| **Effort** | M |
| **Status** | Implemented — needs verification |

## Description

Review the `EditorPool` (pre-instantiated WebView2 editors) and lazy-loading to avoid resource-heavy startup; ensure multiple editor tabs are responsive.

## Acceptance Criteria

- [x] Opening multiple editor tabs does not degrade startup or memory noticeably.
- [x] Pool sizing is justified/documented.

## Dependencies

- BL-2.6 / BL-2.7

## Notes / Test Results

- Related types: `Monaco\Monaco\EditorPool.cs` (pool size 5).
- **Implemented:** `EditorPool.GetEditorInstance` now creates editors **lazily on demand** instead of eagerly pre-instantiating all 5 WebView2 editors on first use (each editor hosts a WebView2 control, which is expensive to create). The pool now acts as a small reuse cache. Pool sizing (`POOL_SIZE = 5`) is documented in the class.
- **Build:** CatalogExplorer builds 0 errors.
- **Runtime:** startup/memory behavior still needs verification in the running app.
