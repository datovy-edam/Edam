# Area 1 — Schema-Mapping Notebook / Booklet User Workflow (Spec)

> **Type:** Spec / design (BL-1.3)
> **Status:** Done — basis for the Area 1 test scenarios
> **Authority:** This document describes the *intended end-to-end workflow* the Booklet feature supports. It is grounded in the current implementation (`DataMapContext`, `BookModel`, `LexiconDataModel`, `AssetUseCaseMap`). Every step below traces to an Area 1 test item (BL-1.4–BL-1.12) and is the basis for the corresponding test cases in `Edam.Test.Studio`.

---

## 1. Purpose & scope

The Notebook/Booklet feature lets a user map a **Source schema (A)** onto a **Target schema (B)** and document that mapping in a *booklet* (a set of **text cells** and **code cells**) that captures the reasoning, can be executed to transform/sample data, and can produce a **semantic similarity** read-out of how closely source and target names align.

This spec defines the end-to-end workflow so that each step becomes a verifiable scenario. Because **the features are already implemented and the goal is verification** (see `docs/backlog/README.md`), each step is annotated with the test item and concrete test case that proves it.

---

## 2. Actors & core components

| Component | Role | Key API |
|---|---|---|
| `DataMapContext` | Central orchestrator; holds Source/Target instances, map item collections, use case, lexicon model | `CreateContext`, `SetUpMapping`, `SetMapItemReferences`, `Execute`, `LexiconSemanticTextCompare` |
| `DataMapInstance` | Wraps a Source or Target; carries the schema `Instance` and `AssetConsoleArgumentsInfo` | `Instance`, `Arguments`, `SetContext` |
| `AssetUseCaseMap` | The mapping use case: source namespace, map items, book/booklets | `FromUriVersion`, `SaveUseCase`, `SetUpMapping` |
| `BookModel` / `BookViewModel` | Hosts the booklet and its cells (text/code) | `AddControl`, `SelectedBooklet`, `FindBooklet`, `ClearAll` |
| `LexiconDataModel` | Semantic similarity provider | `SetContext`, `Compare` → `SimilarityScores` |
| `IBookItemProcessor` / `JsonProcesor` | Executes code-cell JSONata against the source sample | `Execute` |

---

## 3. The workflow

> Steps are grouped by phase. Each step lists the **data involved** and the **verification hook**.

### Phase 1 — Load the source schema (A)

**Step 1.1 — Create / resolve the data-map context.**
The app starts from `AssetConsoleArgumentsInfo` (current project arguments). `DataMapContext.CreateContext(context, source, arguments)` builds a context if the current namespace differs from the requested one, otherwise reuses the existing context. It locates the use case via `AssetUseCaseMap.FromUriVersion(arguments.NamespaceUri, name, projectVersionId)` and assigns it when found; otherwise a fresh use case is created.
- **Data involved:** `Source` (a `DataMapInstance` whose `Instance` is the loaded source UI/data object), `UseCase.Namespace`, `UseCase.SourceUriText`.
- **Verification hook:** BL-1.4 (Source loading), BL-1.13 `DataMapContext_Construct_*`.

**Step 1.2 — Populate the source map items.**
`DataMapContext.SetMapItemReferences(AssetDataMapItem)` clears `MapItems`/`SourceItems`/`TargetItems` and repopulates them from the item's `SourceElement` (marked `MapItemType.Source`). This drives the source tree/list the user sees.
- **Data involved:** `SourceItems` (ObservableCollection of `MapItemInfo`, each with `Side = Source`).
- **Verification hook:** BL-1.4, BL-1.13 `DataMapContext_LoadSourceAndTarget_PopulatesItems`, `DataMapContext_SetMapItemReferences_Null_ClearsCollections`.

### Phase 2 — Load the target schema (B)

**Step 2.1 — Resolve the target mapping.**
`DataMapContext.SetUpMapping(context)` walks `Source.Arguments.Process.MapItem`, finds the `MapItemType.Target` entry, and loads the target process arguments via `ProjectContext.GetArgumentsByProcessName(...)`. It then sets the target context on `context.Target` (`DataMapInstance.SetContext`).
- **Data involved:** `Target.Arguments`, `Target.Instance`.
- **Verification hook:** BL-1.5 (Target loading). Requires `ProjectContext.Arguments` setup (needs a real project / process-argument fixture).

