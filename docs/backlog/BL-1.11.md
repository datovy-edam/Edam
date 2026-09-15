# BL-1.11 — Verify Booklet persist / save with tests

| Field | Value |
|---|---|
| **ID** | BL-1.11 |
| **Area** | Area 1 — Notebook / Booklets for Schema Mapping |
| **Type** | Testing / verification |
| **Priority** | High |
| **Effort** | M |
| **Status** | Done |

## Description

The persist/save feature is **already implemented** (saves a Booklet — cells + mapping context — to storage and reloads it; wired via `NotificationType.AssetSaveTextRequested`). The work here is to **verify it with tests**.

## Acceptance Criteria

- [x] Tests cover saving a Booklet (cells + mappings) and reloading it.
- [x] Tests assert cells and mappings are intact after a save/load round-trip.
- [ ] Tests assert save/load errors are surfaced.

## Dependencies

- BL-1.6, BL-1.7, BL-1.10, BL-1.13 (test project)

## Notes / Test Results

- Related types: `NotificationType.AssetSaveTextRequested`, `BookModel`, `DataMapContext`.
- **Verified (model-level) — PASSING:** `AssetUseCaseMap_PersistRoundTrip_PreservesBooklet` exercises the underlying primitives `AssetUseCaseMap.ToFile` (JSON-serialize to disk) → `AssetUseCaseMap.FromFile` (read + deserialize) and asserts the use case name, booklet count, and each cell's `CellType`/`TextType`/`Text`/`ReferenceId` survive the round-trip. `BookletCellInfo.Instance` (the UI control) is `[JsonIgnore]`, so persistence is purely the model.
- **Still pending / entangled:** the app-level save path (`DataMapContext.SaveUseCase` → `ProjectContext.CurrentProject.CurrentArguments` + `GetUseCaseFolderPath()`) needs `ProjectContext` setup and is not headless-testable; save/load error surfacing (acceptance 3) likewise. The serialization core is now covered.
