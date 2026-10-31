# BL-5.9 — The packaged app cannot start on this machine: Windows runs it isolated, so its app data and every profile write are denied

| Field | Value |
|---|---|
| **ID** | BL-5.9 |
| **Area** | Area R — Repository hygiene / recommended fixes (deployment & environment) |
| **Type** | Bug (deployment / environment) |
| **Priority** | **High** — the application cannot be used until this is resolved |
| **Effort** | S to work around, unknown to root-cause |
| **Status** | Done (2026-10-25) — signed MSIX installed and launched successfully; loose Visual Studio registration was the failing path |

## Symptom
Starting the **deployed** application dies before any window appears:

```
System.UnauthorizedAccessException: Access is denied.
Error trying to open the explicit application data handle
   at Windows.Storage.ApplicationData.get_Current()
   at Edam.Studio.App.OnLaunched(...)      App.xaml.cs:76
```

## What is *not* the cause (checked, with evidence)
| Suspicion | How it was checked | Result |
|---|---|---|
| The settings work (ST-1…ST-5) | the app's own `startup-trace.log` stops at the identity line, which runs **before** any settings code; the previously deployed binaries (built hours earlier) fail identically | **not the cause** |
| Package not deployed | `Get-AppxPackage` → `f4cc7158-…_1.0.1.0_x64__qnet310c0zzg2`, `Status=Ok`, `IsDevelopmentMode=True`, registered from the `…\win-x64\AppX` layout | registered |
| Missing package identity | the app's trace prints `identity=f4cc7158-… family=…_qnet310c0zzg2 arch=X64` **before** the failing call | identity **is** present |
| Manifest wrong | generated `AppxManifest.xml` has `EntryPoint="Windows.FullTrustApplication"` + `<rescap:Capability Name="runFullTrust" />`; `Get-AppxPackageManifest` shows the capability granted | correct |
| Developer mode / sideloading off | `HKLM\…\AppModelUnlock`: `AllowDevelopmentWithoutDevLicense=1`, `AllowAllTrustedApps=1` | enabled |
| App data folder ACLs | `icacls` on the package folder and `LocalState` compared with `Microsoft.WindowsTerminal…` and `…Notepad…`: **identical** shape (user FullControl, capability SID F + IO, inherited SYSTEM/Administrators) | healthy |
| AppX / state repository services | `AppXSvc`, `StateRepository`, `AppXDeploymentServer`, `ClipSVC` | all **Running** |
| A stale/loose registration | `Add-AppxPackage -Register …AppxManifest.xml` → `RE-REGISTERED ok`, relaunch → **same failure** | not it |
| A corrupt packaged settings hive | moved `Settings\settings.dat*` aside (backed up, then restored), relaunch → **same failure** | not it |
| Filesystem redirection / junctions in the path | walked every ancestor of the package folder for reparse points | none |
| An OS update | `Win32_QuickFixEngineering` in the last 3 days; boot time | **none**; last boot 2026-10-18 |

