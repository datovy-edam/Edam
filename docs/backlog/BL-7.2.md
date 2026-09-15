# BL-7.2 — Store-agnostic catalog + PostgreSQL back-end (MS-SQL/EF deferred)

| Field | Value |
|---|---|
| **ID** | BL-7.2 |
| **Area** | Area W1.1 — Wave 1.1: Catalog decoupling |
| **Type** | Refactor / architecture |
| **Priority** | **High** |
| **Effort** | L |
| **Status** | New |

## Description
Make the catalog core **DB-independent** and supply a **cost-free relational back-end** so Wave 1.1 delivers "a service that in the back uses a database," while keeping the storage provider hidden from the end-user (they consume an instance via DI and never see the DB/EF).

- Remove the baked-in **EF Core / SQL Server** dependency from the catalog core (today `Edam.Data.CatalogModel` references `Microsoft.EntityFrameworkCore.SqlServer`; `CatalogContext` in `CatalogDb` is SQL-specific).
- Add a provider-agnostic **`ICatalogStore`** (container/item/item-data metadata) + **`IContentStore`** (binary content) — following Wave-1's `ICatalogStore` provider pattern.
- **Wave 1.1 relational back-end = PostgreSQL (Npgsql)**, free and consistent with Wave-1 BL-6.6 (`PostgresCatalogStore`), implemented behind `ICatalogStore`/`IContentStore` and registered via DI so the provider is **hidden behind instancing**.
- **MS-SQL / EF is a deferred, optional future target — NOT implemented this wave** (per ADR-0007).
- **Resource addressing is path/URI-based:** a catalog resource reference is a path-like identifier that resolves to a file, JSON, XML, or blob/binary (content-agnostic) — encoded in `IContentStore` and the item model.
- Fix layering smells: `Edam.Data.CatalogServiceClient` must **stop referencing `Edam.Data.CatalogDb`**, and **drop `Microsoft.AspNetCore.OpenApi`** from the client.

## Dependencies
- BL-7.1 (move/extract), BL-7.3 (contracts/base platform defines `ICatalogStore`/`IContentStore` + path-addressable item model).

## Acceptance criteria
- [ ] Catalog core has **zero** `Microsoft.EntityFrameworkCore*` references; **no EF code in Wave 1.1**.
- [ ] A PostgreSQL (`Npgsql`) `ICatalogStore`/`IContentStore` provider implements the catalog surface and is registered via DI so consumers get an instance and never reference the provider/DB (hidden back-end).
- [ ] Resource access is path/URI-keyed — file/json/xml/blob/binary all resolvable through `IContentStore`/the item model.
- [ ] MS-SQL/EF recorded as a deferred future option (no code).
- [ ] `CatalogServiceClient` has no `CatalogDb` project reference and no `AspNetCore.OpenApi` package reference.
- [ ] A future back-end (e.g. Azure blob, Wave 2) can be added without touching core.
- [ ] **Provider-conformance suite** (ADR-0006 purity): the same catalog-behavior tests pass against FileSystem and PostgreSQL providers, proving back-ends swap without changing callers. (Testhost = user-shell; the suite is written now.)

## Progress
(planned)

## Why
Delivers the Wave-1.1 goal of a **service backed by a database at no cost** (PostgreSQL), consistent with Wave-1 persistence; keeps Microsoft SQL/EF as an optional future target; honors the hidden-provider/instancing mandate (ADR-0007, ADR-0006).
