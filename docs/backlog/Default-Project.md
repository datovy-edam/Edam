# Area DP — First-run default project (exploration → plan)

> **Status:** **Exploration complete, decision pending (2026-09-27)** — **ADR-0012** is *Proposed*; the gating questions are tracked as **D9** in `Open-Decisions.md`. **Nothing is implemented and no code was written for this area.**

## Why this exists

The Studio now starts, logs in, resolves its databases, and the code editor works — and the project tree is **empty on a fresh installation**. Everything a first run is meant to demonstrate (open a document, edit it, run a process) needs a project, so the empty tree is where the product currently ends. The question explored here: should the application set up a default project for the user, and under what conditions?

## What was verified (facts this plan rests on)

| Fact | Evidence |
|---|---|
| The packaged seed ships content but **no inputs** | `app-data/Edam.App.Data`: `Templates` 23 files, `TextMaps` 8 files; `Samples`, `Files`, `Arguments`, `Documents` **empty** |
| The starter template expects an input that cannot exist yet | `Templates/ToAssets.Args.json` → reads `./Files/schema.local.v1r0.xsd`, writes `./Documents/referenceData.xlsx`, references `./TextMaps/XsdTextMap.json` |
| Both halves already exist in the platform | `IProjectStore.CreateAsync` (structure, idempotent) and `IProjectSeeder.SeedArgumentsAsync` (content by address); the Studio's new-project flow already calls both |
| Creation is currently a **one-way door** | no delete/remove-project affordance found anywhere in the Studio UI |
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

## Steps (proposed; none started)

| Step | What | Why / acceptance | Depends on |
|---|---|---|---|
| **DP-0** | **Decide D9** — name, local-only vs any binding, switch default, sample input, delete-first, created vs offered | everything below follows from this; recorded as a decision, then ADR-0012 moves to Accepted | — |
| **DP-1** | **Empty state** in the project tree: "no projects yet" + create action + template choice | the safe half; no writes, no gating needed; makes the current dead end actionable | DP-0 (templates) |
| **DP-2** | **Project delete/rename** in the UI | removes the one-way door; ADR-0012 decision 8 makes it a precondition for silent creation | — |
| **DP-3** | **The host policy**: gated, one-time default project — marker, container-empty check, local-binding check, config switch, diagnostics reporting | the feature itself; creation goes through the existing seams only | DP-0, DP-2 |
| **DP-4** | **Content**: ship a small sample input in the seed; define the starter recipe (folders + starter arguments + input) as a documented address sequence | a project that looks complete and cannot run is worse than none; the recipe is reusable by CLIs and tests | DP-0 |
| **DP-5** | **Verification**: the headless conformance group below + the Studio first-run check (user) | the only honest proof that the gates behave | DP-3, DP-4 |

## Conformance sketch (proposed — to be implemented after DP-0)

A new **`default project`** group in `Edam.Data.Projects.Conformance`, run over the **existing targets** (file-system **and** catalog), so the gates are proven without a UI:

- **creates exactly one** default project when first-run + container empty + local binding + enabled → assert the **8 standard folders** exist **and** the starter arguments were seeded (`/Arguments/<name>.ToAssets.Args.json` with the template's content);
- **does nothing** — each with its own distinct reported reason — when: it is not the first run; the container already holds a project; the binding is `service`/`postgres`; the feature is disabled;
- **never resurrects**: run, delete the project, run again → absent;
- the created project is addressable as `/Projects/<name>` and readable through `IProjectResources` with **no path and no CWD change** (the runner's existing global assertion still holds);
- the seeded arguments file resolves its `./Files/…` reference **inside the project**, per ADR-0011 decision 6 — which is also the check that proves the sample input landed where the template looks for it.

## Open questions (D9)

1. Default project **name/prefix** (and should it carry the organization's name)?
2. **Local-only** (recommended) or allowed for any binding?
3. Switch default **on** or **off**?
4. Ship a **sample input** so the default project runs (recommended), or keep it structure + arguments and accept that it is inert?
5. Add **delete/rename** first (recommended) or accept a one-way door?
6. **Create** it, or **offer** it in the empty state (one click, clearly explained)?

> **Plan + decision:** `../adr/0012-default-project-on-first-run.md` (**ADR-0012**, proposed; builds on **ADR-0011** and **ADR-0010**). Model: a project is an ordinary project — a branch `/Projects/<name>` in a container — created through the same seams as any other, with content written by **address**; the *decision that it should exist* belongs to the host, not the platform.
