# Projects Enhancements (PE) — Projects in the Catalog instead of a file system

> **Status:** Started (2026-09-18) — **PE-0…PE-4 complete**; **PE-5 in progress (5a–5c done; 5d: option A implemented — opt-in, build-verified, runtime validation pending)**.
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
| **PE-5** | Consumers + execution — in verified sub-steps: **5a** ✅ runner seam · **5b** ✅ asset-console adapter · **5c** ✅ real process (part 1) + ✅ Studio UI via DI (part 2) · **5d** retire the statics | **5a/5b/5c done 2026-09-18.** **5a:** runner materializes inputs, runs, **captures outputs back** (all targets, CWD unchanged). **5b:** the adapter runs `AssetServiceHelper` with the CWD contained and restored. **5c part 1:** attempting a **real** process found + fixed two defects (arguments file looked up at the working-folder root instead of its **project-relative** path; only `PrepareProceduresRegistry()` instead of the console's full **`Initialize()`** incl. the type registry); the real run still needs real project data (`Asset Data Items expected but not found`), reported as **informational probes**. **5c part 2:** `ProjectServicesHelper` resolves the platform via DI (catalog target when a catalog connection is configured, else file system with today's `AssetConsolePath`); the Studio's **collection list**, **projects tree** and **new-project creation** now come from the platform, and the static `Project.SetProjectsPath` side effect is gone from that path. The **execution sites** stay on the legacy console until the runner is proven with real data. `Edam.WinUI.Controls` builds 0 error and the **Studio app now builds too** — `Edam.Studio.sln` 0 error with correct PRI output (the PRI-off library workaround was incompatible with the MSIX-packaged app; see HANDOFF item 40 / BL-3.1 "Superseded"). **5d partial (2026-09-18):** the three members PE-5c left **unreferenced** were **deleted** (`Project.GetProjectItems` ×2, `Project.SetProjectsPath`) and the superseded-but-live project-lifecycle members (`InitializeProject`, `GetProjectsPath`, `SetProjectsDirectory`, `GotoProject`, `CreateProject`) are marked **`[Obsolete]`** pointing at the platform — the build now lists the remaining usage as `CS0618`. **Full deletion is blocked** — see the corrected finding and the options in **"PE-5d — what actually remains"** below (it needs a design decision, **not** a reader refactor). |

**Evidence:** `Edam.Data.Projects.Conformance` → `result: projects conformance (PE-2…PE-5c) ALL CONFORM` — **9 target groups** (`file-system` with 17 checks, the other eight with 16 each) plus **`asset-console adapter` (6 checks)**, incl. *no process current-directory change*, and three **informational probes** reporting the real console/process outcomes. Pass a PostgreSQL DSN as the first argument to include the postgres targets (the default run is hermetic).

Both providers are registered by `AddProjectServices`; the default target is **file-system**, so switching to the catalog is a configuration change.

## PE-5d retirement audit (2026-09-18) — what was retired and what still blocks deletion

**Deleted — verified to have zero callers** (PE-5c removed the last ones; the compiler confirms by building clean):

| Member | Why it was dead |
|---|---|
| `Project.GetProjectItems()` | the Studio's project-tree read now comes from `ProjectServicesHelper` (PE-5c) |
| `Project.GetProjectItems(string)` | same |
| `Project.SetProjectsPath(string)` | the static path side effect removed from `FetchProjectFolderInfo` (PE-5c) |

**Deprecated in place** — `[Obsolete]` with a message naming the ADR-0009 replacement: `InitializeProject`, `GetProjectsPath`, `SetProjectsDirectory`, `GotoProject`, `CreateProject`. Safe because **no project sets `TreatWarningsAsErrors`**, and useful because the build now **enumerates the remaining usage** as `CS0618` (6 warnings in `Edam.Data.Assets` alone). One live internal use is covered by a file-local `#pragma` — `GetTextMapPath` → `GetProjectsPath`, a text-map folder that is not part of the project model.

