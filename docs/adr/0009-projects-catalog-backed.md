# ADR-0009 — Projects are catalog-backed; the project surface is interface-bound

- **Status:** Accepted — direction (2026-09-18)
- **Context:** An EDAM **Project** is presently a **file-system convention**: a folder containing `Archive/Arguments/Documents/Files/UseCases/…` plus one or more `*.Args.json` process definitions (source: *EDAM Studio — Understanding Projects*, 2023-02-05). It is implemented by **static** types that read `AppSettings`, walk directories and **mutate the process current directory** (`Edam.Data.AssetProject.Project`, `ProjectConsole`, plus `AssetUseCaseMap`/`AssetReportBuilder`/`AppData`), with global UI state in `ProjectContext`. Consequences today: projects cannot be stored anywhere but a local disk, relative paths in `*.Args.json` (`./Archive/x.xlsx`, `./Files`, `./Documents`) only resolve against the CWD, the code is not thread-safe or headlessly testable, and the spec itself notes the collection URI "may eventually point to some Web/Cloud resource".
- **Decision:**
  1. **A Collection is the container; a Project is a branch.** Collections map to catalog containers (the existing `ContainerBinding` already carries `{ContainerId, Target, BaseUri}` — the same shape as an `Edam.Settings.json` `UriList` entry).
  2. **All project artifact content lives in the Catalog** through `IContentStore`, on whatever provider is configured (PostgreSQL, FileSystem, later blob). Project storage must not depend on a file system.
  3. The project surface is **interface-bound** and **dependency-free** in `Edam.Data.Projects.Contracts`: `IProjectCatalog` (discovery), `IProjectStore` (lifecycle + import/export), `IProjectResources` (resource access — the hinge), `IProjectRunner` (processing). Implementations are replaceable and DI-registered.
  4. **`ProjectPath`** (project-relative, provider-agnostic) replaces disk-relative paths; the project surface **never** calls `Directory.SetCurrentDirectory`.
  5. **`*.Args.json` remains the process-definition format** — its shape is a wire contract and does not change.
  6. **Import = upload, Export = download** — move a project between a source location and the collection, mirroring the existing "copy a project folder into the catalog" capability.
- **Options considered:**
  1. **Keep the file system and mirror into the catalog** — rejected: two sources of truth, and the file-system/CWD dependency survives.
  2. **Project = container (rather than branch)** — rejected: a collection already maps to a container; branch-per-project keeps a collection cohesive and lets per-Container provider resolution work at collection granularity.
  3. **Metadata in the catalog, bytes left on disk** (index-only) — rejected: still requires a file system, so a remote/cloud collection could not work, contradicting the spec's own direction.
  4. **One "project service" interface instead of four** — rejected: discovery, lifecycle/transfer, resource access and processing change for different reasons and have different consumers; four narrow seams keep implementations replaceable.
  5. **Wrap the existing statics behind interfaces** — rejected: `Project`/`ProjectContext`/`ProjectConsole` are static and CWD-bound, so wrapping would preserve the hazard the work exists to remove.
- **Consequences:**
  - *Positive:* projects become provider-independent, remote-capable (the Catalog REST API), headlessly testable, and usable by CLI + Studio + services through one seam; the file-system assumption and the CWD hazard disappear.
  - *Cost:* the asset pipeline currently opens inputs **by path** (`UriResourceInfo.GetUriList` → `FolderFileReader` → `ExcelDocumentReader.ReadDocument(fname)`), so those readers must consume `IProjectResources`; the static surface must be retired behind DI; a file-system implementation must be kept during the transition so nothing regresses.
  - MS-SQL/EF remains out of scope (ADR-0007); Azure/blob stays Wave 2 (the `IContentStore` seam is already reserved).
- **Related:** ADR-0006 (interface purity), ADR-0007 (catalog decoupling), ADR-0008 (Contracts value records are the canonical wire/domain contract). Work items: `docs/backlog/Projects-Enhancements.md` (PE-0…PE-5).
