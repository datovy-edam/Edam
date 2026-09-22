# EDAM — Solution Review & Handoff Notes

> **Status:** Complete — review of the three solutions under `src/` consolidated.
> **Purpose:** Provide a clear, current map of the repository so work can be handed off at any time.
> **Authority:** Per `AGENTS.md`, the live repository state is the current project authority; this document is a planning/handoff aid and does not replace original source artifacts.

---

## 0. Repository Overview

The repository root contains:

| Path | Purpose |
|---|---|
| `AGENTS.md` | Operational contract for AI agents/contributors (read in full before work). |
| `docs/` | Documentation home (this handoff lives here). |
| `src/` | All application source, organized into three solutions. |
| `tests/` | Top-level tests folder — **currently empty**. |

There is **no `.git` directory** in the repo root (not a git working tree as of this review).

### The three solutions under `src/`

| Solution | Path | Role |
|---|---|---|
| **Edam.Libraries** | `src/Edam.Libraries/Edam.Libraries.sln` | Common code library used by the other solutions. Produces `Edam.*` NuGet packages. |
| **Edam.Data.Catalog.WinUI** | `src/Edam.Data.Catalog.WinUI/Edam.Data.CatalogExplorer.sln` | Documents / artifacts catalog (containers, items, item data). |
| **Edam.Studio** | `src/Edam.Studio/Edam.Studio.sln` | Edam WinUI code libraries + the app entry point ("EDAM Studio"). |

### 0.1 EDAM v2.0 (forward direction)

- **Vision/introduction:** `docs/EDAM-v2.0-Introduction.md` — frames EDAM v2.0 as an enterprise-grade Data Assets Management environment delivered as distributed apps/services, on three pillars: **.NET Aspire** scaffold, **program-to-interfaces + DI** (reusable, pluggable components), and **enterprise-grade = applicable standards & best practices mechanically enforced**. It is **not approved requirements** — no implementation is authorized from it alone.
- **Standards baseline / enterprise guidance:** `docs/EDAM-Engineering-Standards-and-Enterprise-Guidance.md` — the live EDAM engineering-standards & enterprise-guidance document (UI-agnostic, reusable components; SDLC gates; **AI Coding specification/schema-first pillar aligned to the ALETHEIA ISL r3 specs**; enforcement-mapped engineering baseline; applicable-standards matrix; DoD; RACI; practical checklists). Standards are adopted via ADRs. This supersedes the prior `00-SDLC-…` statement, which described a different product (Legislature.TrackingSystem/WaTech) and has been removed.
- **Open decisions (ADR-tracked next):** Aspire version/.NET-SDK compat, web shell Uno-vs-Blazor, persistence model, plugin scope, API/MCP contracts.
- **ADRs:** `docs/adr/0001-monaco-integration-strategy.md` (Area 2); `docs/adr/0002-ai-spec-schema-first-governance.md` (Accepted — adopts the ISL r3 spec/schema-first + readiness/governance model for EDAM engineering & AI coding, applied incrementally); `docs/adr/0003-ai-transferable-context-management.md` (Proposed — layered, lazy-loaded kernel context; see `docs/CONTEXT.md` L0 kernel and `docs/doc-health-checklist.md` anti-drift gate).
- **Tech stack:** `docs/EDAM-v2.0-Tech-Stack.md` (Draft) — current v1 stack (verified) vs v2.0 target (proposed/ADR-tracked); AI modeled behind interfaces (`ICoder`, `IChatAssistant`, `IRetriever`, `IEmbeddingProvider`, `IVectorStore`, `ITextSimilarityService`) instantiated via DI, with Microsoft Agent Framework as a candidate implementation.

### Key architectural fact (critical for handoff)

- **Edam.Libraries** projects are built with `GeneratePackageOnBuild=True` and output NuGet packages to a **local feed at `c:\nugetlocalfeed`** (e.g. `Edam.System`, `Edam.DataObjects`, `Edam.Application`, `Edam.Data.Lexicon`, `Edam.Language.Python`, `Edam.Security`, etc.).
- **Edam.Studio** and **Edam.Data.Catalog.WinUI** consume Edam.Libraries **via NuGet `PackageReference`s** (versions 1.0.0 / 1.0.1), **not** project references.
- There is **no `nuget.config`** in the repo, so package resolution relies on the local feed / global package source being configured on the build machine.
- **Consequence:** to build the apps, the `Edam.*` packages must first be built/published to the local feed. A fresh machine must have `c:\nugetlocalfeed` (or an equivalent source) configured.

---

## 1. Edam.Studio — `src/Edam.Studio/Edam.Studio.sln`

> Review complete.

### 1.1 Overview & Purpose

`Edam.Studio.sln` is the **WinUI 3 desktop application** ("EDAM Studio"). It is the interactive front-end that lets users manage **reference data**, **projects**, **assets**, **activities**, **entities/people**, **notes**, **booklets**, **lexicon/dictionaries**, and **data sources**, backed by the Edam.Libraries domain libraries (consumed as NuGet packages). The app hosts a **Monaco editor** (web) inside a WebView2 control for code editing, and integrates **Python** via pythonnet.

### 1.2 Project Inventory

| Project | Folder | Target Framework | Purpose |
|---|---|---|---|
| **Edam.Studio** | `Edam.Studio\Edam.Studio\` | `net9.0-windows10.0.19041.0` (WinExe, WinUI 3) | App entry point: `App`, `MainWindow`, navigation host, app data init, reference-data templates, resources, strings, MSIX packaging. |
| **Edam.UI** | `UI\Edam.UI\` | `net9.0-windows10.0.19041.0` (WinUI 3) | Small WinUI code library: `ObservableObject`, `Command`, `ObservableRangeCollection`, `StorageFolderFileHelper`. |
| **Edam.UI.DataModel** | `UI\Edam.UI.DataModel\` | `net9.0-windows10.0.19041.0` (WinUI 3) | Data models & view models: Activities, Devices, Entities, Notes, ReferenceData, References, base Models, MenuItem. |
| **Edam.WinUI.Controls** | `UI\Edam.Xaml\Edam.WinUI.Controls\` | `net9.0-windows10.0.19041.0` (WinUI 3) | Reusable WinUI controls + their view models/data models (Accounts, Activities, Assets, Booklets, Dialogs, Editors, Entities, Helpers, Home, Lexicon, Navigation, Notes, Projects, ReferenceData, ReferenceLists, Utilities, Viewers, Web). |
| **Microsoft.Toolkit.Mvvm** | `UI\Edam.Xaml\Microsoft.Toolkit.Mvvm\` | `netstandard2.0;netstandard2.1;net6.0` | Vendored MVVM Toolkit. **Not listed in the .sln** — orphaned/vendored source. |

### 1.3 Dependency Graph

```
Edam.Studio ──(ProjectRef)──▶ Edam.WinUI.Controls
Edam.WinUI.Controls ──(ProjectRef)──▶ Edam.UI.DataModel
Edam.WinUI.Controls ──(ProjectRef)──▶ Edam.UI
Edam.UI.DataModel ──(ProjectRef)──▶ Edam.UI
Edam.UI ──(no project refs)──▶ (uses CommunityToolkit.Mvvm package)
Microsoft.Toolkit.Mvvm ──(standalone, not referenced by project refs)
```

All four projects additionally depend on **Edam.Libraries** via NuGet `PackageReference`s: `Edam.Application`, `Edam.DataObjects`, `Edam.System`, `Edam.Data.Assets`, `Edam.Data.Templates`, `Edam.Data.Lexicon`, `Edam.Data.Schema`, `Edam.Data.AssetDb`, `Edam.Data.AssetMapping`, `Edam.Data.Asset.Services`, `Edam.Data.Dictionary`, `Edam.Json`, `Edam.Xml`, `Edam.B2b`, `Edam.Language.Python`, `Edam.Security`.

### 1.4 Tech Stack & Key Packages

- **WinUI 3 / Windows App SDK** `1.2.230118.102`; `Microsoft.Windows.SDK.BuildTools 10.0.22621.755`; min platform `10.0.17763.0`.
- **.NET 9.0** (`net9.0-windows10.0.19041.0`); app is `WinExe`, MSIX-packaged, x64.
- **MVVM**: `CommunityToolkit.Mvvm 7.1.2` + `CommunityToolkit.Common 7.1.2` (Edam.UI); vendored `Microsoft.Toolkit.Mvvm` (netstandard) present but unused by project refs.
- **Monaco editor** (web) vendored under `Edam.Studio\web\monaco-editor\` (~1000 files), hosted in a **WebView2** control (`CodeEditorControl` / `WebBrowserControl`); JS bridge via `ExecuteScriptAsync`.
- **Python**: `pythonnet 3.0.3` + `Edam.Language.Python` (interpreter registered in DI).
- **Data**: `Microsoft.EntityFrameworkCore.SqlServer 6.0.25`, `System.Data.SqlClient 4.8.6`, `sqlite-net-pcl 1.8.116` (Edam.UI.DataModel).
- **Serialization/processing**: `Newtonsoft.Json 13.0.3`, `Newtonsoft.Json.Schema 4.0.1`, `Jsonata.Net.Native 2.4.1`, `DocumentFormat.OpenXml 2.19.0` (Excel viewer).
- **DI**: `Microsoft.Extensions.DependencyInjection 8.0.0`; config via `Microsoft.Extensions.Configuration(.Json) 8.0.0`.
- **UI toolkit**: `CommunityToolkit.WinUI.UI.Controls 7.1.2`.

### 1.5 Key Namespaces / Classes & Responsibilities

**Edam.Studio (entry point)**
- `App` (`App.xaml.cs`) — `OnLaunched` calls `ApplicationHelper.InitializeApplication()` then creates/activates `MainWindow`.
- `MainWindow` — hosts `NavigationControl` + a status bar; sets title "EDAM Studio"; calls `AppSettings.VerifySetConnectionString()`.
- `Helpers/FileHelper.cs` — file I/O helper.

**Edam.WinUI.Controls.Application**
- `ApplicationHelper` — app bootstrap: initializes AppData folders, session, security vault, DI, loads reference-data templates, resolves connection strings, home control, login/logout.
- `DependencyInjectionHelper` — registers `IFileHelper`, `IReferenceDataTemplateReader`, `IApplicationResource`, plus asset readers, lexicon, Python interpreter, dictionary API.
- `ApplicationCode` — typed cache wrapper; `ApplicationResource` — resource-string loader; `ApplicationLog` — logging; `AccountHelper` — account/login helpers.

**Edam.WinUI.Controls.Navigation**
- `NavigationControl` (UserControl) — the app's main nav shell (NavigationView); implements `IMenu`.
- `MenuController` — builds the main menu (Home, Reference Data, Lists, Projects, Login, Reset, Pin Login) and presents pages in a Frame.

**Edam.WinUI.Controls.Controls.\*** — reusable control library (each with a matching `.xaml` + `.xaml.cs` and often a `ViewModels\*ViewModel`):
- **Accounts**: `AccountLoginControl`, `AccountPinLoginControl`
- **Activities**: `ActivityPeriodRatingGridControl`, `ParticipantRatingControl`, `PeriodControl`, `ProgramControl`
- **Assets**: `AssetViewerControl`, `AssetDataTreeControl`, `AssetDataTreeViewerControl`, `AssetMap*Control`, `AssetSidePanelControl`, `AssetUseCaseGridControl`, `AssetDataElementGridControl`
- **Booklets**: `BookletPanelControl`, `BookletTextCellControl`, `BookletCodeCellControl`, `FramePanelControl`
- **Dialogs**: `DialogBox`, `DialogMessageBox`, `StoragePickerDialog`, `DialogInfo`, `StorageInfo`
- **Editors**: `CodeEditorControl` (Monaco/WebView2)
- **Entities**: `EntityGroupControl`, `EntityFollowUpGridControl`, `EntityFollowUpViewControl`, `ParticipantListViewControl`, `PersonBaseEditorControl`
- **Helpers**: `ControlHelper`, `FormControl`, `RecordStatusControl`
- **Home**: `DashboardControl`
- **Lexicon**: `DictionaryViewerControl`, `TextSimilarityScoreViewerControl`
- **Notes**: `NotesViewEditControl`
- **Projects**: `ProjectViewerControl`, `ProjectSidePanelControl`, `ProjectFileEditorControl`
- **ReferenceData**: `ReferenceDataEditorControl`, `ReferenceDataGridControl`, `ReferenceDataFormControl`, `ReferenceDataFormStackControl`, `ReferenceDataDomain*Control`, `ReferenceDataValidation*`
- **ReferenceLists**: `ReferenceListGroupEditControl`, `ReferenceListViewControl`
- **Utilities**: `DiagnosticsLogControl`, `FolderViewControl`, `KeyPadControl`, `TextButtonControl`, `TextEditorControl`
- **Viewers**: `ExcelViewerControl`
- **Web**: `WebBrowserControl`

**Edam.WinUI.Controls.DataModels** — `ProjectDataModel`, `ProjectContext`, `ProjectHelper`, `DataSourceModel`, `DataDomainModel`, `DataTreeModel`, `DataMapContext/Instance`, `BookModel`, `TextDocumentModel`, `LexiconDataModel`, `DictionariesModel`, `DatePeriodModel`, `ActivityEventModel`, `ActivityPeriodRatingModel`, `SaveOptionInfo`, `ListItemInfo`, `KeyEventData`.

**Edam.WinUI.Controls.ViewModels** — `NavigationViewModel`, `CodeEditorViewModel`, `AssetViewerViewModel`, `AssetDataTreeViewModel`, `AssetMap*ViewModel`, `ProjectViewerViewModel`, `ReferenceData*ViewModel`, `BookViewModel`, `CellViewModel`, `LoginViewModel`, `WebBrowserViewModel`, `TextEditorViewModel`, `FolderViewModel`, `DiagnosticsLogViewModel`, `KeyPadViewModel`, `LexiconSimilarityTextViewModel`, etc.

**Edam.UI.DataModel**
- `Models`: `BaseViewModel`, `BaseDataObject`, `ElementComponentViewModel`, `Item`
- `Activities`: `ActivityProgramModel` (fetches programs/content via `Edam.DataObjects.Services`)
- `Devices`: `DeviceHelper`, `DeviceUserData`, `DeviceUserViewModel`
- `Entities`: `PersonModel`, `PersonViewModel`, `EntityFollowUpViewModel`
- `Notes`: `NoteModel`, `NoteTypeViewModel`, `NoteViewModel`
- `ReferenceData`: `ReferenceDataTemplateFileReader` (reads template JSON from folder)
- `References`: `ReferenceItemViewModel`, `ReferenceListGroupModel`, `ReferenceListGroupViewModel`
- `ViewModels`: `MenuItem` (implements `IMenuItem`/`IMenuProcess`/`IMenuNavigation`), `Separator`, `Header`

**Edam.UI**
- `ObservableObject` (extends `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`; adds `Editing`/`InEditor`/`InSearch`/`InViewer`/`IsAdding` visibility indicators + dispatcher), `Command` (wraps `StandardUICommand`), `ObservableRangeCollection<T>`, `InOut/StorageHelper` (`StorageFolderFileHelper`).

### 1.6 Notable TODOs / Incomplete Areas / Risks

- **21 TODO markers** across the solution, e.g.:
  - `DependencyInjectionHelper` — "Implement it using the interface !!!" (lexicon registered by concrete type).
  - `ApplicationHelper` — license hardcoded, labels not in resources.
  - `DashboardControl` — hardcoded to `ProjectViewControl`.
  - `ActivityPeriodRatingViewModel` — hardcoded option numbers (9=all, 1=ratings).
  - `ProjectHelper` — several "manage results/failed results ASAP" and hardcoded "ReferenceData" name.
- **Empty stub**: `MainWindow.myButton_Click` is an empty handler; `App.xaml.cs` has many unused usings.
- **Commented-out menu items** in `MenuController.GetMainMenu()` (Browse, Dashboard, FollowUp, Person, Group, Report, Administration, Logout) — dead/legacy code.
- **csproj naming mismatch**: `Edam.WinUI.Controls.csproj` `Page Update` entries reference `Controls\Notebooks\...` but the actual files live under `Controls\Booklets\...` — inconsistent and likely a build risk.
- **Excluded controls**: `AssetDataTreeControl.xaml` and `DiagnosticsLogControl.xaml` have `Resource Remove` / `CustomAdditionalCompileInputs Remove` entries (possibly intentionally excluded from build).
- **Machine-specific config**: `appsettings.json` hardcodes `PythonDllPath` (`C:\Users\esobr\...\python312.dll`) and `PythonModulesPath`; connection strings point to local SQL Server (`.\`); `AppInstallerUri` is `c:/temp/edam.studio.pack`.
- **Version skew**: Windows App SDK `1.2` (2023-era) and EF Core `6.0.25` against .NET 9.0; `Microsoft.Toolkit.Mvvm` (netstandard) is vendored but the code actually uses `CommunityToolkit.Mvvm` — the vendored project appears unused/orphaned.
- **Orphaned project**: `Microsoft.Toolkit.Mvvm` is not in the `.sln`; `Edam.UI` has an empty `App\` folder.
- **Legacy namespaces**: code references `Edam.Uwp.ViewModels`, `Edam.Data.AssetManagement`, `Edam.Data.AssetConsole`, `Edam.Data.AssetProject`, `Edam.Data.AssetSchema` — some may not match current Edam.Libraries package namespaces (potential compile risk).

### 1.7 How It Relates to the Other Solutions

- **Edam.Libraries**: consumed **only via NuGet packages** (`Edam.*` v1.0.0/1.0.1) — no project references. Must be built against a published/restored set of those packages.
- **Edam.Data.Catalog.WinUI**: a **separate, independent WinUI app**; not referenced by Edam.Studio. The two solutions are siblings that both depend on Edam.Libraries rather than on each other.

---

## 2. Edam.Libraries — `src/Edam.Libraries/Edam.Libraries.sln`

> Review complete.

### 2.1 Overview & Purpose

`Edam.Libraries` is the shared common-code library for the Edam repository, consumed by the other two solutions. It is a broad, multi-domain toolkit centered on **data asset modeling and schema management**: it models data elements/assets, reads and writes schemas in multiple grammars (XSD/XML, JSON/JSON-LD, GSQL/TigerGraph, DDL/SQL), maps between them, and provides supporting infrastructure (configuration, data access, security, HTTP, EDI, dictionary/lexicon, Python interop, Apache Atlas connector).

The solution root `README.md` is effectively empty (a single heading line) — a documentation gap given the AGENTS.md mandate.

The `.sln` contains **30 projects** in 6 solution folders: **System, Data, Application, Connectors, AI, Tests**. All target `net9.0` and set `GeneratePackageOnBuild=True` + `PackageOutputPath=c:/nugetlocalfeed`.

> **Orphaned projects (on disk, NOT in the .sln):** `Edam.GraphQl`, `Edam.TigerGraph`, `Edam.Data.AssetML` (project name `InXone.Data.AssetML`), and ~8 test projects (`Edam.Test.Apache.Atlas`, `Edam.Test.BookItemProcessor`, `Edam.Test.Ddl`, `Edam.Test.Edi`, `Edam.Test.Json`, `Edam.Test.Json.Console`, `Edam.Tests.JsonLd`, `Edam.Test.OpenXml.Word`). These are not built by the solution.

### 2.2 Project Inventory

**System folder**

| Project | Target | Purpose |
|---|---|---|
| **Edam.System** | net9.0 | Foundation. Root namespace `Edam`. App settings/config, data access (DataConnection/DataProvider), diagnostics/logging, serialization, text helpers, and a large set of `DataObjects` (Activities, Documents, Entities, DataCodes, Devices, Dashboards, etc.). |
| **Edam.Net** | net9.0 | HTTP/web client helpers (`WebApiClient`, `WebHelper`), environment, IP-geo, request info. Depends on Edam.System. |
| **Edam.Security** | net9.0 | Security keys and user security helpers. Depends on Edam.System. |
| **Edam.Help** | net9.0 | Notifications (calendar/recipient) and request processing. Depends on Edam.DataObjects + Edam.System. |

**Data folder**

| Project | Target | Purpose |
|---|---|---|
| **Edam.Data.Assets** | net9.0 | Core data-asset model: `IDataElement`, `ElementBaseInfo`, `QualifiedNameInfo`, `NamespaceInfo`, `DataElement`, `DataDictionary`, `DataDomain`, `DataGroup`, `DataTextMap`, asset schema objects, asset console, asset reports. Depends on Edam.System. |
| **Edam.Data.Templates** | net9.0 | Data templates / dynamic models / reference-data templates / presentation components. Root namespace `Edam.DataObjects`. Depends on Edam.System + Edam.Data.Assets. |
| **Edam.DataObjects** | net9.0 | Higher-level data objects and services (ReferenceData, SelfHelp, Users, Documents, ViewModels, Services). Depends on Edam.Net + Edam.System + Edam.Data.Assets + Edam.Data.Templates. |
| **Edam.Data.Schema** | net9.0 | Database schema reading (SchemaReader), DDL schema objects, import/export, XSD writer. Depends on Edam.System + Edam.Data.Assets + Edam.Data.Templates + Edam.Xml. |
| **Edam.Json** | net9.0 | JSON schema (JSD), JSON-LD, JSON query/merge, JSON instance inspection, Jsonata. Depends on Edam.System + Edam.Data.Assets. |
| **Edam.Xml** | net9.0 | XSD reading, XML crawling/inspection, OpenXML (Excel/Word), EDI dictionary. Depends on Edam.System + Edam.Data.Assets. |
| **Edam.Gsql** | net9.0 | TigerGraph GSQL schema generation (vertices/edges). Depends on Edam.Data.Assets. |
| **Edam.Data.AssetDb** | net9.0 | Schema writers (DDL, GSQL, JSD, XSD) and resource context. Depends on Edam.System + Edam.Data.Assets + Edam.Data.Schema + Edam.DataObjects + Edam.Gsql + Edam.Json + Edam.Xml. |
| **Edam.Data.Dictionary** | net9.0 | EF Core dictionary context (terms/words/queue) + FreeDictionary API. Depends on Edam.Net + Edam.Data.Assets. |
| **Edam.Data.Lexicon** (folder `Edam.Data.Vocabulary`) | net9.0 | EF Core lexicon/vocabulary context, semantics (text similarity), import/export. Depends on Edam.System + Edam.Data.Assets + Edam.Xml. |
| **Edam.Data.AssetMapping** | net9.0 | Map-language info/helper. Depends on Edam.System + Edam.Data.Assets + Edam.Json. |
| **Edam.Data.Asset.Services** | net9.0 | Asset console application, project console, asset service helper. Depends on AssetDb + Assets + Schema + Templates + DataObjects + Json + Xml. |
| **Edam.Api** | net9.0 | Microsoft Data API Builder (DAB) config generation (NJsonSchema-generated DTOs + builder helpers). Depends on Edam.System + Edam.Data.Assets. |
| **Edam.B2b** | net9.0 | EDI (Electronic Data Interchange) document/instance/segment model, exchange service. Depends on Edam.System + Edam.Data.AssetDb + Edam.Data.Assets + Edam.DataObjects + Edam.Xml. |

**Application folder**

| Project | Target | Purpose |
|---|---|---|
| **Edam.Application** | net9.0 | App settings / Edam settings model. Depends on Edam.DataObjects. |

**Connectors folder**

| Project | Target | Purpose |
|---|---|---|
| **Edam.Connector.Atlas** | net9.0 | Apache Atlas HTTP client + entity/type-def helpers. Depends on Edam.Data.Assets + Edam.Net. |

**AI folder**

| Project | Target | Purpose |
|---|---|---|
| **Edam.Language.Python** | net9.0 | Python interpreter interop via pythonnet (run scripts, modules, GIL management). Depends on Edam.System. |

**Tests folder**

| Project | Target | Purpose |
|---|---|---|
| **Edam.Test.Library** | net9.0 | Shared test helper library (references many libs). |
| **Edam.Test.DataApiBuilder** | net9.0 | MSTest for Edam.Api DAB. |
| **Edam.Test.Dictionary** | net9.0 | MSTest for Edam.Data.Dictionary. |
| **Edam.Test.Identity** | net9.0-windows7.0 | MSTest, standalone (no project refs). |
| **Edam.Test.Python** | net9.0 | MSTest for Edam.Language.Python. |
| **Edam.Test.Lexicon** | net9.0 | MSTest for Edam.Data.Lexicon. |
| **Edam.Tests.WebApi** | net9.0 | MSTest, standalone (no project refs). |
| **Edam.Test.Xsd** | net9.0 | MSTest for XSD/asset services. |
| **Edam.Test.AssetReports** | net9.0 | MSTest for asset reports. |

### 2.3 Dependency Graph

```
Edam.System  (foundation, no project refs)
├── Edam.Net
├── Edam.Security
├── Edam.Data.Assets
├── Edam.Language.Python
├── Edam.Api
├── Edam.Data.Dictionary
└── (transitively everything)

