# BL-2.5 — Add dirty-state tracking (unsaved-changes indicator)

| Field | Value |
|---|---|
| **ID** | BL-2.5 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Feature |
| **Priority** | Medium |
| **Effort** | M |
| **Status** | Implemented — needs verification |

## Description

Track whether the editor content has changed since last save (via the content-changed event) and surface an unsaved indicator / prompt on close.

## Acceptance Criteria

- [x] Editor shows a "modified" indicator when content differs from last saved.
- [x] Closing a modified document prompts to save/discard/cancel.

## Dependencies

- BL-2.2 or BL-2.4

## Notes / Test Results

- Related types: `MonacoEditor.EditorContentChanged`, `EVENT_EDITOR_CONTENT_CHANGED`.
- **Implemented (Catalog tabbed editor):**
  - `MonacoEditorViewModel.IsDirty` property (with change notification).
  - `MonacoEditorControl.EditorContentChanged` event forwarded from the underlying editor.
  - `EditorTabsControl` marks the current model dirty on content change, clears it on save, and shows an orange "●" on the tab header when dirty (via new `BoolToVisibilityConverter`).
  - `TabView_TabCloseRequested` prompts **Save / Discard / Cancel** when the document is dirty.
- **Build:** CatalogExplorer builds 0 errors.
- **Runtime:** indicator + close prompt still need verification in the running app.
