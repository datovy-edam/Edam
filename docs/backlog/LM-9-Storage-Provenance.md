# LM-9 — Project storage is a deployment fact, and it is always stated

- **Status:** **In progress (2026-10-08)** — the **configuration half is delivered and build-verified**; the
  **inspection surface (the UI half)** is designed, not built. Authority: **ADR-0014** (*Accepted*), building
  on **ADR-0007**, **ADR-0009**, **ADR-0010**, **ADR-0011** and **ADR-0013**.
- **Why:** a person using the Studio asked *"where is my project data stored? I expect it in the Catalog, on
  MS-SQL"* — and the application could not tell them. The projects were on the **file system** (no catalog
  connection string was configured), and answering the question required reading configuration and code.
  ADR-0014 turns that into a rule: *the storage kind is stated, the binding is a deployment fact, and the app
  says where the data is.*

## Delivered (2026-10-08; `Edam.Studio.sln` x64 builds 0 errors; runtime verification is the user's F5)

1. **The Catalog on MS-SQL is configured for the Studio** (`src/Edam.Studio/Edam.Studio/appsettings.json`):
   root-level `Edam:Catalog:Target = mssql`, `Edam:Projects:DefaultCollection = default`, and
   `ConnectionStrings:catalog = Server=.;Database=Edam.Database;Integrated Security=True;Encrypt=False;TrustServerCertificate=True`
   — **Integrated Security**, so no secret exists anywhere (ADR-0010/ADR-0014 decision 2). The `edam_*`
   schema is created idempotently on first use (`MsSqlSchema.EnsureAsync`).
2. **`Edam:Projects:DefaultCollection` now reaches the composition root.** The Studio hands the container id
   to `AddProjectServices` (it previously only *read* it for a UI preference), so the container the store
   uses is the container configuration states — ADR-0014 decision 6.
3. **The resolved storage is stated at startup** (`ProjectServicesHelper.LogStorageSummary`, from the ONE
   place that resolves it): `target=… storage=… collection=… location=…` with the location **redacted** by a
   single rule, written to the diagnostics panel and the log file. A configured engine that contradicts the
   connection string is **reported by name** (`…configured as 'postgres' but the connection string looks like
   MS-SQL…`), instead of failing obscurely — ADR-0014 decisions 3–5. `StorageSummary`, `UsesDatabase` and
   `StorageKind` are exposed for the UI surface.
4. **The container is ensured and made usable** (`EnsureDefaultContainerAsync` + `SeedSharedLocationsAsync`):
   enlistment is idempotent (a binding is never rewritten, ADR-0011); the container's **shared locations**
   (`Templates`, `TextMaps`, `Samples`) are seeded from the app-data seed **only when their probe file is
   missing** (LM-5's `CatalogFolderSeeder`, the same "read a folder, write a location" step the conformance
   suite uses), because a catalog container starts empty and a project created there would otherwise have
   **no recipe at all** — the regression DP-4 exists to prevent. Every outcome is reported.
5. **One content policy**: seeding resolves the container's content store through
   `CatalogScopedContent.Factory` (LM-2b-ii) — a named container gets its scoped store, the sentinel
   `default` is the documented legacy/unscoped namespace — so seeding and the projects layer cannot drift.
6. **The dead key is removed, not activated**: the shipped `Edam:Projects:Process = runner` sat under
   `"AppSettings"`, where the code never reads it, so PE-5d's runner was **silently off**. It was removed
   rather than switched on: enabling an unvalidated execution path is not part of a storage change. The
   opt-in is now root-level `Edam:Projects:Process = runner` when someone wants to validate PE-5d.

## Remaining

- **The inspection surface (the UI half of ADR-0014 decision 8):** a **container info surface** — hover /
  flyout on the collection selector and the projects tree root — rendering the *same* record the diagnostics
  already carry: container · storage kind (`MS-SQL` / `PostgreSQL` / `file system` / `a catalog service`) ·
  **redacted** location · local vs remote · the **source** of the value (configuration · environment · your
  answer) · project count · "could not verify right now" when the store is unreachable. The value record
  should be produced by one headless-testable helper (the platform already has `DescribeStore()`,
  `ProjectBindingInfo` and `ConfigurationItemInfo.Source`); the UI only renders it, because redaction has
  exactly one definition (ADR-0014 decision 5). Also worth carrying: the same summary **at project level**
  ("a branch in container X; its files are content in Y").
- **Migration of what is on disk:** switching the target moves nothing. `IProjectStore.ImportAsync` (upload)
  exists and is exercised only by tests; there is **no import/export UI**. Projects created while the
  file-system target was active must be imported, or re-created in the catalog.
- **Runtime verification (user, F5):** after deploying, the diagnostics panel must show
  `Projects storage: target=catalog storage=mssql collection='default' location=Server=.;Database=Edam.Database;Integrated Security=True;…`
  followed by the container/`seeded N file(s)` lines; the projects panel must list the catalog collection;
  and the next first-run creation must land **in the catalog** — with its starter arguments seeded from the
  container's `/Templates`.
- **Not verified here (honest):** the Studio's catalog path had never been exercised before this change (the
  DP-5/conformance runs drive the platform directly). `Edam.Data.Catalog.slnx` and its conformance suite are
  green, but **this host-side wiring is build-verified only** until the user's F5.
