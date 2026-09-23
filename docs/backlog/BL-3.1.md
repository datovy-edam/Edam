# BL-3.1 — Upgrade runtime to .NET 10 (move off .NET 9)

| Field | Value |
|---|---|
| **ID** | BL-3.1 |
| **Area** | Area P (Platform / Runtime) |
| **Type** | Platform / dependency upgrade (priority — next sprint, ASAP) |
| **Priority** | **Critical** |
| **Effort** | L |
| **Status** | **Complete — verified 2026-09-13** (all target solutions build 0-error on .NET 10; headless suite green: **26 pass / 2 skip**) |

## Description

**.NET 9 is an STS release nearing/at its end-of-support window. .NET 10 is the current LTS release** (`net10.0`). Move the entire EDAM codebase off .NET 9 and onto **.NET 10 (latest greatest supported)**, including the WinUI/target-platform TFM and the test project. The build machine already has the **.NET 10 SDK (10.0.401)**, so the move is reachable now.

## Why now (enterprise posture)

- Running on an out-of-support (or about-to-expire) STS runtime is an enterprise risk (no patches/security fixes).
- Aligns with the enterprise-grade pillar (ADRs 0002/0003) and the honesty discipline — a supported LTS is the correct baseline.
- Doing it now, before v2.0 services (Aspire/API/MCP) are added, avoids building new components on an aging runtime.

## Target framework changes

| Project | From | To |
| --- | --- | --- |
| `Edam.Libraries` (plain libs) | `net9.0` | `net10.0` |
| `Edam.Studio` + `Edam.WinUI.Controls` | `net9.0-windows10.0.19041.0` | `net10.0-windows10.0.<WinSDK>` |
| `Edam.Data.Catalog.WinUI` | `net9.0-windows…` | `net10.0-windows…` |
| `Edam.Test.Studio` | `net9.0-windows10.0.26100.0` | `net10.0-windows10.0.26100.0` |

(Target the installed Windows SDK — currently **10.0.26100.0** — for `-windows` TFMs.)

## Acceptance criteria

- [x] All solutions build **0 errors** on .NET 10 (Edam.Libraries, Edam.Studio, Edam.Data.Catalog.WinUI, Edam.Test.Studio).
- [x] Full headless test suite runs green on .NET 10 (baseline 26 passing; re-verified — 28 total / 26 pass / 2 skip).
- [x] No behavioral/API regressions in the mapping/booklet/catalog code under the new runtime.
- [x] Docs reflect the move: `HANDOFF`, standards baseline, and `EDAM-v2.0-Tech-Stack.md` (no stale `.NET 9` claims).
- [x] Package consumers (Edam.Data.Assets 1.0.1 etc.) still restore/build; any package retarget bumped via SemVer + local-feed republish.

## Known risks / issues to watch (documented from this machine)

- **NETSDK1083** — the .NET 10 SDK rejects Windows App SDK `win10-*` RIDs unless the `RuntimeIdentifier`/`RuntimeIdentifiers` are pinned on the MSBuild command line for WinUI projects (already solved pattern for `Edam.Test.Studio` — reuse it).
- **Windows App SDK 1.2 (2023-era)** compatibility with .NET 10 — may require a Windows App SDK update.
- **EF Core 6.0.25** is old relative to .NET 10 — version skew is an existing risk (recorded in HANDOFF §1.6) and should be resolved here.
- **Windows SDK 10.0.26100.0** is the only one installed — target that for `-windows` TFMs.
- `CommunityToolkit.Mvvm 7.1.2` and Newtonsoft.Json 13.0.3 are expected to be compatible (verify).

## Dependencies

- Confirm .NET 10 SDK + Windows SDK compatibility on the build machine first.
- Existing build-blocker fixes (RID pin, `AppxGeneratePriEnabled=false`) must be carried into the .NET 10 build.
- BL-3.2 (verify WinAppSDK/EF Core versions under .NET 10) if required.

## Notes / Test Results

- Created as the **next-sprint, ASAP** priority per direct request. Effort is **L** because it is cross-cutting (every target framework in the solution set) but structurally straightforward.

