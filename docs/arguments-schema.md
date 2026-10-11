# Arguments-file schema and validation (`*.Args.json`)

> **Status:** Delivered (2026-10-11). **Source specification:** *EDAM Studio — Understanding Projects* §2.3, kept at
> `docs/reference/0010 - Edam.Studio.Understanding.Projects.v0.docx`. **Authority:** the live repository is the
> current authority (`AGENTS.md`); this document is the operator's guide to the mechanism.

## 1. What an arguments document is

Every process EDAM runs is described by one or more **`*.Args.json`** documents. The specification defines nine
steps; they are the sections of the document:

| Step | Section | Required? |
|---|---|---|
| 1 | `@context` — the JSON vocabulary ("always include") | **required** |
| 2 | `Domain` — the asset domain (`DomainId`); the application looks for it when the document is selected and adds the domain to the EDAM database when it is missing | recommended (reported as a warning) |
| 3 | `Namespace` — URI, prefix, root element | optional |
| 4 | `Project` — name + version; **the name must equal the project folder** | optional (see §3) |
| 5 | `Process` — the procedure to execute (`ProcedureName`) | **required** |
| 6 | `InputFile` / `OutputFile` — the default files | optional |
| 7 | `UriList` — where the input file(s) are, relative to the project | recommended |
| 8 | `InspectArguments` — how to inspect artifacts | *TBD in the specification* |
| 9 | `ConnectionString`, `ElementTransform`, `TextMapFilePath` | optional (*`ElementTransform` is TBD*) |

## 2. The schema, and where it is found

* **The schema is one file:** `app-data/Edam.App.Data/Templates/Arguments.schema.json` (draft-07). It **ships as
  seed content** — the same mechanism as the templates themselves — so every collection's `Templates/` receives
  it, and it is an **address**: `app:templates/Arguments.schema.json`.
* **The configuration entry** (`Edam:Arguments:Schema`, a registry item with a **silent default** — documented,
  overridable, never asked) states where to read it:

  ```jsonc
  "Edam": {
    "Arguments": {
      "Schema": "app:templates/Arguments.schema.json"
    }
  }
  ```

  Because the value is an address, the same entry works for a **file-system** project and for a **catalog**
  project alike (ADR-0011). A schema that cannot be found is **reported** — validation is never silently
  skipped.
* **Lenient by default.** Unknown properties are reported, not fatal, because the specification leaves
  `InspectArguments`/`ElementTransform` as TBD and its own `InspectArguments` values are *strings*. A CI gate can
  ask for the strict behaviour (unknown properties become errors).
* An invalid document is **reported and never blocks editing**.

## 3. Two layers, because a schema is not enough

A JSON schema describes shape. These rules come from the specification and need the project itself, so the
validator (`Edam.Data.Projects.DependencyInjection.ArgumentsValidator`) applies them beside the schema:

| Code | Rule | Severity |
|---|---|---|
| `shape` | the schema's own rules (types, required sections) | Error |
| `procedure-missing` / `procedure-unknown` | §2.3 step 5 — the name must be one of `Edam.Data.AssetConsole.AssetConsoleProcedure`'s values | Error |
| `project-name-mismatch` | §2.3 step 4 — *"the name must be the same as the Project folder"* | Error |
| `path-absolute` | a reference that is an absolute/drive path (it leaves the project *and* the container) | Error |
| `path-leaves-project` | a reference that climbs out with `..` (the address model clamps it, but it leaves the project) | Warning |
| `domain-missing` | §2.3 step 2 — no `DomainId`, so the domain cannot be identified/added | Warning |
| `inputs-missing` | no `UriList` and no `InputFile` — nothing to read | Warning |
| `unknown-property` | a property outside the contract | Warning (Error in strict mode) |
| `schema-unavailable` | no schema was found, so only the semantic rules ran | Information |

**Drift control:** the schema carries the `ProcedureName` enumeration, and the conformance suite asserts that
list **equals** the code's enumerator — so adding a procedure fails the checks until the schema is updated, and
the published contract cannot drift away from the runner in silence.

## 4. Where it runs

* **In the application:** opening an arguments document validates it and reports the findings to the
  **diagnostics panel** — *before* the document reaches the process/domain path, where a mistake used to surface
  far away (item 127). Editing is never blocked.
* **In the conformance suite:** the `arguments (§2.3)` group (12 checks) proves the schema is found, that it
  accepts **every document this repository ships** and the specification's own example, that the mistakes above
  are refused, that leniency holds, and that the enumeration matches the code.

  ```powershell
  src\Edam.Data.Projects\Edam.Data.Projects.Conformance\bin\Debug\net10.0\Edam.Data.Projects.Conformance.exe
  ```

* **Next (not built):** a `--validate` CLI gate a pipeline can run over a submitted arguments document.
