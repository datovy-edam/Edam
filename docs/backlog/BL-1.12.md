# BL-1.12 — Verify cell reorder / delete with tests

| Field | Value |
|---|---|
| **ID** | BL-1.12 |
| **Area** | Area 1 — Notebook / Booklets for Schema Mapping |
| **Type** | Testing / verification |
| **Priority** | High |
| **Effort** | S |
| **Status** | Done |

## Description

The cell-management feature — move cells via `MoveCellDown`, delete via `DeleteCell`. The work here is to **verify the model behavior with tests**.

## Acceptance Criteria

- [x] Tests cover moving a cell down via `BookViewModel.MoveCellDown` / `DataMapContext.MoveCellDown`.
- [x] Tests cover deleting a cell via `BookViewModel.DeleteCell`.
- [x] Tests assert the model and cell order stay consistent.

## Dependencies

- BL-1.6, BL-1.7, BL-1.13 (test project)

## Notes / Test Results

- Related types: `BookViewModel.MoveCellDown`, `BookViewModel.DeleteCell`, `DataMapContext.MoveCellDown`.
- **Reorder — VERIFIED (PASSING).** Decoupled (behavior-preserving): pure `BookModel.MoveCellDown` (swap within `SelectedBooklet.Items`) + `DataMapContext.MoveCellDown` now calls it and only refreshes when a move occurred. Tests: `BookModel_MoveCellDown_SwapsAndMoves`, `BookModel_MoveCellDown_LastCell_DoesNotMove`.
- **Delete — IMPLEMENTED + VERIFIED.** `BookViewModel.DeleteCell` was an empty stub; implemented it and added `BookModel.DeleteCell` (removes the cell from `SelectedBooklet.Items`, clears `SelectedCell` if it pointed at the deleted cell, and removes the cell's UI control from the `ListView` when present). Passing tests: `BookModel_DeleteCell_RemovesFromSelectedBooklet`, `BookModel_DeleteCell_ClearsSelectedCell`, `BookViewModel_DeleteCell_NoArg_UsesSelectedCell`, `BookViewModel_DeleteCell_WithExplicitCell`.
- Controls library builds 0 errors.
