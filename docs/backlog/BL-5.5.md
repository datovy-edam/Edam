# BL-5.5 — Align EF Core versions + add migrations

| Field | Value |
|---|---|
| **ID** | BL-5.5 |
| **Area** | Area R — Repository hygiene (also v2.0 persistence) |
| **Type** | Dependency / data |
| **Priority** | Medium |
| **Effort** | L |
| **Status** | New |

## Description
Catalog uses EF Core `EnsureCreated()`; versions are skewed (EF Core 6.0.x vs .NET 9/10). Align EF Core to the runtime and introduce migrations (no `EnsureCreated()` for a governed store).

## Acceptance criteria
- [ ] EF Core version aligned with the .NET 10 target (do with/after BL-3.1).
- [ ] Migrations used instead of `EnsureCreated()`; schema versioned.

## Related
`docs/HANDOFF.md` §1.6 / §4.4 item 5; BL-3.1 (runtime), tech-stack persistence row.