## The decisive observation
Under the debugger the failure finally names its mechanism (user's run, 2026-10-25 11:56):

```
onecoreuap\base\appmodel\statemanager\winrt\srv\abiservermodule.cpp(99) Windows.Storage.ApplicationData.dll
LogHr(3) tid(8214) 80070005 Access is denied.
Msg:[Failed to set process redirection guard from DisableMitigations]
```

**Windows could not set the process *redirection guard* for the application**, so the state-manager server refuses
every application-data call. The redirection guard is the mechanism behind MSIX file/registry redirection — without
it there is no package app data, and no user-writable location is reachable either:

```
Tried: packaged application data (0x80070005: … explicit application data handle);
       'C:\Users\esobr\AppData\Local' (0x80070005: … 'Edam.Studio' is denied.);
       'C:\Users\esobr\AppData\Local\Temp\' (0x80070005: … 'Edam.Studio' is denied.)
```

while **the same user account** (and an ordinary shell) *can* create folders in both. Nothing in the repository sets
that flag: `DisableMitigations` appears **nowhere** in the source, in the restored NuGet packages' `.props/.targets`,
or in the Visual Studio MSBuild tree (grepped), so it is **Windows-side state**, not a build setting.

Two facts bracket the change: the same binaries ran normally at **06:45** and **07:06** (the tree's own log and
`LocalState\Edam.Diagnostics.log`), and fail from **11:33** onwards — and at 11:33 the AppX layout was re-generated
and the package **re-registered** (the user's later Clean → Rebuild → Deploy registers the same **loose layout**, and
behaves identically). No OS update and no reboot happened in between. The machine is Windows **11 build 26300
(Canary)**, where AppModel packaging behaviour has been changing; the process is also placed in a *Desktop AppX
container* (`AppModel-Runtime` event **211**, `Added process … to Desktop AppX container …`) on launch. That the loose
(development) registration is what now trips the guard is the **leading hypothesis, not a proven fact** (the search
endpoint returned `HTTP 402`, so no external source was reached).

## Second failure mode: unpackaged hosting (2026-10-25, 14:48–14:52)
Running the app **unpackaged** (the workaround) fails differently, and that failure was diagnosed too:

```
Class not registered (0x80040154 REGDB_E_CLASSNOTREG)
   at Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentInitializeOptions..ctor()
   at DeploymentManagerCS.AutoInitialize.AccessWindowsAppSDK()   (DeploymentManagerAutoInitializer.cs:30)
```

That is the **Windows App SDK auto-initializer**: an unpackaged app must add the framework runtime to its process
package graph, and it could not activate the runtime's WinRT classes because the machine's **Windows App Runtime
install is inconsistent**:

| Piece | State found |
|---|---|
| Framework `Microsoft.WindowsAppRuntime.1.6` | present, version **6000.519.329.0** |
| Main `MicrosoftCorporationII.WinAppRuntime.Main.1.6` | present, version 6000.519.329.0 |
| DDLM for 1.6 (the dynamic-dependency lifetime manager) | **missing** — only `…DDLM.5001.373.1736.0` (the **1.5** series) was installed |
| Singleton | present, but from the **1.8** series (8002.5.1.0) |

The DDLM must correspond to the framework servicing version it bootstraps. The only 1.6 DDLM available offline (in the
restored `microsoft.windowsappsdk/1.6.250205002` NuGet cache, `tools\MSIX\win10-x64`) is **6000.401.2352.0** — i.e.
*older* than the installed framework 6000.519.329.0. **That DDLM was installed as a diagnostic step** (Microsoft-signed,
`Add-AppxPackage`; it can be removed with `Remove-AppxPackage`). It is not sufficient on its own: the bootstrapper
still fails, because the DDLM version does not match the framework version. Fixes, in order of cost:

1. **Install the matching Windows App Runtime 1.6 installer** (`WindowsAppRuntimeInstall-x64.exe`, the redistributable
   for 6000.519.329.0). It registers the DDLM/Main/Singleton set for exactly that servicing version.
2. **Or self-contained** (`WindowsAppSDKSelfContained=true`): no runtime, no DDLM. This was tested and **works much
   further** — the app started, `OnLaunched` ran, the guard fell back to a writable root, `InitializeApplication`
   **completed** and `MainWindow` construction began. It then failed on XAML resource lookup only:
   `Cannot locate resource from 'ms-appx:///Controls/Navigation/NavigationControl.xaml'`. **Correction (measured, not
   assumed): this is *not* a missing PRI merge.** `makepri dump` of the generated `resources.pri` shows the entry is
   there — `Files/Edam.WinUI.Controls/Controls/Navigation/NavigationControl.xbf` — and the per-assembly
   `Edam.WinUI.Controls.pri` (8512 bytes) sits next to the executable too. The failing lookup asks for the **unprefixed**
   `Files/Controls/Navigation/NavigationControl.xbf`, which neither index holds, so the gap is WinUI's **per-assembly
   resource scope** not being established for an unpackaged self-contained host — a different (and deeper) problem than
   merging indexes. Substituting the packaged layout's merged index moved the failure to
   `ms-appx:///Microsoft.UI.Xaml/Themes/themeresources.xaml`, which is consistent with that reading.
3. Building with `WindowsPackageType=None` while leaving the MSIX tooling **on** (`PublishAppxPackage=false`) *does*
   generate a `resources.pri` in the output, so the merge hook exists — it just does not include referenced assemblies.

**Conclusion on unpackaged:** it is a viable host once (1) the runtime is installed consistently, and (2) the resource
index is merged at build time. Both are ordinary work items; see `docs/HANDOFF.md` item 147 for the code that was
already made host-agnostic (`GetApplicationInstalledLocation` no longer requires a package).

**Packaged launch retested after the runtime install (14:5x): unchanged** — same three denied roots, same message box.
So the runtime install neither caused nor fixes the packaged-app-data failure; the two problems are independent.


## Workaround / next steps (in order) — *historical; step 1 is what the Resolution records as done*
**Note (2026-10-25):** this list was written while the packaged path was still failing. Step 1 was carried out and
succeeded, which overtook steps 2–3. Two claims in it were later corrected and are marked inline rather than left
standing.

1. **Install it as a real package rather than registering the layout**: in Visual Studio *Publish → Create App
   Packages → Sideload*, then install the produced `.msix` (and launch from the Start menu). The signing certificate
   is present and valid — `CN=esobr`, thumbprint `B5C164EC40289FF1A3A02914EC8EB8613FD1CB5D`, expires 2027-09-23, has
   a private key — so packaging can succeed. A proper install is the supported path and is the most likely to avoid
   whatever now isolates loose-file registrations. *(Attempted from the command line with
   `GenerateAppxPackageOnBuild=true` + `UapAppxPackageBuildMode=SideloadOnly`, which builds 0 errors but emits **no**
   `.msix`. That was a wrong **build-flag combination**, not evidence that packaging needs the IDE: `MSBuild /t:Publish`
   does produce the signed `.msix` — see the Resolution and `HANDOFF` item 151.)*
2. If it still fails, the Canary build's packaging behaviour is the remaining suspect: report it with the exact
   `Failed to set process redirection guard from DisableMitigations` line, and try a release build of Windows (or the
   build where it last worked).
3. **Unpackaged hosting needs research, not a flag** (see the section above): the runtime must be installed
   *consistently* for 1.6 (matching DDLM — the missing 6000.519.329.0 set, which **winget does not install**: it
   reports the framework as already present and offers no upgrade, while the DDLM stays missing; only
   `WindowsAppRuntimeInstall-x64.exe` registers the full set), and separately WinUI's **per-assembly resource scope**
   must be established for an unpackaged host — the index is *not* missing entries, the scope is missing. With the
   runtime route the app should behave like it does packaged (where this scope works), which is why installing the
   redistributable is still the cheapest experiment. **Correction (2026-10-25):** the resource question was answered —
   an unpackaged host resolves XAML from **loose `.xbf` files beside the executable**, not from the merged index, and
   copying the referenced projects' compiled XAML fixed it (third fix under *Status of the two paths*); and building
   **self-contained** removes the runtime/DDLM requirement altogether, so neither branch of this step is still open.
4. The application now **survives and explains** this (see item 147): it falls back to a writable root when one
   exists, records every rejected candidate with its HRESULT, and — when none is writable — shows a
   **message box** naming the reasons and this remedy instead of dying silently.

## Third observation: the refusal came from the launching shell (2026-10-25, 15:04–15:08)
After the **Windows App Runtime 1.6 redistributable** was installed (which the user did, and which **fixed**
`REGDB_E_CLASSNOTREG` — the app now boots, reaching `OnLaunched` and the data-root guard), every candidate root was
still refused with `0x80070005`. The failure was **not** the application's: the run's own `%TEMP%` was
`C:\Users\esobr\AppData\Local\Temp\dsh-e7qkqt` — the DSH harness's sandbox folder — so a process started from the
**agent terminal** inherits the harness environment *and* its write confinement; the same account creates those
folders normally from an ordinary window, and two shells in the same session measured **Low** and **Medium**
integrity respectively. **Launches from the agent terminal are therefore not a valid test bed**, and that includes
this backlog item's earlier "packaged app is confined" reading: the packaged failure is real and has its own cause
(the redirection guard, above), but profile-write denials seen from harness-launched runs must **not** be attributed
to the application or to Windows' packaging. **Instruction to verify:** start the application from **Explorer** or
its **Start-menu entry**. The guard's message box now says exactly this, and prints the security context; the probe
folders created during this diagnosis were deleted (both empty).

## Status of the two paths (2026-10-25, 15:17) — *historical; superseded by the Resolution below*
**Historical:** this section was written while the packaged path was still believed broken. The signed MSIX was
subsequently installed and launched successfully (see **Resolution** at the end of this file and `docs/HANDOFF.md`
item 153), so the closing paragraph of this section — "the packaged defect itself is still open" — no longer holds.
It is kept because the unpackaged findings it records (runtime/DDLM, the data root, the loose-`.xbf` requirement) are
still the accurate recipe for the development fallback in `run-unpackaged.cmd`.

**Unpackaged hosting now works** — verified end to end: `OnLaunched: completed`, a responding `EDAM Studio` window,
and application data written to the configured root. Three things were needed, each found by measurement:

1. **The runtime.** Installing the Windows App Runtime 1.6 redistributable removed the `REGDB_E_CLASSNOTREG`
   bootstrap failure. Framework-dependent unpackaged hosting still fails on this machine (the DDLM present is
   `6000.401.2352.0` while the framework is `6000.519.329.0`, and the DDLM must match the framework it bootstraps),
   so the documented unpackaged configuration is **self-contained** (`WindowsAppSDKSelfContained=true`) — no runtime,
   no DDLM.
2. **A writable data root.** `EDAM_STUDIO_DATA_ROOT` names it explicitly (checked after the packaged location and
   before the profile), because this machine refused `%LOCALAPPDATA%` and `%TEMP%` to the application even when it was
   launched from Explorer. `run-unpackaged.cmd` sets it to `.local-data` inside the repository.
3. **The referenced assemblies' compiled XAML.** This was the last wall and it was **not** a resource-index merge:
   a packaged app resolves `ms-appx:///Controls/…/Foo.xaml` through the package's merged index, while an
   **unpackaged** one resolves it against **loose `.xbf` files relative to the application folder** — and the SDK only
   copies the app project's own `.xbf` there. Copying the referenced projects' intermediate `.xbf` files into the
   output (MSBuild target `CopyUnpackagedXamlResources` in `Edam.Studio.csproj`, scoped to
   `WindowsPackageType=None`, 106 files) is what made the window appear. The packaged build is unaffected (the target
   does not run).

**~~The packaged defect itself is still open~~ — superseded:** the signed MSIX was installed and launched successfully
later on 2026-10-25 (see **Resolution** below and `docs/HANDOFF.md` item 153). The "untried remedy" named in this
paragraph became the working one; only the loose Visual Studio registration remains broken, by design.

## Related
`docs/HANDOFF.md` items 147–158 (investigation, the signed-MSIX resolution, the documentation reconciliation, the
loose-deployment fallback and the two follow-up fixes); **ADR-0017** (the loose development data root);
ST-2 (its runtime verification was blocked by this and is now open — the installed package is the way to run the
checks); ADR-0010 / ADR-0013 (the packaged app-data root and the packaged-state/overlay split this affects).

## Packaging validation update (2026-10-25)

The current MSIX publish had a separate, reproducible defect: `Edam.Studio.csproj` recursively included
`app-data/Edam.App.Data/Projects`, which copied developer workspace files into the package and failed on a nested
path beyond `MAX_PATH`. The project now packages only the application-default seed folders (`Templates`, `TextMaps`,
`Samples`) plus `app-data/Edam.Settings.json`; `Projects` and template `.vs` metadata are excluded.

Evidence from the clean Release package:

- Publish completed with exit code 0.
- The MSIX contains the deployed settings seed, 22 template entries, 7 text-map entries and 1 sample.
- It contains 0 `Projects` entries, 0 `.vs` entries, and its longest archive entry is 93 characters.
- `signtool verify /pa /all` completed successfully.
- The development certificate was imported into the machine-level `TrustedPeople` store successfully.
- `Add-AppxPackage` now reaches deployment validation but returns `0x80073CFB`: the same app identity is already installed through the unpackaged/development registration, and Windows will not replace it with an MSIX in place.

Next step: remove the existing same-identity unpackaged/development registration (after preserving any required app data), install this MSIX, and launch it from its Start-menu entry for the first clean runtime validation. That removal was not performed automatically because it changes the current app installation. *(Superseded: the removal, installation and Start-menu launch were performed later on 2026-10-25 — see **Resolution**.)*

## Resolution (2026-10-25)

The development registration was removed, then the signed MSIX was installed after the development certificate had been
trusted in `LocalMachine\\TrustedPeople`. The installed package now reports `IsDevelopmentMode=False`,
`SignatureKind=Developer`, and an install location under `C:\\Program Files\\WindowsApps`. Launching its Start-menu
identity produced the real `EDAM Studio` login window; the startup failure did not occur. Its package `LocalState`
exists and contains 34 files, confirming that packaged application data is available.

## Evidence gaps (recorded 2026-10-25, after the resolution)
Two steps of the now-formal `deploy-app` contract (`AGENTS.md` §5A) have no evidence behind them for *this* deployment,
because they were not part of the procedure when it ran. They are recorded here rather than inferred:

- **Manifest version:** still `1.0.1.0`; no bump was applied. None was needed — the same-identity development
  registration was removed *before* the MSIX was installed, so nothing had to be superseded. §5A step 3 now requires
  that "no bump was needed" decision to be stated rather than left implicit.
- **Pre-removal `LocalState` inventory:** not recorded. The `34 files` figure above describes the state *after*
  installation. Whether the development registration held user data at removal time is therefore unevidenced; §5A
  step 2 now requires the inventory to be captured before any removal.

## Decisions
- **ADR-0017 records the loose-deployment data root**: packaged `LocalFolder` → `EDAM_STUDIO_DATA_ROOT` →
  `<deployment folder>\.local-data` (loose Visual Studio layout only, detected by an installed location whose last
  segment is `AppX`) → `%LOCALAPPDATA%` → `%TEMP%` → visible refusal. A signed MSIX never takes the development
  branch, so packaged `LocalState` behavior is unchanged.
- **Packaged markers follow that root.** `PackagedStateStore` uses `LocalSettings` when it works and otherwise keeps
  the once-per-installation markers in `Edam.PackagedState.json` under the resolved root — a file separate from the
  overlay, so a reset of the answers still cannot clear a marker (ADR-0012 decision 4). Before this, the loose
  deployment logged `Could not write packaged state 'Edam:Catalog:ContainerReady': Access is denied` and no marker
  persisted there.
- **The configuration ask is single-flight** (`ConfigurationPrompt.ShowIfNeededAsync`): two callers arriving within
  270 ms used to produce `Only a single ContentDialog can be open at any time`, which left both questions deferred.
  The later caller now yields to the dialog already on screen.
- **Unpackaged hosting is a development fallback, never a deployment path.** The shipped path is the signed MSIX
  (§5A). `run-unpackaged.cmd`, `EDAM_STUDIO_DATA_ROOT`, the `CopyUnpackagedXamlResources` target and the VS launch
  profile remain for local development only; the profile is labelled
  *"Edam.Studio (Unpackaged - needs a WindowsPackageType=None build)"* because the default MSIX build cannot run that
  way. Recorded in `docs/HANDOFF.md` item 155.
- **Loose Visual Studio deployment now has a development-only data root.** When `ApplicationData.Current` fails for
  the loose Visual Studio package layout, `Edam.Studio/App.xaml.cs` uses `.local-data` beside the loose deployment
  after first honoring `EDAM_STUDIO_DATA_ROOT`. This does not alter signed MSIX behavior: packages installed under
  `WindowsApps` continue to use package `LocalState`. The fallback is intentionally local to the deployment output
  and may be removed by a Clean operation; packaged-runtime validation remains the signed-MSIX `deploy-app`
  workflow.
