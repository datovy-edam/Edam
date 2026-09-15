# BL-2.2 — Implement Ctrl-S → save in the WinUI.Monaco control (Catalog)

| Field | Value |
|---|---|
| **ID** | BL-2.2 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Feature |
| **Priority** | Medium |
| **Effort** | S |
| **Status** | Implemented — needs verification |

## Description

Add a Monaco keybinding for `CtrlCmd + S` in the editor's `index.html` that posts an `EVENT_EDITOR_SAVE` message; handle it in `ProcessMonacoEvents` and trigger the save flow (get content → persist).

## Acceptance Criteria

- [ ] Pressing Ctrl-S in the editor triggers the save command (content is read and persisted).
- [ ] The default browser save dialog is suppressed.
- [ ] Works for both Ctrl (Windows) and Cmd (Mac) modifiers.

## Dependencies

- BL-2.1

## Notes / Test Results

- Related types: `Monaco\Monaco\MonacoEditor.xaml.cs` (`ProcessMonacoEvents`, `WebMessageReceived`), `Monaco\monaco-editor\index.html`.
- **Implemented:**
  - `Monaco\monaco-editor\index.html` — added a Ctrl-S / Cmd-S keybinding (`monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS`) that posts `EVENT_EDITOR_SAVE_REQUESTED`.
  - `Monaco\Monaco\MonacoEditor.xaml.cs` — added an `EditorSaveRequested` event and handled `EVENT_EDITOR_SAVE_REQUESTED` in `ProcessMonacoEvents`.
  - `Edam.UI.CatalogExplorer\Controls\MonacoEditorControl.xaml.cs` — exposed `EditorSaveRequested` and forwarded it from the underlying editor.
  - `Edam.UI.CatalogExplorer\Controls\EditorTabsControl.xaml.cs` — subscribed to `EditorSaveRequested` and calls `EditorTabsViewModel.UpdateModel` (reads content → `PostItemAsync`).
- **Build blocker (pre-existing):** the Catalog solution could not build because its projects referenced Edam.Libraries via **stale DLL `HintPath`s to a non-existent `src\Edam.Common` folder** (HANDOFF §4.2 risk #1). **RESOLVED** — see below.
- **Build fix (done):** repointed all 8 Catalog projects from the stale `Edam.Common`/`Edam.Net` HintPaths to **ProjectReferences** to `Edam.System`/`Edam.Net`; reimplemented the missing tree types (`TreeItemType`, `ITreeItem`, `ITreeContainer`, non-generic `TreeItem`) and other missing members in `Edam.System`/`Edam.Net`; fixed the `Application` namespace conflict in `Edam.CatalogExplorer\App.xaml.cs`. The **full Catalog solution now builds with 0 errors**, and Edam.Studio still builds (0 errors).
- **Runtime:** pressing Ctrl-S in the running app still needs verification.
