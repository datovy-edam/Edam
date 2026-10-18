# ADR-0015 — Component identity by convention (today: text-map files)

- **Status:** Accepted (2026-10-11) — sprint S3, item S3-3.
- **Context:** the application must **discover** the artifacts it uses (text maps now; other families later) without
  hardcoding file names and without asking a person to know them. Earlier components were named by hand, so
  "which file is the XSD text map?" lived in people's heads and in scattered literals; a file that did not fit was
  silently unusable, which is indistinguishable from a missing file.
- **Decision:**
  1. **A discoverable artifact is identified by its file name, by convention**, in the shape
     `<Identity><Family>[.<Purpose>].<extension>` — the identity says *what it serves*, the family says *what it
     is*, the optional dotted purpose qualifies the support.
  2. **Today's instance — text maps:** `<TypeId>TextMap[.<Purpose>].json`, where `TypeId` ∈
     **{Xsd, Jsd, Sql, Json, JsonLd, Gql}** (the set grows by adding one entry; comparison is case-insensitive and
     the canonical spelling is what the code returns), the literal **`TextMap`** sits in the middle, the extension
     is `.json`, and **the dotted remainder is the purpose verbatim** (`XsdTextMap.HL7.Fhir.json` → `HL7.Fhir`) with
     **every dotted segment also matching as a tag** so `fhir` finds it.
  3. **One parser owns the rule** (`TextMapConvention`), and **every refusal carries its reason** — no `TextMap`
     marker, an unknown type identifier, a wrong extension, or the marker not at the start. A rejected file and a
     missing file must never look the same.
  4. **Resolution is by KIND, never by name.** A person knows they are processing an XSD, a JSON schema, JSON-LD,
     GraphQL or SQL. The resolver (`ITextMapCatalog`) declares **`SupportedKinds`**, and a surface offers only
     what that capability allows.
  5. **Nothing is hardcoded or pinned:** a component presented at project initialization *is* the one the
     container returns (`ProjectServices` registers a default; the UI never names a provider).
- **Consequences:**
  - Adding a family or a type identifier is a **one-line change in the parser**, and every surface follows because
    they all ask it.
  - A misnamed file is **ignored with a stated reason** rather than silently unused.
  - The convention is a *claim* about the artifact, so it is stated in the diagnostics and proven by a conformance
    group over accepted/rejected names (S3-3's remaining work).
  - `TextMapConvention` must remain the **only** place the naming rule lives; a second regex anywhere else would
    re-create the drift this ADR exists to prevent.
- **Verification:** the parser is exercised by the `text maps` conformance group (accepted names incl. the dotted
  purpose, and each rejection case); `Edam.Data.Projects.slnx` builds 0 errors.
- **Supersedes / relates:** ADR-0006 (interface purity), ADR-0007 (provider-agnostic catalog), ADR-0011 (addresses),
  ADR-0013 (a stated entry beats a silent fallback).
