# BL-1.3 — Define the schema-mapping user workflow (spec)

| Field | Value |
|---|---|
| **ID** | BL-1.3 |
| **Area** | Area 1 — Notebook / Booklets for Schema Mapping |
| **Type** | Spec / design |
| **Priority** | High |
| **Effort** | M |
| **Status** | Done |

## Description

Document the end-to-end mapping workflow the Booklet supports: load Source schema A, load Target schema B, create a Booklet for a use case, add text/code cells, execute cells, run semantic similarity, view results, save/load.

**Reframed for the testing focus:** this workflow doc becomes the basis for the **test scenarios** — each step maps to a test case that verifies the existing implementation.

## Acceptance Criteria

- [x] A written workflow doc (in `docs/`) describing each step and the data involved.
- [x] Each Area 1 test scenario traces to a step in this workflow.

## Dependencies

- None (can run in parallel with BL-1.1).

## Notes / Test Results

- **Deliverable:** `docs/area1-schema-mapping-workflow.md` — end-to-end workflow grounded in the current API (`DataMapContext.CreateContext`/`SetUpMapping`/`SetMapItemReferences`/`Execute`/`LexiconSemanticTextCompare`, `BookModel.AddControl`, `AssetUseCaseMap.FromUriVersion`/`SaveUseCase`).
- **Traceability matrix** maps every workflow phase to BL-1.4–BL-1.12 and to concrete `Edam.Test.Studio` test cases, marking which are covered, skipped (WinUI UI host), or pending (need `ProjectContext`/lexicon fixtures).
- **Related types:** `DataMapContext` (Source/Target `DataMapInstance`, `SourceItems`/`TargetItems`, `AssetUseCaseMap`, `LexiconModel`), `BookModel`, `BookViewModel`.
