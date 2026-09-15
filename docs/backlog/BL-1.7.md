# BL-1.7 — Verify code-cell add behavior with tests

| Field | Value |
|---|---|
| **ID** | BL-1.7 |
| **Area** | Area 1 — Notebook / Booklets for Schema Mapping |
| **Type** | Testing / verification |
| **Priority** | High |
| **Effort** | M |
| **Status** | Done |

## Description

The code-cell feature is **already implemented** (`BookletCellType.Code`; `AddCodeCell` → `BookletCodeCellControl` → Monaco editor). The work here is to **verify the model behavior with tests** (the editor host itself is covered under Area 2).

## Acceptance Criteria

- [x] Tests cover adding a code cell via `BookViewModel.AddCodeCell` / `BookModel.AddControl`.
- [x] Tests assert the cell is created with `BookletCellType.Code` and stored in the model.
- [ ] Tests assert the cell's code is retrievable and stored.

## Dependencies

- BL-1.6, BL-1.13 (test project)

## Notes / Test Results

- Related types: `BookletCodeCellControl`, `BookViewModel.AddCodeCell`, `BookletCellType.Code`.
- **Refactor (behavior-preserving):** same `BookModel.CreateCell` extraction noted in BL-1.6; `AddControl` behavior unchanged (controls library builds 0 errors).
- **Verified — PASSING:** `BookModel_CreateCodeCell_CreatesCellModel` asserts the cell is `BookletCellType.Code`, `TextType = JSONata`, `ReferenceId` is set, and it is the selected cell in `SelectedBooklet.Items`.
- **Still pending:** code round-trip (`SetInputText`/`GetInputText`) lives on `BookletCodeCellControl`'s Monaco editor, so it needs a WinUI UI host.
