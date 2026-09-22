# Projects Enhancements (PE) — Projects in the Catalog instead of a file system

> **Status:** Started (2026-09-18) — **PE-0…PE-4 complete**; **PE-5 in progress (5a done, 5b–5d remaining)**.
> **Goal:** move EDAM **Projects** off the file system and onto the **Catalog** platform, behind interfaces so the storage is replaceable and the surface is easy to consume (CLI, Studio, services).
> **Decision record:** **ADR-0009**. Source specification: *EDAM Studio — Understanding Projects* (2023-02-05).
> **Authority:** per `AGENTS.md`, the live repository is the authority; this document is a planning aid.

## Why

A Project is currently a **folder convention** driven by **static** types that read `AppSettings`, walk directories and **mutate the process current directory**. Relative paths in `*.Args.json` (`./Archive/x.xlsx`, `./Files`, `./Documents`) only resolve because of that CWD. Therefore projects cannot be stored anywhere but local disk, are not thread-safe or headlessly testable, and the spec already anticipates collection URIs "point[ing] to some Web/Cloud resource".

## Model (agreed)

| Concept | Realisation |
|---|---|
| **Collection** (`UriList` entry: Name / Type=1 ConsolePath / UriText) | a **catalog container** (`ContainerBinding{ContainerId, Target, BaseUri}`) |
| **Project** (folder with fixed layout; `Name` + `VersionId`) | a **branch** under the collection |
| `Archive / Arguments / Documents / Files / UseCases / Libraries / Samples / TextMaps` | sub-branches |
| Artifacts (`.xlsx`, `.json`, `.xsd`, `*.Args.json`, …) | catalog **items** + binary content in **`IContentStore`** |
| `./Archive/x.xlsx` in `UriList` | a **`ProjectPath`** resolved against the project — no disk, no CWD |
| Import / Export | **upload / download** a project between a source location and the collection |

## Today's surface — inventory (what must be retired behind the interfaces)

| Type | Layer | Nature | Side effects / coupling |
|---|---|---|---|
| `Edam.Data.AssetProject.Project` | `Edam.Data.Assets` | static class — 11 consts, 4 static fields, 12 static methods | **7× `Directory.SetCurrentDirectory`**, `AppSettings` reads, `Directory.CreateDirectory`, template `File.Copy` |
| `Edam.Data.AssetProject.ProjectInfo` | `Edam.Data.Assets` | data only (`Name`, `VersionId`) | `ProjectVersionId` is threaded through ~40 call sites (DDL/XSD writers, connectors, B2B, inspectors, templates) |
| `ProjectConsole` | `Edam.Data.Assets.Services` | all-static (static ctor + 4 static methods) | **6× `Directory.SetCurrentDirectory`**; static ctor warms `AssetServiceHelper.PrepareProceduresRegistry()` |
| `ProjectContext` | Studio UI | **static class, 16 static members** | global "current project / arguments / asset data / tree view" |
| `ProjectHelper` | Studio UI | static class (9 static methods, 2 consts) | writes into `Documents/`; drives `ProjectConsole` |
| `ProjectDataModel` | Studio UI | static `ToObservable` | builds the UI tree from `FolderFileItemInfo` |
| `ProjectItem` | Studio UI | instance (wraps `ItemBaseInfo`) | path string surgery with `"\\"` |
| `ProjectViewerViewModel` | Studio UI | instance VM | reads `AppSettings.GetUriList(UriType.ConsolePath)` for the collection picker |
| `ApplicationHelper.ProjectInfo` | Studio UI | **static** `ProjectDataModel` | a second global |
| `EdamSettings` / `AppSettings` | `Edam.Application` | static access | `App.UriList`, `App.ConsolePath`, `Edam.Settings.json` |
| `FolderFileReader` / `UriResourceInfo.GetUriList` | `Edam.System` / `Edam.Data.Assets` | static helpers | **file-system enumeration + path resolution** — the hinge the readers depend on |

CWD is also mutated outside the project types: `AssetUseCaseMap` (×2), `AssetReportBuilder`, `AppData`, `FolderFileHelper`, plus tests.

## Seams (the four interfaces — PE-1, done)

`Edam.Data.Projects.Contracts` (net10, **zero dependencies**):

- **`IProjectCatalog`** — collections + projects discovery.
- **`IProjectStore`** — create / **import (upload)** / **export (download)** / delete.
- **`IProjectResources`** — **the hinge**: `List/Exists/OpenRead/Write/CreateFolder/Delete` by `ProjectPath`. Replaces the file-system calls *and* the CWD dance.
- **`IProjectRunner`** — run a project's `*.Args.json` process (as-is, or with an explicit output).

