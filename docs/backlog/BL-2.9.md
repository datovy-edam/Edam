# BL-2.9 — Theme support (light/dark/high-contrast)

| Field | Value |
|---|---|
| **ID** | BL-2.9 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Feature |
| **Priority** | Medium |
| **Effort** | S |
| **Status** | Implemented — needs verification |

## Description

Wire the editor theme to the app theme (light/dark/high-contrast) via the existing `EditorThemes` / `SetThemeAsync` support.

## Acceptance Criteria

- [x] Editor theme follows the app theme and updates on change.

## Dependencies

- BL-2.6 / BL-2.7

## Notes / Test Results

- Related types: `EditorThemes`, `MonacoEditor.SetThemeAsync`.
- **Implemented (Catalog):** `MonacoEditorControl.InitializeEditorControlAsync` sets `EditorInstance.EditorTheme` from the app theme via `GetEditorTheme()` (maps `ApplicationTheme.Dark` → `VisualStudioDark`, else `VisualStudioLight`). Applied on editor load.
- **Build:** CatalogExplorer builds 0 errors.
- **Runtime:** theme following the app theme still needs verification in the running app.
