# BL-5.7 — Dependency vulnerability cleanup (NuGet Audit findings)

| Field | Value |
|---|---|
| **ID** | BL-5.7 |
| **Area** | Area R — Repository hygiene (security) |
| **Type** | Dependency / security |
| **Priority** | **High** |
| **Effort** | M |
| **Status** | **In Progress** — 5 of 6 advisories cleared 2026-09-18; 1 blocked offline |

## Description

`Edam.Studio.sln` pulled **vulnerable transitive packages** through the `Edam.*` feed packages and
EF Core 6.x, so NuGet Audit (`NU1901`–`NU1904`) reported them on every restore. BL-3.1 recorded the
findings and deferred them ("track cleanup via a security/dependency-hygiene backlog item"); this is
that item.

The fix pattern is a **direct `PackageReference` at the fixed version** in the consuming projects,
which overrides the vulnerable transitive resolution.

## Resolved — 2026-09-18 (verified by re-audit **and** the resolved dependency graph)

| Package | Was | Advisory | Now |
|---|---|---|---|
| `Microsoft.Data.SqlClient` | 2.1.4 | **high** GHSA-98g6-xh36-x2p7 | **5.1.6** |
| `Microsoft.Extensions.Caching.Memory` | 6.0.1 | **high** GHSA-qj66-m88j-hmgj | **10.0.0** |
| `System.Drawing.Common` | 4.7.0 | **critical** GHSA-rxg9-xrhp-64gj | **6.0.0** |
| `System.IdentityModel.Tokens.Jwt` | 6.8.0 | moderate GHSA-59j7-ghrg-fj52 | **8.19.2** |
| `Microsoft.IdentityModel.JsonWebTokens` | 6.8.0 | moderate GHSA-59j7-ghrg-fj52 | **8.19.2** |

Also brought to the **latest available offline**: `Newtonsoft.Json` 13.0.3→**13.0.4**,
`Microsoft.Windows.SDK.BuildTools` 10.0.22621.756→**10.0.26100.1742**,
`Microsoft.Extensions.DependencyInjection` / `Microsoft.Extensions.Logging.Abstractions`
10.0.0→**10.0.11**, `CommunityToolkit.Mvvm` 7.1.2→**8.4.0**, `CommunityToolkit.Common` 7.1.2→**8.2.1**.

## Acceptance criteria

- [x] The five fixable advisories no longer appear in the audit — only the SQLite item remains.
- [x] Security pins present in the consuming projects (`Edam.Studio`, `Edam.WinUI.Controls`, `Edam.Test.Studio`).
- [x] `Edam.Studio.sln` builds **0 error**; `Edam.slnx` builds **0 error**.
- [ ] **`SQLitePCLRaw.lib.e_sqlite3` ≥ 2.1.0** — **blocked**: the fixed version is not in the offline
      cache. Needs nuget.org, or the feed owner republishing. Remedy: bump `sqlite-net-pcl`
      (1.8.116 → a release built on SQLitePCLRaw 2.1.x) or pin `SQLitePCLRaw.bundle_green` ≥ 2.1.0.
      The reference **cannot** simply be dropped: `Edam.UI.DataModel` genuinely uses SQLite
      (`using SQLite`, `SQLite.CreateTableResult`).
- [ ] **Runtime validation** of `Microsoft.Data.SqlClient` **5.1.6** under **EF Core 6.0.25** (EF 6
      was compiled against SqlClient 2.x) — compile-verified only in this environment; the app's DB
      paths must be exercised. Recorded as a risk in `HANDOFF` §5.5.
- [ ] **Complete audit on a networked machine**:
      `dotnet list package --vulnerable --include-transitive` — several restores reported `NU1900`
      (vulnerability data could not be fetched offline), so the offline audit is a **lower bound**.

## Not "latest" yet — deliberate, with the reason

| Package | Now | Newest known | Why not now |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore` (+`.SqlServer`) | 6.0.25 | 9.0.2 cached / 10.x online | EF 6→9/10 is a **breaking migration**; the `Edam.*` feed packages are compiled against EF 6. Needs a deliberate, runtime-validated upgrade — that is **BL-5.5** |
| `Microsoft.WindowsAppSDK` | 1.6.250205002 | 1.7 / 1.8 online | newer version **not in the offline cache** |
| `DocumentFormat.OpenXml` | 2.19.0 | 3.x online | newer version **not cached** |
| `pythonnet` | 3.0.3 | newer online | **not cached** |
| `MSTest.TestAdapter` / `.TestFramework` | 3.6.4 | newer online | **not cached** |
| `CommunityToolkit.WinUI.UI.Controls` | 7.1.2 | — | package line **discontinued** (superseded by `CommunityToolkit.WinUI.Controls.*`); needs a coordinated migration |
| `System.Data.SqlClient` | 4.8.6 | — | legacy package (not flagged vulnerable); successor is `Microsoft.Data.SqlClient` |

## Related

BL-3.1 (findings first recorded + deferred), BL-5.5 (EF Core alignment + migrations),
BL-4.8 (security tooling / SAST / SBOM), `docs/HANDOFF.md` item 42.