Edam.Data.Assets
├── Edam.Data.Templates
├── Edam.Json
├── Edam.Xml
├── Edam.Gsql
├── Edam.Data.Schema
├── Edam.Data.AssetMapping
├── Edam.Data.Dictionary
├── Edam.Data.Lexicon
├── Edam.Connector.Atlas
└── Edam.DataObjects

Edam.Net
├── Edam.DataObjects
├── Edam.Connector.Atlas
└── Edam.Data.Dictionary

Edam.Data.Templates
└── Edam.DataObjects, Edam.Data.Schema, Edam.Data.Asset.Services

Edam.DataObjects
├── Edam.Data.AssetDb
├── Edam.B2b
├── Edam.Application
├── Edam.Help
└── Edam.Data.Asset.Services

Edam.Xml
├── Edam.Data.Schema
├── Edam.Data.AssetDb
├── Edam.Data.Lexicon
├── Edam.B2b
└── Edam.Data.Asset.Services

Edam.Json
├── Edam.Data.AssetDb
├── Edam.Data.AssetMapping
└── Edam.Data.Asset.Services

Edam.Gsql → Edam.Data.AssetDb
Edam.Data.Schema → Edam.Data.AssetDb, Edam.Data.Asset.Services
Edam.Data.AssetDb → Edam.B2b, Edam.Data.Asset.Services
Edam.DataObjects → Edam.Application, Edam.Help, Edam.B2b, Edam.Data.Asset.Services
```

**Layering summary:** `Edam.System` is the root. `Edam.Data.Assets` is the second-level core. `Edam.Data.AssetDb` and `Edam.Data.Asset.Services` are the most-connected aggregation points (schema writers + services). `Edam.Application`, `Edam.B2b`, `Edam.Help` sit on top of DataObjects. Tests reference the library projects directly (project references), not packages.

### 2.4 Tech Stack & Key Packages

- **Target frameworks:** `net9.0` (dominant), `net9.0-windows7.0` (Edam.Test.Identity), `net6.0` (orphaned Edam.GraphQl, Edam.TigerGraph, Edam.Data.AssetML).
- **JSON:** Newtonsoft.Json 13.0.3, Newtonsoft.Json.Schema 3.0.15, json-ld.net 1.0.7, Jsonata.Net.Native 2.4.1 (+ JsonNet/SystemTextJson adapters), YamlDotNet 13.7.1.
- **Data access:** System.Data.SqlClient 4.8.6 (Edam.System), sqlite-net-pcl 1.8.0-beta (Edam.DataObjects), **Microsoft.EntityFrameworkCore 6.0.25 + EF Core SqlServer** (Edam.Data.Dictionary, Edam.Data.Lexicon).
- **Configuration/DI:** Microsoft.Extensions.Configuration 8.0.0, Configuration.Json 8.0.0, DependencyInjection 8.0.0 (Edam.System).
- **Office/XML:** DocumentFormat.OpenXml 2.19.0 (Edam.Xml).
- **Python interop:** pythonnet 3.0.3 (Edam.Language.Python).
- **API generation:** NJsonSchema 10.9.0 + CodeGeneration.CSharp (Edam.Api / Edam.Test.DataApiBuilder).
- **Testing:** MSTest 2.2.x, Microsoft.NET.Test.Sdk 17.x, coverlet.collector.
- **Graph/TigerGraph:** GSQL generation is hand-rolled in Edam.Gsql (no TigerGraph SDK package). Apache Atlas is accessed via a hand-rolled HTTP client (Edam.Connector.Atlas), no Atlas SDK.

### 2.5 Key Namespaces / Classes & Responsibilities

- **Edam.Application** (`Edam.System`): `AppSettings` (JSON config/connection strings), `Session`, `AppData`, `Cache`, `DependencyService`, `RuntimeEnvironment`.
- **Edam.Data** (`Edam.System`): `DataConnection`, `DataProvider`, `DataProviderFactory`, `DataReader`, `DataSourceInfo` — ADO.NET connectivity.
- **Edam.Data.Asset** (`Edam.Data.Assets`): `IDataElement`, `ElementBaseInfo`, `QualifiedNameInfo`, `NamespaceInfo`, `NamespaceList`, `ElementType`, `ConstraintType`, `DataTransformItem`.
- **Edam.Data.AssetManagement** (`Edam.Data.Assets`): `DataElement` (rich EF-style entity implementing `IDataElement`), `DataDictionary`, `DataDomain`, `DataGroup`, `DataTextMap`, `DataTerm`, `DataNote`.
- **Edam.Data.AssetSchema** (`Edam.Data.Assets`): `AssetDataElement`, `AssetDataTree`, `AssetDataMap`, `AssetDataItem`.
- **Edam.Data.Schema** (`Edam.Data.Schema`): `SchemaReader` (reads DB schema via `GetSchema`), `SchemaSet`, `DdlSchema`, `XsdWriter`, import/export.
- **Edam.Json** (`Edam.Json`): `JsonSchemaInfo`, `JsonComplexType`, `JsonQuery`, `JsonMergeProcessor`, `JsonLdHelper`, `JsonInspector`, JSD schema types.
- **Edam.Xml** (`Edam.Xml`): `XsdReader`, `XsdSchema`, `XmlCrawler`, `XmlInspector`, `XmlSchemaInspector`, `ExcelDocument`, `WordDocument`, `XmlEdiDictionary`.
- **Edam.Gsql** (`Edam.Gsql`): `GsqlSchema` (emits `CREATE VERTEX/EDGE` statements).
- **Edam.Data.AssetDb** (`Edam.Data.AssetDb`): `SchemaWriterAbstract<T>`, `DdlWriter`, `GsqlWriter`, `JsdWriter`, `XsdWriter`, `ResourceContext`.
- **Edam.Data.Dictionary** (`Edam.Data.Dictionary`): `DictionaryContext` (EF DbContext, schema `Dictionary`), `TermInfo`, `FreeDictionaryApi`.
- **Edam.Data.Lexicon** (`Edam.Data.Vocabulary`): `LexiconContext` (EF DbContext, schema `Vocabulary`), `LexiconData`, `TextSimilarityInstance`, `Introspector`, `TermCounter`.
- **Edam.Application** (`Edam.Application`): `AppSettings`, `EdamSettings`.
- **Edam.Connector.Atlas** (`Edam.Connector.Atlas`): `AtlasHttpClient` (Put/Upsert), `EntityHelper`, `AtlasHelper`, `Apache.Atlas`.
- **Edam.Language.Python** (`Edam.Language.Python`): `Interpreter` (GIL, module loading, `RunScript`), `PythonHelper`, `Module`, `Parameters`.
- **Edam.Api** (`Edam.Api`): `BuilderLibrary` (NJsonSchema-generated DAB config DTOs), `EntityBuilder`, `BuilderHelper`.
- **Edam.B2b** (`Edam.B2b`): `EdiDocument`, `EdiInstance`, `EdiSegmentInfo`, `EdiFileReader`, `ExchangeService`.

### 2.6 Notable TODOs / Incomplete Areas / Risks

1. **Empty README** — solution root README is a single heading; no architecture/usage docs despite AGENTS.md documentation mandate.
2. **EF Core version mismatch** — `Edam.Data.Dictionary` and `Edam.Data.Lexicon` use **EF Core 6.0.25** while targeting **net9.0**. Likely needs upgrade to EF Core 9.
3. **Orphaned projects not in the .sln** — `Edam.GraphQl`, `Edam.TigerGraph`, `Edam.Data.AssetML`, and ~8 test projects exist on disk but are not built by the solution. `Edam.GraphQl`/`Edam.TigerGraph`/`Edam.Data.AssetML` target net6.0 (inconsistent with the rest).
4. **Stale/broken consumer reference** — `Edam.Data.Catalog.WinUI` references `Edam.Net` (and `Edam.Common`) via direct DLL `HintPath` to `..\..\Edam.Common\Edam.Net\bin\Debug\net9.0\Edam.Net.dll`. The `Edam.Common` folder **does not exist** in the repo, so these references are broken/stale.
5. **Local NuGet feed dependency** — all library projects pack to `c:/nugetlocalfeed`, and Edam.Studio consumes them as NuGet packages. No `nuget.config` exists in the repo, so the local feed must be configured in the user's global NuGet settings; a fresh machine won't resolve these packages.
6. **~83 TODO markers** across the codebase — mostly hardcoded strings/labels, catalog issues, EF-context coupling, and unimplemented features. Notable: `Edam.System\Text\TableBuilder.cs` has 5 `NotImplementedException` throws; `Edam.Connector.Atlas\AtlasHttpClient.cs` has a large commented-out `Search` method (Atlas search not implemented); `Edam.Data.AssetDb\Writers\Jsd\JsdWriter.cs` notes a removed EF dependency.
7. **Mixed code style / legacy** — `Edam.System` uses old-style `String`/`Int16` and has `Device\**` and `Helpers\**` folders excluded from compilation; `Edam.DataObjects` excludes `B2B\**`; `Edam.Help` excludes `Merchants\**` and `UserAccounts\**`. Some namespaces are inconsistent (e.g. `XmlHelper.Xsd` in Edam.Xml, `Edam.Data.AssetManagement.Writers` in Edam.Data.AssetDb).
8. **`Edam.Data.AssetML` project name mismatch** — csproj is `InXone.Data.AssetML.csproj` (legacy "InXone" branding) and is orphaned.
9. **No test framework consistency** — some test projects set `IsTestProject=true` with MSTest; others (`Edam.Test.Identity`, `Edam.Tests.WebApi`) omit it and have no project references (likely stubs).

### 2.7 How It Relates to the Other Solutions

- **Edam.Studio**: Consumes the library via **NuGet package references** to the local feed — e.g. `Edam.System 1.0.0`, `Edam.DataObjects 1.0.1`, `Edam.Data.Assets 1.0.0`, `Edam.Application`, `Edam.Data.Lexicon`, `Edam.Language.Python`, `Edam.Security`, `Edam.B2b`, `Edam.Data.Asset.Services`, `Edam.Data.AssetDb`, `Edam.Data.AssetMapping`, `Edam.Data.Dictionary`, `Edam.Data.Schema`, `Edam.Data.Templates`, `Edam.Json`, `Edam.Xml`. These are spread across `Edam.Studio`, `Edam.UI`, `Edam.UI.DataModel`, and `Edam.WinUI.Controls`.
- **Edam.Data.Catalog.WinUI**: Consumes the library via **direct DLL references** (`<Reference>` + `HintPath`) to `Edam.Net` and `Edam.Common` under a `..\..\Edam.Common\...` path that does not exist in the repo — **broken/stale**. It otherwise uses EF Core SqlServer 9.0.2 and Newtonsoft.Json directly. Its projects do not reference the Edam.Libraries projects by project reference.

**Bottom line:** the library is a large, layered, net9.0 toolkit with `Edam.System`/`Edam.Data.Assets` at the core and `Edam.Data.AssetDb`/`Edam.Data.Asset.Services` as the main aggregation points. It is distributed via a local NuGet feed (`c:\nugetlocalfeed`) and consumed as packages by Edam.Studio, while Edam.Data.Catalog.WinUI's consumption is currently broken (stale DLL path). Most pressing maintenance items: empty README, EF Core 6-on-.NET-9 mismatch, orphaned projects, and the broken WinUI reference.

---

## 3. Edam.Data.Catalog.WinUI — `src/Edam.Data.Catalog.WinUI/Edam.Data.CatalogExplorer.sln`

> Review complete.

### 3.1 Overview & Purpose

This solution implements a **documents/artifacts catalog** modeled on a file system: **Containers** (≈ drives), **Items** (≈ folders/files, branches/leaves of a tree), and **Item Data** (≈ file blobs stored as binary). It provides three interchangeable catalog "clients" behind a common interface set:
1. **Context DB Client** (default) — EF Core repository over SQL Server.
2. **REST API Client** — HTTP access to the catalog service.
3. **File System Client** — direct access to a local folder hierarchy.

It also ships a **WinUI 3 desktop app** (`Edam.CatalogExplorer`) that browses the catalog tree and edits item content in a **Monaco (VS Code) editor** hosted in a WebView2 control.

The codebase is currently in a **prototype/early state** with several incomplete areas (see Risks).

### 3.2 Project Inventory

| Project | Folder | Target Framework | Purpose |
|---|---|---|---|
| **Edam.CatalogExplorer** | `Edam.CatalogExplorer\` | `net9.0-windows10.0.22621.0` (WinUI 3, WinExe) | WinUI 3 desktop app entry point (App, MainWindow). MSIX packaging enabled. |
| **Edam.Data.CatalogModel** | `Edam.Data.CatalogModel\` | `net9.0` | C# domain model + EF entities (Container/Item/ItemData/ContentType), interfaces, tree builder, file-system mapping, clone helpers. |
| **Edam.Data.CatalogDb** | `Edam.Data.CatalogDb\` | `net9.0` | EF Core repository (`CatalogContext`) + local service instance implementations. |
| **Edam.Data.CatalogService** | `Edam.Data.CatalogService\` | `net9.0` (ASP.NET Core Web, `Microsoft.NET.Sdk.Web`) | REST API host (minimal API). Containerized (nanoserver). |
| **Edam.Data.CatalogServiceClient** | `Edam.Data.CatalogServiceClient\` | `net9.0` | REST client (`CatalogClient`), file-system client, and the actual REST endpoint map (`CatalogServiceMap`) + WebApp plumbing. |
| **Edam.UI.CatalogExplorer** | `Edam.UI.CatalogExplorer\` | `net9.0-windows10.0.22621.0` (WinUI 3) | WinUI controls/ViewModels for the explorer UI (tree, container list, editor tabs, Monaco host). |
| **Monaco** | `Monaco\` | `net9.0-windows10.0.22621.0` (WinUI 3) | Vendored copy of the **WinUI.Monaco** editor control (WebView2 + monaco-editor JS). |
| **Edam.Test.TestBuilder** | `Testing\Edam.Test.TestBuilder\` | `net9.0` (MSTest) | Test project (clone + file-system client tests). |
| **Edam.Test.TestCatalogLibrary** | `Testing\Edam.Test.TestCatalogLibrary\` | `net9.0` | Shared test helper library (initializes a `CatalogBuilderServiceInstance`). |
| *(not in solution)* **CommunityToolkit.WinUI.Controls.Sizers** | `CommunityToolkit.WinUI.Controls.Sizers\` | `net9.0-windows10.0.19041.0` (WinUI 3) | Local source copy of the CommunityToolkit Sizers package. **Not referenced by the solution** — the UI project uses the NuGet package instead. |

### 3.3 Dependency Graph

```
Edam.CatalogExplorer (WinUI app)
  └─ Edam.UI.CatalogExplorer
       ├─ Edam.Data.CatalogDb
       ├─ Edam.Data.CatalogModel
       ├─ Edam.Data.CatalogServiceClient
       └─ Monaco