### Executed (spike validated, 2026-09-10)
- **Toolchain confirmed:** only .NET SDK **10.0.401** + Windows SDK **10.0.26100.0** are installed — exactly what the .NET 10 move needs (no .NET 9 SDK present).
- **Library tier retargeted to `net10.0`:** 28 `Edam.Libraries` plain-net projects + `Edam.System` changed (`<TargetFramework>net9.0</TargetFramework>` → `net10.0`). The `net6.0` legacy projects and the `net9.0-windows7.0` test project were intentionally left unchanged.
- **Build verified on net10.0 (0 errors, offline/cached):** `Edam.System` (leaf) and `Edam.Security` (depends on `Edam.System`) both compile clean. Non-blocking warnings only (`SYSLIB0021/0060` obsolete crypto in `Encryptor.cs`, `CS8981` lowercase type names, `NU1900` nuget.org unreachable).

### Executed (republish + consumer retarget, 2026-09-13)
- **Feed write unblocked by user approval ("go"):** `c:\nugetlocalfeed` writes allowed; **all 21 pack-enabled `Edam.Libraries` net10 libraries rebuilt + republished** to the feed (`pass=21 fail=0`), confirmed by `dotnet build` (GeneratePackageOnBuild). `Edam.System.1.0.0.nupkg` recreated as net10.
- **Consumers/test projects retargeted to net10** (`net9.0-windows10.0.x`→`net10.0-windows10.0.26100.0`, plain `net9.0`→`net10.0`): `Edam.Studio`, `Edam.Test.Studio`, `Edam.WinUI.Controls`, `Edam.UI*`, `CommunityToolkit.WinUI.Controls.Sizers`, `Edam.CatalogExplorer`, `Edam.Data.Catalog{Db,Model,Service,ServiceClient}`, `Edam.UI.CatalogExplorer`, `Monaco`, `Edam.Test.{Monaco,TestBuilder,TestCatalogLibrary}` (16 projects). The `net9.0-windows7.0` `Edam.Test.Identity` left unchanged.

### Sequenced WinAppSDK fix (2026-09-13) — unblocks consumer build under .NET 10
After the SDK was repaired, `Edam.Test.Studio` failed `NETSDK1083` (RID not recognized) for `win10-*` RIDs. **Root cause:** `Microsoft.WindowsAppSDK` **1.2** injects `win10-*` RIDs in `MrtCore.PriGen.targets`, which .NET 10 removed. **Fix applied:** bumped the four `Edam.Studio` WinUI projects (`Edam.WinUI.Controls`, `Edam.UI`, `Edam.UI.DataModel`, `Edam.Studio`) to **`Microsoft.WindowsAppSDK` 1.6.250205002** (already the version the `Edam.Data.Catalog.WinUI` set uses; uses `win-*` RIDs for net8+). Local sandbox could not fully restore (no outbound network for nuget.org), so the build/test runs here (see below). Also noted: restore emits **NU1902** — `System.IdentityModel.Tokens.Jwt` 6.8.0 has a known moderate vulnerability (GHSA-59j7-ghrg-fj52); defer the package bump to the security/dependency hygiene pass.

**Follow-up (2026-09-13):** After the 1.6 bump, restore reported `NU1605` downgrade — WindowsAppSDK 1.6 requires `Microsoft.Windows.SDK.BuildTools >= 10.0.22621.756` but the four Studio projects pinned `.755`. **Fixed:** all four now reference `10.0.22621.756` (Catalog set already on `10.0.26100.1742`). NuGet Audit also flags these **known-vulnerability warnings (non-blocking, defer to dependency-hygiene/security pass)** across the WinUI projects: `SQLitePCLRaw.lib.e_sqlite3` 2.0.4 (high, GHSA-2m69-gcr7-jv3q), `Microsoft.Data.SqlClient` 2.1.4 (high, GHSA-98g6-xh36-x2p7), `Microsoft.Extensions.Caching.Memory` 6.0.1 (high, GHSA-qj66-m88j-hmgj), `System.Drawing.Common` 4.7.0 (critical, GHSA-rxg9-xrhp-64gj), `Microsoft.IdentityModel.JsonWebTokens`/`System.IdentityModel.Tokens.Jwt` 6.8.0 (moderate, GHSA-59j7-ghrg-fj52). Track cleanup via a security/dependency-hygiene backlog item.

