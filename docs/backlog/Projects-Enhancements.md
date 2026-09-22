# Projects Enhancements (PE) — Projects in the Catalog instead of a file system

> **Status:** Started (2026-09-18) — **PE-0…PE-2 complete**; PE-3…PE-5 planned.
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
| **PE-3** | **Catalog** implementation: project = branch, folders = branches, artifacts = items + `IContentStore`; collections via `ContainerBinding`; import via `FolderCatalogIndexer`; upload/download | Catalog conformance run: import → run → export on **PostgreSQL + FileSystem**, and remotely over the REST API |
| **PE-4** | `AddProjectServices(config)` DI composition root; retire the statics | Consumers resolve interfaces; no static project state; filesystem + catalog providers both registered |
| **PE-5** | Consumers **and `IProjectRunner`**: bind the asset pipeline (`ProjectConsole`/`AssetServiceHelper`, plus the path readers `UriResourceInfo`/`FolderFileReader`/`ExcelDocumentReader`) to `IProjectResources`, then the Studio UI (`ProjectViewerViewModel`/`ProjectContext`) | Pipeline reads/writes project resources through the seam (no CWD); the spec's example `*.Args.json` actually **runs** through `IProjectRunner`; UI browses/edits from the catalog |

**Evidence:** `Edam.Data.Projects.Conformance` → `result: PE-2 conformance ALL CONFORM` (15 checks, incl. *no process current-directory change*).

Filesystem remains a **registered provider** throughout so nothing regresses before PE-3 lands.

## Open questions

1. **Where does collection registration live?** Today `UriList` in `Edam.Settings.json`; catalog collections may belong in catalog configuration/DI. Decide in PE-2.
2. **Project path convention** within a collection — `/<ProjectName>` (assumed) vs `/Projects/<ProjectName>`.
3. **`ProjectInfo.VersionId`** — keep as catalog item metadata, or introduce explicit versions? (Only needed when versioning becomes a feature.)
4. **Do we keep a file-system provider as the default** until PE-3 is proven, or switch the default to catalog immediately?