Edam.Data.CatalogService (REST host)
  ├─ Edam.Data.CatalogDb
  ├─ Edam.Data.CatalogModel
  └─ Edam.Data.CatalogServiceClient

Edam.Data.CatalogServiceClient
  ├─ Edam.Data.CatalogDb
  └─ Edam.Data.CatalogModel

Edam.Data.CatalogDb
  └─ Edam.Data.CatalogModel

Edam.Test.TestBuilder
  ├─ Edam.Data.CatalogDb
  ├─ Edam.Data.CatalogModel
  ├─ Edam.Data.CatalogServiceClient
  └─ Edam.Test.TestCatalogLibrary

Edam.Test.TestCatalogLibrary
  ├─ Edam.Data.CatalogDb
  ├─ Edam.Data.CatalogModel
  └─ Edam.Data.CatalogServiceClient

ALL projects → Edam.Libraries (Edam.Common/Edam.System + Edam.Net) via HintPath
```

**Layering note:** `Edam.Data.CatalogServiceClient` depends on `Edam.Data.CatalogDb` (not just the Model), and the REST endpoint map (`CatalogServiceMap`) lives in the **ServiceClient** project while the **Service** host references the ServiceClient. This is an unusual/cyclic-feeling arrangement worth flagging.

### 3.4 Tech Stack & Key Packages

- **.NET 9** throughout; WinUI 3 projects target `net9.0-windows10.0.22621.0` (min `10.0.22621.0`).
- **Windows App SDK** `1.6.250205002` + **Microsoft.Windows.SDK.BuildTools** `10.0.26100.1742` (WinUI 3).
- **EF Core** — `Microsoft.EntityFrameworkCore.SqlServer` `9.0.2` (Model project). DbContext uses `UseSqlServer`, schema `Catalog`, `EnsureCreated()` (no migrations).
- **ASP.NET Core Web API** — `Microsoft.AspNetCore.OpenApi` `9.0.5`; minimal-API endpoint mapping; CORS enabled; containerized with `mcr.microsoft.com/dotnet/aspnet:9.0-nanoserver-1809`, port 8081.
- **CommunityToolkit** — `CommunityToolkit.Mvvm` `8.4.0` (ViewModels), `CommunityToolkit.WinUI.Controls.Sizers` `8.1.240916` (splitters).
- **Monaco Editor** — vendored `WinUI.Monaco` control + `monaco-editor` JS assets (WebView2).
- **Newtonsoft.Json** `13.0.3` (Model), **Microsoft.Extensions.Configuration(.Json)** `9.0.x`, **Microsoft.Extensions.Localization** `9.0.2`, **System.Private.Uri** `4.3.2`.
- **Testing** — MSTest `3.6.4`, `Microsoft.NET.Test.Sdk` `17.12.0`.
- **Edam.Libraries** — referenced as raw DLLs via `<Reference HintPath>` (see Risks).

### 3.5 Key Namespaces / Classes & Responsibilities

**Edam.Data.CatalogModel (`Edam.Data.CatalogModel`)**
- **Entities (EF):** `ContainerInfo` (Table `Container`, unique `ContainerId`), `ItemInfo` (Table `Item`, FK→Container, `FullPath`, `ItemType` Branch/Leaf), `ItemDataInfo` (Table `ItemData`, FK→Item, `byte[] Data` blob + `DataText`), `ContentTypeInfo` (Table `ContentType`).
- **Interfaces:** `ICatalogService` (top-level service), `ICatalogClient` (adds `InitializeClient`), `ICatalogBaseClient` (adds `WebApiClient`, `ResultsLog`, `Cataloger`), `ICatalogContainer`, `ICatalogItem`, `ICatalogItemData`, `ICatalogs` (named-catalog factory), `IItemContent`.
- **Tree/path support:** `CatalogTreeBuilder` (builds a `CatalogItemInfo` tree from paths, registers branches/leaves in a path dictionary), `CatalogPathItem` (wraps `ItemInfo` + path/media/extension parsing), `CatalogItemInfo` (tree node), `CatalogInfo` (root item + path dictionary + default service).
- **File-system mapping:** `CatalogFileSystem` (reads a folder tree into the builder), `CatalogClone` (clone items/data between catalogs).
- **Enums/records:** `ContainerType` (Unknown/DataContext/FileSystem), `CatalogPathItem`, `ItemDataInfo.CreateDataLeaf`.

**Edam.Data.CatalogDb (`Edam.Data.CatalogDb`)**
- `CatalogContext` — EF DbContext (DbSets: ContentTypes, Containers, Items, DataItems; schema `Catalog`; `UseSqlServer`).
- `CatalogContainer` / `CatalogItem` / `CatalogItemData` — EF repository implementations of the container/item/data interfaces (CRUD, root-item creation, branch/leaf creation, enlist/delist containers).
- `CatalogBaseClient` — base client holding `WebApiClient`, `ResultsLog`, URI constants for the REST API, session id.
- `CatalogServiceInstance` — local service instance (singleton `DefaultInstance`), `IDisposable`.
- `CatalogBuilderServiceInstance` — the concrete local EF-backed service; initializes DbContext, seeds content types, creates default container + root item, builds `CatalogInfo`/`CatalogTreeBuilder`.
- `CatalogInstance` — `ICatalogs` factory for the local DB catalog (`edam.catalog.db`).

**Edam.Data.CatalogService (`Edam.Data.CatalogService`)**
- `Program.cs` — minimal-API host. **The actual endpoint mappings are commented out** in Program.cs; it only sets up CORS, exception handling, and instantiates `CatalogServiceMap`. Contains a leftover `WeatherForecastController`/`WeatherForecast` (template boilerplate).

**Edam.Data.CatalogServiceClient (`Edam.Data.CatalogServiceClient`)**
- `CatalogClient` — REST client (`ICatalogClient`) using `WebApiClient` + query-string builders.
- `ClientCatalogContainer` / `ClientCatalogItem` / `ClientCatalogItemData` — REST-backed implementations of the container/item/data interfaces.
- `FolderCatalogClient` — file-system client; maps a local folder into the catalog tree (uses `FolderCatalogItem`/`FolderCatalogItemData`).
- `CatalogServiceMap` — **the actual REST endpoint definitions** (session/info, container CRUD, item CRUD, branch, data, content-type) wired into a `WebApplication`.
- `WebApp\CatalogSystem` + `WebApp\WebAppService` — web-app plumbing; session verification is a stub (`VerifySessionId` always returns true).
- `CatalogInstance` — factory for REST (`edam.base.uri.db`) and file-system (`edam.file.system.catalog`) clients.

**Edam.UI.CatalogExplorer (`Edam.UI.CatalogExplorer`)**
- **App state:** `AppSession` (static window, app state, file/folder pickers), `AppModelState`, `AppConfig` (record), `CatalogServiceHelper` (chooses local vs REST client based on platform).
- **Controls/ViewModels (MVVM, CommunityToolkit):** `CatalogExplorerControl`/`CatalogExplorerViewModel` (tree view), `CatalogContainerControl`/`CatalogContainerViewModel` (container list, add/clone), `CatalogPanelControl` (wires notifications), `CatalogViewModel` (catalog init, post/get item data, client selection by container type), `EditorTabsControl`/`EditorTabsViewModel` (tabbed editor, save-on-close), `MonacoEditorControl`/`MonacoEditorViewModel` (hosts Monaco editor).
- **Models:** `CatalogItemModel` (observable tree node), `ContainerItem`, `CatalogFolder` (uploads a folder into the catalog), `ItemContent`/`ItemContentNotification`/`NotificationEvent` (event plumbing).

**Monaco (`Monaco`)**
- Vendored **WinUI.Monaco** editor: `MonacoEditor` (WebView2 control, `IMonacoEditor`/`IMonacoCore`), `EditorPool` (pre-instantiated editor pool, size 5), `MonacoBaseHandler`/`IMonacoHandler`, `MonacoFileRecognitionHandler` (extension→language map), `MonacoWebViewDevToolsHandler` (stub), `CodeLanguage`, `EditorThemes`, `KeyCode`/`KeyMod`. Ships `monaco-editor\index.html` as content.

### 3.6 Notable TODOs / Incomplete Areas / Risks

1. **Stale/broken Edam.Libraries references (HIGH).** Every project references `Edam.Common.dll` and/or `Edam.Net.dll` via `<Reference HintPath="..\..\Edam.Common\...\bin\Debug\net9.0\...">`. That path resolves to `src\Edam.Common\...`, which **does not exist** in the repo. The actual Edam.Libraries projects are `Edam.System` (RootNamespace `Edam`, assembly `Edam.System.dll`) and `Edam.Net` under `src\Edam.Libraries\System\`. The code consumes namespaces from these (`Edam.Application`, `Edam.Diagnostics`, `Edam.Net.Web`, `Edam.Text`, `Edam.InOut`, `Edam.Serialization`, `Edam.DataObjects.*`). **The build will fail unless these DLLs are present at the stale paths** — the references must be repointed to the real Edam.Libraries projects (or converted to ProjectReferences).
2. **REST endpoints are commented out in `Program.cs`** (a large `/* ... */` block). The live endpoint map is `CatalogServiceMap` in the ServiceClient project, which `Program.cs` instantiates. The commented block is dead/duplicate code that should be removed.
3. **`CatalogServiceMap` lives in the ServiceClient project**, and the ServiceClient depends on `Edam.Data.CatalogDb` (the repository). This inverts the expected layering (a client should not depend on the DB repository) and creates a tight coupling between the REST host, client, and repository. The `CatalogService` project's own README is empty.
4. **`CatalogBuilderServiceInstance.InitializeDbContext` is `async void`** — fire-and-forget initialization from the constructor; exceptions are swallowed (empty `catch`). `EnsureCreated()` is used instead of EF migrations (schema drift risk).
5. **Session handling is a stub.** `WebAppService.VerifySessionId` always returns `true`; `SetupSession` only sets a static id once. No real session lifecycle.
6. **Several methods are stubs / throw:** `ClientCatalogItem.CreateRootItem` and `ClientCatalogItemData.GetContentType` throw "should never be called"; `ClientCatalogContainer.GetContainer(Guid)` throws; `MonacoWebViewDevToolsHandler.OpenDebugWebViewDeveloperTools` is commented out; `CatalogTreeBuilder.GetLeafItems` returns an empty list; `CatalogSystem.GetCatalog` has an empty `else` branch.
7. **`async` anti-patterns:** many sync methods wrap async ones with `.Wait()`/`.Result` (e.g., `CatalogItemData.AddItem`, `ClientCatalog*`), risking deadlocks in UI contexts.
8. **Template boilerplate left in:** `WeatherForecastController`/`WeatherForecast` in the Service project; commented-out code throughout (Program.cs, CatalogTreeBuilder, AppSession, etc.).
9. **`CatalogServiceHelper.GetInstanceAsync`** selects local vs REST by `Environment.OSVersion.Platform == PlatformID.Other` — a fragile heuristic for choosing the client.
10. **Local source copy of CommunityToolkit Sizers** (`CommunityToolkit.WinUI.Controls.Sizers\`) is **not in the solution** and is unused (the UI project uses the NuGet package). It is dead/duplicate source that could be removed.
11. **Monaco is a vendored third-party control** (WinUI.Monaco) with its own `monaco-editor` JS assets; updating it is a manual process (per its README). The `EditorPool` pre-instantiates WebView2 editors, which is resource-heavy.
12. **READMEs are uneven:** solution root, CatalogDb, CatalogModel, and Monaco have content; **CatalogService and CatalogServiceClient READMEs are empty**; the CatalogExplorer `ReadMe.md` duplicates the solution root README.
13. **Hardcoded dev config:** connection string `data source=.; initial catalog=Edam.Catalog; integrated security=true; encrypt=false` and base URI `https://localhost:7069/catalogservice/` appear in multiple appsettings files; `DEVELOPMENT_URI` constant in `CatalogBaseClient`.

### 3.7 How It Relates to the Other Solutions

- **Edam.Libraries**: the common code library. The Catalog solution is **supposed** to depend on it (namespaces `Edam.Application`, `Edam.Diagnostics`, `Edam.Net.Web`, `Edam.Text`, `Edam.InOut`, `Edam.Serialization`, `Edam.DataObjects.*` all come from there), but the dependency is expressed as **stale HintPath references to a non-existent `Edam.Common` project** rather than ProjectReferences to the real `Edam.System`/`Edam.Net` projects. This is the single most important thing to fix for a clean build. The CatalogModel project also pulls in `Microsoft.EntityFrameworkCore.SqlServer` and `Newtonsoft.Json` directly.
- **Edam.Studio**: a **separate** WinUI app solution. It does **not** reference any Catalog solution project. The two WinUI solutions are independent; the Catalog Explorer is a standalone demo/test app, while Edam.Studio is the broader application entry point. The test appsettings reference a `C:/Users/esobr/Documents/Edam.Studio/...` folder, indicating the Catalog file-system client is exercised against Edam.Studio project data.

**Suggested next steps for contributors**
1. Repoint all `<Reference HintPath>` to the real Edam.Libraries projects (`Edam.System`, `Edam.Net`) as ProjectReferences (or a shared NuGet/local-feed package).
2. Delete the commented-out endpoint block in `Program.cs` and keep `CatalogServiceMap` as the single source of REST routes; consider moving it into the Service project.
3. Replace `async void` init and `.Wait()`/`.Result` patterns; add real session management.
4. Remove dead code (WeatherForecast, unused Sizers source copy, empty READMEs) and add EF migrations.
5. Add tests for the REST client and the local repository (only file-system + clone tests exist today).

---

## 4. Handoff Path & Next Steps

> All three solution reviews are consolidated. This section is the working handoff path.

### 4.1 Build prerequisites (critical)

- **Edam.Libraries must be built first** so the `Edam.*` packages land in `c:\nugetlocalfeed` (all library projects set `GeneratePackageOnBuild=True` + `PackageOutputPath=c:/nugetlocalfeed`). **Status: DONE** — the Edam.Libraries solution was built (0 errors) and all `Edam.*` packages are now in the local feed; `dotnet restore` of the Edam.Studio WinUI projects succeeds.
- **Edam.Studio** restores those packages via NuGet `PackageReference`s (versions 1.0.0 / 1.0.1).
- **Edam.Data.Catalog.WinUI** currently references Edam.Libraries via **stale DLL `HintPath`s to a non-existent `src\Edam.Common` folder** — this must be fixed before it can build.
- There is **no `nuget.config`** in the repo, so package resolution relies on the local feed / global package source being configured on the build machine. A fresh machine must have `c:\nugetlocalfeed` (or an equivalent source) configured.
- **Environment limitation:** full WinUI 3 builds require Visual Studio's MSIX packaging tooling (`Microsoft.Build.Packaging.Pri.Tasks.dll`), which is not present in a .NET-SDK-only environment. WinUI projects (and the test project referencing them) must be built on a machine with Visual Studio (Windows App SDK workload).

