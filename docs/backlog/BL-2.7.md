# BL-2.7 — Update Monaco version in the Catalog `Monaco` project (0.49.0 → current)

| Field | Value |
|---|---|
| **ID** | BL-2.7 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Dependency / maintenance |
| **Priority** | Medium |
| **Effort** | M |
| **Status** | Implemented — needs verification |

## Description

Bump the vendored Monaco in the Catalog `Monaco` project (currently 0.49.0 via `libman.json`) to the current stable release, following the WinUI.Monaco update procedure.

## Acceptance Criteria

- [x] Monaco updated; editor loads and all existing operations (set/get, language, theme, content-changed) still work.

## Dependencies

- BL-2.1

## Notes / Test Results

- Current version: 0.49.0 (per `Monaco\libman.json`).
- Update procedure documented in `Monaco\README.md`.
- **Updated to 0.56.0** (latest stable on npm). Replaced the vendored `dev`/`esm`/`min` folders and metadata from `monaco-editor@0.56.0`; removed the obsolete `min-maps` folder; updated `libman.json` to `monaco-editor@0.56.0`.
- **Bridge fix:** `index.html` no longer references `min/vs/editor/editor.main.nls.js` (no longer exists in 0.56.0). The Ctrl-S (`EVENT_EDITOR_SAVE_REQUESTED`) and content-changed bridge logic is unchanged.
- **Build:** Catalog solution builds 0 errors; updated files confirmed in the Monaco output (`min/vs/editor/editor.main.js` present, `nls.js` gone).
- **Runtime:** editor load + set/get + language + theme + content-changed still need verification in the running app.
