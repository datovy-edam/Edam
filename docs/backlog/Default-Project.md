# Area DP — First-run default project (exploration → plan)

> **Status:** **Implemented (2026-10-08)** — **ADR-0012** is *Accepted* and **D9** is resolved: organization **`Edam`**, the default project is offered as **`Edam.Sample`**, **local binding only**, **delete first**, **sample input shipped in the seed**, and the user is **asked until the action completes** rather than surprised. The mechanism it builds on is **ADR-0013** (unset configuration + first-run setup). **Scheduled as Sprint S2** (`Sprint-S2-Configurable-Items-and-FirstRun.md`). **DP-0…DP-4 are DONE; DP-5's headless half is DONE** (`default project (DP-5, file-system)` and `(DP-5, catalog local)` **ALL CONFORM, 8 checks each**; the postgres target is wired and needs the database up) — **what remains is the Studio first-run check on the user's machine** (F5). **Rename (DP-2's second half) is NOT done** — it is a contract change, not a UI one.

## Why this exists

The Studio now starts, logs in, resolves its databases, and the code editor works — and the project tree is **empty on a fresh installation**. Everything a first run is meant to demonstrate (open a document, edit it, run a process) needs a project, so the empty tree is where the product currently ends. The question explored here: should the application set up a default project for the user, and under what conditions?

## What was verified (facts this plan rests on)

| Fact | Evidence |
|---|---|
| The packaged seed shipped content but **no inputs** *(at planning time; **fixed by DP-4** — `Samples/schema.local.v1r0.xsd` now ships)* | `app-data/Edam.App.Data`: `Templates` 23 files, `TextMaps` 8 files, `Samples` **empty** (then) |
| The starter template expects an input that could not exist yet *(**fixed by DP-4**: the sample input ships and is written into the project's `Files/` through the resources seam)* | `Templates/ToAssets.Args.json` → reads `./Files/schema.local.v1r0.xsd`, writes `./Documents/referenceData.xlsx`, references `./TextMaps/XsdTextMap.json` |
| Both halves already exist in the platform | `IProjectStore.CreateAsync` (structure, idempotent) and `IProjectSeeder.SeedArgumentsAsync` (content by address); the Studio's new-project flow already calls both |
| Creation was a **one-way door** *(**fixed by DP-2 delete**, 2026-09-28)* | no delete/remove-project affordance found anywhere in the Studio UI (then) |
| A container may be **shared** | LM-4 bindings: `filesystem` \| `postgres` \| `service` per collection — writing "the default container" blindly is not safe |

## The recommended shape (ADR-0012, proposed)

1. **Explicit empty state first** — the tree states there are no projects and offers to create one, with a template choice. No silent creation as the only answer to emptiness.
2. **A gated, one-time default project** created by the **host** (Studio), only when *all* gates hold: not created/offered before (once-per-installation **marker**, never a catalog item) · container **empty** · binding **local/file-system** (or allow-listed) · feature enabled (`Edam:Projects:CreateDefaultProject`, proposed **on for local, off otherwise**).
3. **Same seams as any project** (`IProjectStore` + `IProjectSeeder`) — no special-casing, so it doubles as a live proof the platform works.
4. **Never resurrects** — keyed off the marker, not off emptiness.
5. **Named by configuration** (`Edam:Projects:DefaultProjectName`), deliberately not "Default".
6. **Reports itself** to the diagnostics panel (created, or skipped *and why*).
7. **Content ships rather than being invented** — the default project's folders, starter arguments **and a small sample input** travel as **seed content** (ADR-0010) and are written **by address** (ADR-0011 decision 11).
8. **A way out is a prerequisite** — delete/rename (or an explicit "remove the project created for you") before any silent creation.

## Steps (all decided; DP-1…DP-4 delivered, DP-5 verified headlessly)

| Step | What | Why / acceptance | Depends on | Status |
|---|---|---|---|---|
| **DP-0** | **Decide D9** — name, local-only vs any binding, switch default, sample input, delete-first, created vs offered | everything below follows from this; recorded as a decision, then ADR-0012 moves to Accepted | — | **Done 2026-09-27** — D9 resolved; ADR-0012 *Accepted* |
| **DP-1** | **Empty state** in the project tree: "no projects yet" + create action + template choice | the safe half; no writes, no gating needed; makes the current dead end actionable | DP-0 (templates) | **Done 2026-09-28** — `ProjectSidePanelControl` + `ProjectViewerViewModel.EmptyStateVisibility`, reusing the panel's own add handler (committed 2026-09-30); the **template choice** belongs to DP-4 |
| **DP-2** | **Project delete/rename** in the UI | removes the one-way door; ADR-0012 decision 8 makes it a precondition for silent creation | — | **Half — delete Done 2026-09-28** (through `IProjectStore.DeleteAsync`, tree reloaded, DP-1's follow-up closed). **Rename NOT done: it is a contract change** — `IProjectStore` has no rename member, so both project stores need one first |
| **DP-3** | **The host policy**: gated, one-time default project — marker, container-empty check, local-binding check, config switch, diagnostics reporting | the feature itself; creation goes through the existing seams only | DP-0, DP-2 | **Done 2026-09-28** — verified end to end on the user's machine (`DefaultProjectService` + `IDefaultProjectHost`); the marker is the proof of the action |
| **DP-4** | **Content**: ship a small sample input in the seed; define the starter recipe (folders + starter arguments + input) as a documented address sequence | a project that looks complete and cannot run is worse than none; the recipe is reusable by CLIs and tests | DP-0 | **Done 2026-09-28** — `Edam:Projects:DefaultTemplate` (silent default) seeds the arguments; the sample XSD ships in `Samples/` and is written into `Files/` through `IProjectResources` |
| **DP-5** | **Verification**: the headless conformance group below + the Studio first-run check (user) | the only honest proof that the gates behave | DP-3, DP-4 | **Headless DONE 2026-10-08** — `default project (DP-5, file-system)` and `(DP-5, catalog local)` **ALL CONFORM (8 checks each)** on the real seams; the postgres target is wired (needs the database up). **Remaining: the Studio first-run check (user, F5)** |

## Verification (DP-5) — what the group proves

`Edam.Data.Projects.Conformance/DefaultProjectRunScenario.cs` runs the gated default project over the
**real targets** (the file-system providers and the Catalog-backed providers) through the **same seams the
Studio uses** — `IProjectCatalog`/`IProjectStore`/`IProjectResources`/`IProjectSeeder` — instead of the
substituted host of the DP-3 group. Each run is prepared from the **shipped repo seed**
(`app-data/Edam.App.Data`: the real `Templates/ToAssets.Args.json` and the real `Samples/schema.local.v1r0.xsd`),
which is a seed **source**, never a configured location. The eight checks:

1. **first run creates exactly ONE** default project (a local target, an empty collection, the switch on);
2. the created project has the **8 standard folders** (scaffolding is complete);
3. it is **SEEDED**: `/Arguments/<project>.<template>` holds the template's content;
4. **the point** — the seeded arguments' **own** `./Files/…` input reference resolves **inside the project**
   and the sample input is where that reference looks (so the project is runnable, not inert);
5. the project is an **ordinary project addressable as `/Projects/<name>`** — no drive path, no CWD;
6. the **action completes the answer**: the ask stops and a second attempt creates nothing;
7. it **never resurrects**: after a delete the marker keeps it deleted although the collection is empty again;
8. a collection that **already holds a project is refused, with its own reason** (a fresh answer state, so the
   marker cannot be the reason).

**Finding recorded by the run (not hidden).** `IDefaultProjectHost.SeedAsync(projectName, templateAddress)`
receives `DefaultProjectService.Template` — the registry's `Edam:Projects:DefaultTemplate` value, which is a
**location path** (`Templates/ToAssets.Args.json`), *not* a `catalog://` address. A host must resolve it: the
Studio takes its **file name** and resolves the well-known `app:templates` alias against the project's scope,
and the conformance host mirrors that (a full address is honoured as given). A host that took the parameter
literally as an address would **silently seed nothing** — which is exactly what the first DP-5 run did, and
what check 3 now catches. **Follow-up refinement (not done here, DP-5 is verification):** rename the
parameter to `templateLocation`, or have the service hand over a resolvable address/alias, so the name and
the value agree.


## Conformance (delivered — `default project (DP-3)` and `(DP-5)` groups)

The **`default project (DP-3)`** group proves the gates with a substituted host (16 checks); the
**`(DP-5, file-system)`** and **`(DP-5, catalog local)`** groups run the same flow over the **real seams**
(8 checks each — listed under *Verification (DP-5)* above). Between them, the sketch below is covered:

- **creates exactly one** default project when first-run + container empty + local binding + enabled → the
  **8 standard folders** exist **and** the starter arguments were seeded (`/Arguments/<name>.ToAssets.Args.json`
  with the template's content);
- **does nothing** — each with its own distinct reported reason — when: it is not the first run (the marker);
  the container already holds a project; the binding is **non-local**; the feature is disabled;
- **never resurrects**: run, delete the project, run again → absent;
- the created project is addressable as `/Projects/<name>` and read through `IProjectResources` with **no disk
  path and no CWD change** (the runner's global assertion still holds);
- the seeded arguments file resolves its `./Files/…` reference **inside the project**, per ADR-0011 decision 6
  — which is also the check that proves the sample input landed where the template looks for it.

## Open questions (D9) — RESOLVED 2026-09-27

1. **Name** — `Edam.Sample`, organization-first and dotted (`Edam` is the organization).
2. **Local-only** — **yes**: a file-system binding, or a catalog whose server is this machine (widened
   2026-09-28 to count a localhost/`.` server, since the user chose MS-SQL).
3. **Switch default** — on for a local target, off otherwise (`Edam:Projects:CreateDefaultProject`).
4. **Sample input** — **ships** (DP-4), so the starter project can actually run.
5. **Delete first** — **yes** (DP-2 delete, 2026-09-28); rename is still open (a contract change).
6. **Offer, not silently create** — the empty state asks; the answer creates it (DP-1/DP-3).

> **Plan + decision:** `../adr/0012-default-project-on-first-run.md` (**ADR-0012**, *Accepted*; builds on
> **ADR-0011** and **ADR-0010**). Model: a project is an ordinary project — a branch `/Projects/<name>` in a
> container — created through the same seams as any other, with content written by **address**; the *decision
> that it should exist* belongs to the host, not the platform.
