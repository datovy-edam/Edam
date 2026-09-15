# BL-2.3 — Add a WebMessage bridge to Edam.Studio's `code-editor.html`

| Field | Value |
|---|---|
| **ID** | BL-2.3 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Feature |
| **Priority** | Medium |
| **Effort** | S |
| **Status** | Implemented — needs verification |

## Description

Edam.Studio's hand-rolled `code-editor.html` has no message bridge (only `setEditorText` / `getEditorText` called via `ExecuteScriptAsync`). Add a `chrome.webview.postMessage` bridge so the editor can push events (e.g., save, content-changed) to the C# side.

## Acceptance Criteria

- [ ] The editor can post messages to the host via WebView2.
- [ ] The C# side can receive and dispatch them.

## Dependencies

- BL-2.1

## Notes / Test Results

- File: `Edam.Studio\Edam.Studio\web\monaco-editor\code-editor.html`.
- **Implemented:** added a `window.chrome.webview.postMessage('save')` bridge (Ctrl-S / Cmd-S keybinding) in `code-editor.html`, and a `WebMessageReceived` handler in `CodeEditorControl.xaml.cs` that dispatches the `'save'` message to `CodeEditorViewModel.SaveRequested()`.
- **Build:** `Edam.WinUI.Controls` builds with 0 errors (VS MSBuild). Runtime behavior needs verification in the running app.
