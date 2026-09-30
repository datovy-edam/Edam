# LM-8 — MS-SQL as a catalog storage kind (the catalog on SQL Server)

- **Status:** **Authorized 2026-09-28** by the user's decision (*"use c"*) — the catalog should be able to
  live in **MS-SQL**, using the installation's existing convention (`Server=.`, integrated security), in
  place of PostgreSQL.
  **Progress 2026-09-28 — tasks 1 and 4 DONE and build-verified.** **Task 1:** `mssql` is now a storage
  kind (`STORAGE_KINDS`) with its aliases normalised (`sqlserver`, `sql-server`, `sql server`,
  `mssqlserver`, `tsql`), and the normaliser's doc updated. **Task 4:** `ProjectServicesHelper.Build()`
  **no longer pins `postgres`** — it takes the kind from `Edam:Catalog:Target` (default `postgres`,
  normalised) and also hands a configured `Edam:Catalog:FileSystemRoot` into the bootstrap, which is
  exactly what option **(b)** (a file-system catalog) was missing. Both solutions build with **0 errors**
  and the suite is **ALL CONFORM**. **Task 3 (the schema) is DONE too:** the new sibling project
  **`src/Edam.Data.Catalog.MsSql`** (`Edam.Data.Catalog.MsSql.csproj`; `net10.0`, ImplicitUsings +
  Nullable, `Microsoft.Data.SqlClient` 5.1.6, a ProjectReference to the Contracts, and an entry in
  `Edam.Data.Catalog.slnx`) contains **`MsSqlSchema`** — the T-SQL peer of the PostgreSQL DDL as ordered
  **idempotent** batches (the five tables, the container's unique index and the two item indexes), with the
  type mapping recorded (`uuid` → `uniqueidentifier`; `text` → `nvarchar(max)` **except key and index
  columns, which must be `nvarchar(450)`** because SQL Server cannot index `nvarchar(max)`;
  `integer` → `int`; `timestamptz` → `datetimeoffset`; `bytea` → `varbinary(max)`) and the content table
  keyed `(container_id, resource_path)`. **Correction recorded:** that composite key needs **no migration**
  in MS-SQL — LM-2b's `ALTER TABLE … ADD PRIMARY KEY` upgraded an existing *PostgreSQL* database, so here
  it is simply part of the create script. **Tasks 2 (the stores) and 5 (the group) remain.**
  **The exact remaining port, taken from the peer:** `ICatalogStore` is a composite of
  **`ICatalogContainer` + `ICatalogItem` + `ICatalogItemData`**, and the peer implements every member as an
  async method **plus a blocking wrapper** — `EnsureSchemaAsync`, `DescribeStore`; container:
  `GetContainerAsync(string?)`, `GetContainerAsync(Guid)`, `GetContainer`, `GetContainersAsync`,
  `EnlistContainerAsync`, `SetContainerAsync`, `DelistContainerAsync`; item: `GetItemAsync(Guid)`,
  `GetItemByPathAsync`, `GetContainerItemsAsync`, `GetContainerRootItemAsync`, `GetBranchAsync`,
  `AddItemAsync`, `CreateBranchAsync`, `CreateRootItem`, `DeleteItemAsync`; item-data:
  `GetItemDataAsync`, `AddItemAsync(ItemDataInfo)`, `GetDataAsync`, `GetDataByNameAsync`,
  `GetContentTypeAsync`, `CreateDataLeaf` (two overloads), `DeleteDataAsync`, `DeleteItemDataAsync`.
  **Task 2 — the CONTENT store is DONE and compiled** (`MsSqlContentStore`, `IContentStore`'s four members:
  `OpenReadAsync`, `WriteAsync`, `DeleteAsync`, `ExistsAsync`): binary content in `varbinary(max)` keyed by
  **container + resource path** (LM-2b), the **unscoped** namespace being the empty container id `''`, the
  lazy once-only `EnsureSchemaAsync` idiom, and the SQL translated faithfully — the `ON CONFLICT (… ) DO
  UPDATE` upsert becoming a **`MERGE`** on the composite key, and `SELECT EXISTS(…)` becoming
  `SELECT COUNT(1)` (MS-SQL has no bare `SELECT EXISTS`). Compiling against `IContentStore` also
  **confirms the interface is exactly those four members**, i.e. the seam is smaller than the catalog
  store's and fully covered. **Task 2 — the CATALOG store (`MsSqlCatalogStore`) REMAINS**, and it is the
  bulk of the work: the peer is 439 lines with its own row mappers, so the next step is to **read
  `PostgreSqlCatalogStore.cs`'s mapper/`SchemaSql` regions and port them**, using the inventory and
  translations below.
  **SQL translations to apply:** `ON CONFLICT (…) DO UPDATE` → **`MERGE`** (or `IF EXISTS … UPDATE ELSE
  INSERT`); `RETURNING` → **`OUTPUT INSERTED.*`**; `LIMIT 1` → **`TOP 1`**; `bytea`/`text` per the mapping
  above; and keep the lazy once-only `EnsureSchemaAsync` idiom (`_initialized` + `Interlocked`), calling
  the new `MsSqlSchema.EnsureAsync`.
  **Porting facts gathered, so a session can start cold:** the peers live in
  `src/Edam.Data.Catalog/Edam.Data.Catalog.PostgreSql/` — `PostgreSqlCatalogStore.cs` (**21.7 KB**),
  `PostgreSqlContentStore.cs` (6 KB), `CatalogProviderResolver.cs`, `CatalogServices.cs` — and implement
  **`ICatalogStore`** from `Edam.Data.Catalog.Contracts`; the project is `net10.0` with ImplicitUsings +
  Nullable and references **Npgsql 10.0.0** only. The PostgreSQL schema is five tables —
  `edam_container`, `edam_item`, `edam_item_data` (uuid primary keys), `edam_content_type`
  (`type_id text`) and `edam_content` (**`resource_path text` primary key**; LM-2's composite key
  `(container_id, resource_path)` came from a later migration, so **read the migration path, not only the
  create script**) — and `CREATE TABLE IF NOT EXISTS` is the idempotency idiom to mirror. The MS-SQL peer
  should be a **new sibling project `Edam.Data.Catalog.MsSql`** referencing `Edam.Data.Catalog.Contracts`
  + **`Microsoft.Data.SqlClient` 5.1.6** (already in the NuGet cache, so **no network is required**).
  **Honest limit:** the MS-SQL stores **cannot** be runtime-verified from the agent sandbox (Windows/SSPI
  auth is blocked), so they must ship with the `catalog (mssql, local)` group **skipped** when no DSN is
  supplied and **user-verified** with a SQL-login DSN.
- **Why:** today the only catalog storages are **PostgreSQL** and the **file system**
  (`ProjectSettings.STORAGE_KINDS = filesystem | postgres | service`), and the Studio's
  `ProjectServicesHelper.Build()` **pins `Edam:Catalog:Target=postgres`** whenever
  `ConnectionStrings:catalog` is set. An MS-SQL connection string is therefore not merely unimplemented —
  the CF-1 reader reports it as **invalid**. The user's environments are MS-SQL based (Windows/SSPI auth,
  `Server=.`), so a SQL Server store is the difference between "the catalog is available" and "the catalog
  needs a second database engine installed".