**Still blocking full deletion (the honest part) — the legacy pipeline itself owns these:**

| Consumer | Member(s) | Why it cannot move yet |
|---|---|---|
| `Edam.Application` — `AppSettings` | `GetProjectsPath` | core setting resolving the app-data/projects path |
| `Edam.Data.Assets.Services` — `AssetServiceHelper` | `CreateProject` | backs the console's `CreateProject`/`UseProject` procedures |
| `Edam.Data.Assets` — `AssetReportBuilder` | `GotoProject` | report building still changes the process current directory |
| `Edam.Data.Assets` — `AssetConsoleArgumentsInfo` | `GetProjectsPath`, `CreateProject` | arguments parsing derives the project folder |
| Studio `ProjectContext` / `ProjectHelper` | `ProjectConsole.Execute` / `ProcessItem` / `GetArgsContext` | the UI's run/save path — deliberately kept until the runner is proven with real data (PE-5c part 1) |
| `Edam.Test.*` (headless suite) | `GotoProject`, `SetDefaultFullPath`, `SetProjectsDirectory` | project fixtures for the existing tests |

**Correction (2026-09-18) — the "deep refactor" I recorded here is NOT needed.** Re-reading the read path showed that **materialization already solves storage-agnostic input**: `ProjectArgumentRunner` copies a project's inputs into a working folder, the console adapter runs with the CWD contained to that folder, and the produced documents are **captured back** through `IProjectResources`. The readers (`UriResourceInfo`/`FolderFileReader`/`ExcelDocumentReader`) therefore **keep opening physical paths** and never need rewriting onto `IProjectResources` — proven by the conformance runs on the **catalog and remote-HTTP** targets (PE-3/PE-5a/PE-5b). So the asset libraries do **not** need a resource abstraction.

**The real remaining blocker is the UI's execution *semantics*, not its file access** — see "PE-5d — what actually remains" below.

> **Republish caveat:** the Studio and the tests consume `Edam.Data.Assets` / `Edam.Data.Asset.Services` as **feed packages** (`c:\nugetlocalfeed`), so the new `[Obsolete]` guidance only reaches them after those packages are republished (a user step). In-repo project references see it immediately.

## PE-5d — what actually remains (2026-09-18), and the decision it needs

Three `ProjectConsole` call sites remain in the UI (`ProjectHelper.PrepareOutputFile` → `GetArgsContext` + `Execute(item, args, filePath)`; `ProjectHelper.Execute/ProcessItem` → `ProcessItem(item)`; `ProjectContext.PrepareArguments` → `Execute`). They map onto the platform **unevenly**:

| UI need | Platform equivalent | Verdict |
|---|---|---|
| run a project to **produce a document** (with a chosen output) | `IProjectRunner.RunAsync(project, args, outputFile)` | ✅ maps directly |
| read a project's arguments to **inspect/modify them** (`GetArgsContext` → `AssetConsoleArgumentsInfo`, then `Duplicate`, procedure switch, output naming) | the runner parses only the *path-relevant* parts (`ProjectArguments`); it does not hand back the argument model | ⚠️ gap |
| `ProcessItem` → **in-memory `AssetData`** for the editor/viewers | the runner is **file-in/file-out** (`ProjectRunResult` = success + message + artifacts); it returns no asset data | ❌ does not map |

So the migration is **not mechanical**: the UI's flagship flow is "execute and hold the assets in memory", while the runner is deliberately "execute and capture the produced artifacts". Forcing them together either changes the UI's semantics or extends the platform contract — a **design decision**, not a refactor.

**Options:**
- **(A) Derive assets from the produced artifact** — run via `IProjectRunner`, then read the captured document back through `IProjectResources` and parse it with the existing readers. Needs **no contract change**, and it matches ADR-0009 ("the Catalog contains all artifact content" — artifacts are the truth). *Recommended.*
- **(B) Extend the platform** — give `IProjectRunner`/a sibling seam a way to return the parsed `AssetConsoleArgumentsInfo` and/or an asset result (`AssetData`), so the UI keeps its in-memory flow. Cleanest for the UI, but grows the contract beyond ADR-0009's four seams.
- **(C) Leave the UI on `ProjectConsole`** (status quo) — the statics stay; PE-5d stays partial. Acceptable as an explicit choice, but then the file-system project surface is never retired.

