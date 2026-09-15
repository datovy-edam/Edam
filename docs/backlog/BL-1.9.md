# BL-1.9 — Verify semantic similarity comparison with tests

| Field | Value |
|---|---|
| **ID** | BL-1.9 |
| **Area** | Area 1 — Notebook / Booklets for Schema Mapping |
| **Type** | Testing / verification |
| **Priority** | High |
| **Effort** | M |
| **Status** | Implemented — needs verification |

## Description

The semantic-similarity feature is **already implemented** (`LexiconModel` / `TextSimilarityScoreViewer`; `SemanticSimilarityCompare_Click` → `LexiconSemanticTextCompare`). The work here is to **verify the scoring logic with tests** (the viewer control itself is not unit-tested).

## Acceptance Criteria

- [ ] Tests cover `DataMapContext.LexiconSemanticTextCompare` / `LexiconModel` scoring.
- [ ] Tests assert similarity scores are computed from Source/Target element names/descriptions.
- [ ] Tests assert expected scores for known fixture inputs.

## Dependencies

- BL-1.4, BL-1.5, BL-1.13 (test project)

## Notes / Test Results

- Related types: `LexiconModel`, `TextSimilarityScoreViewer`, `BookletPanelControl.SemanticSimilarityCompare_Click`, `DataMapContext.LexiconSemanticTextCompare`, `TextSimilarityService`.
- **Input pipeline verified (model-level) — PASSING.** `DataMapContext_SemanticCompare_ProducesScores` now loads the real fixture files (`SourceSchemaA.json`/`TargetSchemaB.json`), populates `SourceItems`/`TargetItems` via `SetMapItemReferences`, and asserts the scoring **inputs** are prepped end-to-end: every item has a non-empty tokenized `GetAnnotation()` description ("Customer Id", "Id"), and 5 × 5 = 25 candidate Source×Target pairs are ready. `DataMapItem_GetAnnotation_ProducesSemanticDescription` independently verifies the tokenizer (proper-case, drops "dbo").
- **Full scoring still NOT headless-testable:** the score computation is `TextSimilarityService.GetSimilarityScore` → `ITextSimilarityInstance.ExecuteScript("semanticTextSimilarity", ...)`, a script-based service that needs a configured text-similarity script/instance + `ProjectContext.Arguments` (via `LexiconModel.SetContext`). That requires the running app or a script fixture — out of the unit-test host.
- **Status** kept at "Implemented — needs verification" because the numeric score output remains uncovered (only its inputs are).