**Step 2.2 — Populate the target map items.**
After Step 1.2, the same `SetMapItemReferences` call fills `TargetItems` (each with `Side = Target`), so the source and target appear side by side.
- **Verification hook:** BL-1.5, BL-1.13 `DataMapContext_LoadSourceAndTarget_PopulatesItems`.

### Phase 3 — Create a booklet for the use case

**Step 3.1 — Establish the Book model.**
`DataMapContext()` constructs a `BookViewModel` wrapping a `BookModel(context)`. `BookModel` requires `context.UseCase.Book` to be non-null (throws otherwise). `BookModel.Items` is the observable cell collection.
- **Data involved:** `BookModel.Book`, `BookModel.SelectedBooklet`, `BookModel.ListView` (bound to `context.BookletViewList`).
- **Verification hook:** BL-1.13 `BookModel_Construct_FromContext`.

### Phase 4 — Add cells to the booklet

**Step 4.1 — Add a text cell (Markdown).**
`BookModel.AddControl(viewModel, BookletCellType.Text, referenceId)` creates a `BookletCellInfo` with `CellType = Text`, `TextType = Markdown`, and `ReferenceId` set. It creates a `BookletTextCellControl`, assigns it as the cell's `Instance`, adds it to `BookletViewList`, and stores the cell in `SelectedBooklet.Items`. If no booklet is selected a fresh `BookletInfo` is created.
- **Data involved:** the `BookletCellInfo`, `SelectedBooklet.Items`.
- **Verification hook:** BL-1.6, BL-1.13 `BookModel_AddTextCell_CreatesTextCell` (needs a WinUI UI host).

**Step 4.2 — Add a code cell (JSONata).**
Same flow with `BookletCellType.Code` (`TextType = JSONata`); the code cell also seeds its input text from `Context.LanguageInstance.GetDefaultEmptyCodeText()`.
- **Data involved:** the `BookletCellInfo` with a `BookletCodeCellControl` instance.
- **Verification hook:** BL-1.7, BL-1.13 `BookModel_AddCodeCell_CreatesCodeCell` (needs a WinUI UI host).

**Step 4.3 — Reorder / delete cells.** (`BookModel`/`BookViewModel` cell management.)
- **Verification hook:** BL-1.12.

### Phase 5 — Execute cells

**Step 5.1 — Execute a single cell.**
`DataMapContext.Execute(cell)` obtains/creates the `IBookItemProcessor` (`GetProcessor()` → `new JsonProcesor(useCase, source.JsonInstanceSample)`), clears the output, reads `cell.Instance.GetInputText()`, sets it as `cell.Text`, runs `Processor.Execute(cell)`, and on success writes `results.ResultText` to the cell output. Empty input short-circuits.
- **Data involved:** `cell.Text`, `cell.OutputText`, `Processor`.Requires a source JSON sample.
- **Verification hook:** BL-1.8. Needs a `Source` with a `JsonInstanceSample` and a code-cell instance (UI host).

**Step 5.2 — Execute a booklet.**
`DataMapContext.Execute(booklet)` runs the processor over the whole booklet.
- **Verification hook:** BL-1.8 (as applicable).

### Phase 6 — Semantic similarity

**Step 6.1 — Compare source vs target semantics.**
`DataMapContext.LexiconSemanticTextCompare(booklet)` calls `LexiconModel.SetContext(this)` then `LexiconModel.Compare(booklet)`, populating `LexiconModel.SimilarityScores` (`ObservableCollection<ITextSimilarityScore>`).
- **Data involved:** `LexiconModel.SimilarityScores`; depends on `ProjectContext.Arguments` and a configured lexicon service.
- **Verification hook:** BL-1.9; `DataMapContext_SemanticCompare_ProducesScores` loads the fixture schemas and verifies the scoring **inputs** (tokenized descriptions + 5×5 pairs); the numeric comparison still needs the script-based lexicon service + `ProjectContext.Arguments`.

### Phase 7 — Mapping creation & persistence

**Step 7.1 — Create the Source→Target mapping.**
The map relationship (a source element paired to a target element) is captured on the use case / map item.
- **Verification hook:** BL-1.10.

**Step 7.2 — Save / load the use case.**
`AssetUseCaseMap.SaveUseCase(...)` persists the use case; `AssetUseCaseMap.FromUriVersion(...)` reloads it back into a context.
- **Data involved:** the serialized `AssetUseCaseMap` (use case + book/booklets + map items).
- **Verification hook:** BL-1.11.

---

## 4. Traceability matrix (step → test item → test case)