**Decision needed before PE-5d can finish.** Whichever way it goes, it also gates deleting the statics, because those three call sites plus the filesystem-mode path helpers (`AppSettings`, `AssetConsoleArgumentsInfo`, `AssetReportBuilder`) are the only remaining consumers.

### Option A — implemented (2026-09-18; build-verified, opt-in, runtime validation pending)

- **Provider mapping (verified).** `FileSystemProjectCatalog.TryResolveResource(physicalPath, …)` resolves a legacy **disk path** back to its project + project-relative resource path — the inverse of `ProjectAddress` plus the on-disk layout. Covered by the conformance runner: **file-system is now 20 checks**, including the round trip, project-folder → project root, and rejection of a path outside the collections.
- **UI bridge.** `ProjectServicesHelper` gains `ProcessRunnerEnabled` (config `Edam:Projects:Process = runner`), `TryResolveProject`, `RunProjectAsync` (→ `IProjectRunner`), `ReadArtifactAsync`, and `TryLoadAssetsFromArtifactAsync` — which feeds the **captured artifact** to the console's own `JsdToAssets`/`XsdToAssets`/`DdlToAssets`. So **no reader is rewritten and no contract is extended**; the produced document becomes the input of record.
- **UI wiring.** `ProjectHelper.ExecuteAsync` = resolve → run → read the artifact → derive the assets; `AssetViewerViewModel.ProcessProjectItem` is now `async void` (the method's own `// TODO: make the following Async...`) and prefers the platform, **falling back to the legacy console on any failure**.
- **Verified:** `Edam.WinUI.Controls` and `Edam.Studio.sln` build **0 error**; the platform conformance is ALL CONFORM; the changed files add **no** new warnings (the solution's warnings are pre-existing + the 5 known `NU1903` SQLite advisories).
- **Not verified:** runtime UI behaviour — WinUI cannot run in this environment, so the flow must be exercised in Studio.
- **Safe by default:** the switch is **off** unless `Edam:Projects:Process = runner`, so today's behaviour is unchanged.
- **Still legacy (honest):** `ProjectHelper.PrepareOutputFile` — it overrides the **procedure** (`AssetsToLexiconDatabase`, `AssetsToLexiconWorkbook`, …), which the runner cannot express because it runs the procedure the arguments declare — and `ProjectContext.PrepareArguments` (the in-memory arguments path). Those two remaining `ProjectConsole` call sites need option **B**'s contract addition (a procedure/argument override on the run request) before they can move.

## Open questions

1. ~~**Where does collection registration live?** Today `UriList` in `Edam.Settings.json`; catalog collections may belong in catalog configuration/DI. Decide in PE-4.~~ — **RESOLVED (2026-09-18, PE-4, recorded here 2026-09-18):** collections come from **configuration/DI** (`Edam:Projects:Collections:<name>` for the file-system target; `Edam:Projects:DefaultCollection` for the catalog) and the platform **never creates collections** — the host enlists them (the conformance runner does this explicitly). That is why `AddProjectServices` reads config rather than settings `UriList`.
2. ~~**Project path convention** within a collection~~ — **RESOLVED (2026-09-18, PE-3):** project paths are **collection-scoped**, `<collectionId>/Projects/<name>`, because catalog item paths are global (two collections collided on `/Projects/<name>`).
3. **`ProjectInfo.VersionId`** — keep as catalog item metadata, or introduce explicit versions? (Only needed when versioning becomes a feature.)
4. ~~**Do we keep a file-system provider as the default** until PE-3 is proven, or switch the default to catalog immediately?~~ — **RESOLVED (2026-09-18, PE-4):** the DI default target is **file-system** (nothing regresses); switching to the catalog is a configuration change.
