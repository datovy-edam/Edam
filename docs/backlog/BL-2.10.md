# BL-2.10 — Read-only mode

| Field | Value |
|---|---|
| **ID** | BL-2.10 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Feature |
| **Priority** | Medium |
| **Effort** | S |
| **Status** | Implemented — needs verification |

## Description

Support opening documents in read-only mode (with an optional message), using the existing `EditorReadOnly` / `SetEditorReadOnly` support.

## Acceptance Criteria

- [x] Read-only documents cannot be edited; a message is shown when appropriate.

## Dependencies

- BL-2.6 / BL-2.7

## Notes / Test Results

- Related types: `MonacoEditor.EditorReadOnly`, `SetEditorReadOnly`, `SetReadOnlyMessage`.
- **Implemented (Catalog):** `MonacoEditorViewModel` gained `IsReadOnly` + `ReadOnlyMessage`; `CurrentPathItem` sets `IsReadOnly` from the file's read-only attribute (`IsFileReadOnly`). `MonacoEditorControl.InitializeEditorControlAsync` applies `EditorReadOnly` and `EditorReadOnlyMessage` to the editor instance.
- **Build:** CatalogExplorer builds 0 errors.
- **Runtime:** read-only behavior still needs verification in the running app.
