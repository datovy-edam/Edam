# BL-2.4 — Implement Ctrl-S → save in Edam.Studio's `CodeEditorControl`

| Field | Value |
|---|---|
| **ID** | BL-2.4 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Feature |
| **Priority** | Medium |
| **Effort** | S |
| **Status** | Implemented — needs verification |

## Description

Using the bridge from BL-2.3, add a Ctrl-S keybinding that posts a save event; the C# side reads the editor text and runs the existing `NotifyEditorTextAvailable` → `AssetSaveTextRequested` flow.

## Acceptance Criteria

- [ ] Ctrl-S in Edam.Studio's editor triggers the save flow.
- [ ] Content is read via `getEditorText()` and persisted.

## Dependencies

- BL-2.3

## Notes / Test Results

- Related types: `Edam.WinUI.Controls.Editors.CodeEditorControl`, `CodeEditorViewModel` (`NotifyEditorTextAvailable`, `GetEditorText`), `NotificationType.AssetSaveTextRequested`.
- **Implemented:** Ctrl-S / Cmd-S in the Monaco editor posts a `'save'` message; `CodeEditorControl.OnWebMessageReceived` calls `CodeEditorViewModel.SaveRequested()`, which reads the editor text via `GetEditorText()` (setting `TextDocument.Text` → `NotifyEditorTextAvailable` → `AssetSaveTextRequested`).
- **Build:** `Edam.WinUI.Controls` builds with 0 errors (VS MSBuild). Runtime behavior needs verification in the running app.
