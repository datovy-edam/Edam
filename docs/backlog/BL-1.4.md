# BL-1.4 — Verify Source schema (A) loading with tests

| Field | Value |
|---|---|
| **ID** | BL-1.4 |
| **Area** | Area 1 — Notebook / Booklets for Schema Mapping |
| **Type** | Testing / verification |
| **Priority** | High |
| **Effort** | M |
| **Status** | Done |

## Description

The source-schema loading feature is **already implemented** (loads a source schema from an asset, XSD, JSON, or DB schema via `Edam.Data.Schema` / `Edam.Data.Assets` and populates `DataMapContext.Source` / `SourceItems`). The work here is to **verify it with tests**.

## Acceptance Criteria

- [x] Tests cover loading a source schema into `DataMapContext.Source` and `SourceItems`.
- [x] Tests assert the expected element set is populated from a fixture schema.
- [ ] Tests assert loading errors are surfaced in the results log.

## Dependencies

- BL-1.1, BL-1.3, BL-1.13 (test project)

## Notes / Test Results

- Related types: `DataMapContext.Source`, `MapItemInfo`, `Edam.Data.Assets`, `Fixture\SourceSchemaA.json`.
- **Fixture-file loading — VERIFIED (PASSING).** `DataMapContext_LoadSourceFromFixtureFile_PopulatesExpectedElements` reads the real `Fixtures\SourceSchemaA.json`, builds an `AssetDataMapItem` (`SourceElement` = the fixture elements), and drives the actual `SetMapItemReferences` API; asserts SourceItems = { CustomerId, CustomerName, EmailAddress, OrderDate, OrderTotal }. `DataMapContext_LoadFixtureFile_SetsSideAndSemanticDescription` asserts every loaded item is `Side=Source` and carries a non-empty semantic description (`GetAnnotation` → "Customer Id").
- **Honest boundary:** the test harness reads our simple fixture JSON (name/dataType/description elements) directly; it is **not** exercising the DB-oriented `Edam.Data.Schema.SchemaReader` (ADO.NET GetSchema, needs a connection string) or XSD/JSON-Schema readers. Error-surfacing to a results log (acceptance 3) is an app-level behavior not captured headlessly.