### Verified 2026-09-13 — final consumer + headless test run (build 0-err, suite green)
- **Blockers cleared (SDK + dependencies):** user repaired the SDK (`dotnet-install.ps1` → 10.0.401, restored **network** + nuget.org) → `MSB4276` gone. WinAppSDK **1.2 → 1.6.250205002** fixed `NETSDK1083` (`win10-*` RIDs removed in .NET 10; 1.6 emits `win-*` for net8+). `Microsoft.Windows.SDK.BuildTools` `.755→.756` fixed `NU1605` (WinAppSDK 1.6 needs ≥ 756). WinUI library projects (`Edam.UI`, `Edam.UI.DataModel`, `Edam.WinUI.Controls`) set `AppxGeneratePriEnabled=false` + `AppxGeneratePrisForPortableLibrariesEnabled=false` + `WindowsPackageType=None` to bypass VS-only MSIX/PRI tasks under bare `dotnet`; `Common/Colour.cs` stale `using Microsoft.UI.Xaml.Core` removed (1.6 API change).
- **`Edam.Test.Studio` headless suite: PASS — 28 total / 26 passed / 2 skipped** (the two `AddControl` tests skip without a live WinUI host). Test-project props `SelfContained=false` + `CopyLocalLockFileAssemblies=true` so the framework host + all NuGet package assemblies land in the test output (was the cause of 26 runtime `FileNotFoundException`s).
- **Runner note (.NET 10):** `dotnet test` fails with **MSB4057 (target VSTest does not exist)** for this WinUI+MSTest host (classic-adapter vs .NET 10 Microsoft-Testing-Platform transition). Working runner: **VS `vstest.console`** against `bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\Edam.Test.Studio.dll /TestAdapterPath:<out>`, with `testhost.runtimeconfig.json` + VS TestPlatform + MSTest framework/adapter assemblies staged in that folder. **Deferred (test-infra):** automate the staging via a csproj post-build target, or migrate the test project to Microsoft Testing Platform so `dotnet test` works; log as a follow-up.
- **Acceptance:** all acceptance boxes ticked above; verified end-to-end by the driving user with evidence pasted into this session.

### Sequencing note
BL-4.1 (MEL bridge, DIM, injected ILogger) + BL-4.2 (collision) core is implemented and verified on net10 (see BL-4.1.md). BL-6.1 Aspire was previously offline-blocked; **network + nuget.org are now restored** on this machine, so the Aspire templates/packages may be reachable — re-verify before treating as still blocked.

### Superseded (2026-09-18) — blanket PRI-off removed from the three Studio XAML libraries
The `AppxGeneratePriEnabled=false` + `AppxGeneratePrisForPortableLibrariesEnabled=false` + `WindowsPackageType=None` workaround (above) was a **bare-`dotnet`** workaround, but it is **incompatible with the MSIX-packaged `Edam.Studio` app**, whose PRI step consumes its referenced libraries' PRIs. Building `Edam.Studio.sln` therefore failed:

```
WINAPPSDKGENERATEPROJECTPRIFILE : error PRI175: Processing Resources failed
WINAPPSDKGENERATEPROJECTPRIFILE : error PRI252: File …\Edam.WinUI.Controls.pri not found   (then …\Edam.UI.pri)
```

**Resolution:** `Edam.WinUI.Controls`, `Edam.UI` and `Edam.UI.DataModel` now generate PRI normally (the three properties removed) — the same configuration the already-working WinUI library `Edam.UI.CatalogExplorer` uses. **Verified (MSBuild/VS path):** `Edam.Studio.sln` builds **0 errors** at `Platform=x64/AnyCPU` and the app output is a correct runnable layout — `Edam.Studio.exe` plus `resources.pri` merged from `Edam.WinUI.Controls.pri`, `Edam.UI.pri` and `Edam.UI.DataModel.pri`. **Not verified here:** the bare-`dotnet` path (this sandbox cannot restore — nuget.org unreachable, and `--no-restore` also aborts on network diagnostics). **`Edam.Test.Studio` keeps PRI-off** (that flag belongs to the test host, and it still builds in the solution). If a bare `dotnet` build of those libraries regresses on a networked machine, the alternative is to make the PRI-off conditional on the build host (`Condition="'$(BuildingInsideVisualStudio)' != 'true'"`) rather than blanket-applying it.

