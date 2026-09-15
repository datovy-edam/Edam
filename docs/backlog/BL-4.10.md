# BL-4.10 — Import / connector layer (i1 #8)

| Field | Value |
|---|---|
| **ID** | BL-4.10 |
| **Area** | Area E — Enterprise (also core product) |
| **Type** | Feature / capability |
| **Priority** | Medium (i1 #8) |
| **Effort** | L |
| **Status** | New |

## Description
EDAM is a *Data Assets Management* platform, but the stack has **no extraction/import/adapter tier**. Add a pluggable import/connector layer as reusable components (behind interfaces + DI), for ingesting assets from common formats/sources (Excel/OpenXML, XML/XSD, JSON, CSV, legacy sources).

## Scope
- Define `IImportAdapter`/`IConnector` interface seams; registry so adapters are plug-and-instantiate.
- Initial adapters: Excel/OpenXML, XML (reuse `Edam.Xml`), JSON, CSV.
- Reuse existing Edam.Xml/Edam.Json components as adapter implementations.

## Acceptance criteria
- [ ] Import adapters behind interfaces, DI-composed, discoverable.
- [ ] At least the primary formats import to the asset model headlessly-testable.
- [ ] Headless tests for each adapter.

## Why
Closest to core product value among the i1 gaps; unblocks asset onboarding and integrates with schema-mapping/booklets (Area 1).
