# ADR-0001 — Monaco Code Editor Integration Strategy

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-09-10 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Backlog** | BL-2.1 |

## Context

The repo contains **two independent Monaco integrations**:

1. **Edam.Studio** — a hand-rolled `CodeEditorControl` (`Edam.WinUI.Controls.Editors.CodeEditorControl`) hosting a WebView2 that loads `web\monaco-editor\code-editor.html`. It uses **Monaco 0.33.0** and exposes only `setEditorText` / `getEditorText` via `ExecuteScriptAsync`. It has no message bridge (until BL-2.3 added one for Ctrl-S).
2. **Edam.Data.Catalog.WinUI** — a vendored **WinUI.Monaco** control (`Monaco\` project) using **Monaco 0.49.0** (via `libman.json`). It is more complete: it exposes handlers, an `EditorPool`, `KeyCode`/`KeyMod` keybindings, `IMonacoEditor`, content-changed events, and a `WebMessageReceived` bridge.

Each solution is its **own GitHub project** (no shared repo / no root `.git`), which makes cross-solution code sharing non-trivial.

The user's stated objective: enhance the code editor (e.g., Ctrl-S triggers save) and review whether to update Monaco to a more complete version.

## Decision

**Adopt a two-track strategy:**

- **Short term (now):** Keep the two integrations independent, but **align Monaco versions** and **enhance the hand-rolled Edam.Studio editor** with the WebMessage bridge and keybindings. This delivers the immediate objective (Ctrl-S → save) with minimal risk.
- **Long term (when the solutions are consolidated):** **Extract the Monaco control into a shared WinUI library** so both solutions use a single, more complete editor.

## Options considered

### (a) Consolidate on one shared control now
- **Pros:** single codebase, consistent behavior, one Monaco version.
- **Cons:** requires cross-solution sharing (each solution is its own GitHub project); large refactor; high risk for the immediate objective.

### (b) Keep both but align versions (chosen for short term)
- **Pros:** minimal change; each solution stays independent; delivers Ctrl-S now.
- **Cons:** duplicated logic; two codebases to maintain; drift risk.

### (c) Extract Monaco into a shared library (chosen for long term)
- **Pros:** single source of truth; reusable; aligns versions; enables the more complete WinUI.Monaco control everywhere.
- **Cons:** requires a shared library project and cross-solution references; only worthwhile once the solutions are consolidated.

## Consequences

- **Edam.Studio:** update Monaco from 0.33.0 to a newer version (align to 0.49.0 or latest ~0.52.x) and keep the WebMessage bridge (BL-2.3) + Ctrl-S (BL-2.4). See BL-2.5.
- **Catalog:** keep the WinUI.Monaco control; add Ctrl-S (BL-2.2) using its existing keybinding support.
- **Long term:** when the two solutions are consolidated, extract the Monaco control into a shared WinUI library (new backlog item).

## Related backlog items

- BL-2.2 (Ctrl-S in Catalog's WinUI.Monaco)
- BL-2.3 (WebMessage bridge in Edam.Studio) — done
- BL-2.4 (Ctrl-S in Edam.Studio) — done
- BL-2.5 (Monaco version update)