Value records: `ProjectCollectionInfo` (+ `ProjectCollectionType`, `ProjectCollectionFolders`), `ProjectInfo` (+ `ProjectFolders`), `ProjectPath` (+ `ProjectResourceInfo`), `ProjectImportResult`, `ProjectRunResult`.

## Steps

| Step | Scope | Acceptance |
|---|---|---|
| **PE-0** | Scope + inventory + decisions (this doc, ADR-0009) | Documents current; index + handoff updated |
| **PE-1** | `Edam.Data.Projects.Contracts` — value records + 4 interfaces | Builds **0 error**, dependency-free, **zero** existing consumers changed ✅ |
| **PE-2** ✅ | **File-system** `IProjectCatalog`/`IProjectStore`/`IProjectResources` (behaviour-preserving) + a headless conformance runner | **Done 2026-09-18** — `Edam.Data.Projects.Conformance` = **15/15 ALL CONFORM**: the spec's `./Archive/…` / `./Documents/…` paths resolve through the interface, binary content round-trips, import (**upload**) / export (**download**) work, and **the process current directory is never changed**. `IProjectRunner`'s real implementation binds to the asset pipeline in **PE-5** |
| **PE-3** ✅ | **Catalog** implementation: project = branch, folders = branches, artifacts = items + `IContentStore`; collections via `ContainerBinding`; import via `FolderCatalogIndexer`; upload/download | **Done 2026-09-18** — `Edam.Data.Projects.Catalog`; the SAME scenario passes on **five** targets — file-system (14 checks), catalog-local, catalog-remote HTTP, **catalog-postgres local** and **catalog-postgres remote HTTP** (13 each) → **PE-3 conformance ALL CONFORM**, no CWD change. Project paths are **collection-scoped** (`<collectionId>/Projects/<name>`). Surfaced + fixed a wire defect (`CatalogHttpItem.CreateBranchAsync` used the full path as the item `Name`) |
| **PE-4** ✅ | `AddProjectServices(config)` DI composition root | **Done 2026-09-18** — `Edam.Data.Projects.DependencyInjection` registers `IProjectCatalog`/`IProjectStore`/`IProjectResources` for `Edam:Projects:Target = filesystem` (**default**; root falls back to today's `AppSettings:AssetConsolePath`, so nothing regresses) or `catalog` (delegating to `AddCatalogServices`). The runner now also resolves **through DI**: `di (file-system)`, `di (catalog, local file-system)`, `di (catalog, postgres)` — all ALL CONFORM. **Static retirement moved to PE-5** (the statics are still referenced by the Studio UI + pipeline that PE-5 rewires) |
| **PE-5** | Consumers + execution — in verified sub-steps: **5a** ✅ runner seam and `ProjectArgumentRunner` over `IProjectResources` · **5b** the asset-console adapter (`AssetServiceHelper`/`ProjectConsole` behind `IProjectProcess`) · **5c** Studio UI (`ProjectViewerViewModel`/`ProjectContext` on the DI-resolved providers) · **5d** retire the statics | **5a done 2026-09-18** — the runner reads the arguments through the seam, materializes inputs, runs, and **captures outputs back into the project**, verified on **all 8 targets incl. remote HTTP**, CWD unchanged. **5b:** the pipeline executes behind `IProjectProcess` (where the asset readers' by-path behaviour is contained). **5c:** UI consumes the seam. **5d:** no static project state remains |

**Evidence:** `Edam.Data.Projects.Conformance` → `result: projects conformance (PE-2/PE-3/PE-4/PE-5a) ALL CONFORM` — **8 targets**: `file-system` (17 checks), `catalog (local)`, `catalog (remote HTTP)`, `catalog (postgres, local)`, `catalog (postgres, remote HTTP)`, **`di (file-system)`**, **`di (catalog, local file-system)`**, **`di (catalog, postgres)`** (16 each), incl. *no process current-directory change* and the three runner checks. Pass a PostgreSQL DSN as the first argument to include the postgres targets (the default run is hermetic).

Both providers are registered by `AddProjectServices`; the default target is **file-system**, so switching to the catalog is a configuration change.

## Open questions

1. **Where does collection registration live?** Today `UriList` in `Edam.Settings.json`; catalog collections may belong in catalog configuration/DI. Decide in PE-4.
2. ~~**Project path convention** within a collection~~ — **RESOLVED (2026-09-18, PE-3):** project paths are **collection-scoped**, `<collectionId>/Projects/<name>`, because catalog item paths are global (two collections collided on `/Projects/<name>`).
3. **`ProjectInfo.VersionId`** — keep as catalog item metadata, or introduce explicit versions? (Only needed when versioning becomes a feature.)
4. ~~**Do we keep a file-system provider as the default** until PE-3 is proven, or switch the default to catalog immediately?~~ — **RESOLVED (2026-09-18, PE-4):** the DI default target is **file-system** (nothing regresses); switching to the catalog is a configuration change.
