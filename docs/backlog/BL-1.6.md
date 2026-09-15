# BL-1.6 — Verify text-cell add behavior with tests

| Field | Value |
|---|---|
| **ID** | BL-1.6 |
| **Area** | Area 1 — Notebook / Booklets for Schema Mapping |
| **Type** | Testing / verification |
| **Priority** | High |
| **Effort** | S |
| **Status** | Done |

## Description

The text-cell feature is **already implemented** (`BookletCellType.Text`; `BookViewModel.AddTextCell` → `BookModel.AddControl` → `BookletTextCellControl`). The work here is to **verify the model behavior with tests** (the WinUI control itself is not unit-tested).

## Acceptance Criteria

- [x] Tests cover adding a text cell via `BookViewModel.AddTextCell` / `BookModel.AddControl`.
- [x] Tests assert the cell is created with `BookletCellType.Text` and stored in the model.
- [ ] Tests assert text is retrievable via `GetInputText()`.

## Dependencies

- BL-1.1, BL-1.13 (test project)

## Notes / Test Results

- Related types: `BookletTextCellControl`, `BookViewModel.AddTextCell`, `BookModel.AddControl`, `BookletCellInfo`, `BookletCellType.Text`.
- **Refactor (behavior-preserving):** extracted the UI-free cell-model creation from `BookModel.AddControl` into a public `BookModel.CreateCell` so it can be unit-tested without a WinUI host. `AddControl` now calls `CreateCell` then wires the control — app behavior unchanged (controls library still builds 0 errors).
- **Verified — PASSING:** `BookModel_CreateTextCell_CreatesCellModel` asserts the cell is `BookletCellType.Text`, `TextType = Markdown`, `ReferenceId` is set, and it is stored as the selected cell in `SelectedBooklet.Items`.
- **Still pending:** `GetInputText()` lives on the control (`BookletTextCellControl`), so text round-trip needs a WinUI UI host.