### 4.2 Cross-cutting risks (highest priority)

1. **Broken Edam.Libraries references in Edam.Data.Catalog.WinUI (HIGH).** All projects referenced `Edam.Common.dll`/`Edam.Net.dll` via `HintPath` to `..\..\Edam.Common\...` which does not exist. **RESOLVED:** repointed all 8 Catalog projects to ProjectReferences to `Edam.System`/`Edam.Net`, reimplemented the missing tree types (`TreeItemType`, `ITreeItem`, `ITreeContainer`, non-generic `TreeItem`) and other missing members in `Edam.System`/`Edam.Net`, and fixed the `Application` namespace conflict in `Edam.CatalogExplorer\App.xaml.cs`. The **full Catalog solution now builds with 0 errors**; Edam.Studio also builds (0 errors).
2. **Local NuGet feed dependency.** No `nuget.config`; the apps depend on `c:\nugetlocalfeed`. Document/configure the source on a fresh machine.
3. **EF Core version skew.** Edam.Libraries uses EF Core 6.0.25 on net9.0 (Dictionary/Lexicon); Catalog uses EF Core 9.0.2. Consider aligning.
4. **Orphaned projects.** Several `.csproj` exist on disk but are not in any `.sln` (Edam.GraphQl, Edam.TigerGraph, Edam.Data.AssetML, ~8 test projects, CommunityToolkit Sizers source copy). Decide whether to include or remove.
5. **Documentation gap.** Solution root READMEs are empty or minimal (Edam.Libraries, CatalogService, CatalogServiceClient). AGENTS.md mandates documentation maintenance.

### 4.3 Open questions to resolve before deep work

- Confirm the local NuGet feed path and whether a `nuget.config` should be added to the repo.
- Confirm whether the repo should be placed under git (currently no `.git`).
- Confirm the intended relationship between Edam.Studio and Edam.Data.Catalog.WinUI (currently independent siblings that both depend on Edam.Libraries).
- Confirm the `Edam.WinUI.Controls.csproj` `Notebooks` vs `Booklets` path mismatch and whether it breaks the build.
- Confirm which Edam.Libraries projects are intentionally excluded from `Edam.Libraries.sln`.

### 4.4 Recommended next work items

> **Active initiative — Wave 1: move EDAM legacy resources into the distributed platform** (`docs/backlog/Wave-1-Migration.md`, Area **BL-6.x**). Platform + back-end + **observability first-class via .NET Aspire** (metrics, diagnostics, environment/services health); **UI is NOT in scope** — **D1 (web-first UI) is PARKED** until after Wave 1. Python offered via CLI/MCP/server-side service (not in-browser). Order: **BL-6.1** Aspire baseline → **BL-6.2** onboard resources → **BL-6.3/BL-6.7** shells + unified MEL/OTel → **BL-6.4** services health → **BL-6.6/BL-6.5** persistence + Python-as-a-service.