| Workflow step | BL-1 item | Status / verification |
|---|---|---|
| 1.1 Create/resolve context | BL-1.4 | Covered — `DataMapContext_Construct_*` |
| 1.2 Populate source items | BL-1.4 | Covered — `DataMapContext_LoadSourceAndTarget_PopulatesItems`, `DataMapContext_ReloadSourceTarget_ResetsCollections`, `DataMapContext_LoadSourceFromFixtureFile_PopulatesExpectedElements` |
| 2.1 Resolve target | BL-1.5 | Needs `ProjectContext` fixture |
| 2.2 Populate target items | BL-1.5 | Covered — `DataMapContext_LoadSourceAndTarget_PopulatesItems`, `DataMapContext_ReloadSourceTarget_ResetsCollections`, `DataMapContext_LoadTargetFromFixtureFile_PopulatesExpectedElements` |
| 3.1 Establish book model | (foundation) | Covered — `BookModel_Construct_FromContext` |
| 4.1 Add text cell | BL-1.6 | Covered (model) — `BookModel_CreateTextCell_CreatesCellModel`; control round-trip pending UI host |
| 4.2 Add code cell | BL-1.7 | Covered (model) — `BookModel_CreateCodeCell_CreatesCellModel`; control round-trip pending UI host |
| 4.3 Reorder/delete | BL-1.12 | Covered (`BookModel_MoveCellDown_SwapsAndMoves`, `BookModel_DeleteCell_RemovesFromSelectedBooklet`) |
| 5.1/5.2 Execute | BL-1.8 | Covered (model) — `DataMapContext_ExecuteCodeCell_ProducesOutput`, `DataMapContext_ExecuteEmptyCodeCell_IsNoop`, `DataMapContext_ExecuteInvalidCodeCell_DoesNotThrow` |
| 6.1 Semantic compare | BL-1.9 | Partial — **input pipeline verified from fixtures** (`DataMapContext_SemanticCompare_ProducesScores` + `DataMapItem_GetAnnotation_ProducesSemanticDescription`); numeric scoring needs the script-based lexicon service |
| 7.1 Create mapping | BL-1.10 | Covered — `DataMapContext_CreateMapping_PairsSourceToTarget` |
| 7.2 Save/load use case | BL-1.11 | Covered (model) — `AssetUseCaseMap_PersistRoundTrip_PreservesBooklet`; app-level `SaveUseCase` path needs `ProjectContext` |

---

## 5. Data involved (summary)

- **Schemas:** Source schema A and Target schema B (JSON documents / asset schemas). Fixtures: `Testing/Edam.Test.Studio/Fixtures/SourceSchemaA.json`, `TargetSchemaB.json`.
- **Map items:** `MapItemInfo` with `Side` (`MapItemType.Source` | `Target`), `Name`, `Path`, `ItemId`, `ReferenceId`.
- **Use case:** `AssetUseCaseMap` — namespace, project, `SourceUriText`, map items, book.
- **Book / booklet / cells:** `BookInfo` → `BookletInfo` → `BookletCellInfo` (cell `Text`/`OutputText`, `CellType`, `TextType`, `ReferenceId`).
- **Similarity:** `ITextSimilarityScore` entries in `LexiconModel.SimilarityScores`.

---

## 6. Constraints & known gaps (important for test authoring)

- **Model/UI entanglement (largely addressed).** The cell-model creation was extracted into `BookModel.CreateCell`, and code-cell execution was decoupled via `BookletCellInfo.OutputText` + `DataMapContext.Execute` falling back to `cell.Text` when no control instance is present (BL-1.8, `Edam.Data.Assets` 1.0.1). The remaining UI-bound parts are `ListView.Items.Add` wiring and the controls themselves (`BookletTextCellControl`/`BookletCodeCellControl`), which still need a **WinUI/XAML UI host** for the raw `AddControl` integration tests.
- **Project/database dependencies.** `SetUpMapping` and `LexiconSemanticTextCompare` depend on `ProjectContext.Arguments` (app settings / connection strings) and a configured **lexicon service**, which are not available in the unit-test context without a fixture.
- **Recommendation:** For full BL-1.6/BL-1.7/BL-1.8 coverage, either (a) introduce a WinUI test host (e.g. UnitTestApp + `DispatcherQueue`), or (b) refactor the cell model from the controls so `AddControl`/`Execute` can run against pure model data.

---

## 7. Related

- Backlog index: `docs/backlog/README.md`
- Test project & build procedure: `docs/backlog/BL-1.13.md`
- Handoff: `docs/HANDOFF.md` §4.5 (Area 1)
