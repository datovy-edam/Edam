# BL-2.1 — Decide and document the Monaco integration strategy

| Field | Value |
|---|---|
| **ID** | BL-2.1 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Architecture / decision |
| **Priority** | Medium |
| **Effort** | S |
| **Status** | Done |

## Description

The repo has two Monaco integrations: Edam.Studio's hand-rolled `CodeEditorControl` (Monaco 0.33.0) and the Catalog solution's vendored `WinUI.Monaco` control (Monaco 0.49.0). Decide whether to (a) consolidate on one shared control, (b) keep both but align versions, or (c) extract Monaco into a shared library. Record the decision in `docs/`.

## Acceptance Criteria

- [ ] A written decision (ADR) in `docs/` with rationale and chosen approach.
- [ ] The chosen approach is reflected in the subsequent backlog items.

## Dependencies

- None.

## Notes / Test Results

- Edam.Studio: `Edam.WinUI.Controls.Editors.CodeEditorControl` + `web\monaco-editor\` (Monaco 0.33.0).
- Catalog: `Monaco\` project (vendored WinUI.Monaco, Monaco 0.49.0 via `libman.json`).
- **Decision recorded in `docs/adr/0001-monaco-integration-strategy.md` (ADR-0001).** Chosen: two-track — short term keep both but align versions + enhance the hand-rolled editor (Ctrl-S done); long term extract Monaco into a shared WinUI library when the solutions are consolidated.
