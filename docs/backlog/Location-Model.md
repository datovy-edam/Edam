# Location Model (LM) — the Catalog as the single place project locations are stated

> **Status:** Started (2026-09-18) — **plan agreed, no code yet**. Start at **LM-1** (with **LM-0** alongside).
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

## Well-known locations (the frequently-referenced ones)

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

## Steps

| Step | What | Acceptance | Verification | Risk |
|---|---|---|---|---|
| **LM-0** *(safe, do first)* | **Settings debt cleanup — data only, no schema change:** (a) sanitize the **seed source** `app-data/Edam.Settings.json` — it still carries two machine-specific absolute paths (`C:\prjs\Datovy.Edam/…`, `C:\Users\esobr\Documents/…`) and a committed connection string, contrary to **ADR-0010**'s own rule; (b) fix `App.ConsolePath` (names `app-data/Edam.Studio/Edam.App.Data/`; the real folder is `app-data/Edam.App.Data/`, and the separators are mixed); (c) **re-wire the packaged seed** — `Edam.Studio.csproj` includes `ApplicationData\**` but that folder no longer exists and nothing includes `app-data/`, so a clean build packages **no seed**; (d) collapse the 13 duplicated location keys in `appsettings.json` | no absolute machine path and no secret in any committed settings file; a **clean** build packages the seed; the settings agree with the repo layout | grep + a settings-vs-folders check + a clean-build check of the packaged seed | **none** (data/build wiring only) — and it must not be mistaken for the fix |
| **LM-1** *(start here)* | **The address core:** a value type for `container + path` with parse/format/validate/normalise; the well-known-location table; alias expansion; relative resolution **against a referring address**. No consumer changes, no storage changes | addresses round-trip; `./Archive/x.xsd` resolved against a referring address stays in the same container; invalid input (drive path, `..` escaping the container, unknown alias) is rejected with a clear message | **headless conformance** — extend `Edam.Data.Projects.Conformance` with an `address` group | **none** — purely additive |
| **LM-2** | **Container-scoped content keys:** make `IContentStore`/`IProjectResources` keys `(container, path)`; drop the `/<collectionId>/` path prefix convention | two containers may hold identical paths (the PE-3 collision becomes structurally impossible) and existing single-container content still resolves | conformance with two containers sharing paths (file-system + Postgres) | **medium-high** — provider change with migration implications; needs a documented compat rule |
| **LM-3** | **Settings schema + compatibility reader:** one `collections[]` registry (+ optional `locations{}` aliases, `secrets{}` references); a reader translating every legacy key — `AssetConsolePath`/`ConsolePath` → the default container's binding, `AssetProjectsPath` → the `Projects/` segment, `DefaultTextMapFolder` → `app:textMaps`, `DefaultInPath`/`OutPath` → `project:files`/`project:documents`, `UriList[]` → `collections[]` | legacy settings load unchanged; new settings load; the resolved table is logged once | conformance + a legacy/new settings fixture pair resolving to identical addresses | low-medium |
| **LM-4** | **Bindings:** one statement per container (config / environment / DI), credentials as vault references; document precedence (defaults → user file → environment → command line) and the `EDAM_ROOT` / `EDAM_COLLECTION_<id>__ROOT` overrides | no storage location in project settings; switching a container from file-system to Postgres/service changes **only** the binding | **the payoff test:** the same settings file drives both targets in conformance | low-medium |
| **LM-5** | **Seed `app-data` into a container; app-level locations as items:** repo `app-data/` = seed/import source; `Templates`/`TextMaps`/`Samples` become catalog items (self-hosting), with a file-system-backed container for development | dev and production differ only by binding; the repo folder is never itself a configured location | import/export conformance (exists) + a dev binding | low |
| **LM-6** | **Consumer migration:** `AppSettings`/`ConfigurationHelper`/`AppData` path helpers, the Studio bridge (`ProjectServicesHelper`), args resolution and the deprecated static `Project` surface move to the resolver; legacy keys warn | no consumer resolves a path itself; `Directory.SetCurrentDirectory` unused; the PE-5d `CS0618` set shrinks | builds + conformance + Studio (runtime, user) | medium |
| **LM-7** | **Delete the legacy keys + the duplicated settings copies**, and the helpers that existed only for them; republish affected feed packages if the settings/`[Obsolete]` surface changed | one settings file for locations; grep proves no legacy key remains | builds + conformance | low |

## Where to start (recommendation)

**LM-1, with LM-0 alongside.**

- **LM-1 is additive** — nothing consumes it yet, so it cannot regress anything, and it is the one step that is **fully verifiable headlessly** (a real ceiling in this environment: `Edam.Data.Projects.Conformance` already runs 9 targets + adapter checks). Everything else depends on the address type existing.
- **LM-0 is data-only** — it stops the immediate bleeding (a stale path, two machine paths, a committed connection string, 13 divergent copies) with no design risk. It is *not* the fix, and should not be presented as one.
- **LM-2 should wait for LM-1.** It is the only step with provider/migration risk, and doing it first would mean designing content keys before the address exists.

Suggested order: **LM-0 + LM-1 → LM-3 → LM-4 → LM-5 → LM-2 → LM-6 → LM-7**. (LM-3/4 give the "single place" outcome early; LM-2 can land when the address shape has proven itself, before consumers move in LM-6.)

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
