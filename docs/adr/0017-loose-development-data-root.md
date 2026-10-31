# ADR-0017 — A loose development deployment keeps its data beside the deployment

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-10-25 |
| **Scope** | `Edam.Studio` desktop host — development deployments |
| **Related** | ADR-0010 (packaged app-data), ADR-0013 decision 6 (packaged state vs overlay), `docs/HANDOFF.md` items 147–158, `BL-5.9` |

## Context

A **signed MSIX** installs under `C:\Program Files\WindowsApps` and gets working package application data:
`ApplicationData.Current.LocalFolder` is the data root (ADR-0010) and `LocalSettings` holds the
once-per-installation markers (ADR-0013 decision 6). Both were verified after the signed deployment.

Visual Studio's **Clean → Rebuild → Deploy** produces a different thing: a *loose* layout registered from
`…\win-x64\AppX`. It has package identity, but on Windows 11 build 26300 the process is placed in a Desktop
AppX container whose redirection guard cannot be set (`Failed to set process redirection guard from
DisableMitigations`, `0x80070005`), so `ApplicationData.Current` — and, for that process, `%LOCALAPPDATA%` and
`%TEMP%` as well — are denied. The application therefore died before its first window, and the only way to run
it was to build a separate unpackaged flavour with an explicit data root. That made the fast debug loop much
more expensive than it should be, and it meant the shipped shape was rarely exercised during development.

## Decision

1. **The loose deployment gets a deployment-local root.** At startup the host resolves its data root in this
   order, each candidate proved writable before it is adopted:
   packaged `ApplicationData.Current.LocalFolder` → `EDAM_STUDIO_DATA_ROOT` → **`<deployment folder>\.local-data`**
   (only when the package is the loose Visual Studio layout) → `%LOCALAPPDATA%` → `%TEMP%` → visible refusal.
2. **"Loose layout" is detected by shape, not by signature**: `Package.Current.InstalledLocation` whose final
   path segment is `AppX`. A signed MSIX — including a developer-signed one — never matches, so its behaviour is
   unchanged. An unpackaged process has no `Package.Current`, is not "loose", and uses the explicit override or
   the normal fallback.
3. **Packaged markers follow the same root.** `PackagedStateStore` keeps using `LocalSettings` when it works, and
   otherwise stores the markers in `Edam.PackagedState.json` **under the resolved root**. It is a different file
   from the overlay (`Edam.UserState.json`), so resetting the user's answers still cannot clear an
   installation marker (ADR-0012 decision 4).
4. **This is development-only behaviour.** The refusal path, the reason it exists, and the shipped path are all
   still reported: the loose root is logged, and the message box (when nothing is writable) still names every
   rejected candidate with its HRESULT.

## Consequences

- The fast loop is Visual Studio's own **Clean → Rebuild → Deploy → F5**, and it starts on this machine. Debug
  builds are therefore exercised in the packaged shape (identity, resources, activation) rather than in a separate
  unpackaged flavour.
- **Development data is disposable and lives in the build output**: `…\win-x64\AppX\.local-data` (settings answers,
  `Edam.db3`, `Edam.UserState.json`, `Edam.PackagedState.json`, diagnostics). A **Clean** removes it, and the app
  recreates it on the next deployment. Nothing there is user data worth preserving, and it is not the packaged app's
  `LocalState`.
- Two data roots now exist on a developer machine and must not be confused: the loose deployment's
  `AppX\.local-data` and the unpackaged flavour's repository-root `.local-data` (used by `run-unpackaged.cmd` with
  `EDAM_STUDIO_DATA_ROOT`). They are separate by design — the marker file in one is invisible to the other.
- **Debug evidence is not packaged-runtime evidence.** A loose deployment proves the debug build; signed-MSIX
  behaviour must still be validated with `deploy-app` (`AGENTS.md` §5A). The loose registration also *replaces* an
  installed signed MSIX of the same identity, so `deploy-app` has to be run again afterwards.
- The unpackaged self-contained flavour is no longer needed for everyday debugging. It remains only as a fallback
  for a machine where even the loose registration cannot start (for example when the package cannot be registered
  at all), and its data root stays separate.
- If Windows later fixes the redirection guard for loose registrations, branch 3 simply stops being reached; the
  code needs no change, and the fallback file is not read by a signed MSIX.

## Validation

- `Edam.Studio` Debug x64 builds with **0 errors** with the fallback in place (existing warnings only).
- Seven consecutive loose-deployment starts between 16:49 and 17:06 on 2026-10-25 each logged
  `loose development package app-data root configured at '…\win-x64\AppX\.local-data\Edam.Studio'`, then
  `OnLaunched: InitializeApplication completed`, `MainWindow ctor: completed`, and `OnLaunched: completed`; the
  window reported `EDAM Studio` and the root gained `Edam.db3`, `Edam.UserState.json`, `app-data\Edam.Settings.json`
  and `Edam.Diagnostics.log`.
- The signed MSIX was installed before this change and remains `IsDevelopmentMode=False` under `WindowsApps`; the
  fallback is gated on the `AppX` layout and cannot be reached by it.
