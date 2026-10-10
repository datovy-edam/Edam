# LM-9 — Project storage is a deployment fact, and it is always stated

- **Status:** **In progress (2026-10-08)** — the **configuration half is delivered and build-verified**, and
  the **first runtime run found and closed a real defect** (`mssql` was never a *binding target*, item 114 of
  `docs/HANDOFF.md`); the **inspection surface (the UI half)** is designed, not built. Authority:
  **ADR-0014** (*Accepted*), building on **ADR-0007**, **ADR-0009**, **ADR-0010**, **ADR-0011** and
  **ADR-0013**.
- **Why:** a person using the Studio asked *"where is my project data stored? I expect it in the Catalog, on
  MS-SQL"* — and the application could not tell them. The projects were on the **file system** (no catalog
  connection string was configured), and answering the question required reading configuration and code.
  ADR-0014 turns that into a rule: *the storage kind is stated, the binding is a deployment fact, and the app
  says where the data is.*

## The first runtime run (2026-10-08) — a defective assumption, found and closed

The user deployed and ran it. The log is the whole story, and the **missing** line said as much as the
present ones: there was no container/`seeded` line, and

```
Projects storage: the collection 'default' could not be ensured: Unknown binding target 'mssql' for
collection 'default' — expected filesystem, postgres or service (the PROVIDER family is chosen separately
by Edam:Projects:Target).
```

**`mssql` was a storage *kind* (LM-8) but never a *binding target*:** `ProjectServices.AddCatalog` switches
on the binding target (`filesystem` · `postgres` · `service` · `default: throw`) and MS-SQL was missing, so
`AddProjectServices` **threw while building the provider** and the whole project platform was unreachable.
**Fixed** — the `mssql` case (identical in shape to `postgres`) plus corrected messages, and **two
conformance checks** in the `binding` group (**9 → 11**, both PASS, no database needed because the store is
constructed and never opened):

- *"An `mssql` binding DERIVED from the catalog keys builds the composition root"* → `store=mssql` — the
  Studio's exact shape, i.e. the path that failed;
- *"An `mssql` binding DECLARED by name is honoured exactly like postgres"*.

Three secondary defects from the same log are also fixed: the failed composition root is **reported once and
cached** (it was rebuilt and re-logged on every call — four identical storage lines), the storage line is
announced **once per distinct resolution**, and a storage/listing failure is now a **diagnostic** (it went to
the editor's document title, and the panel area claimed "No projects yet" when the truth was "we could not
look").

**Lesson recorded:** LM-8 was verified against the **catalog** DI and the conformance *groups*, never against
the **projects** DI binding switch — so a kind accepted in one composition root and refused in the other
looked verified. A kind must be checked in **every** composition root that names it.

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
