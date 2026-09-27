# EDAM — Sprint S2 plan: configurable items & first-run setup

> **Status:** **Proposed next-sprint plan (2026-09-27) — not started; no code written.** Per `docs/backlog/README.md`, items live in files by ID; this file groups the work and its sequencing. **Authority:** approved requirements + AGENTS.md remain the current authority; this is a planning aid.

## Sprint S2 — "Ask once, explain, remember: unset configuration + a first-run that works"

**Primary goal:** make the values the application *cannot invent* resolvable **with the user in the loop** — informed, validated, asked once, and never guessed silently — and land the first instance of it: a **default project** offered as **`Edam.Sample`** (organization = **Edam**).

**Decisions this sprint implements (already made):** **ADR-0013** (unset configuration: registry, per-item policy, batched ask, user-state overlay, validation at ask, diagnostics audit) and **ADR-0012** (first-run default project: empty state first; a gated, one-time project created by the host through the existing seams; `Edam.Sample`; local binding only; never resurrecting after a delete; content shipped as seed content).

### Priority order

1. **CF-1 — Settings reader states `unset` / `set` / `invalid`** — extend LM-3's `ProjectSettings` so "not configured" is distinguishable from "configured to empty" (the `AssetConsolePath` lesson), and so an invalid value is reported rather than replaced. *Headless; do first — everything else depends on it.*
2. **CF-2 — The configurable-items registry** — one definition per askable item: id, one-line "what this is", storage file+key, **policy** (`silent default` / `ask once when unset` / `required`), default, validator. First entries: the default project name (`Edam.Sample`) and the once-per-installation marker. *Headless.*
3. **CF-3 — The user-state overlay** — a per-user app-data file for answers (kept out of the packaged seed, ADR-0010), plus packaged `LocalSettings` for the once-per-install marker; read/write/reset semantics and a documented lifecycle (upgrade, reset, multi-user). *Headless.*
4. **CF-4 — One ask surface, generated from the registry** — a **skippable** first-run setup surface shown **after the shell is up** (never during `OnLaunched`), plus **lazy** ask-at-first-need for anything discovered later. No item hardcoded in the UI. *UI; user-verified.*
5. **CF-5 — Name validation, reused in the dialog** — the path-safety rules from ADR-0013 decision 7 (character allow-list; no leading/trailing dot or space; reserved device names rejected; **no `.`/`..` segments**; length cap; case-insensitive uniqueness): a name must not be able to escape `/Projects`, and the New-Project dialog must reject one at entry. *Headless + UI.*
6. **DP-1 — Empty state in the project tree** — "no projects yet" plus a create action and a template choice. *Safe half: no writes, no gates. UI; user-verified.*
7. **DP-2 — Project delete / rename** — the **precondition** for creating anything on the user's behalf (creation is otherwise a one-way door). *UI; user-verified.*
8. **DP-3 — The gated, one-time default project** — offered as **`Edam.Sample`**; gates: asked/answered once (marker) · container **empty** · binding **local/file-system** · switch `Edam:Projects:CreateDefaultProject` (on for local, off otherwise); created through the **existing seams** (`IProjectStore` + `IProjectSeeder`, no special-casing); **never resurrects**; reports itself to the diagnostics panel. *Depends on CF-3/CF-4 and DP-2.*
9. **DP-4 — Content that can actually run** — ship a small sample input in the packaged seed and define the starter recipe (folders + starter arguments + input) as an address sequence, so the sample project is not structurally complete and inert. *Content + docs.*
10. **DP-5 — Verification** — the headless `default project` conformance group (creates exactly one when every gate holds; does nothing with a distinct reason per gate; never resurrects; addressable as `/Projects/<name>` with no path and no CWD; the seeded arguments resolve `./Files/…` inside the project) plus the Studio first-run check (user). *Headless + user.*

### Sequencing note

**CF-1 → CF-2 → CF-3** are headless and verifiable here; **CF-4, CF-5 (UI half), DP-1, DP-2** are UI and user-verified; **DP-3** must follow **DP-2** (no silent creation while there is no way out). Nothing in this sprint writes into the packaged seed at runtime.

### Sprint gates (Definition of Sprint-complete)

- Build **0 errors** across the affected solutions; the existing headless conformance suites stay green, plus the new `default project` group (and the CF checks) **ALL CONFORM**.
- The **first-run surface is skippable** and never blocks startup; deferring leaves a documented fallback and a diagnostic entry, never a silent guess.
- `unset` / `set-to-empty` / `invalid` are distinguished and reported distinctly; an item is asked **once** and never again; an invalid answer is rejected where it is entered.
- The default project is created **only** through the existing seams, is **never resurrected** after a delete, and is **not** created when the container is not empty or the binding is not local.
- ADR-0012 and ADR-0013 recorded; `docs/HANDOFF.md`, the backlog index, `Open-Decisions.md` and the Area CF/DP docs current.
- **D9** resolved (recorded) — name `Edam.Sample`, organization `Edam`, local-only gating, user-state overlay for answers, `LocalSettings` for the marker, delete-before-create, offer/ask rather than silent create.

### Carried / next

- **Area LM** leftovers: the rest of LM-6 (the deprecated static pipeline surface needs a user-side validation; the 12 duplicated settings copies) and LM-7 (delete the legacy keys) — unchanged, and CF-1/CF-2 make LM-7's deletion safer because "unset" becomes visible.
- **Area DP** items not in this sprint: nothing — DP-1…DP-5 are all here, sequenced behind CF-1…CF-4.
- Future configurable items (the reason this mechanism exists) are added as **registry entries**, not as new dialogs.
