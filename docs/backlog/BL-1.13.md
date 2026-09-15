# BL-1.13 — Establish the Edam.Studio test project and Area 1 coverage

| Field | Value |
|---|---|
| **ID** | BL-1.13 |
| **Area** | Area 1 — Notebook / Booklets for Schema Mapping |
| **Type** | Testing / infrastructure |
| **Priority** | High |
| **Effort** | M |
| **Status** | In Progress |

## Description

Edam.Studio currently has **no dedicated test project**. This item establishes the test foundation for all of Area 1:
1. Create a test project (e.g. `Edam.Test.Studio`) in the Edam.Studio solution, using **MSTest** (consistent with the rest of the repo).
2. Add test fixtures — sample **Source schema A** and **Target schema B** (JSON/XSD) to drive the mapping tests.
3. Provide the coverage umbrella for BL-1.4 through BL-1.12 (model/viewmodel-level tests).

## Acceptance Criteria

- [x] A test project exists in the Edam.Studio solution and builds.
- [x] Fixtures for Source A and Target B are added.
- [ ] Tests for BL-1.4 – BL-1.12 are written and pass.
- [ ] Tests run in CI.

## Dependencies

- BL-1.1 (build must succeed)

## Notes / Test Results

- **Scaffolded:** `Edam.Test.Studio` project created at `src\Edam.Studio\Testing\Edam.Test.Studio\` and added to the Edam.Studio solution (WinUI MSTest).
- **Build blocker RESOLVED:** the test project failed to build because the machine has only **Windows SDK 10.0.26100.0** installed. Fixed by:
  - Retargeting the test project to `net9.0-windows10.0.26100.0` (matches installed SDK).
  - Disabling PRI generation (`AppxGeneratePriEnabled=false`, `AppxGeneratePrisForPortableLibrariesEnabled=false`).
  - Pinning `RuntimeIdentifier=win-x64` (the Windows App SDK transitively advertises `win10-*` RIDs that .NET SDK 10 rejects; override resolves it).
  - **Build command:** `MSBuild Edam.Test.Studio.csproj -p:Configuration=Debug -p:Platform=x64 -p:RuntimeIdentifier=win-x64 -p:RuntimeIdentifiers=win-x64`.
- **Test results:** 28 tests total → **26 passed, 2 skipped**, run via `vstest.console.exe`. Covers context construction, **fixture-file source/target loading** (SourceSchemaA/TargetSchemaB → SourceItems/TargetItems + side + semantic description), reload/reset semantics, null-`SetMapItemReferences`, empty-`ClearAll`, mapping creation (Source→Target pair), annotation/description tokenization + semantic-compare input pipeline, lexicon-model availability, `BookModel` construct, `FindBooklet`, `ClearAll`, text-cell and code-cell model creation via `CreateCell`, **code-cell execution** (JSONata success + empty-input + malformed-query), **cell reorder and delete** (MoveCellDown + DeleteCell, model + view-model), and **use-case persist round-trip** (AssetUseCaseMap ToFile/FromFile).
- **Skipped (need a WinUI UI host):** `BookModel_AddTextCell_CreatesTextCell`, `BookModel_AddCodeCell_CreatesCodeCell` — `AddControl` instantiates `ListView`/`BookletTextCellControl`/`BookletCodeCellControl`, which require an initialized WinUI/XAML host not available under vstest. Marked `[Ignore]`.
- **Design note:** the Area 1 model layer (`BookModel`, `DataMapContext`) is entangled with WinUI controls (`ListView`, `UserControl`), so full cell-add/code-execution/semantic tests need a WinUI test host or a model/control refactor (see HANDOFF §4.5).
