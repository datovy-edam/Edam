# BL-1.2 — Standardize "Notebook" vs "Booklet" naming across the codebase

| Field | Value |
|---|---|
| **ID** | BL-1.2 |
| **Area** | Area 1 — Notebook / Booklets for Schema Mapping |
| **Type** | Refactor / consistency |
| **Priority** | Low |
| **Effort** | S |
| **Status** | Deferred |

## Description

The concept is called "Notebook" in some places (csproj, legacy) and "Booklet" in the current code (`Edam.Data.Books`, `Controls\Booklets\`). Pick one canonical term (recommend **Booklet**) and sweep namespaces, folder names, csproj entries, and comments so there is no drift.

**Note:** Secondary to the Area 1 testing focus. Deferred until the verification work is underway; revisit after the test project is established.

## Acceptance Criteria

- [ ] One canonical term used consistently in code, folders, and csproj.
- [ ] No stale "Notebook" references remain (grep returns none).
- [ ] Build still succeeds.

## Dependencies

- BL-1.1

## Notes / Test Results

- Canonical term decision: **Booklet** (recommended).
