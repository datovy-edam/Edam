# ADR-0007 — Catalog decoupling: UI/DB-independent `Edam.Data.Catalog` platform, PostgreSQL back-end, MS-SQL/EF deferred

- **Status:** Accepted — architecture direction (2026-09-15)
- **Context:** Today the reusable catalog code lives inside the **`Edam.Data.Catalog.WinUI`** solution folder alongside WinUI apps (`Edam.CatalogExplorer`, `Edam.UI.CatalogExplorer`, `CommunityToolkit.WinUI.Controls.Sizers`, `Monaco`) and carries a baked-in **EF Core / SQL Server** dependency (`CatalogContext`; `Microsoft.EntityFrameworkCore.SqlServer` is referenced by `Edam.Data.CatalogModel`). The name and coexistence wrongly advertise a WinUI-bound catalog, and the SQL-specific storage prevents cleanly adding other back-ends. The catalog is already interface-based (`ICatalogService`, `ICatalogClient:ICatalogService`, `ICatalogContainer`, `ICatalogItem`, `ICatalogItemData`, `ICatalogs`, `ICatalogBaseClient`, `IItemContent`), with targets today of **DataContext (MS-SQL/EF)**, **File system**, and **Service (HTTP/REST client)**. A catalog "resource" is addressed by a **path-like identifier** that may resolve to a file, JSON, XML, or a blob/binary.
- **Decision:**
  1. The catalog core is **UI-independent and DB-independent**. Move the pure catalog code from `Edam.Data.Catalog.WinUI` into a headless, net10 `Edam.Data.Catalog` platform under `Edam.Libraries` (naming: `Edam.Data.Catalog` core; `...Contracts` interface/base-code platform).
  2. **No EF in Wave 1.1.** Persistence sits behind a provider-agnostic `ICatalogStore` (containers/items/item-data metadata) + `IContentStore` (binary content, keyed by the **path/URI** resource address — file, JSON, XML, or blob/binary). **Wave 1.1 relational back-end = PostgreSQL (Npgsql), free and consistent with Wave-1's BL-6.6 PostgresCatalogStore**, selected via the interface so **the end-user only ever consumes an instance via DI — the DB/EF used behind it is hidden and they need not know**.
  3. **MS-SQL/EF is a deferred, optional future target — not implemented today.** No EF code is introduced now.
  4. **Interface/base-code platform:** shared contracts + base classes (base client, base item/item-data, base container) + `ContainerType` form `Edam.Data.Catalog.Contracts`, so all targets (FileSystem, PostgreSQL, and future blob) implement the same surface.
  5. **Azure/blob and other storage targets (S3/GCP) are deferred to Wave 2** — the `IContentStore`/path-addressable seam is reserved but no Azure code now.
  6. Targets are registered/selected via DI (instancing) + `ContainerType`/config (aligned with Wave-1's `ICatalogStore` / `AddWave1Services`), replacing factory `switch` selection in `CatalogInstance`.
- **Consequences:**
  - Clean reuse: the real, **PostgreSQL-backed** catalog can back Wave-1's `ICatalogService`/`ICatalogStore` in `Edam.Services.Core`/WebApi (BL-7.5), replacing the in-memory stand-in; the MS-SQL/EF path is an optional future option.
  - Provider is hidden behind interfaces/DI — consumers never depend on DB/EF specifics.
  - Remove layering smells: `CatalogServiceClient` → no `CatalogDb`, and no `Microsoft.AspNetCore.OpenApi` in the client.
  - Cost-free relational store for the service-backed catalog; container images (BL-4.15) keep EF out of core.
  - Tracked as **Wave 1.1** (`Wave-1.1-Catalog-Decoupling.md`, Area W1.1 / BL-7.x); Azure-blob content targets as Wave 2.
- **Related:** ADR-0006 (interface-boundary mandate), ADR-0002/0003 (governance/anti-drift).
