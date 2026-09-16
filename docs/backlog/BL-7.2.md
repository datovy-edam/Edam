# BL-7.2 — Store-agnostic catalog + PostgreSQL back-end (MS-SQL/EF deferred)

| Field | Value |
|---|---|
| **ID** | BL-7.2 |
| **Area** | Area W1.1 — Wave 1.1: Catalog decoupling |
| **Type** | Refactor / architecture |
| **Priority** | **High** |
| **Effort** | L |
| **Status** | **In Progress** — store/content seam added to Contracts; **EF→Npgsql data-layer rewrite gated on live Postgres + online build (user shell)** |

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
- [x] **Provider-conformance suite** (ADR-0006 purity): the same catalog-behavior tests pass against FileSystem and PostgreSQL providers, proving back-ends swap without changing callers. (Harness written (`Edam.Data.Catalog.Conformance`) + **verified offline against the in-memory reference provider: ALL CONFORM**; live-PostgreSQL run = user shell, plus a FileSystem `ICatalogStore` provider fixture to join.)

## Progress
## Progress
- (2026-09-15) **Retirement (staged) — sub-step 1 done:** relocated `CatalogBaseClient` **out of `Edam.Data.CatalogDb` into `Edam.Data.CatalogModel`** (it is a pure client base — WebApiClient + catalog model contracts, **no EF/DB dependency**; it sat in the DB project by accident, beside the `ICatalogBaseClient` it implements). All 10 referencing files already `using Edam.Data.CatalogModel;`, so the move is un-ambiguous. EF-based `CatalogServiceInstance`/`CatalogContext` etc. (which extended it) remain in `CatalogDb` for the next sub-steps. Note: Model still carries EF (`Microsoft.EntityFrameworkCore.SqlServer`) until sub-step 2, so sub-step-1 landings are not yet offline-buildable.
- (2026-09-15) **Retirement (staged) — sub-step 2 done + verified offline:** stripped all EF from the catalog **core** `Edam.Data.CatalogModel` — removed the `Microsoft.EntityFrameworkCore.SqlServer` package + the `[Index]` attribute + EF usings in `ContainerInfo.cs`/`ContainerType.cs` (remaining `[Table]`/`[Key]`/`[MaxLength]`/`[ForeignKey]` are EF-free data annotations). `Edam.Data.CatalogModel` now **builds offline, 0-error**. `CatalogDb` got a **temporary EF bridge** (its own EFSqlServer 9.0.2 reference, documented) so the graph still compiles online until it is retired in sub-steps 3-4.
- (2026-09-15) **Retirement (staged) — sub-step 3a done:** wired the Npgsql provider into the running catalog service via DI — `builder.Services.AddCatalogServices(builder.Configuration)` in `Edam.Data.CatalogService\Program.cs`, `appsettings.json` gains `ConnectionStrings:catalog` (Postgres DSN; the `catalogDb` MS-SQL string stays as the deferred target). No behavioral change yet (endpoints still back onto `CatalogDb`).
- **Wire-contract gate found for the endpoint rewire (sub-steps 3b/4):** the catalog endpoints return `Edam.Data.CatalogModel`'s **rich EF entities** (`ContainerInfo`/`ItemInfo` with many props), while the Npgsql `ICatalogStore` returns `Edam.Data.Catalog.Contracts`' **lean value records** (`ContainerInfo(Guid Id, string ContainerId, string Description, ContainerType ContainerType, string ContainerUri, string ContentType)`, etc.). Switching endpoints onto the store **changes the JSON wire contract** that the HTTP clients (`CatalogClient`, WinUI) deserialize into — so it is a breaking change, not mechanical, and must be landed with compile+runtime verification (user online build + live Postgres) plus an explicit reconciliation of the two `ContainerType` enums (Model vs Contracts). **Not done blind.** -> **DECISION (ADR-0008, 2026-09-15): migrate the service clients/UI onto the `Contracts` value-record shapes** as the single canonical wire/domain contract. Service endpoints will emit Contracts records via the Npgsql `ICatalogStore`; `CatalogClient`/`ClientCatalog*`/WinUI deserialize/bind Contracts records; the `CatalogModel` EF entity set + `CatalogDb` are retired from the active path. Compile/runtime-gated (user online build + live Postgres) — not done blind. Remaining sub-steps: 3b (endpoints emit Contracts records), 4 (drop `CatalogDb`), 5 (WinUI/Testing).
- (2026-09-15) Started. Added the catalog **`ICatalogStore`** metadata seam to `src\Edam.Data.Catalog\Edam.Data.Catalog.Contracts\` (aggregates `ICatalogContainer`/`ICatalogItem`/`ICatalogItemData`, plus `DescribeStore()`), completing the **`ICatalogStore`/`IContentStore`** store+content seam pair BL-7.2 names. **Verified offline: Contracts builds net10, 0-error.**
- (2026-09-15) **PostgreSQL environment provisioned as a container.** Added root `compose.yaml` (service `postgres`, image `postgres:17-alpine`, container `edam-postgres`, persistent volume `edam_pgdata`, healthcheck, `localhost:5432`). **Npgsql connection string: `Server=localhost;Port=5432;Database=edam;User Id=edam;Password=edam`.** Container must be started by a human: `docker compose up -d --pull always` — the agent sandbox cannot reach the Docker engine pipe (open //./pipe/dockerDesktopLinuxEngine = `Access is denied`, same boundary as `git push`).
- (2026-09-15) **Npgsql provider + DI + provider-conformance suite — verified offline.** New `Edam.Data.Catalog.PostgreSql` provider (`PostgreSqlCatalogStore : ICatalogStore`, `PostgreSqlContentStore : IContentStore`, idempotent schema, `AddCatalogServices()` DI) references **only `Contracts` + Npgsql** (no Model/EF). New `Edam.Data.Catalog.Conformance` harness runs the **same provider-agnostic scenario** (ADR-0006) against any `ICatalogStore`/`IContentStore` — **built & run offline against the in-memory reference provider: ALL CONFORM (exit 0)**. Run against live Postgres with `Edam.Data.Catalog.Conformance postgres "<connection>"` (user shell).
- **Gate found (recorded):** the whole catalog graph references `Edam.Data.CatalogModel`, which still carries the **uncached `Microsoft.EntityFrameworkCore.SqlServer`** package — so **no EF-coupled project builds offline until EF is removed**. Removing EF from Model requires rewriting `CatalogDb`'s EF data layer onto **Npgsql** (runtime-verified against a live PostgreSQL = **user shell**). Remaining BL-7.2 chunks: (1) `CatalogDb` → Npgsql + Model EF removal (now that the Npgsql path is proven, this is the safe next step); (2) `CatalogServiceClient` decouple (drop `CatalogDb` + `Microsoft.AspNetCore.OpenApi`); (3) DI wiring into a consumer host; (4) run the conformance suite against live Postgres.

## Why
Delivers the Wave-1.1 goal of a **service backed by a database at no cost** (PostgreSQL), consistent with Wave-1 persistence; keeps Microsoft SQL/EF as an optional future target; honors the hidden-provider/instancing mandate (ADR-0007, ADR-0006).
