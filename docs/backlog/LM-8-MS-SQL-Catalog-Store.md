# LM-8 — MS-SQL as a catalog storage kind (the catalog on SQL Server)

- **Status:** **Authorized 2026-09-28** by the user's decision (*"use c"*) — the catalog should be able to
  live in **MS-SQL**, using the installation's existing convention (`Server=.`, integrated security), in
  place of PostgreSQL.
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
