# Sprint S3 — Arguments documents, text-map resolution, and the explorer that stays put

> **Status:** **In progress (2026-10-11)** — **S3-1 delivered** (build-verified; the user's runtime pass pending);
> **S3-2…S3-6 planned**.
> **Authority:** the rulings in `docs/HANDOFF.md` item 134 (and item 133's lesson) are the requirement for this
> sprint; the Projects source specification is `docs/reference/0010 - Edam.Studio.Understanding.Projects.v0.docx`
> (§2.1 locations, §2.3 arguments document). Per `AGENTS.md` §2 the live repository is the authority; this plan is
> an aid.
> **Carry-over context:** the arguments-file schema and validation (items 127–133) are delivered;
> `docs/arguments-schema.md` describes them.

## Goal

Make creating and running an arguments document **comfortable and predictable**: the explorer keeps the person's
place, the document names a **real text map** chosen by the kind of schema being processed (never by file name), and
the map files themselves are **identified by one convention** that one parser owns and that says why it ignored a
file.

## Scope

### S3-1 — The explorer updates in place (no reload, no lost place)

**Why:** creating an arguments file reloaded the tree, which collapsed it and forced the person to navigate back.
**Do:** insert the new node into the `Arguments` folder's own `Children` collection and select it; keep the reload
only as a fallback, and when it is used restore the expanded path and the selection.
**Done when:** creating a document leaves every other node exactly as it was, the new node is selected, and the
editor follows it.

**State (2026-10-11):** delivered, build-verified. The node is built with the tree's own factory
(`ProjectItem.GetProjectItem`), appended to the folder's bound `Children` (only that folder updates) and selected
(`TreeView.SelectedItem` — the sprint's one open question, settled: the setter exists). The reload is kept **only**
as a stated fallback when the in-place insert throws. **User's F5:** the folder must stay expanded and the editor
follow the selection.

### S3-2 — `ITextMapCatalog`: resolution by schema kind, injected, never hardcoded

**Why:** a person knows the *kind* of schema they process (XSD, JSON schema, JSON-LD, GraphQL, SQL), not the name of
a map file — and no component may be hardcoded.
**Do:** in `Edam.Data.Projects.Contracts`, define `ITextMapCatalog` (`ListAsync(project)`,
`ResolveAsync(project, kind)`, and **`SupportedKinds`** as the capability the UI may offer), with
`TextMapOption { TypeId, Kind, Purpose, Name, Address, Description }` and
`TextMapKind { Xsd, Json, JsonLd, Gql, Sql, Other }`. Register a **default provider** in the projects composition
root that searches the project's **`./TextMaps` first**, then the app-data **`TextMaps`**
(`app:textmaps` — an existing declared location, so it serves file-system *and* catalog collections). A component
supplied at project initialization is simply the one used; every discovery decision is **reported to the
diagnostics**.
**Done when:** the resolver is reachable through DI, its `SupportedKinds` drives what the UI offers, and a replaced
implementation changes behaviour with no UI change.

### S3-3 — Map-file identity by convention, with reasons for every rejection

**Why:** a convention that is only in people's heads cannot be relied on, and silence about a rejected file is
indistinguishable from a missing one.
**Do:** one parser owns `<TypeId>TextMap[.<Purpose>].json`:
`TypeId` ∈ **{Xsd, Jsd, Sql, Json, JsonLd, Gql}** (case-insensitive; canonical spelling displayed; the set grows by a
one-line change), the literal `TextMap` in the middle, an optional **dotted Purpose whose whole remainder is the
purpose** (`XsdTextMap.HL7.Fhir.json` → `HL7.Fhir`, with each dotted segment *also* matched as a tag), and the
extension `.json`. Anything else is **ignored with its reason** (no marker · unknown type id · wrong extension ·
marker not at the start). Record the convention as a short **ADR** ("component identity by convention") and a
documentation section, since the next discoverable family reuses the shape.
**Done when:** the parser is the only place the rule lives, and every rejection appears in the diagnostics with the
rule it broke.

### S3-4 — Creating a document asks the KIND, and the document ends up concrete

**Why:** the skeleton shipped in item 132 asserts XSD (extension, procedure and map) — three assertions the person
never made.
**Do:** the create flow asks the schema kind, derives `InputFile.Extension`, a compatible `ProcedureName` and the
text map from it; the chosen default map is **copied into the project's `TextMaps/`** and referenced as
`./TextMaps/<name>` (the project stays self-contained, which the catalog target needs); when nothing fits, the field
is left **empty** and Validate names it. The dialog's summary states what was chosen and why.
**Done when:** a new document created for XSD, JSON schema, JSON-LD or GQL validates with only the placeholders the
person still owns — no fabricated path, no assumed procedure.

### S3-5 — Conformance proof for all of it

**Do:** a `text maps` group in `Edam.Data.Projects.Conformance`: accepted/rejected names (including the dotted
purpose and the marker-not-at-start case), resolution over a temporary collection (project map wins over the app
one), the copy-into-project behaviour, and a **DI replacement** proving the seam is honoured. Plus the existing
`arguments (§2.3)` group stays green with a skeleton created through the same code path.
**Done when:** the whole suite is `ALL CONFORM` and the new group fails if any rule above regresses.

### S3-6 — The banner's Save button (small, already found)

**Why:** item 131's recorded finding — `ProjectFileEditorControl.SaveText()` reads the editor text and its
`ItemSave(...)` call is **commented out**, so saving works only through the editor's own Ctrl-S. A button that looks
like Save and does nothing is the same defect class as item 133.
**Done when:** the banner's Save writes the document and reports it like every other command.

## Definition of done (sprint)

- `Edam.Studio.sln` (x64), `Edam.Data.Projects.slnx`, `Edam.Data.Catalog.slnx`, `Edam.slnx` build **0 errors**.
- The projects conformance suite is **ALL CONFORM**, including the new `text maps` group.
- The ADR and the documentation section for the convention exist and are reachable from `docs/CONTEXT.md`.
- `docs/HANDOFF.md` carries a numbered item per delivered item, with what was verified and what is the user's F5.
- The user's runtime evidence (create a document for two different kinds; the tree does not move) is recorded.

## Explicitly OUT of scope (deferred, stated)

- The **runner** resolving its text map through `ITextMapCatalog` (the same seam, next sprint — a run must not need a
  remembered file name either).
- The `--validate` CLI/CI gate over a submitted arguments document.
- The registry-row check in `tools/verify-environment.ps1` (a candidate gate: the `fk_DataDomainType` class has been
  met twice).
- The asset-definition path passing an **asset** type where the domain FK expects a **domain** type (item 127's
  residual; revisit once the registry rows are confirmed loaded in `Edam.Database`).
- Repacking `Edam.DataObjects` so the improved failure message (item 127) reaches the application.

## Risks

- **Kind ↔ procedure coupling** is a claim about behaviour; if a kind maps to a procedure that cannot handle it, the
  skeleton is confidently wrong. Mitigation: derive from the shipped templates and the `AssetConsoleProcedure`
  values, and let the conformance group assert every offered pair is a real procedure.
- **`app:textmaps` may not exist** in a given installation (seed not deployed). Mitigation: search order plus an
  explicit diagnostic, never a silent empty field.
