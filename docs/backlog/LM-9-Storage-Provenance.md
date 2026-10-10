# LM-9 — Project storage is a deployment fact, and it is always stated

- **Status:** **Delivered (2026-10-10)** — the configuration half, the **container question** (the one part a
  person names), the **storage info surface** (the ⓘ button) and the deployment fix are all in place and
  build-verified; the **user's runtime confirmation** and the removal of the test containers already in the
  database remain. Authority: **ADR-0014** (*Accepted*), building on **ADR-0007**, **ADR-0009**, **ADR-0010**,
  **ADR-0011** and **ADR-0013**.
- **Update (2026-10-10):** the container is **asked for** — a registry item
  (`Edam:Projects:DefaultCollection`, offered as `Edam.Projects`, action-backed on
  `Edam:Catalog:ContainerReady`, asked **only** when a catalog is configured via the new `Applies` predicate) —
  and the answer is **used**: `UserConfiguration.Answers` is layered into the composition root, and the
  container is read **answer first, then deployment**. The **ⓘ button** beside the container selector renders
  `DescribeStorageAsync` (kind · **redacted** location · container · **source of the value** · local/remote ·
  what the database holds). The conformance runner now **delists the containers it enlists in a database**, so
  a run leaves nothing behind for a person to meet by accident (**that** is where `pe3-mssql-d3ca8a12` came
  from). `registry (CF-2)` **13 → 17 checks ALL CONFORM**.
- **Why:** a person using the Studio asked *"where is my project data stored? I expect it in the Catalog, on
  MS-SQL"* — and the application could not tell them. The projects were on the **file system** (no catalog
  connection string was configured), and answering the question required reading configuration and code.
  ADR-0014 turns that into a rule: *the storage kind is stated, the binding is a deployment fact, and the app
  says where the data is.*

## The second runtime run (2026-10-10) — the provider was not deployed

`Could not load file or assembly 'Edam.Data.Catalog.MsSql, Version=1.0.0.0' … The system cannot find the file
specified.` The configuration was right; the **provider assembly was not in the deployment**.

**Root cause:** the platform projects the app builds (`Edam.Data.Projects.*`, `Edam.Data.Catalog.*`) were
**ProjectReferences but not members of `Edam.Studio.sln`** — so the solution's build/restore/packaging graph
never owned them. The compile stayed green (types come from the referenced assembly) and nothing revealed the
missing deployment artifact; the failure appears only when the composition root resolves the provider **by
type** at run time. The projects platform had arrived (its assemblies were copied); the **MS-SQL provider** —
added to the graph by LM-8 and first exercised from the Studio by LM-9 — had not.

**Fixed:** the 11 platform projects are now members of `Edam.Studio.sln`; `Edam.WinUI.Controls` carries the
catalog references **explicitly** (not transitively); `Edam.System`/`Edam.Net` stay **out** (they are consumed
as feed packages — as solution members they would be built *and packed* on every Studio build). A new
ProjectReference also requires a **restore** (`NU1105` otherwise), so the recipe is
**Clean → Restore → Rebuild → Deploy**. A missing `Edam.*` assembly is now reported as a **deployment** fault
with the remedy (`ProjectServicesHelper.ExplainFailure`).

**Verified:** `Edam.Studio.sln` (x64) restores and builds 0 errors; **8** `Edam.Data.Catalog*.dll` in the app
output **and** in its `AppX` layout. **Pending:** the user's redeploy, then the container/`seeded` lines.

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

- **The user's runtime confirmation** and the **data cleanup**: the containers the earlier verification runs
  created are still in `Edam.Database` — they can now be removed **from the panel** (the Remove-container button,
  item 119), or in SQL, or by dropping the five `edam_*` tables and letting the schema be recreated.
- **Migration of what is on disk:** switching the target moves nothing. `IProjectStore.ImportAsync` (upload)
  exists and is exercised only by tests; there is **no import/export UI**.
- **Still open (not required now):** a **rename** affordance for a container (removal is delivered), the same
  storage description **per project** ("a branch in container X; its files are content in Y"), and moving the
  bridge's collection-resolution rule (id first, never a silent fallback) into the platform so it is headlessly
  testable — today it lives in WinUI code and only the runtime check covers it.
- **Not verified here (honest):** everything above is **build-verified** plus headless conformance; the Studio's
  catalog path is proven at run time only by the user's F5.
