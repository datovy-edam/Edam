# Location Model (LM) — the Catalog as the single place project locations are stated

> **Status:** Started — **LM-1 DONE** (address core; 19 checks) · **LM-3 core DONE 2026-09-24** (`ProjectSettings` reader + the composition root consumes it; 12 checks, DI groups unchanged) · **LM-0 done (a)+(b)** · next: **LM-4** (bindings), **LM-5** (seeding), **LM-2** (needs the 2a/2b call), **LM-6** (consumer migration — which is where the 12 duplicated settings copies collapse as hosts adopt the reader).
> **Goal:** replace the multiple divergent location settings with **one declaration per container** plus **URI addresses**, so a file-system container and a catalog container are configured *identically* — the point of ADR-0009.
> **Decision record:** **ADR-0011** (the location model). Related: **ADR-0010** (WinUI packaged app-data — the host-level instance of the same rule: no Documents/OneDrive, no machine paths or secrets in the shipped seed), ADR-0009, `Projects-Enhancements.md` (Area PE).
> **Scope note:** this is about **where things are**, not about project semantics (Collection = container, Project = branch stays).

## The model in one page

- **Address = `catalog://<container>/<path>`.** The container is the authority, the path is the "file-system-like" catalog path. Storage-independent: the same address works for a disk, PostgreSQL or a service.
- **Locations are stated once per container** — the container registry. Every sub-path is **computed**.
- **Bindings are separate:** *where* a container's storage is (folder / DSN / service base URI) is one machine/service-level statement per container, never part of a project location.
- **Well-known locations are named**, not pathed (table below).
- **Aliases are sugar:** `project:documents` → `<default-container>/Projects/<name>/Documents`.
- **Relative references resolve against the referring artifact's address** — `./Archive/x.xsd` in `…/Projects/<name>/Arguments/x.Args.json` means the same container's `…/Projects/<name>/Archive/x.xsd`. `*.Args.json` shape unchanged (ADR-0009).
- **Physical paths are ephemeral:** they exist only in the runner's materialized working folder (PE-5a/5b).
- **Secrets are references** (`vault://…`), never values in files.
- **The Catalog is artifact-agnostic** — it stores *any* artifact, not only projects. The layers are: **generic** (container + path + content) ← **project** (`Projects/<name>/{8 folders}`, `*.Args.json`) ← **app conveniences** (`project:documents`, `app:templates`). `Projects` is **one location family, not a privileged root**, and a new artifact kind needs **no new configuration keys** — only a path in a container.
- **Scaffolding is structure; seeding is content** — the platform creates *locations* (the project branch + its standard folders); starter artifacts are seeded **through addresses** (a template in `Templates/` is read and written into the new project's `Arguments/`), never via an app-settings path or the CWD.

## Frequently-referenced locations (an alias set — not a closed taxonomy)

| Alias | Path within the container | Purpose |
|---|---|---|
| `project:archive` | `Projects/<name>/Archive` | source/archive inputs |
| `project:arguments` | `Projects/<name>/Arguments` | `*.Args.json` process definitions |
| `project:documents` | `Projects/<name>/Documents` | produced documents (outputs) |
| `project:files` | `Projects/<name>/Files` | working inputs |
| `project:useCases` | `Projects/<name>/UseCases` | use cases |
| `project:samples` | `Projects/<name>/Samples` | samples |
| `project:libraries` | `Projects/<name>/Libraries` | libraries |
| `project:textMaps` | `Projects/<name>/TextMaps` | project text maps |
| `app:templates` | `Templates` | args / DDL / template files |
| `app:textMaps` | `TextMaps` | shared text maps |
| `app:samples` | `Samples` | shared samples |
| `app:temp` | `Temp` | scratch — **never** in a catalog container |

The right-hand column is the *only* thing that varies; everything else is derived from the container.

> These names exist because they are **referenced often** — they are conveniences, not the model. Any artifact may live at any path in any container (dictionaries, vocabularies, schemas, reports, arbitrary files), and `Projects` is simply one location family among them. Adding a family must require **no new configuration key**.
>
> **Scaffolder behaviour to preserve (see LM-5):** `Edam.Data.AssetProject.Project.CreateProject` is the scaffolder of record — it created **7** folders (no `TextMaps`) and **seeded the arguments template** as `<project>.<template>`. The platform scaffolders create **8** folders (`ProjectFolders.All`) idempotently but **do not seed**; the folder set and idempotency are improvements, the lost seeding is a regression to fix deliberately (by the consumer, through addresses).

## Steps

| Step | What | Acceptance | Verification | Risk |
|---|---|---|---|---|
| **LM-0** *(safe, do first)* | **Settings/seed debt cleanup** — (a) ✅ sanitized the **seed source** `app-data/Edam.Settings.json` (machine paths, third collection entry, committed connection string, stale mixed-separator `ConsolePath` removed); (b) ✅ **re-wired the packaged seed** — `Edam.Studio.csproj` now links `app-data/**` into the packaged `ApplicationData/Edam.Studio/Edam.App.Data/**` layout (the vacuous `ApplicationData\**` include is gone); (c) ⏳ **moved to LM-3**: collapsing the duplicated location keys is not a data edit — see "LM-0(c)" below | a **clean** build packages a **clean** seed ✔ (verified) | clean-build check of the packaged seed + byte comparison with the source | **done for (a)+(b)** |
| **LM-1** *(start here)* | **The address core** — ✅ **DONE 2026-09-24**: `CatalogAddress` (container + `ProjectPath`, `Parse`/`TryParse`, `ToString()` → `catalog://<container>/<path>`, `Combine`/`Parent`/`TryResolve`) and `ProjectLocations` (the `project:`/`app:` alias table) in `Edam.Data.Projects.Contracts` | addresses round-trip; a `./Archive/x.xsd` reference resolves against its scope **inside the same container**; drive paths, other schemes, empty containers, unknown aliases and cross-container references are rejected | **`address` conformance group — ALL CONFORM (19 checks)** in `Edam.Data.Projects.Conformance`, plus no regressions in the other groups | **none** — purely additive |
| **LM-2** | **Container-scoped artifact addressing:** the container must be part of the address/key for **items *and* content** `(container, path)`; drop the `/<collectionId>/` path-prefix convention | two containers may hold identical paths — for **any** artifact, not just projects — and existing single-container content still resolves | conformance with two containers sharing paths (file-system + Postgres) | **medium-high** — provider change with migration implications; needs a documented compat rule |
| **LM-3** | **Settings schema + compatibility reader** — ✅ **CORE DONE 2026-09-24**: `ProjectSettings.Read(config)` (in `Edam.Data.Projects.DependencyInjection`) resolves the ADR-0011 shape (`Edam:Projects:Root`, declared collections `Edam:Projects:Collections:<name>`, `DefaultCollection`, `WorkingRoot`), **translates** the legacy keys with a direct equivalent (`AppSettings:AssetConsolePath`, `ConsolePath`), and **reports** those that land in a later step (`AssetProjectsPath`, `AssetDataPath`, `DefaultInPath`/`OutPath`, `DefaultTextMapFolder`, the connection string) so nothing is silently dropped; `AddProjectServices` now consumes this one reader. ⏳ Remaining: the 12-copy collapse is the **consumer** half — it follows as hosts adopt the reader (LM-6) | legacy and new spellings resolve to the **same** root; translated keys are reported; recognized-but-pending keys are reported; the composition root still fails fast with no root | **`settings` conformance group — ALL CONFORM (12 checks)** + the two DI groups unchanged (behaviour-preserving refactor) | low |
| **LM-4** | **Bindings:** one statement per container (config / environment / DI), credentials as vault references; document precedence (defaults → user file → environment → command line) and the `EDAM_ROOT` / `EDAM_COLLECTION_<id>__ROOT` overrides | no storage location in project settings; switching a container from file-system to Postgres/service changes **only** the binding | **the payoff test:** the same settings file drives both targets in conformance | low-medium |
| **LM-5** | **Seed `app-data` into a container; app-level locations as items; reconcile the scaffolder:** `Templates`/`TextMaps`/`Samples` (and any other family) become catalog artifacts, with a file-system-backed container for development; **restore template seeding through addresses** (`Templates/<template>` → `Projects/<name>/Arguments/<name>.<template>`) and keep the platform scaffolder structure-only | dev and production differ only by binding; the repo folder is never itself a configured location; creating a project yields a **seeded** `Arguments/` folder again, without app-settings or CWD | import/export conformance (exists) + a dev binding + a scaffolding check (folders + seeded template) | low |
| **LM-6** | **Consumer migration:** `AppSettings`/`ConfigurationHelper`/`AppData` path helpers, the Studio bridge (`ProjectServicesHelper`), args resolution and the deprecated static `Project` surface move to the resolver; legacy keys warn | no consumer resolves a path itself; `Directory.SetCurrentDirectory` unused; the PE-5d `CS0618` set shrinks | builds + conformance + Studio (runtime, user) | medium |
| **LM-7** | **Delete the legacy keys + the duplicated settings copies**, and the helpers that existed only for them; republish affected feed packages if the settings/`[Obsolete]` surface changed | one settings file for locations; grep proves no legacy key remains | builds + conformance | low |

## LM-0(c) — why collapsing the duplicated settings is **LM-3** work, not a data edit

Measured 2026-09-24 across every `appsettings.json` in the repo (18 files; **12** carry the location keys — 11 test/service projects + the app):

| Key | Values found | Verdict |
|---|---|---|
| `AssetDataPath` | `Edam.Studio/Edam.App.Data/` (**11 copies**), `ApplicationData/Edam.App.Data/` (Json.Console), `app-data/Edam.App.Data/` (**the app**) | **three values for one key** |
| `DefaultInPath` | `Edam.App.Data/AM_Console/Samples/` (8), `Edam.App.Data/Samples/` (Dictionary, Python, app), `ApplicationData/AM_Console/Samples/` (Json.Console) | three variants |
| `DefaultOutPath` | `Edam.App.Data/Temp/` (11), `ApplicationData/Temp/` (Json.Console) | two variants |
| `AssetConsolePath` | `""` (11), **`C:\prjs\`** (Json.Console) | ⚠️ an absolute machine path — and **`C:\prjs` exists on this machine**, so it is a *live dev dependency*, not a stale value |
| `DefaultTextMapFolder` | `../../TextMaps/` (**all 12**) | uniform, but **CWD-relative** in every copy |
| `AssetProjectsPath` | `/Projects/` (all 12) | uniform |

**Why this is not a data-only cleanup:**
1. **A blind dedupe would change test behaviour and cannot be verified here** — the MSTest host does not run in this environment (documented for `Edam.Test.Studio`), so editing 12 test fixtures would be unverified change to test inputs.
2. **One value is genuinely machine-specific** (`C:\prjs\` in `Edam.Test.Json.Console`, and the folder exists) — it must remain an *override*, not be "aligned" away. That is exactly what a binding/override layer is for.
3. **The duplication is a symptom of having no single authoritative source.** Collapsing the files without the layering would just move the divergence; the honest fix is **LM-3** (one settings schema + a compatibility reader for the legacy keys) and **LM-4** (bindings/overrides, e.g. `EDAM_ROOT`), after which the 12 copies reduce to overrides — or disappear.

**Recorded as the LM-3 deliverable:** one authoritative settings set, the legacy-key compatibility reader, *and* the collapse of these 12 copies (kept deliberately as an explicit step rather than a silent fixture edit).

## LM-2 in detail — where does the container live?

The container is currently absent from **four** layers, so nothing but a calling convention stops two containers from holding the same path — for *any* artifact, not just projects:

| Layer | Evidence | Effect |
|---|---|---|
| Content contract | `IContentStore` → `OpenReadAsync(string resourcePath)` (+ `Write`/`Delete`/`Exists`) | cannot name a container |
| Content schema | `PostgreSqlContentStore`: `edam_content(resource_path text PRIMARY KEY, body bytea)` | the path alone is the key |
| Content instance | `FileSystemContentStore(rootPath)` → `<root>/content/…`, registered once | containers sharing the store share the namespace |
| Item **reads** + wire | `ICatalogItem.GetItemByPath(path)` ignores the container (stated in `CatalogProjectSupport`); `CatalogHttpContent` sends only `TAG_RESOURCE_PATH` | the same hole locally *and* remotely |

Writes are already container-aware (`CreateBranchAsync(path, name, containerId)`) — **reads and keys are not**. The `/<collectionId>/Projects/…` convention (`CatalogProjectSupport.ProjectsRoot`) exists purely to paper over this, and it conflates two jobs in one string: the artifact's **location** and its **storage key**.

| Option | Shape | Cost | Benefit |
|---|---|---|---|
| **2a** container in the contract | `(containerId, path)` on content + container-aware item reads; Postgres composite key; wire carries the container | breaking change across 3 content impls + item reads + REST wire + the project providers; **plus** content migration | impossible to get wrong; paths container-independent |
| **2b** container-bound instances *(lean)* | one provider per container (BL-7.4's per-Container resolution); file-system already takes a root; Postgres instance carries its container id and the schema gains the dimension | same schema/wire work as 2a, **no API break**; wrong-instance risk (mitigated by resolving through the per-container factory) | keeps `IContentStore`/`ICatalogItem` pure (ADR-0006); the container is a *composition* fact |
| **2c** status quo | container embedded in the path by convention | paths are not locations; the same artifact has different paths per container; every consumer must know the convention | none — cannot satisfy ADR-0011 |

**Migration stance (choose when LM-2 starts):** existing content is conformance/test data plus any imports (real projects are still on disk), stored under prefixed paths — so re-keying is mechanical. Options: scripted re-key, dual-read compat during transition, or declare it disposable and reset. Adding the container dimension to `edam_content` is a **real schema change**, the natural moment to introduce the migrations deferred in BL-5.5.

## How to start (concrete)

**Order: LM-0 (data/build) + LM-1 (address core) in parallel. Do *not* start with LM-2** — it is the only step with provider/migration risk, and it needs the address shape to exist first.

### LM-0 — stop the bleeding (data and build wiring only)
1. **Sanitize the seed source** `app-data/Edam.Settings.json`: remove `C:\prjs\Datovy.Edam/…` and `C:\Users\esobr\Documents/…`, drop `DataSource.DefaultConnectionString`, and fix `App.ConsolePath` (the real folder is `app-data/Edam.App.Data/`; today it also mixes separators and names a folder that does not exist). ADR-0010 already requires the shipped seed to be free of machine paths and secrets.
2. **Re-wire the packaged seed.** `Edam.Studio.csproj` includes `<Content Include="ApplicationData\**">`, but that folder no longer exists and nothing includes `app-data/` — so a clean build packages **no seed**. Point the include at the real source with the `ApplicationData/Edam.Studio/Edam.App.Data/**` layout, or restore the folder. *Acceptance: a clean build packages the seed.*
3. **Collapse the duplicated location keys** across the ~13 `appsettings.json` copies into one default set (or a shared defaults file), after step 2 so there is one obvious source.
4. **Verify:** no drive-letter path or connection string in any committed settings file; build; inspect the packaged `ApplicationData/…/Edam.Settings.json`.

### LM-1 — the address core (the first code) — ✅ DONE 2026-09-24
Delivered (all additive; nothing consumes it yet, so nothing could regress):
1. **`CatalogAddress`** in `Edam.Data.Projects.Contracts` — `Container` + `ProjectPath`; `Parse`/`TryParse` (with a reason), `ToString()` → **`catalog://<container>/<path>`**, `ForContainer`, `IsRoot`, `Combine`, `Parent`, and **`TryResolve`** (a reference resolves against the scope root — the project root for `*.Args.json` — staying in the **same container**; `..` cannot escape). `ProjectPath` was reused, so normalisation existed already.
2. **`ProjectLocations`** — the `project:<folder>[/<rest>]` and `app:<folder>[/<rest>]` aliases over `ProjectFolders.All` and the app-level set (`Templates`, `TextMaps`, `Samples`, `Temp`). Aliases are case-insensitive but resolve to the **canonical** folder name (a bug the conformance caught).
3. **`address` conformance group** — **19 checks, ALL CONFORM**: parse/format round-trip, container root, normalisation, `..` containment, rejection of drive paths / other schemes / empty containers / blanks, `./Archive` + `./Documents` + `../Files` resolution inside the project, same-container absolute references, cross-container rejection, alias expansion (project folder, file, app folder), unknown alias/folder rejection, and the ADR-0011 seeding shape (`app:templates` → `project:arguments/<file>`).
   *Acceptance met:* the report shows `address: ALL CONFORM (19 checks)` beside the existing targets, with the other groups unchanged.

**What is needed from you:** nothing to start LM-1 (the scheme is settled as `catalog://`). The five open decisions are needed by **LM-3**, except the **2a/2b** choice, which LM-2 needs.

**What cannot be verified in this environment:** WinUI/runtime behaviour and anything needing a deploy — those remain user-side, as with PE-5c.

Suggested order overall: **LM-0 + LM-1 → LM-3 → LM-4 → LM-5 → LM-2 → LM-6 → LM-7** (LM-3/4 deliver the "single place" outcome early; LM-2 lands once the address shape has proven itself, before consumers move in LM-6).

## Open decisions (needed by LM-3 — not by LM-1)

1. **URI shape:** `catalog://<container>/<path>`, or `edam://…`? (Recommend a URI; the scheme name is cosmetic and can be settled at LM-1.)
2. **Container-scoped content keys (LM-2):** adopt, and what is the compat rule for already-stored content?
3. **App-level locations** (`Templates`, `TextMaps`, `Samples`): catalog items (self-hosting) or files beside the app?
4. **Where bindings live** (settings vs environment/DI) and what is overridable per machine?
5. **Default container:** declared in settings, or ambient from the current project?

## Non-goals

- Not changing the `*.Args.json` format (ADR-0009 §5).
- Not introducing EF/MS-SQL (ADR-0007) or blob storage (Wave 2).
- Not changing project semantics (Collection = container, Project = branch).
- Not a UI redesign: the Studio keeps its tree; only what it resolves *from* changes.
