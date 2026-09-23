# BL-5.5 — Align EF Core versions + add migrations

| Field | Value |
|---|---|
| **ID** | BL-5.5 |
| **Area** | Area R — Repository hygiene (also v2.0 persistence) |
| **Type** | Dependency / data |
| **Priority** | Medium |
| **Effort** | L |
| **Status** | **In Progress** — version alignment done 2026-09-18; migrations + feed republish pending |

## Description

Catalog uses EF Core `EnsureCreated()`; versions are skewed (EF Core 6.0.x vs .NET 9/10). Align EF Core to the runtime and introduce migrations (no `EnsureCreated()` for a governed store).

## Alignment done — 2026-09-18 (EF Core 6.0.25 → **9.0.2**, every EF-referencing project)

| Project | Was | Now |
|---|---|---|
| `Edam.Data.Lexicon` (`Edam.Data.Vocabulary`) | EFCore + Abstractions + Relational + SqlServer 6.0.25 | **9.0.2** |
| `Edam.Data.Dictionary` | EFCore + SqlServer 6.0.25 | **9.0.2** |
| `Edam.Test.Dictionary` | EFCore + SqlServer 6.0.25 | **9.0.2** |
| `Edam.Studio` | EFCore.SqlServer 6.0.25 | **9.0.2** |
| `Edam.WinUI.Controls` | EFCore 6.0.25 | **9.0.2** |

**Why 9.0.2 and not 10.x:** EF Core 10's **SqlServer provider is not in the offline cache** (only 6.0.25 and 9.0.2 are) and core/provider must match versions, so 9.0.2 is the newest **coherent** set available offline. EF 10 needs network access.
**Scope note:** the Catalog platform is now **EF-free** (BL-7.2), so this alignment serves the **Lexicon/Dictionary** stores plus the two consumer projects — not the catalog.

## Verification (2026-09-18)

- `Edam.Data.Lexicon`, `Edam.Data.Dictionary` and `Edam.Test.Dictionary` build **0 error** against EF 9 — the packages that *genuinely use* EF **compile**, which is the real migration signal. (The Studio solution's own code uses **no** EF APIs: 0 hits for `DbContext`/`DbSet`/`EnsureCreated`/`ModelBuilder`/`Database.Migrate` across all five projects.)
- `Edam.Studio.sln` **0 error** (15 warnings); `Edam.slnx` 0 error; the projects conformance is unaffected (**ALL CONFORM**).
- Resolved graph confirms EF **9.0.2** (core / SqlServer / Relational / Abstractions) together with `Microsoft.Data.SqlClient` **5.1.6** (EF 9's own requirement — consistent with the BL-5.7 pin) and `Microsoft.Extensions.Caching.Memory` 10.0.0.
- NuGet Audit unchanged: only the `SQLitePCLRaw.lib.e_sqlite3` item remains (BL-5.7) — **EF 9.0.2 introduces no new advisory**.

## Acceptance criteria

- [x] EF Core version aligned with the .NET 10 target (6.0.25 → 9.0.2) in every EF-referencing project.
- [ ] **Feed republish required (delivery step — user):** `Edam.Data.Lexicon` / `Edam.Data.Dictionary` still pack as `Version 1.0.0`, so republishing would produce the **same** version and NuGet would keep serving the cached **EF-6-compiled** assemblies. Completion therefore needs a **SemVer bump** (e.g. 1.1.0 — Engineering Standards **E6** package hygiene), consumer reference updates, and a cache clear. Until then the app runs **EF 9 assemblies against EF-6-compiled package code** — the residual risk.
- [ ] **Runtime validation:** exercise the Lexicon/Dictionary database paths. The packages now compile against 9.0.2, but this environment cannot run them against a live database.
- [ ] Migrations used instead of `EnsureCreated()`; schema versioned. (The user also **deferred Postgres schema versioning for the Catalog** — revisit alongside that.)

## Related

`docs/HANDOFF.md` §1.6 / §4.4 item 5 and item 43; BL-3.1 (runtime move), BL-5.7 (dependency security),
BL-7.2 (catalog EF-independence), Engineering Standards **E6** (package hygiene / SemVer).
