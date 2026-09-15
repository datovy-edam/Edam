# BL-1.1 — Fix `Notebooks` vs `Booklets` csproj path mismatch

| Field | Value |
|---|---|
| **ID** | BL-1.1 |
| **Area** | Area 1 — Notebook / Booklets for Schema Mapping |
| **Type** | Bug / build blocker |
| **Priority** | High |
| **Effort** | S |
| **Status** | Done |

## Description

`Edam.WinUI.Controls.csproj` `Page Update` entries reference `Controls\Notebooks\NotebookTextCellControl.xaml`, `NotebookPanelControl.xaml`, `BookletCodeCellControl.xaml`, and `FramePanelControl.xaml`, but the actual files live under `Controls\Booklets\`. The `None Remove` entries already use `Booklets`. This mismatch can break the XAML build.

**This is a prerequisite for the Area 1 testing work** — tests cannot run against a project that does not build.

## Acceptance Criteria

- [ ] All `Page Update` entries point to the real `Controls\Booklets\` paths.
- [ ] `dotnet build` of `Edam.WinUI.Controls` succeeds with no missing-XAML errors.
- [ ] No `Notebooks` path remains in the csproj.

## Dependencies

- None.

## Notes / Test Results

- Files confirmed present under `Controls\Booklets\`: `BookletPanelControl.xaml`, `BookletTextCellControl.xaml`, `BookletCodeCellControl.xaml`, `FramePanelControl.xaml`.