- **Scope (tasks, in order):**
  1. **Vocabulary.** Add **`mssql`** to `ProjectSettings.STORAGE_KINDS` and to the target normalizer, so
     `Edam:Catalog:Target=mssql` is a *valid* storage kind (CF-1 must report it as `Set`, not `Invalid`).
     Update the CF-1 conformance group accordingly.
  2. **The stores.** Implement **`MsSqlCatalogStore`** and **`MsSqlContentStore`** as peers of
     `PostgreSqlCatalogStore` / `PostgreSqlContentStore`. *Read those two first* and implement the **same
     interfaces with the same semantics** — do not invent a parallel shape.
  3. **Schema (idempotent ensure-schema).** Mirror the PostgreSQL schema, including LM-2's **composite key
     `(container_id, resource_path)`**, the container metadata (`ContainerType`, the default flag) and
     whatever the content store needs. The store must be able to create what it needs on first use, the way
     its Postgres peer does.
  4. **Containers.** `EnlistContainer` + the default flag + container types must work, so both
     `Edam:Projects:DefaultCollection` and the provider's `IsDefault` path behave exactly as they do on
     Postgres (the Studio's `ResolveCollectionAsync` already honours the configured default first).
  5. **Wiring.** `ProjectServicesHelper.Build()` must stop pinning `postgres`: choose the kind from
     configuration (`Edam:Catalog:Target`, defaulting sensibly), keep using `ConnectionStrings:catalog` as
     the binding's **credential reference** (`ProjectSettings` already reads it that way), and select the
     MS-SQL stores when the kind says so.
  6. **Locality.** `LocalTarget.IsLocalServer` (already implemented and checked: `.`, `(local)`,
     `localhost`, `127.0.0.1`, `::1`, `.\INSTANCE`, `host,port`) is what lets ADR-0012's starter project be
     offered on a **local** catalog. `Server=.` therefore counts as local — verify this with the MS-SQL
     store in place.
  7. **Conformance.** Add a **`catalog (mssql, local)`** group mirroring `catalog (postgres, local)`
     (containers · projects · resources · the composite key · the scoping checks). **Important:** the agent
     sandbox **cannot** authenticate with Windows/SSPI (verified: `sqlcmd -E` fails with *"No credentials
     are available in the security package"*), so this group must either take a **SQL login** DSN or be run
     by the **user**. The group must be **skipped**, never failed, when no DSN is supplied — the same rule
     the PostgreSQL groups already follow.
  8. **Secrets.** No password in the packaged seed (ADR-0010): integrated security, an environment
     variable, the binding's `Credential` (LM-4), or the per-user overlay.
- **Acceptance:** with `ConnectionStrings:catalog` pointing at MS-SQL, `Edam:Projects:Target=catalog` and a
  container `default` enlisted, the Studio lists, creates and seeds projects from the catalog; the starter
  project is offered and created (local target); the `catalog (mssql, local)` group is green when a DSN is
  supplied; every existing group is unchanged.
- **Out of scope:** a migration tool; an **enlistment UI** (the Studio still has no way to enlist a
  container — an ops gap shared with the PostgreSQL path, worth its own small item).
- **Size:** medium–large (two stores + schema + wiring + conformance). **No new package** —
  `Microsoft.Data.SqlClient` is already referenced and version-pinned.
- **Note:** the `default` container for the user's installation still has to be **enlisted**; LM-8 makes it
  possible to enlist it *in MS-SQL*.

## References
- `docs/adr/0011-*.md` (the catalog is the location system), `docs/adr/0012-*.md` (the gated starter
  project — "local" now includes a local catalog), `docs/adr/0013-*.md` (the storage-kind vocabulary).
- `src/Edam.Data.Projects/*` — `ProjectSettings` (vocabulary), the Postgres stores (peers), the conformance
  runner.
- `docs/HANDOFF.md` items 99–100 (the catalog configuration findings and this authorization).
