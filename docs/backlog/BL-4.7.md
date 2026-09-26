# BL-4.7 — CI/CD + deployment + containerization (i1 #5)

| Field | Value |
|---|---|
| **ID** | BL-4.7 |
| **Area** | Area E — Enterprise |
| **Type** | Infrastructure |
| **Priority** | Medium (i1 #5) |
| **Effort** | L |
| **Status** | In Progress — desktop MSIX packaging and package-local app-data seeding fixed; CI, services, and Aspire deployment remain |

## Description
Build gates exist (build 0 errors, headless tests, `doc-health-checklist`), but there is **no pipeline, no containerization, no deployment path**. Add the release/deployment tier for the v2.0 services (Aspire/API/CLI/MCP) and desktop packaging.

## Scope
- **CI:** pipeline running the existing gates (WinUI via VS18 MSBuild; libs via `dotnet build`; headless tests; doc-health).
- **CD/deployment:** containers (Docker) for services; deploy target for Aspire output; win/installer packaging for the WinUI shell.
- Keep honest: pipeline is infrastructure; validate each gate as it lands.

## Acceptance criteria
- [ ] CI runs the standard gates and reports results on PR/commit.
- [ ] Services can be containerized; **desktop can be packaged** (verified 2026-09-23).
- [ ] Deployment path documented for Aspire output.

### Desktop packaging increment — 2026-09-23

- Added the tracked `Edam.Studio/Properties/PublishProfiles/win10-x64.pubxml` profile with `PublishAppxPackage=true`, x64/win-x64 targeting, MSIX bundling disabled, and App Installer generation disabled until a real distribution URI exists.
- Restored packaged Debug builds so the solution's Deploy mapping retains package identity for WinUI/XAML activation.
- Set the local Visual Studio active launch profile to `Edam.Studio (Package)` so Windows App SDK deployment initialization is not invoked from a raw unpackaged executable.
- Replaced the stale signing thumbprint with the valid local `CN=esobr` development certificate and verified a signed MSIX with `signtool verify /pa /all`.
- The profile must be executed by Visual Studio 18 MSBuild (or `dotnet publish` with `AppxMSBuildToolsPath` pointed at the installed VS AppxPackage tooling); the .NET SDK alone does not include the WinUI PRI packaging tasks.
- Implemented the package-local app-data design: the seed is sanitized and packaged as content; `Edam.Application.AppData` accepts a host-provided writable root; Studio points it at `Windows.Storage.ApplicationData.Current.LocalFolder`; initialization copies the packaged `ApplicationData/Edam.Studio` seed into `LocalFolder/Edam.Studio` and adds only missing files on later launches.
- The Release MSIX was rebuilt and inspected: it contains `ApplicationData/Edam.Studio/Edam.App.Data/Edam.Settings.json` with no machine-specific absolute paths. User settings/project data no longer default to redirected Documents/OneDrive storage.
- Remaining: CI, service containerization, and Aspire deployment. A one-time migration of existing settings from the old Documents location is intentionally not automatic and remains a follow-up if required.

## Related
Tech-stack build/toolchain + orchestration rows; BL-3.1 (.NET 10) must land first.