> **Next sprint is captured as Sprint S1** in `docs/backlog/Sprint-S1-Next-Sprint.md` (grouped under new backlog **Area E** BL-4.x = enterprise/instrumentation, **Area R** BL-5.x = repo hygiene, **Area W1** BL-6.x = Wave 1). **Sequencing/readiness triage + gate decisions in `docs/backlog/Open-Decisions.md`** (Ready / Parked-by-decision / Idea; D7 Aspire governs Wave 1; D2/D4 AI+vector, D3 persistence, D5 MCP gate the parked clusters; **D1 web-UI parked**). Top of the sprint: BL-3.1 (.NET 10), BL-4.1 (ResultLog→MEL), BL-4.2 (name-collision), BL-4.3 (governance runtime, i1#1), BL-5.1/BL-5.2 (build hygiene).

> **Next sprint — ASAP (BL-3.1, .NET 10) — VERIFIED DONE (2026-09-13):** lib tier + consumers/test all retargeted to `net10.0`; **all 21 `Edam.Libraries` packages republished to `c:\nugetlocalfeed` as net10**. SDK repaired by the user via `dotnet-install.ps1` (10.0.401); `Microsoft.WindowsAppSDK` **1.2→1.6.250205002** (fixed `NETSDK1083`/`win10-*` RIDs) + `Windows.SDK.BuildTools` `.755→.756` (NU1605); WinUI libraries PRI-off + `WindowsPackageType=None`; test project `SelfContained=false` + `CopyLocalLockFileAssemblies=true`; `Common/Colour.cs` stale `using Microsoft.UI.Xaml.Core` removed. **Headless suite VERIFIED GREEN on net10: 28 / 26 pass / 2 skip** (via VS `vstest.console`; `.NET 10` `dotnet test` → `MSB4057` for this WinUI host, MTP transition). Full runner + staging recipe: `docs/backlog/BL-3.1.md`.

Full itemized list (each now a backlog item): BL-3.1; BL-5.1 (HintPath); BL-5.2 (nuget.config); BL-5.3 (dead/duplicate code); BL-5.4 (async-void/.Wait()); BL-5.5 (EF Core align + migrations); BL-5.6 (catalog REST/local-repo tests).

> **Executed in-session (2026-09-10):** **BL-4.1 (MEL bridge) + BL-4.2 (collision)** — additive non-breaking MEL surface implemented + **verified: `Edam.System` builds 0 errors on net10**. `IResultsLog` gained a DIM `Logger` (collision-safe aliasing); `ResultLog` has injectable `Logger`, `ToLogLevel`, `LogMEL`, `Add`→MEL, `UseLogging(ILoggerFactory)`. **Runtime harness `src/Edam.Libraries/Tests/Edam.Test.ResultLogMel/` PASSES** (`MEL bridge forwarded, level=Critical`) and reproduces/resolves the BL-4.2 `ILogger` ambiguity. Remaining: `[LoggerMessage]`, WinUI in-memory provider, `EVENT_LOG_SUPPORT` (see `BL-4.1.md`). **Progress 2026-09-14:** BL-4.1 remainder implemented + compiling 0-error on net10 — precompiled `LoggerMessage.Define` trace, `Edam.WinUI.Controls.Logging.InMemoryLoggerProvider` + `DiagnosticsLogViewModel` rewired off static `LogMessageHandler`, and `Log.Write` `EVENT_LOG_SUPPORT` no-op → MEL forwarding (`Log.UseLogging`). `Edam.System 1.0.0` republished to feed (global cache cleared). **BL-4.1 marked COMPLETE (2026-09-14):** user re-ran the headless suite in a normal shell → `EdamTest-BL41.trx` = **Total 28 / Passed 26 / Failed 0 / 2 not-executed** (the `AddControl` WinUI-host tests), matching the BL-3.1 baseline. Deferred: full app-root `LoggerFactory` wiring at Edam.Studio root (BL-6.1/BL-4.3).
>
> **Environment blockers (2026-09-10/13):** **(a)** BL-3.1's machine SDK defect (`MSB4276`, missing workload-locator folders in .NET SDK 10.0.401) was **RESOLVED** by the user repairing the SDK via `dotnet-install.ps1`; BL-3.1 build + headless suite now **verified green on net10** (see above). Feed-write (`c:\nugetlocalfeed`) was resolved earlier (user "go") — all 21 `Edam.Libraries` net10 packages present. **(b)** BL-6.1 Aspire previously **offline-blocked** (no `dotnet new` Aspire templates / `Aspire.*` packages / reachable `nuget.org`). **2026-09-14:** network restored; user installed `Aspire.ProjectTemplates`; **scaffolded offline** — root `Edam.slnx` + `src\Edam.AppHost` (`Aspire.AppHost.Sdk/13.5.3`, net10) + `src\Edam.ServiceDefaults` + `src\Edam.WebApi` (minimal API → ServiceDefaults), all net10, wired + in `Edam.slnx`. **Remaining (network-bound, user shell):** `dotnet restore/build Edam.slnx`, then run AppHost and verify WebApi resource + OTLP + `/health`/`/alive`. Naming: Wave-1 API = `Edam.WebApi` (avoids existing `Edam.Api` lib). **BL-6.1 COMPLETE (2026-09-14):** user restored/built and ran the AppHost (dashboard + `edam-web-api` resource up); in-sandbox verification after warm cache — `Edam.ServiceDefaults`, `Edam.WebApi`, `Edam.AppHost` each build **0 Error / 0 Warning** on net10. Acceptance met (service under Aspire, OTLP + health emitting, dashboard visible). Deferred: production OTLP collector/dashboards as a separate item.

### 4.5 Current work status (Area 1 — Notebook/Booklets, Area 2 — Monaco editor)

> **Priority next-sprint task added: BL-3.1 (Area P) — .NET 10 runtime upgrade (move off .NET 9).** Next-sprint plan + enterprise-alignment items (Area E: diagnostics→MEL, name-collision, governance runtime i1#1, and the remainder of the i1 stack gaps; Area R: repo hygiene) in `docs/backlog/Sprint-S1-Next-Sprint.md`.

> **Wave-1 execution log (2026-09-15):** BL-6.1 ✅, **BL-4.2 ✅**, **BL-6.2 ✅**, **BL-6.3 ✅** (CLI+API verified live; **MCP shell now builds 0-error** — KernelHost fixed via `PrepareKernelHost(new ProviderConfig())` + `RequestResult.Okey`; **full `Edam.slnx` builds 0-error = 7 projects**), **BL-6.7 ✅**, **BL-6.4 ✅**, **BL-6.6 🟡 store layer** (containers need Docker/BL-4.15), **BL-6.5 ⚠️ PARKED/dropped** (Python removed — functionality via MCP; **MCP/AI pinned, shaped later on MAF** — ADR-0004/0005), **BL-4.3 ✅ capstone**. **→ Full Wave-1 completion/transfer: §5 below.** Toolchain: sandbox `dotnet` fails MSB4276 → **VS MSBuild builds the whole Wave-1 graph offline**. Pending (user-shell): containerized Postgres/blob (Docker/BL-4.15). Each item tracked in its file + this index.


> Tracked in `docs/backlog/` (see `docs/backlog/README.md`). Area 1 is a **verification effort** — the features are implemented; the work is to test them. Area 2 is the Monaco editor enhancement.

**Area 1 — Notebook/Booklets**
- **BL-1.1 (csproj fix): DONE.** Fixed the `Notebooks` vs `Booklets` path mismatch in `Edam.WinUI.Controls.csproj`; no `Notebook` references remain.
- **BL-1.3 (schema-mapping workflow spec): DONE.** `docs/area1-schema-mapping-workflow.md` describes the full workflow (load Source A, load Target B, create booklet, add text/code cells, execute, semantic similarity, save/load) and provides a **traceability matrix** mapping every phase to BL-1.4–BL-1.12 and to concrete `Edam.Test.Studio` test cases — the basis for the remaining verification tests.
- **Edam.Libraries packages: DONE.** Built and published to the local feed; WinUI restore succeeds.
- **BL-1.13 (test project): BUILD BLOCKER RESOLVED — RUNS.** Retargeted `Edam.Test.Studio` to `net9.0-windows10.0.26100.0` (matches the only installed Windows SDK), disabled PRI generation, and pinned `RuntimeIdentifier=win-x64` (the Windows App SDK advertises `win10-*` RIDs that .NET SDK 10 rejects). Exact build command recorded in `BL-1.13.md`. **28 tests: 26 pass, 2 skipped** (the remaining `AddControl` integration tests need a WinUI UI host). Coverage added across BL-1.4/1.5/1.6/1.7/1.8/1.9/1.10/1.11/1.12 — see those backlog items.
- **BL-1.6 / BL-1.7 (cell add): DONE (model-level).** Refactored `BookModel.AddControl` (behavior-preserving) to extract the UI-free cell model into `BookModel.CreateCell`; `BookModel_CreateTextCell_CreatesCellModel` and `BookModel_CreateCodeCell_CreatesCellModel` pass (TextType/Markdown and Code/JSONata).
- **BL-1.8 (code-cell execution): DONE.** Decoupled execution from the UI host: added `BookletCellInfo.OutputText` (models the output) and made `SetOutputText` forward to the control only when `Instance` is present; made `DataMapContext.Execute` fall back to `cell.Text` when `Instance` is null. **`Edam.Data.Assets` bumped 1.0.0 → 1.0.1** and republished to `c:\nugetlocalfeed`; `Edam.WinUI.Controls` + `Edam.UI` package refs updated. Tests pass: `DataMapContext_ExecuteCodeCell_ProducesOutput`, `DataMapContext_ExecuteEmptyCodeCell_IsNoop`, `DataMapContext_ExecuteInvalidCodeCell_DoesNotThrow`.
- **BL-1.11 (persist/save): DONE (model-level).** `AssetUseCaseMap_PersistRoundTrip_PreservesBooklet` verifies `AssetUseCaseMap.ToFile`→`FromFile` round-trip preserves use-case name, booklet, and cell content. App-level `SaveUseCase` path still needs `ProjectContext` setup.
- **BL-1.12 (reorder/delete): DONE.** Reorder decoupled (behavior-preserving) into pure `BookModel.MoveCellDown` + `DataMapContext.MoveCellDown` refresh-on-move; tests `BookModel_MoveCellDown_SwapsAndMoves`/`_LastCell_DoesNotMove` pass. **Delete implemented** (was an empty stub): added `BookModel.DeleteCell` (removes cell from `SelectedBooklet.Items`, clears `SelectedCell` when it pointed at the deleted cell, removes the UI control from `ListView` when present) + wired `BookViewModel.DeleteCell` (explicit cell or selected-cell fallback). Tests: `BookModel_DeleteCell_RemovesFromSelectedBooklet`, `BookModel_DeleteCell_ClearsSelectedCell`, `BookViewModel_DeleteCell_NoArg_UsesSelectedCell`, `BookViewModel_DeleteCell_WithExplicitCell`.
- **BL-1.10 (mapping creation): DONE (model-level).** `DataMapContext_CreateMapping_PairsSourceToTarget` passes.
- **BL-1.4 / BL-1.5 (schema loading): DONE.** Added a fixture loader in `Edam.Test.Studio` that reads the real `Fixtures\SourceSchemaA.json` / `TargetSchemaB.json`, builds `AssetDataMapItem` (Source/TargetElement), and drives the actual `SetMapItemReferences`. Passing: `DataMapContext_LoadSourceFromFixtureFile_PopulatesExpectedElements`, `DataMapContext_LoadTargetFromFixtureFile_PopulatesExpectedElements`, `DataMapContext_LoadFixtureFile_SetsSideAndSemanticDescription` (side tags + `GetAnnotation` → e.g. "Customer Id"). Note the harness reads our simple fixture JSON, not the DB `SchemaReader` (ADO.NET) or XSD/JSON-Schema readers.
- **BL-1.9 (semantic scoring): partial — input pipeline verified, numeric score still not headless.** `DataMapContext_SemanticCompare_ProducesScores` now loads the fixture schemas and verifies the scoring **inputs** (tokenized descriptions, 5×5 pairs). The score computation itself is script-based (`TextSimilarityService` → `ITextSimilarityInstance.ExecuteScript`) and needs a configured text-similarity script + `ProjectContext.Arguments`, so it stays "needs verification" in the running app.
- **Key finding — UI entanglement (mostly resolved):** cell **model** behavior (creation, execution/JSONata, reorder, delete) is now headless-testable, and **schema fixture loading** (BL-1.4/1.5) is verified headlessly. Remaining UI-bound / not headless: `BookModel.AddControl`'s `ListView`/control wiring and the `Booklet*CellControl` instances; the **numeric semantic score** (`LexiconSemanticTextCompare` → script `ExecuteScript`) needs `ProjectContext.Arguments` + the configured lexicon/text-similarity service; target resolution via `SetUpMapping` needs a project/process-argument fixture.

**Area 2 — Monaco editor**
- **BL-2.1 (integration strategy): DONE.** Decision recorded in `docs/adr/0001-monaco-integration-strategy.md` (ADR-0001): two-track — short term keep both integrations but align versions + enhance the hand-rolled editor; long term extract Monaco into a shared WinUI library.
- **BL-2.3 (WebMessage bridge in Edam.Studio): IMPLEMENTED.** Added a `chrome.webview.postMessage('save')` bridge in `code-editor.html` and a `WebMessageReceived` handler in `CodeEditorControl.xaml.cs`. Builds 0 errors.
- **BL-2.4 (Ctrl-S in Edam.Studio): IMPLEMENTED.** Ctrl-S/Cmd-S posts `'save'` → `CodeEditorViewModel.SaveRequested()` → `GetEditorText()` → `AssetSaveTextRequested`. Builds 0 errors.
- **BL-2.2 (Ctrl-S in Catalog's WinUI.Monaco): IMPLEMENTED.** Keybinding in `index.html` posts `EVENT_EDITOR_SAVE_REQUESTED` → `MonacoEditor.EditorSaveRequested` → `EditorTabsControl` → `UpdateModel`. Compiles now that the Catalog builds.
- **BL-2.5 (dirty-state tracking): IMPLEMENTED.** `MonacoEditorViewModel.IsDirty`; `MonacoEditorControl.EditorContentChanged` forwarded; `EditorTabsControl` marks dirty on change, clears on save, shows an orange "●" on the tab header (via new `BoolToVisibilityConverter`), and prompts **Save/Discard/Cancel** on close of a modified tab. Builds 0 errors.
- **BL-2.8 (language detection): IMPLEMENTED.** Already present in `MonacoEditorViewModel.CurrentPathItem` via `FileRecognitionHandler.RecognizeLanguageByFileType` (falls back to `plaintext`). Builds 0 errors.
- **BL-2.9 (theme support): IMPLEMENTED.** `MonacoEditorControl.GetEditorTheme()` maps the app theme to `EditorThemes` and applies it on editor load. Builds 0 errors.
- **BL-2.10 (read-only mode): IMPLEMENTED.** `MonacoEditorViewModel.IsReadOnly`/`ReadOnlyMessage` (set from the file's read-only attribute); `MonacoEditorControl` applies `EditorReadOnly` + `EditorReadOnlyMessage`. Builds 0 errors.
- **BL-2.11 (editor performance/pooling): IMPLEMENTED.** `EditorPool.GetEditorInstance` now creates editors lazily on demand instead of eagerly pre-instantiating 5 WebView2 controls; pool sizing documented. Builds 0 errors.
- **BL-2.12 (editor tests): IMPLEMENTED.** New WinUI MSTest project `Edam.Test.Monaco` (added to the Catalog solution) with 5 tests for `MonacoFileRecognitionHandler.RecognizeLanguageByFileType`. **Found & fixed a real bug:** null/empty input threw `ArgumentNullException`; now falls back to `plaintext`. **5/5 tests pass** via `vstest.console.exe`.
- **BL-2.6 (Monaco update — Edam.Studio): IMPLEMENTED.** Vendored Monaco in `Edam.Studio\web\monaco-editor\` updated **0.33.0 → 0.56.0** (replaced `dev`/`esm`/`min` + metadata; removed obsolete `min-maps`). `code-editor.html` no longer references the removed `editor.main.nls.js`. Edam.Studio builds 0 errors.
- **BL-2.7 (Monaco update — Catalog): IMPLEMENTED.** Vendored Monaco in the Catalog `Monaco` project updated **0.49.0 → 0.56.0** (`libman.json` bumped; `dev`/`esm`/`min` + metadata replaced; `min-maps` removed). `index.html` no longer references the removed `editor.main.nls.js`. Catalog builds 0 errors; updated files confirmed in output.
- **Area 2 status: ALL ITEMS (BL-2.1–BL-2.12) IMPLEMENTED.** Remaining work is runtime verification in the running apps (editor load, Ctrl-S, dirty indicator, theme, read-only, language detection).
- **Catalog build fix: DONE.** Repointed all 8 Catalog projects from stale `Edam.Common`/`Edam.Net` HintPaths to ProjectReferences to `Edam.System`/`Edam.Net`; reimplemented missing tree types and other members in `Edam.System`/`Edam.Net`; fixed the `Application` namespace conflict in `App.xaml.cs`. **Full Catalog solution builds with 0 errors; Edam.Studio also builds (0 errors).** (See §4.2 risk #1.)

### 4.6 Validation report (per AGENTS.md)

- **Completed scope:** Reviewed all three solutions under `src/`; produced this handoff; fixed the Area 1 csproj mismatch; published Edam.Libraries packages; unblocked and ran the Edam.Studio test project; **complete Area 1 verification at the model level** (BL-1.1, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.10, 1.11, 1.12 — see §4.5), incl. **fixture-file schema loading** (Source Schema A / Target Schema B); implemented Ctrl-S → save in both Edam.Studio and the Catalog's Monaco editor; fixed the Catalog solution so it builds.
- **Requirements coverage:** **Area 1** — schema-mapping workflow spec done (BL-1.3); fixture-file source/target loading (BL-1.4/1.5), cell add (BL-1.6/1.7), code-cell execution (BL-1.8), mapping creation (BL-1.10), persist/save (BL-1.11), reorder/delete (BL-1.12) verified with passing tests; BL-1.9 input pipeline verified (numeric scoring still needs the script-based lexicon service + `ProjectContext.Arguments`). **Area 2** — Monaco editor enhanced (Ctrl-S etc.), runtime verification pending in the running apps (see §4.5).
- **Validation evidence:** `Edam.Test.Studio` builds and runs **28 tests → 26 passed / 2 skipped** (skipped = `AddControl` integration tests needing a WinUI UI host) via VS18 MSBuild + `vstest.console.exe` (command in `BL-1.13.md`); Edam.Libraries solution builds (0 errors); `Edam.WinUI.Controls` builds (0 errors); full Catalog solution builds (0 errors); `Edam.Data.Assets` 1.0.1 republished to the local feed.
- **Delivered artifacts:** `docs/HANDOFF.md`, `docs/backlog/` (README + 26 item files), `docs/adr/0001-monaco-integration-strategy.md`, `docs/area1-schema-mapping-workflow.md`, `Edam.Test.Studio` test project (+ `DataMapContextTests.cs` with fixture loader, `BookModelTests.cs`, fixtures `SourceSchemaA.json`/`TargetSchemaB.json`), `BookModel.CreateCell`/`MoveCellDown`/`DeleteCell` + `BookViewModel.DeleteCell`, `BookletCellInfo.OutputText` + `DataMapContext.Execute` decoupling, `Edam.Data.Assets` 1.0.1, Catalog build fix (csproj repointing + `Edam.System`/`Edam.Net` additions).
- **Risks and deferred items:** See §4.2, §4.3, §4.5. Notable: BL-1.9 numeric scoring, `SetUpMapping` target resolution, and `AddControl`/`Booklet*CellControl` need a WinUI test host or `ProjectContext`/script fixtures; Area 2 runtime verification pending; `Edam.Data.Assets` package version bumped (1.0.1) — consumers updated.
- **Validation outcome:** Review complete; **Area 1 verification closed out at the model level** (fixture-file loading included); Area 2 implemented (runtime verification pending in the running apps).
- **AI usage metrics:** Unavailable (not recorded in this session).

---

## 5. Wave 1 (net10/Aspire, observability-first) — completion & transfer

> **Wave-1 migration = distributed platform baseline.** See `docs/backlog/Wave-1-Migration.md` + `docs/backlog/README.md` (index, **Area W1**). All 8 Wave-1/enterprise items below are **implemented** on net10; most are **build-verified (0-error, offline via VS MSBuild) + live-verified in-sandbox**. The remaining gates are **user-shell** (online/Docker/python-native/Aspire host).

### 5.1 Completed scope (each item: its backlog file + acceptance)
| Item | Deliverable | Status / evidence |
|---|---|---|
| BL-4.2 | ILogger name-collision — `Edam.Diagnostics.*` `[Obsolete]`, `Logger.cs` CS0618-suppress | ✅ Complete. `Edam.System` builds 0-error; collision scan in `BL-4.2.md` |
| BL-6.2 | Onboard legacy resources → net10 `Edam.Services.Contracts` (`IWave1Service`, `ICatalogService`, `IBookletMappingService`, `IVocabularyService`, `Wave1ServiceInfo`) + `Edam.Services.Core` (in-memory + `AddWave1Services`). `Wave-1-Inventory.md` classification | ✅ Complete. Contracts+Core build 0-error |
| BL-6.3 | Shells: `Edam.WebApi` (minimal API, `/`, `/wave1`, `AddServiceDefaults`), `Edam.Cli` (`edam wave1`/`health`), `Edam.Mcp.Shell` (stdio MCP over Wave-1 descriptors) | ✅ Complete — **all three shells build 0-error** (full `Edam.slnx`); CLI+API verified live; MCP stdio server compiles (KernelHost fixed) |
| BL-6.7 | Unified MEL/OTel + correlation — `CorrelationIdMiddleware`, Wave-1 services log via `ILogger<T>` | ✅ Verified live: `/wave1` echoes `X-Correlation-ID` + OTel `traceId`; all services log same trace id |
| BL-6.4 | Health first-class — `WaveOneServiceHealthCheck<T>` per service, `/health`(ready), `/alive`(live), `/health/report` heartbeat | ✅ Verified live (all 200) |
| BL-6.6 | Persistence boundary — `ICatalogStore` + `InMemoryCatalogStore` + `PostgresCatalogStore` (Npgsql 10), `/catalog/items`, switchable via config | 🟡 Store layer **verified live** (`store:"in-memory"`); **containers (Postgres/blob) need Docker/BL-4.15** (packages not cached) |
| BL-6.5 | ~~Python-as-a-service~~ — **Parked/dropped (2026-09-15)** | ⚠️ **Parked** — Python removed; functionality managed via MCP (ADR-0004); MCP/AI shaped later on MAF (ADR-0005) |
| BL-4.3 | Governance capstone — `GovernanceEngine` (readiness×risk), `InMemoryApprovalGate`, `InMemoryAuditLog` (SHA-256 chain, `VerifyIntegrity`), `InMemoryConformanceRegistry`; `/governance/decision` + `/audit` | ✅ Implemented + **verified live**: `Draft/Low→Block`, `MachineValid/High→ApprovalRequired`, `AutoReady/Critical→OverrideRequired`; `/audit` `verified:true` |

### 5.2 Requirements coverage
`IWave1Service` descriptor boundary (onboarded surfaces) → WebApi/CLI/MCP shells → unified MEL/OTel + correlation → per-service health → persistence boundary → governance runtime + immutable audit. **(Python scripting dropped; functionality will be managed through MCP — ADR-0004. MCP/AI-agent pinned, shaped later on Microsoft Agent Framework — ADR-0005.)** UI out of Wave 1 (WinUI = reference model; **D1 web-UI parked**). Governance/AI pillars align with ADR-0002 (governance model) + ADR-0003 (anti-drift). **AgentFramework access is interface-only** (`IKernelHost`/`IKernelIO`/`ITextChunker`/`IChunkData`/`RequestResult`), never concrete classes — **ADR-0006.** **Wave 1.1 planned (next increment):** catalog decoupling — UI/EF-independent `Edam.Data.Catalog` platform behind interfaces, surfaced through the Wave-1 boundary (**ADR-0007**; `Wave-1.1-Catalog-Decoupling.md`, Area W1.1 / BL-7.1–7.5). Azure/blob targets deferred to Wave 2.

### 5.3 Validation evidence
- All builds via **VS MSBuild** (`C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`) on net10: **full `Edam.slnx` — 0 error (7 projects: AppHost, ServiceDefaults, WebApi, Services.Contracts, Services.Core, Cli, Mcp.Shell)** (only benign `NU1900` offline vuln-audit warnings). Sandbox `dotnet` itself is broken (SDK defect **MSB4276**, missing workload-locator Sdk dirs); VS MSBuild works.
- **One consolidated live WebApi run** → all 6 endpoints 200: `/`, `/wave1`, `/health/report`, `/catalog/items`, `/governance/decision`, `/audit`.
- CLI live: `edam wave1`, `edam health`.
- Headless suite (Wave-1 tests): not yet added/run — pending a small `Edam.Test.Wave1` (would need testhost = user-shell). Existing suite stays green (BL-3.1/BL-4.1 baselines: 28 / 26 pass / 2 skip).

### 5.4 Deliverables
`Edam.slnx` (7 projects) + `src\Edam.AppHost`, `Edam.ServiceDefaults`, `Edam.WebApi`, `Edam.Services.{Contracts,Core}`, `Edam.Cli`, `Edam.Mcp.Shell`; code under `src\Edam.Services\` + `src\Edam.WebApi\`; docs `Wave-1-Inventory.md`, `BL-6.x.md`/`BL-4.2.md`/`BL-4.3.md`, `README.md` index updated, this handoff.

### 5.5 Risks / deferred (user-shell next-session gates)
1. **Tech-stack alignment — MAF, not SemanticKernel (2026-09-15).** SK is pulled **only transitively** via feed packages: `Edam.AgentFramework.Core 1.0.1` (→ `Microsoft.SemanticKernel.Connectors.Qdrant 1.74.0-preview`, alongside MAF `Microsoft.Agents.AI 1.19.0` + `Microsoft.Extensions.VectorData.Abstractions`) and `Edam.Mcp 1.0.0` (→ `Edam.Format.SemanticKernel 1.0.1`). **No direct SK usage in Wave-1 `src/`** (only `Mcp.Shell.csproj` → `Edam.Mcp 1.0.0`). **Fix = feed/source owner republishes on Microsoft Agent Framework (MAF) only**: drop the Qdrant SK connector (AgentFramework already references the `Microsoft.Extensions.VectorData` abstraction) and remove/replace `Edam.Format.SemanticKernel`. Until then `NU1904` (SK.Core 1.68.0 critical, GHSA-2ww3-72rp-wpp4) remains a build warning. **Source is in-house:** sibling solution `C:\Users\esobr\source\repos\Edam.AgentFramework\Edam.AgentFramework.slnx` (republish on MAF from there; see ADR-0005).
2. **BL-6.6 containers** — `AddPostgres`/blob resources + `Aspire.Hosting.PostgreSQL`/blob packages + Docker round-trip (BL-4.15 prerequisite).
3. **~~BL-6.5 pythonnet~~ RESOLVED/PARKED (2026-09-15)** — Python removed; needed functionality managed through MCP instead (ADR-0004). No native Python runtime in Wave-1 images.
4. **~~Aspire host run~~ DONE (2026-09-15)** — user ran `dotnet run --project src\Edam.AppHost`; all good (WebApi resource + health/OTel up).
5. **Headless Wave-1 test project** (`Edam.Test.Wave1`) — testhost needs user-shell; add deterministic tests for governance/audit/store.
6. OpenAPI (`AddEndpointsApiExplorer`/Swagger) — follow-up; no Swashbuckle package offline.
7. **AgentFramework interface boundary (ADR-0006)** — before MCP/AI resumes, refactor pinned `Edam.Mcp.Shell` (and feed `Edam.Mcp`) to bind to `IKernelHost`/interfaces, add a store interface (drop the SK `QdrantVectorStore` concrete), and republish on MAF. Until then ADR-0006 is the governing mandate.
8. **Wave 1.1 — catalog decoupling (BL-7.1–7.5, ADR-0007)** — DB-independent `Edam.Data.Catalog` (PostgreSQL/Npgsql hidden behind DI; MS-SQL/EF deferred) surfacing the real catalog behind the Wave-1 `ICatalogService`/`ICatalogStore`. **De-risked order (relocate→prove→extract→abstract):** **1 Relocate** (BL-7.1, move real projects as-is) → **2 Prove** (BL-7.5, working FileSystem slice end-to-end) → **3 Extract** (BL-7.3, derive contracts from the relocated types) → **4 Abstract** (BL-7.2, EF-independence + PostgreSQL) → **5 Seam** (BL-7.4, DI + per-**Container** resolution via `ContainerBinding`/`ICatalogProviderResolver`). Azure/blob → Wave 2. **Repo baseline (2026-09-15):** a **private GitHub repo for `Edam.slnx`** is being set up **before** BL-7.1's relocation; internal packages restore from `c:\nugetlocalfeed` (not published/vendored — a clone/CI needs the feed; BL-4.15), and `.gitignore`/secret guard must be confirmed before first push. **Progress:** `Edam.Data.Catalog.Contracts` scaffolded (`src\Edam.Libraries\Data\Edam.Data.Catalog\...`, net10, 0-error) with `ContainerType`/`IContentStore`/`ContainerBinding`/`ICatalogProviderResolver` — currently a **draft to validate/discard in BL-7.3**, NOT built ahead of BL-7.1/7.5. Purity mandate (depend on contracts, no provider/infra leakage) codified in ADR-0006; provider-conformance gating in BL-7.2/7.4. **Step 1 BL-7.1 (2026-09-15):** the 4 catalog projects moved **as-is** to `src\Edam.Data.Catalog\` (model/db/service/serviceclient), consumers + `Edam.Data.CatalogExplorer.sln` repointed, standalone `Edam.Data.Catalog.slnx` added, and all refs validated to resolve. **Pending (user online shell, not offline-cacheable):** compile the catalog graph + repointed WinUI sln (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.OpenApi`, `System.Private.Uri` not in sandbox cache) — then commit the baseline.
10. **Wave 1.1 step 1 + Monaco verified (2026-09-15):** the relocated catalog graph (`src\Edam.Data.Catalog\`), the repointed WinUI consumers, and `Edam.Monaco` all **compiled clean on the user's online build**; commits pushed to `datovy-edam/Edam` (HEAD `5efaeae`; the two local commits `55e7e37` + `5efaeae`). Next in ordering: **Step 2 = BL-7.5** (FileSystem-first working slice through the Wave-1 boundary).
11. **Wave 1.1 step 2 = BL-7.5 verified (2026-09-15):** a real FileSystem catalog store (`src\Edam.Services\Edam.Services.Core\FileSystemCatalogStore.cs`, `ICatalogStore`) surfaces actual file-system catalog assets through the Wave-1 boundary, selected by DI/config (`Edam:CatalogRoot`/`DefaultRootFileFolder`/`Edam:CatalogStore=filesystem`; in-memory is now only the registered fallback; WebApi `/catalog/items`, CLI `edam wave1`, and the `Wave1ServiceInfo`/health boundaries were **unchanged**). Verified live: `/catalog/items` returned `Store: filesystem` with 50 real recursive assets. **Scoping note:** the store is self-contained `System.IO` for now — pointing it at the relocated catalog's FileSystem/PostgreSQL provider behind DI (seam swap) is the BL-7.2 (EF-independence) / BL-7.4 (per-**Container** resolution) work. Next in ordering: **Step 3 = BL-7.3 (Extract)** — derive the contracts from real relocated types.
12. **Wave 1.1 step 3 = BL-7.3 Extract verified (2026-09-15):** created `src\Edam.Data.Catalog\Edam.Data.Catalog.Contracts\` (net10, no-EF, dependency-light; added to `Edam.Data.Catalog.slnx`; old `Edam.Libraries` draft home removed). It holds the **validated seams** (`ContainerType` extended `DataContext=1/FileSystem=2/PostgreSql=3/Service=4`, `ContainerBinding`, `ICatalogProviderResolver`, `IContentStore`) plus the **derived** interface + value surface from the relocated real types (`ContainerInfo`/`CatalogInfo`/`ItemInfo`/`ItemDataInfo`/`ContentTypeInfo` value records, `ItemType`, and `ICatalogService`/`ICatalogClient`/`ICatalogContainer`/`ICatalogItem`/`ICatalogItemData`/`ICatalogs`/`IItemContent`). **Verified offline: builds net10 0-error (no EF, no packages).** **Decision (ADR-0006):** a *derived* dependency-light platform, not a literal move — the relocated value POCOs are EF-annotated and live in EF-coupled `Edam.Data.CatalogModel`, so migrating targets onto this platform (implement `ICatalog*`, `AddCatalogServices()`) is **BL-7.2 (EF-independence) + BL-7.4** (per-**Container** resolution). Next in ordering: **Step 4 = BL-7.2 (Abstract)**.
13. **Wave 1.1 step 4 = BL-7.2 In Progress (2026-09-15):** added the catalog **`ICatalogStore`** metadata seam to `Edam.Data.Catalog.Contracts` (aggregates `ICatalogContainer`/`ICatalogItem`/`ICatalogItemData` + `DescribeStore()`), completing the `ICatalogStore`/`IContentStore` seam pair BL-7.2 needs; **verified offline** (Contracts net10 0-error). **Gate:** the whole catalog graph depends on `Edam.Data.CatalogModel` (uncached EF SqlServer package) → nothing in the catalog builds offline until EF is removed, and removing EF requires rewriting `CatalogDb`'s EF data layer to **Npgsql** (runtime-verified against a live Postgres = **user shell**). Remaining: Model EF removal + `CatalogDb`→Npgsql, `CatalogServiceClient` decouple (drop `CatalogDb` + `Microsoft.AspNetCore.OpenApi`), DI `AddCatalogServices()`, and the provider-conformance suite (ADR-0006; testhost = user shell).
14. **Wave 1.1 step 4 = BL-7.2 additive provider + conformance — verified offline (2026-09-15):** new `Edam.Data.Catalog.PostgreSql` provider (`PostgreSqlCatalogStore : ICatalogStore`, `PostgreSqlContentStore : IContentStore`, idempotent schema, `AddCatalogServices()` DI) references **only `Contracts` + Npgsql — no Model/EF**. New `Edam.Data.Catalog.Conformance` harness runs the **same provider-agnostic scenario** (ADR-0006) against any `ICatalogStore`/`IContentStore`; **built + run offline against the in-memory reference provider: ALL CONFORM (exit 0)**. Both added to `Edam.Data.Catalog.slnx`. Run against live Postgres: `Edam.Data.Catalog.Conformance postgres "<connection>"` (user shell). **Remaining BL-7.2:** `CatalogDb`→Npgsql + Model EF removal (now unblocked by the proven Npgsql path), `CatalogServiceClient` decouple (drop `CatalogDb` + `Microsoft.AspNetCore.OpenApi`), DI into a consumer host, FileSystem conformance fixture, live-Postgres run.
15. **Wave 1.1 PostgreSQL container provisioned (2026-09-15).** Root `compose.yaml` defines `edam-postgres` (`postgres:17-alpine`), persistent volume `edam_pgdata`, healthcheck, `localhost:5432`. **Npgsql connection string: `Server=localhost;Port=5432;Database=edam;User Id=edam;Password=edam`.** The agent sandbox **cannot** reach the Docker engine pipe (`Access is denied`, same boundary as `git push`) — a human must run **`docker compose up -d --pull always`** from the repo root, then verify `docker ps` shows `edam-postgres` healthy. **Deferred until human starts the container:** the live-Postgres conformance run and any runtime validation of the Npgsql-backed catalog path.
16. **Wave 1.1 BL-7.2 retirement — sub-step 1 (2026-09-15):** relocated `CatalogBaseClient` out of `Edam.Data.CatalogDb` into `Edam.Data.CatalogModel` (pure client base — WebApiClient + catalog model contracts, no EF/DB dependency). All 10 referencing files (CatalogDb `CatalogServiceInstance`/`CatalogContainer`/`CatalogItem`/`CatalogItemData`; ServiceClient clients; `Edam.UI.CatalogExplorer\CatalogServiceHelper.cs`) already import `Edam.Data.CatalogModel`, so the move is un-ambiguous. **Pending sub-steps (retire EF CatalogDb, staged):** (2) strip EF from Model (drop EFSqlServer package + `[Index]` + EF usings); (3) rewire active catalog path onto Npgsql `ICatalogStore`; (4) drop `CatalogDb` from forced project references; (5) update WinUI/Testing consumers (user online build); then final offline build + live-Postgres conformance. **Not yet offline-buildable until sub-step 2 removes Model's EF package.** -> **SUB-STEP 2 DONE + VERIFIED (2026-09-15):** removed EF package + `[Index]` + EF usings from `Edam.Data.CatalogModel` — **Model builds offline 0-error, catalog core EF-independent**. `CatalogDb` carried a temporary EF bridge (EFSqlServer 9.0.2) to keep the graph compiling; remove it when `CatalogDb` is retired. **Remaining: sub-steps 3-5** (rewire active catalog path onto Npgsql, drop CatalogDb refs, update WinUI/Testing — the unverifiable-offline bulk). -> **SUB-STEP 3a done (2026-09-15):** wired Npgsql provider into `Edam.Data.CatalogService\Program.cs` via `AddCatalogServices(builder.Configuration)`; `appsettings.json` gains `ConnectionStrings:catalog` (Postgres DSN). **WIRE-CONTRACT GATE for 3b/4:** service endpoints return Model's rich EF entities while `ICatalogStore` returns Contracts' lean value records, so the endpoint rewire changes the JSON wire contract (client `ContainerInfo`/`ItemInfo` deserialization) + there are two `ContainerType` enums (Model vs Contracts). The rewire is NOT mechanical and is not done blind — it needs compile (user online build) + runtime (live Postgres) verification and a wire-contract reconciliation decision. **DECISION LANDED (ADR-0008, 2026-09-15):** **migrate the service clients/UI onto the `Contracts` value-record shapes** as the single canonical wire/domain contract. Plan: service endpoints emit Contracts records via the Npgsql `ICatalogStore`; `CatalogClient`/`ClientCatalog*`/`FolderCatalogClient`/WinUI deserialize/bind Contracts records; retire `CatalogModel` EF entity set + `CatalogDb`. **Gated:** needs compile (which the agent CAN do — it has internet/NuGet and can build the catalog graph online) + **runtime** live Postgres (the user starts `docker compose up`; the agent cannot reach the Docker engine pipe — `Access is denied`). NOT done blind: the agent compile-verifies each sub-step. Remaining: **3b** (endpoints emit Contracts records), **4** (drop `CatalogDb` refs incl. `Microsoft.AspNetCore.OpenApi`; remove `CatalogDb`), **5** (migrate WinUI/Testing). **CORRECTION:** an earlier note claimed the agent has no internet — **wrong**. NuGet/internet works (full `Edam.Data.Catalog.slnx` restored+build online, exit 0). Only the Docker engine **pipe** is a boundary (`Access is denied`). Builds = agent; Docker/Postgres runtime = user.
- **SUB-STEP 3b-1 DONE + VERIFIED (2026-09-15, online build exit 0):** rewrote `Edam.Data.CatalogServiceClient\CatalogServiceMap.cs` so every catalog endpoint backs onto the Npgsql `ICatalogStore` and **emits `Contracts` value records** (routes unchanged, `Program.cs` injects the store, ServiceClient gained a `Contracts` ProjectReference). Service no longer serves catalog data from EF `CatalogDb`. **3b-2 pending** -> **SUB-STEP 3b-2 DONE + VERIFIED (2026-09-15, offline build exit 0):** added the **Contracts-native REST client group** to `Edam.Data.CatalogServiceClient` — `CatalogHttpClient` (`Contracts.ICatalogClient`) + `CatalogHttpContainer`/`CatalogHttpItem`/`CatalogHttpItemData` (the `Contracts.ICatalogContainer`/`ICatalogItem`/`ICatalogItemData` surfaces), which deserialize the service's **`Contracts` value records** directly (no Model-EF, no CatalogDb) and mirror the routes in `CatalogServiceMap`. These are the transition clients for step 5 (WinUI/Testing). **Full `Edam.Data.Catalog.slnx` builds `0 error`** (offline, `GeneratePackageOnBuild=false`, VS MSBuild — the sandbox can't write `c:\nugetlocalfeed`). **also (verified online, exit 0):** dropped `Microsoft.AspNetCore.OpenApi` from `CatalogServiceClient` (unused; replaced by `FrameworkReference Include="Microsoft.AspNetCore.App"` so the hosted map still gets `WebApplication`). `CatalogServiceClient` still references `CatalogDb` via `FolderCatalogClient`/`CatalogInstance` — removal pending step 4 (the last Model/CatalogDb ties: `FolderCatalogClient.GetClientAsync` uses `CatalogServiceInstance.DefaultInstance`).
9. **WinUI components consolidation (2026-09-15)** — the **Monaco** WinUI control was canonicalized to a single reusable home `src\Edam.Monaco\` (owning one embedded `monaco-editor` bundle; consumers `Edam.UI.CatalogExplorer` + `Edam.Test.Monaco` repointed; standalone `Edam.Monaco.slnx`). **Note:** `Edam.Studio\Edam.Studio\web\monaco-editor` is still a separate tracked copy (~93 MB) used by Studio's `CodeEditorControl` — unify onto `src\Edam.Monaco` when we return to WinUI. Also **deleted** the orphan local `CommunityToolkit.WinUI.Controls.Sizers` project (UI uses the NuGet package `8.1.240916`).
10. **Wave 1.1 BL-7.2 sub-step 3b-2 — Contracts-native REST client done + verified (2026-09-15, offline build **0 error**).** Added `CatalogHttpClient` / `CatalogHttpContainer` / `CatalogHttpItem` / `CatalogHttpItemData` in `src\Edam.Data.Catalog\Edam.Data.CatalogServiceClient\` implementing the `Edam.Data.Catalog.Contracts` client interfaces and deserializing the `Contracts` value records (ADR-0008) — no Model-EF, no CatalogDb. Verification loop established: `dotnet build` is unusable here (SDK defect MSB4276), so builds use **VS MSBuild** (`C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`) with `/t:Build /p:Configuration=Debug /p:GeneratePackageOnBuild=false` (the library projects pack to `c:\nugetlocalfeed` on build, which the sandbox can't write). **Next: BL-7.2 step 4** (retire the Model HTTP path + drop `CatalogDb` from `CatalogServiceClient`) then **step 5** (WinUI/Testing rebind to `CatalogHttpClient`).
11. **Wave 1.1 BL-7.2 step 4 — CatalogDb dropped from ServiceClient, done + verified (2026-09-15, offline `Edam.Data.Catalog.slnx` **0 error**; WinUI `Edam.UI.CatalogExplorer` library also **0 error**).** Added the **pure-Model in-memory catalog service** (`Edam.Data.CatalogModel.ModelCatalogService` + `ModelCatalogContainer`/`ModelCatalogItem`/`ModelCatalogItemData`, EF-free) and pointed `FolderCatalogClient` at `ModelCatalogService.Default` (replacing `Edam.Data.CatalogDb.CatalogServiceInstance.DefaultInstance`); removed every `using Edam.Data.CatalogDb;`/EF using in `Edam.Data.CatalogServiceClient` (incl. `WebApp/CatalogSystem.cs`) and dropped the `Edam.Data.CatalogDb` ProjectReference from the ServiceClient csproj. **Now meets BL-7.2's "CatalogServiceClient has no CatalogDb project reference" acceptance criterion.** Committed locally (pending push). **Remaining: BL-7.2 step 5** — migrate the WinUI/Testing consumers (`Edam.UI.CatalogExplorer\CatalogServiceHelper.cs`/`CatalogViewModel.cs`, `Edam.Test.TestCatalogLibrary\AppHelper.cs`, `Edam.Test.TestBuilder`) onto the Contracts platform (`CatalogHttpClient` / `ModelCatalogService` / `ICatalogStore` seams) and drop their direct `CatalogDb` ProjectReferences.
12. **Wave 1.1 BL-7.2 step 5 — Testing consumers migrated off CatalogDb, done + verified (2026-09-15, offline `Edam.Test.TestBuilder` **0 error**).** `Edam.Test.TestCatalogLibrary` (`AppHelper`) + `Edam.Test.TestBuilder` dropped their `CatalogDb` ProjectReferences and `AppHelper` now uses `ModelCatalogService.Default` (the EF `CatalogBuilderServiceInstance` was dead — nothing read `AppHelper.CatalogInstance`); removed the stray EF using in `TestFileSystemClient.cs`. Also removed the unused plain `using Edam.Data.CatalogDb;` from WinUI `CatalogServiceHelper.cs`/`CatalogViewModel.cs` (kept the `catDb` alias). **WinUI desktop local back-end DEFERRED to BL-7.4:** `Edam.UI.CatalogExplorer.CatalogServiceHelper.GetLocalInstance` runs on the Windows desktop (`PlatformID != Other`) and uses the EF `catDb.CatalogInstance` for a persistent local catalog; replacing it with the in-memory `ModelCatalogService` would regress desktop persistence, so it is deferred to the BL-7.4 Npgsql/DI wiring + a user (online) build + runtime validation. Documented in `BL-7.2.md`. Committed locally (pending push). **→ SUPERSEDED by Validation Report item 14 (2026-09-15): the desktop local back-end is now Npgsql store-backed; the last `CatalogDb` references are gone.**
13. **Wave 1.1 BL-7.2 — live-PostgreSQL conformance VERIFIED (2026-09-15) — ALL CONFORM 12/12.** User started the container (`docker compose up -d`). Ran `Edam.Data.Catalog.Conformance postgres "<catalog DSN>"` → all provider-conformance checks pass against the real DB (enlist/get container; create/get branch by path; container items; data-leaf create/get/delete; `IContentStore` read/write/exists/delete; path-addressed resources), schema created idempotently (`EnsureSchemaAsync`) on a fresh volume. Closes the deferred BL-7.2 acceptance item. Sandbox can reach Postgres over TCP (`localhost:5432`) but not the Docker engine pipe. Committed the doc evidence (pending push).
14. **Wave 1.1 BL-7.2 — WinUI desktop local back-end migrated to Npgsql; last `CatalogDb` references retired (2026-09-15, verified).** `Edam.UI.CatalogExplorer` dropped the `Edam.Data.CatalogDb` ProjectReference and added `Edam.Data.Catalog.PostgreSql` + `Contracts`; `CatalogServiceHelper.GetLocalInstance` now constructs a `PostgreSqlCatalogStore` (from `ConnectionStrings:catalog`, fallback = the compose DSN) wrapped in the new **`StoreBackedCatalogService`** (`Edam.Data.CatalogServiceClient`) — a Model `ICatalogService` facade adapting the provider-agnostic Contracts `ICatalogStore` to the Model surface (Model↔Contracts mapping for `ContainerInfo`/`ItemInfo`/`ItemDataInfo`/`ContentTypeInfo`; `TreeItemType` matches `ItemType` 0–2). The desktop catalog **stays persistent without EF**; the tree-builder/view-models are unchanged. Also dropped `CatalogDb` from the service host `Edam.Data.CatalogService` (only a commented dead block used it). **Grep confirms no `.csproj` references `CatalogDb`.** Builds **0 error**: `Edam.Data.Catalog.slnx`, `Edam.Data.CatalogService`, `Edam.UI.CatalogExplorer`, `Edam.Test.TestBuilder`. **Runtime smoke of the facade against the live DB passed (`SMOKE_OK`):** a throwaway console wrapped `PostgreSqlCatalogStore` in `StoreBackedCatalogService` and round-tripped through Model `ICatalogContainer`/`ICatalogItem`/`ICatalogItemData` (enlist → `CreateBranch` `/smoke/beta` → `GetItemByPath` → `CreateDataLeaf`/`AddItem`/`GetDataByName` "hello-store-backed"); throwaway removed. Remaining/minor: a FileSystem `ICatalogStore` conformance fixture. Committed locally (pending push).
15. **Wave 1.1 BL-7.2 — FileSystem `ICatalogStore`/`IContentStore` provider added + unit tests (2026-09-15, verified).** New `Edam.Data.Catalog.FileSystem` — `FileSystemCatalogStore : ICatalogStore` + `FileSystemContentStore : IContentStore` (ADR-0006/0007): a durable, path-addressed file-system back-end with no DB/EF. Metadata persists as JSON under a root dir (survives re-open); content maps `/docs/x` → physical files under `content/`. Wired into the conformance runner (`Edam.Data.Catalog.Conformance filesystem <root>` → **ALL CONFORM 12/12**, mirroring PostgreSQL + in-memory). Added **`Edam.Data.Catalog.Tests`** (MSTest, net10, in `Edam.Data.Catalog.slnx`): FileSystem & in-memory provider conformance, FileSystem durability across instances, `IContentStore` roundtrip + physical-file layout, and `StoreBackedCatalogService` Model↔Contracts facade mapping (container/branch/data roundtrip). **Full catalog slnx builds 0 error.** Sandbox caveat: the MSTest host (`vstest.console` → testhost) cannot run inside the agent sandbox (the testhost's parent-process exit watch throws `Access is denied`), so in-sandbox runtime evidence came from the conformance console runner; the MSTest suite builds 0 error and is runnable in a normal CI/user environment. This closes the file-system fixture gap noted in item 14. Committed locally (pending push).
16. **Wave 1.1 BL-7.4 — DI composition root + per-Container resolver (2026-09-15, verified 0-error + conformance-through-DI ALL CONFORM).** New `Edam.Data.Catalog.DependencyInjection` — `CatalogServices.AddCatalogServices(config)` (single composition root; registers catalog interfaces by `ContainerType`/config: `Edam:Catalog:Target`, provider config via `ConnectionStrings:catalog` / `Edam:Catalog:FileSystemRoot` / `Edam:Catalog:ServiceBaseUri`; hidden default `ICatalogStore`/`IContentStore` registered only when the default target resolves; `ICatalogClient` when a service base URI is set) and **`CatalogProviderRegistry`** implementing `ICatalogProviderResolver<ICatalogStore>` + `<IContentStore>` (per-**Container** resolution, default-target fallback). `Edam.Data.CatalogService` host now uses the consolidated `AddCatalogServices` (resolves `ICatalogStore` by contract; never names a provider). **Verified** with a throwaway DI probe: provider-conformance through the DI surface = **ALL CONFORM** (live Postgres), and per-Container — `FileSystem` Container → FileSystem store, `PostgreSql` Container → Postgres store, unconfigured `Service` → default fallback. MSTest `CatalogResolutionTests` covers the registry mapping + `ParseTarget`. **Also** fixed a BL-7.2 store bug surfaced by re-validation: `PostgreSqlCatalogStore.EnlistContainerAsync` returned a fresh `Guid` on `ON CONFLICT`, so idempotent re-enlist returned an id that did not match the row and `GetContainer(byId)` returned null — now `RETURNING id` (standalone Postgres conformance again **ALL CONFORM 12/12** with leftover rows). **Remaining:** migrate the WinUI desktop app to resolve stores via this DI seam (it still constructs its local providers directly). Committed locally (pending push).
18. **Wave 1.1 — catalog REST service orchestrated in the Aspire AppHost (2026-09-15, offline build 0 error).** `src/Edam.AppHost/AppHost.cs` now declares a PostgreSQL container (`catalogdb`: postgres:17-alpine, user/db `edam`, fixed host:container port 5432, named volume) and **`edam-catalog-service`** (`Projects.Edam_Data_CatalogService`), wired with `.WaitFor(catalogDb)` and the `ConnectionStrings__catalog` env var (→ `ConnectionStrings:catalog`, the key `AddCatalogServices` reads). Uses `AddContainer` (not `AddPostgres`) because `Aspire.Hosting.PostgreSQL` isn't in the offline NuGet cache — a nicer `AddPostgres` swap is a future enhancement once online. Runs all resources with `dotnet run --project src/Edam.AppHost`. **User step:** launch the AppHost (needs a Docker engine, unavailable in-sandbox) and validate the catalog service + postgres come up. Committed locally (pending push).
17. **Wave 1.1 BL-7.4 — WinUI local back-end resolved via DI (2026-09-15, offline build 0 error).** `Edam.UI.CatalogExplorer/CatalogServiceHelper.GetLocalInstance()` no longer constructs `new PostgreSqlCatalogStore(...)`: it resolves `ICatalogStore` from a `ServiceProvider` built by the consolidated `AddCatalogServices(config-map)` (new `IReadOnlyDictionary<string,string>` overload — new `MapConfiguration.cs` — for desktop hosts without appsettings.json), cached per connection string. Direct `PostgreSql` project reference removed from the WinUI csproj (provider now transitively via `Edam.Data.Catalog.DependencyInjection`); stale `using Microsoft.EntityFrameworkCore.Metadata.Internal;` dropped from `AppSession.cs`. WinUI rebuilds **0 errors**. **BL-7.4 now Complete** (DI composition root + per-Container resolver + service-host & WinUI local store resolved via DI; conformance-through-DI ALL CONFORM). **Deferred** to the Model-HTTP-client retirement item: `Controls/CatalogViewModel`'s `new FolderCatalogClient(...)` file-catalog feature and the `PlatformID.Other`-guarded remote `GetClientInstanceAsync`/`CatalogInstance.GetCatalog`. **User step remaining:** run the WinUI desktop app against `edam-postgres`. Committed locally (pending push).

19. **Wave 1.1 BL-7.2 — store-conformance-driven HTTP integration test for the catalog REST API (`Edam.Data.Catalog.HttpConformance`) + REST wire-layer fixes (2026-09-16, verified).** When asked whether the API's catalog methods were under test: the store level was (provider conformance 12/12) but the REST wire layer was **not**. Added `src/Edam.Data.Catalog.HttpConformance` (new console runner in the catalog slnx, net10, `FrameworkReference Microsoft.AspNetCore.App`): hosts `Edam.Data.CatalogService` **in-process** (Kestrel on an ephemeral loopback port — required refactoring `Program.BuildApp()` out of the top-level host) and drives it through the **real `CatalogHttpClient`** (Contracts REST client) with 13 checks mirroring the provider-conformance operations over the wire (session/info, enlist, get-by-name, get-by-**GUID** id, create branch, get item by path, list containers, container items, add data leaf, get data by name, delete data, get content type, get branch). Runner is hermetic per run (unique container id + branch path). `filesystem` and `postgres` targets both report **HTTP conformance ALL CONFORM (13/13)**. **It surfaced + fixed these real defects (the point of the test):** (a) `CatalogHttpItemData.AddItemAsync` posted the data leaf to the item endpoint (`/catalog/item`) instead of `/catalog/data/item` (added `URI_ITEM_DATA_ADD`); (b) `GetDataByName` returned HTTP 400 because the map route param was `itemId` while the client sends `id`; (c) no true by-GUID container route — `CatalogHttpContainer.GetContainer(Guid)` hit `/container/info?containerId=<guid>` → null; the `/container/id` map route now dispatches between a Guid and a containerId string; (d) the client's `CreateBranchAsync` sent `DateTimeOffset.Now` (local −04:00), which Npgsql rejects for `timestamptz` (UTC only) — now `UtcNow` (canonical, ADR-0008) plus the Postgres `AddItemAsync` binds `.ToUniversalTime()`. Also surfaced **two untested Postgres provider gaps** now fixed: `PostgreSqlCatalogStore.GetBranchAsync` scoped items to `container_id = Guid.Empty` (never found branches stored in real containers) — now iterates all items by path prefix like FileSystem; `GetContentType` returned null because `edam_content_type` had no seeded rows — now falls back to a bare descriptor like FileSystem. (The earlier `GetItemByPath` "FAIL" was a stale hardcoded-path comparison artifact in the runner, now an id-based check.) Builds: catalog slnx `Edam.Data.Catalog.slnx` + `Edam.UI.CatalogExplorer` **0 error**. Committed locally (pending push).

20. **Wave 1.1 — catalog REST API operationalized + part of the default stack (2026-09-16, builds 0 error; standalone run verified against live Postgres).** Follow-up to "how do we get the Catalog API up and running by default in the AppHost": the AppHost already orchestrated `catalogdb` + `edam-catalog-service` (item 18) but the **service itself was not operational**. Now: (a) `Edam.Data.CatalogService` adopts **`Edam.ServiceDefaults`** (`AddServiceDefaults()` + `MapDefaultEndpoints()`) → `/health` + `/alive` probes, OTel, resilience, service discovery — a first-class Aspire resource the dashboard can show healthy; (b) added to the **root `Edam.slnx`** under `/src/Edam.Data.Catalog/` so the default solution builds it; (c) `AppHost.cs` now also sets `Edam__Catalog__Target=postgres` beside `ConnectionStrings__catalog`; (d) `CatalogHttpClient`'s default base URI aligned to the service's real launch URL — `http://localhost:5194/catalogservice/` (it was a stale `https://localhost:7069/…` that could never connect); (e) wrote the service `README.md` (endpoints, standalone run, AppHost run, config table, verify); (f) deleted the `WeatherForecast` template cruft. **Verified:** `Edam.slnx` + catalog slnx + runner build **0 error**; HTTP conformance still **ALL CONFORM** (filesystem + postgres); standalone service run against live Postgres returns `/alive` 200, `/health` 200, `session/info` 200, `container/list` 200. **Two gotchas recorded:** `Edam.ServiceDefaults` is `IsAspireSharedProject`, so its **package deps do not flow transitively** — the in-process `HttpConformance` host needed a **direct** reference *and* a `/t:Restore` (adding a ProjectReference does not refresh `project.assets.json`/`deps.json`, so the assembly was present but unresolvable); and the host uses the **current directory** as content root, so run it via `dotnet run --project` (or from the app dir) for `appsettings.json` to load. **OpenAPI intentionally not shipped:** the only cached `Microsoft.AspNetCore.OpenApi` is **9.0.5 (net9)**, which faults (`JsonException: … DateTimeOffset`) on the net10 runtime — re-add `AddOpenApi()`/`MapOpenApi()` with the **10.x** package once restorable online (documented in the README). **Remaining (next):** WebApi still surfaces the BL-7.5 `Edam.Services.Core` stand-in store instead of the real platform provider (the "seam swap"), and WinUI's remote path still uses the old Model client (see item 17's deferred note). **User step:** `dotnet run --project src/Edam.AppHost` (needs Docker) to see `edam-catalog-service` + `catalogdb` up together in the dashboard. Committed locally (pending push).

21. **Wave 1.1 BL-7.5 — seam swap: the Wave-1 catalog boundary now resolves the REAL catalog platform (2026-09-18, builds 0 error; live-verified both paths).** Closes the coupling BL-7.5 explicitly deferred. Added **`Edam.Services.Core/CatalogPlatformStore`** — a Wave-1 `ICatalogStore` adapter over the platform's `Edam.Data.Catalog.Contracts.ICatalogStore` (enumerates containers→items, path-based asset Ids, degrades to empty when unreachable, reports `catalog:<provider>` without naming it), and rewired **`AddWave1Services(IConfiguration?)`** (new config overload; WebApi now passes `builder.Configuration`) to map the Wave-1 keys onto the platform composition root (`Edam:CatalogStore`/`Edam:CatalogRoot`/`DefaultRootFileFolder`/`ConnectionStrings:catalog` → `Edam:Catalog:Target`/`Edam:Catalog:FileSystemRoot`) and call **`AddCatalogServices`** — so the store comes from the platform behind DI. The **in-memory store is now only the registered fallback** (BL-7.5's AC); the superseded Services.Core stand-ins (`FileSystemCatalogStore` — System.IO file enumeration; `PostgresCatalogStore` — read a non-existent `edam.catalog_assets` table) are **deleted**, and `Npgsql` was dropped from `Edam.Services.Core`. **`AppHost.cs`** now gives **`edam-web-api`** the same `ConnectionStrings__catalog` + `Edam__Catalog__Target=postgres` as the catalog service, so the default stack serves **one** real catalog. **Verified live:** `Edam.slnx` builds **0 error**; `/catalog/items` with the catalog configured → `Store='catalog:postgres'` with **7 real catalog items** from the live DB; unconfigured → `Store='in-memory'` + 3 seed assets (fallback); `/wave1` and `Edam.Cli wave1` unchanged (all three surfaces running/healthy). **Gotcha recorded (BL-4.2 name-collision class):** the seam swap pulls `Edam.System` transitively into `Edam.Services.Core`; it declares `Edam.Convert`, which — because Services.Core's types live in an `Edam.*` namespace — **shadows the BCL `System.Convert`** under C#'s enclosing-namespace lookup. Fixed by fully qualifying `System.Convert.ToHexString` in `Governance.cs`. **Sandbox artifact:** the new fallback `LogWarning` reaches the Windows EventLog provider, which the sandbox denies (`Win32Exception(5)`), aborting the request — in-sandbox verification suppresses it with `Logging__EventLog__LogLevel__Default=None`; harmless in a normal environment. **User step:** re-run `dotnet run --project src/Edam.AppHost` (I stopped the earlier instance to unlock the WebApi build output) and confirm `/catalog/items` shows the real catalog. Committed locally (pending push).

22. **Wave 1.1 — WinUI consumer rebind: the desktop remote path now uses the Contracts HTTP client (2026-09-18, builds 0 error; Model-facade-over-HTTP verified on both back-ends).** Closes the BL-7.4 deferred "guarded remote `GetClientInstanceAsync`/`CatalogInstance.GetCatalog`" item. (a) **`StoreBackedCatalogService`** (the Model↔Contracts facade) now adapts **either** a local `ICatalogStore` **or** a remote `ICatalogClient` — it holds the three Contracts sub-interfaces (`ICatalogContainer`/`ICatalogItem`/`ICatalogItemData`) behind two convenience ctors, so the Model `ICatalogService` surface is identical for both paths. (b) **`CatalogServiceHelper.GetClientInstanceAsync`** resolves the Contracts `ICatalogClient` (`CatalogHttpClient`) from the DI composition root (`Edam:Catalog:ServiceBaseUri`) instead of constructing the retired Model `CatalogInstance`/`CatalogClient`; `GetLocalInstance` is unchanged. (c) Remote-vs-local is now **configuration-driven** (`IsServiceBaseUri` — an HTTP(S) base URI, explicit or the configured `CatalogServiceBaseUri`), replacing the `PlatformID.Other` check that made the remote path **unreachable on Windows**; `AppModelState.GetConnectionUri` updated to match. (d) `CatalogViewModel.GetFileSystemProviderAsync` resolves its per-Container provider from the **BL-7.4 registry** (`CatalogServiceHelper.GetContainerStore` → `ICatalogProviderResolver<ICatalogStore>`) instead of `new FolderCatalogClient(...)` — **REVERTED, see item 24: the platform `FileSystemCatalogStore` is metadata-only and does NOT enumerate a folder, so this feature correctly stays on the Model `FolderCatalogClient`.** **Verified:** catalog slnx + `Edam.UI.CatalogExplorer` + `Edam.slnx` build **0 error**; the HTTP conformance runner gained three **Model-facade-over-HTTP** checks (create branch / add data leaf / get data by name through `StoreBackedCatalogService` over the real `CatalogHttpClient`) → **ALL CONFORM (16 checks) on filesystem AND postgres**. **Remaining Model-client usage:** `Edam.Data.CatalogServiceClient`'s `CatalogInstance`/`CatalogClient`/`FolderCatalogClient` are now referenced only by the `Edam.Test.TestBuilder` harnesses (which deliberately exercise the file client) and the unused `WebApp/CatalogSystem.cs` — candidates for deletion once those harnesses migrate. Committed locally (pending push).

23. **Wave 1.1 — the Model HTTP catalog client is retired (deleted) (2026-09-18, builds 0 error; HTTP conformance still ALL CONFORM).** Completes the "Model-HTTP-client retirement" tracked since BL-7.2. Deleted from `Edam.Data.CatalogServiceClient`: `CatalogInstance` (the invariant-name factory), `CatalogClient` (the Model HTTP client) with its `ClientCatalogContainer`/`ClientCatalogItem`/`ClientCatalogItemData` adapters, and the unreferenced legacy `WebApp/CatalogSystem` + `WebApp/WebAppService` helpers (the factory's only remaining consumer). Verified nothing else referenced them: the live path is the Contracts-native `CatalogHttpClient` (`CatalogHttpContainer`/`Item`/`ItemData`) + `StoreBackedCatalogService`, and `CatalogBaseClient` (CatalogModel) **stays** — the WinUI helper uses its `SessionId`. **Residual (deliberate):** the Model **file** client (`FolderCatalogClient` + `FolderCatalogItem`/`FolderCatalogItemData`) remains, referenced only by the `Edam.Test.TestBuilder` harnesses (`TestFileSystemClient`, `TestCatalogClone`), which exercise it by design; **it is NOT deletable as-is — it is the folder-as-catalog provider (see item 24).** **Verified:** catalog slnx, `Edam.UI.CatalogExplorer`, `Edam.Test.TestBuilder`, and `Edam.slnx` build **0 error**; HTTP conformance still **ALL CONFORM** (filesystem + postgres). Committed locally (pending push).

24. **Wave 1.1 — CORRECTION: the Model file client is NOT dead legacy code; it is the folder-as-catalog provider (2026-09-18, builds 0 error).** While attempting to finish the retirement by migrating the `Edam.Test.TestBuilder` harnesses off `FolderCatalogClient`, I found that `FolderCatalogClient.InitializeFileItems` calls **`CatalogFileSystem.FileSystemToCatalogAsync(baseUri, builder)`** (`Edam.Data.CatalogModel`) — it **enumerates a real folder tree into catalog items**. Grep confirms it is that method's **only** consumer, so deleting the client would have removed the capability and orphaned the helper. Consequently: (a) the Model **file** client (`FolderCatalogClient` + `FolderCatalogItem`/`FolderCatalogItemData` + `CatalogFileSystem`) is **kept** — a live provider, not retirement waste; (b) the test harnesses are **kept unchanged** (they correctly exercise folder→catalog); (c) **a regression I introduced earlier is reverted** — item 22(d) had pointed `CatalogViewModel.GetFileSystemProviderAsync` at the BL-7.4 `ICatalogProviderResolver<ICatalogStore>`, which resolves the platform's **metadata-only** `FileSystemCatalogStore`; that silently swapped a folder catalog for an empty metadata store. It is restored to `new FolderCatalogClient(...)`, the now-unused `CatalogServiceHelper.GetContainerStore` helper is removed, and the reason is recorded in-line so it is not "migrated" again. **Verified:** `Edam.UI.CatalogExplorer`, `Edam.Test.TestBuilder`, catalog slnx, `Edam.slnx` build **0 error**. **Correct follow-up (new work, not cleanup):** port folder→catalog into the platform as a real provider behind the Contracts seam (e.g. an `ICatalogStore` wrapping `CatalogFileSystem.FileSystemToCatalogAsync`, or a folder-indexing step that populates the metadata store); only then can the Model file client retire. Committed locally (pending push).

25. **Wave 1.1 — folder-as-catalog is now a first-class, clearly-named provider (2026-09-18, builds 0 error).** Acting on the decision that "open a folder as a catalog" is a real desktop scenario, the Model folder client is **kept and de-collided** rather than shelved: renamed `CatalogFileSystemClient` → **`FolderCatalogClient`**, `CatalogFileSystemItem` → **`FolderCatalogItem`**, `CatalogFileSystemItemData` → **`FolderCatalogItemData`**, and its namespace moved from `Edam.Data.CatalogService` to **`Edam.Data.CatalogServiceClient`** — so nothing in the client library impersonates the service host's namespace or the platform's metadata-only `FileSystemCatalogStore`. Consumers updated (WinUI `CatalogViewModel`, the `Edam.Test.TestBuilder` harnesses); a stale `using Edam.Data.CatalogService;` in `CatalogContainerViewModel` was dropped. Names corrected in items 23/24, BL-7.2/7.4/7.5 and ADR-0008 (with an Update note). **Verified:** catalog slnx, `Edam.UI.CatalogExplorer`, `Edam.Test.TestBuilder`, `Edam.slnx` build **0 error**. **Remaining (the actual port):** expose folder→catalog behind the Contracts seam (`ICatalogStore`/`IContentStore`) so the desktop resolves it via DI like every other provider and the Model client can retire for real. — **DONE: see items 27 (ingestion) and 28 (retirement).** Committed locally (pending push).

26. **Wave 1.1 — path-addressed content (`IContentStore`) is now on the wire (2026-09-18, builds 0 error; wire-verified on both back-ends).** Closes the gap where the `IContentStore` blob/binary seam existed for *providers* and the conformance suite but had **no REST surface or client accessor** — so ADR-0007's "path-addressed file/JSON/XML/blob content" was not reachable through the API. Added: a **`ContentInfo`** Contracts value record (`ResourcePath`, `Exists`, `Length`, `ContentType`, `ContentBase64`) so binary content rides the existing JSON wire **base64-encoded** and the single canonical contract is preserved; **`ICatalogClient.Content`** — an **`IContentStore`** accessor, so callers keep the stream-based seam and base64 stays a wire detail; **`CatalogHttpContent`** (new) implementing `IContentStore` over `GetDataFromJsonAsync`/`PostAsync`/`DeleteAsync`; four routes in `CatalogServiceMap` (`GET content/info`, `GET content/item`, `POST content/item`, `DELETE content/item`) mapped **only when a content provider resolved**; and `Program.BuildApp` now resolves the optional `IContentStore` and passes it to the map. **Verified live:** provider-conformance unchanged; the HTTP conformance runner gained four content checks (write+exists, read round-trip, delete, missing→null) → **ALL CONFORM (20 checks) on filesystem AND postgres**; catalog slnx, `Edam.UI.CatalogExplorer`, `Edam.Test.TestBuilder`, `Edam.slnx` all build **0 error**. Service `README.md` documents the content endpoints. Committed locally (pending push).

27. **Wave 1.1 — folder-as-catalog ported behind the Contracts seams (folder ingestion), verified end-to-end (2026-09-18, builds 0 error).** Answers item 24's follow-up. Rather than a second folder *provider* (which would collide with the durable `FileSystemCatalogStore` and needs an id-based data surface folders don't have), a folder is now **ingested into a catalog** through the Contracts seams: new project **`Edam.Data.Catalog.Folder`** (Contracts-only) with **`FolderCatalogIndexer`** — directories → branch items, files → leaf items, file bytes → `IContentStore` at the same path. It drives `ICatalogContainer`/`ICatalogItem`/`IContentStore` **directly**, so the identical call works against any local provider (PostgreSQL/FileSystem) or a remote `ICatalogClient` over the wire; item ids are **deterministic** (container+path hash) so re-indexing is idempotent (a snapshot, not a live view). The WinUI `FileSystem`-container path now resolves the store/content via DI (`CatalogServiceHelper.GetLocalStore`) and indexes the container's folder, wrapped in `StoreBackedCatalogService` — **replacing `new FolderCatalogClient(...)`**; the desktop no longer uses a bespoke Model client for folders. **Verified:** HTTP conformance gained three checks — index a real folder, then confirm **items and content are visible over the API** → **ALL CONFORM (23 checks) on filesystem AND postgres**; catalog slnx, `Edam.UI.CatalogExplorer`, `Edam.Test.TestBuilder`, `Edam.slnx` build **0 error**. **Remaining:** `FolderCatalogClient` (+ `FolderCatalogItem`/`FolderCatalogItemData`, and `CatalogFileSystem` in the Model) is now referenced only by the two `Edam.Test.TestBuilder` harnesses — **retired in item 28.** Committed locally (pending push).

28. **Wave 1.1 — the Model folder client is retired (deleted); folder coverage moved to the indexer (2026-09-18, builds 0 error).** Completes items 24/25/27. Deleted `FolderCatalogClient` + `FolderCatalogItem` + `FolderCatalogItemData` (`Edam.Data.CatalogServiceClient`) and the now-orphaned `CatalogFileSystem` helper (`Edam.Data.CatalogModel`, whose `FileSystemToCatalogAsync` had only that one consumer). With them went the two `Edam.Test.TestBuilder` harnesses that existed solely to exercise them (`TestFileSystemClient`, `TestCatalogClone`) — they encoded the retired client's semantics (folder-relative `GetBranch` counts, hardcoded `d:/temp/test/` and user-specific `/PSJ.Courts/...` paths; `TestCatalogClone` had no assertions at all) and could not be migrated faithfully to platform semantics. **Coverage was replaced, not dropped:** new `Edam.Data.Catalog.Tests/FolderCatalogIndexerTests` covers the indexer that superseded them (files→leaves, folders→branches, content round-trip, **idempotent re-index** via deterministic ids, missing-folder no-op), and the HTTP conformance runner proves the same behavior end-to-end through the API on both back-ends. **Deliberately kept:** `CatalogClone` (Model) is now an *orphaned capability* (its only caller was the deleted `TestCatalogClone`) — kept rather than deleted, per the item-24 lesson that an unused capability is not the same as dead client code; it wants its own test when someone needs it. **Verified:** catalog slnx (incl. tests), `Edam.UI.CatalogExplorer`, `Edam.Test.TestBuilder`, `Edam.slnx` build **0 error**; HTTP conformance still **ALL CONFORM** (filesystem + postgres). Committed locally (pending push).

29. **Wave 1.1 — AppHost/compose port clash fixed; EF `CatalogDb` deleted; OpenAPI confirmed blocked (2026-09-18, builds 0 error).** Three follow-ups: **(a) AppHost ↔ compose 5432 clash — FIXED.** Both bound host **5432**, so `docker compose up -d` plus `dotnet run --project src/Edam.AppHost` fought over the port and the catalog services blocked on `WaitFor(catalogDb)`. The AppHost now provisions its **own** PostgreSQL on host port **5433** (`ep.Port = 5433`, container `TargetPort = 5432`, separate `catalogdb-data` volume) and points both services at it; the DSN is overridable with `--CatalogConnectionString "<DSN>"` to share one database with compose instead. `compose.yaml` and the service `README.md` document the split (5432 = standalone/CLI/conformance; 5433 = AppHost). Note the two instances have **separate** data. **(b) `Edam.Data.CatalogDb` deleted.** No `.csproj` referenced it (only historical comments), so the project directory was removed and its entries dropped from **both** solution files (`Edam.Data.Catalog.slnx`, and the WinUI `Edam.Data.CatalogExplorer.sln` — its project line + 16 configuration lines). Building the WinUI solution exposed a **stale-restore** artifact: `Edam.CatalogExplorer`'s `obj/` still listed the deleted project (its csproj was clean), breaking the MSIX target — cleared `obj` + re-restored. **(c) OpenAPI 10.x — genuinely blocked, documented.** A network probe showed nuget.org:443 reachable but the **NuGet service index unreachable** (same TLS/schannel failure as `git fetch`), so the 10.x package cannot be restored; and it is **not** in the .NET 10 shared framework/ref pack (`Microsoft.AspNetCore.App` 10.0.12 has no `Microsoft.AspNetCore.OpenApi.dll`). Only 9.0.5 (net9) is cached and it faults on net10, so the mapping stays removed; the README records the exact steps for a future online restore. **Verified:** `Edam.slnx`, catalog slnx, `Edam.AppHost`, `Edam.UI.CatalogExplorer`, `Edam.Test.TestBuilder` build **0 error**; the WinUI `Edam.Data.CatalogExplorer.sln` now builds **0 error for `Platform=x64`** (its AnyCPU failure is the pre-existing MSIX "app host cannot be architecture-neutral" constraint); HTTP conformance still **ALL CONFORM** (filesystem + postgres). Committed locally (pending push).

30. **Wave 1.1 — OpenAPI is live; the real blocker was a `DateTimeOffset` schema fault, not the package (2026-09-18, builds 0 error; verified on both back-ends).** The user restored `Microsoft.AspNetCore.OpenApi` **10.0.12** (matching the ASP.NET Core 10.0.12 shared framework), `AddOpenApi()`/`MapOpenApi()` were re-added, and a conformance check added — but `/openapi/v1.json` still returned **500**, so the earlier "9.0.5 on net10" diagnosis was **wrong**. The real exception: `System.Text.Json.JsonException: The JSON value could not be converted to System.DateTimeOffset` inside `Microsoft.AspNetCore.OpenApi.OpenApiSchemaService.CreateSchema`. Isolating it (excluding the item endpoints via `OpenApiOptions.ShouldInclude` → document generated fine) proved the culprit was **`ItemInfo`**: its record parameter defaults `DateTimeOffset CreatedDate = default` cannot be round-tripped by the schema service. **Fix:** made every `ItemInfo` parameter **required** (`= default` removed; C# then also forbids the optional `Description`/`Type` defaults, since required parameters must precede optional ones) — a one-file Contracts change; all 8 construction sites already passed every argument, so nothing else moved. **Verified:** `/openapi/v1.json` → **200, 30,773 bytes**, item endpoints included (diagnostic filter removed); HTTP conformance now **24 checks ALL CONFORM on filesystem AND postgres**; `Edam.slnx`, catalog slnx, `Edam.AppHost`, `Edam.UI.CatalogExplorer`, `Edam.Test.TestBuilder` and the WinUI `Edam.Data.CatalogExplorer.sln` (x64) all build **0 error**. The "why" is recorded in the `ItemInfo` remarks + the service README so the defaults aren't re-added. Committed locally (pending push).

31. **Projects Enhancements (PE) — PE-0 + PE-1 complete: Projects are now specified to live in the Catalog (2026-09-18, builds 0 error; no existing consumer changed).** New initiative (the user's "PE" effort) to move EDAM **Projects** off the file system onto the **Catalog**. **(PE-0)** Wrote `docs/backlog/Projects-Enhancements.md` — goal, agreed model, and a full **inventory of today's static/file-system surface** (`Project`, `ProjectConsole`, `ProjectContext`, `ProjectHelper`, `ProjectDataModel`, `ApplicationHelper.ProjectInfo`, `AppSettings`/`EdamSettings`, `FolderFileReader`/`UriResourceInfo`, plus the CWD mutations in `AssetUseCaseMap`/`AssetReportBuilder`/`AppData`/`FolderFileHelper`), the four seams, steps PE-0…PE-5 with acceptance criteria, and open questions — and recorded the decisions in **ADR-0009** (**Collection = container, Project = branch**; all artifact content in the Catalog via `IContentStore`; interface-bound, dependency-free surface; `ProjectPath` replaces disk-relative paths **and the process CWD**; `*.Args.json` format unchanged; **import = upload / export = download**). Added **Area PE** to `docs/backlog/README.md` (area table + index + sequencing). **(PE-1)** Created **`Edam.Data.Projects.Contracts`** (net10, **zero dependencies**) with `ProjectCollectionInfo` (+ `ProjectCollectionType`, `ProjectCollectionFolders`), `ProjectInfo` (+ `ProjectFolders`), `ProjectPath` (+ `ProjectResourceInfo`), `ProjectImportResult`, `ProjectRunResult`, and the four interfaces **`IProjectCatalog`** (discovery) / **`IProjectStore`** (create/import/export/delete) / **`IProjectResources`** (the hinge: List/Exists/OpenRead/Write/CreateFolder/Delete by `ProjectPath`) / **`IProjectRunner`** (run a `*.Args.json` process); plus `Edam.Data.Projects.slnx`. **Verified:** the contracts solution builds **0 error**; **no existing consumer changed** (nothing referenced). Also **confirmed the feature the user remembered**: `CatalogClone.CloneProjectLeaf` (kept as the orphaned capability in item 28) plus the retired `FolderCatalogClient` (exercised by the old `TestCatalogClone`) is the "read a project folder → copy into the catalog" path, and `FolderCatalogIndexer` is its modern source side — PE-3 promotes this to first-class upload/download. **Next step (PE-2):** the file-system `IProjectResources` implementation, behaviour-preserving, proven by running the spec's example `*.Args.json` headlessly with paths resolved through the interface and **no `Directory.SetCurrentDirectory`**. Committed locally (pending push).

32. **PE-2 — the project seam works over a file system with NO current-directory dependency (2026-09-18, builds 0 error; 15/15 ALL CONFORM).** Implemented the file-system providers behind the PE-1 contracts in a new **`Edam.Data.Projects.FileSystem`**: `FileSystemProjectCatalog` (the **default collection is the app-data root** and projects live in its `Projects/` folder — today's layout, so existing installs keep working; extra collections accepted), `FileSystemProjectStore` (creates the standard `ProjectFolders` structure — **idempotent**, unlike the old `Project.CreateProject`, and no app-settings template copy; **import = upload**, **export = download**), and **`FileSystemProjectResources`** — the hinge: `ProjectPath` → physical path with an escape guard, recursive list with extension filter, binary-safe `OpenRead`/`Write`, `CreateFolder`, `Delete`. Added **`Edam.Data.Projects.Conformance`** (headless runner in the same style as the catalog conformance runners) proving **15 checks**: default-collection discovery; scaffold of all standard folders; spec-relative normalization (`./Archive/x.xlsx` → `/Archive/x.xlsx`) and `..` neutralization; mapping to the expected physical location; **binary round-trip**; folder+extension listing; **the arguments' own input/output paths resolving through the seam** (`/Archive/…` → `/Documents/datovy.hc.cd.dictionary.xlsx`); exists/delete; import from a folder; browse via the catalog; export back out; project enumeration; `/Projects/<name>` addressing; and the point of the work — **`Directory.GetCurrentDirectory()` identical before and after** → `result: PE-2 conformance ALL CONFORM`. **Scope note (honest):** process execution (`IProjectRunner`) is **not** wired yet — the asset console's readers (`UriResourceInfo`/`FolderFileReader`/`ExcelDocumentReader`) still open files by path, so binding them to `IProjectResources` is **PE-5**; PE-2 proves the seam and the paths, not the asset pipeline. Filesystem remains a registered provider throughout. **Next step (PE-3):** the Catalog implementation — project = branch, folders = branches, artifacts = items + `IContentStore`; collections via `ContainerBinding`; import via `FolderCatalogIndexer`; upload/download (including remotely over the REST API). Committed locally (pending push).

33. **PE-3 — Projects now live in the Catalog: the same scenario conforms on file-system, catalog-local AND catalog-remote HTTP (2026-09-18, builds 0 error; ALL CONFORM).** Implemented the Catalog-backed providers in a new **`Edam.Data.Projects.Catalog`**: `CatalogProjectCatalog` (a **Collection is a container**; a **Project is the branch** `/Projects/<name>`; discovery reads the container's items), `CatalogProjectStore` (create scaffolds the standard folder branches — idempotent; **import = upload** by indexing the source folder **under** the project branch; **export = download**; delete deepest-first), and `CatalogProjectResources` (the hinge over the catalog: a project-relative `ProjectPath` → an item whose **full path is also the `IContentStore` content key**, so artifact bytes live in the catalog). They drive only `<see cref="ICatalogContainer"/>`/`ICatalogItem`/`IContentStore`, so the **identical** code serves a local provider and a **remote catalog client** — no remote-specific implementation. **Supporting changes:** `FolderCatalogIndexer` gained an optional `pathPrefix` (so a folder imports *under* a project), a no-enlist `Guid` overload (an import can never rewrite a container's URI), `IndexDetailedAsync`/`FolderIndexResult` (folder vs file counts) and a public `DeterministicId`. **Verification:** `Edam.Data.Projects.Conformance` was refactored into a shared `ProjectScenario` and now runs it against **three** back-ends — `file-system` (14 checks), `catalog (local)` (13) and `catalog (remote HTTP)` (13, with the catalog service hosted in-process) → **`result: PE-3 conformance ALL CONFORM`**, and the process current directory is unchanged. **Real wire defect found + fixed:** `CatalogHttpItem.CreateBranchAsync` passed the **full path** as the item's `Name`, so a branch created over REST was unfindable by name (the local stores derive the leaf) — it broke the imported-project lookup; now fixed (recorded in BL-7.2). No regressions: catalog slnx + `Edam.slnx` build 0 error; catalog HTTP conformance (filesystem) still ALL CONFORM; the postgres target could not run because **Postgres is not currently up** (connection refused — environmental). **Next step (PE-4):** the `AddProjectServices(config)` DI composition root + retiring the static project surface (`Project`, `ProjectContext`, `ProjectConsole`, `ProjectHelper`). Committed locally (pending push).

34. **PE-3 follow-up — PostgreSQL verified across the full matrix; project paths are now collection-scoped (2026-09-18, ALL CONFORM).** With Postgres up, the PE-3 matrix covers **five** targets: `file-system` (14 checks), `catalog (local)`, `catalog (remote HTTP)`, **`catalog (postgres, local)`** and **`catalog (postgres, remote HTTP)`** (13 each) → **`result: PE-3 conformance ALL CONFORM`**; separately the catalog HTTP conformance (**filesystem + postgres**) and the provider conformance (**filesystem + postgres**) are also ALL CONFORM; catalog slnx, projects slnx and `Edam.slnx` build **0 error**. Running postgres exposed a genuine **model** issue: catalog item paths are **global, not container-scoped** (`GetItemByPath(path)` ignores the container), so two collections collided on `/Projects/<name>` — the postgres-local run created it first, then the postgres-remote run's `CreateAsync` found *that other container's* item, returned early, and its own project was never scaffolded (symptoms: empty folder list, and only the imported project enumerated). **Fixed by scoping project paths by collection id** — a project is now `<collectionId>/Projects/<name>` (`CatalogProjectSupport.ProjectsRoot(collectionId)`), so paths are unambiguous across collections; the scenario's address check was updated accordingly. Also the runner now takes an **optional PostgreSQL DSN argument**, keeping the default run hermetic (file-system + in-process HTTP only). **Decision:** the collection-scoped path convention supersedes the `/Projects/<name>` assumption left open in PE-0 (question 2). Committed locally (pending push).

### 5.6 Validation outcome
Wave 1 implemented + build-verified (full `Edam.slnx` 0-error); in-sandbox-verifiable surfaces verified live; MCP shell compiles; **Aspire host runs (user-confirmed)**. **BL-6.5 Python dropped** (functionality via MCP; MCP/AI pinned — ADR-0004/0005). Remaining to formally close Wave 1 (user-shell): containerized Postgres/blob (Docker/BL-4.15), a headless `Edam.Test.Wave1`, and the feed-side MAF (not SK) republish before any production MCP/AI work. **PE (Projects Enhancements) 2026-09-18:** PE-0 (scope + inventory) + ADR-0009; PE-1 `Edam.Data.Projects.Contracts`; PE-2 file-system providers; **PE-3 Catalog-backed providers — `Edam.Data.Projects.Conformance` ALL CONFORM on all five targets (file-system, catalog-local, catalog-remote HTTP, catalog-postgres local, catalog-postgres remote), no CWD mutation** — next is PE-4 (DI composition root + retire the statics), then PE-5 (consumers + `IProjectRunner`).

### 5.7 AI usage metrics
Unavailable (not recorded in this session).

