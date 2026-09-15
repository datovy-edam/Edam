# BL-1.10 — Verify mapping creation (Source→Target) with tests

| Field | Value |
|---|---|
| **ID** | BL-1.10 |
| **Area** | Area 1 — Notebook / Booklets for Schema Mapping |
| **Type** | Testing / verification |
| **Priority** | High |
| **Effort** | M |
| **Status** | Done |

## Description

The mapping feature is **already implemented** (`MapSidePanel` lets the user associate a Source element with a Target element, reflected in `DataMapContext` / `MapItemInfo`). The work here is to **verify the mapping logic with tests** at the model level. UI-automation of the `MapSidePanel` control is deferred unless needed.

## Acceptance Criteria

- [x] Tests cover creating a Source→Target mapping in `DataMapContext` / `MapItemInfo`.
- [x] Tests assert the mapping is stored and retrievable from the context.

## Dependencies

- BL-1.4, BL-1.5, BL-1.13 (test project)

## Notes / Test Results

- Related types: `MapSidePanel`, `DataMapContext`, `MapItemInfo`, `MapItemType`.
- UI automation of `MapSidePanel` deferred (WinAppDriver / Windows App SDK UI testing) unless required.
- **Model-level verification — PASSING:** `DataMapContext_CreateMapping_PairsSourceToTarget` asserts that a Source→Target schema pair surfaces in `MapItems` with both `SourceItems` (Side = Source) and `TargetItems` (Side = Target). The mapping is stored on the context and retrievable.
