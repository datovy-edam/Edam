# ADR-0010 — WinUI packaged app-data uses a package default plus LocalFolder overlay

| Field | Value |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-09-23 |
| **Scope** | `Edam.Studio` packaged desktop host |
| **Related** | BL-4.7; `docs/HANDOFF.md` item 51 |

## Context

The packaged Studio host previously resolved its settings through `MyDocuments`. On this machine that path is redirected into OneDrive, and a first-run/partial-folder state could leave `Edam.App.Data` absent. The MSIX install directory is read-only and cannot be used for mutable settings.

## Decision

Use a sanitized `ApplicationData/Edam.Studio` tree inside the MSIX as the read-only default seed. At startup, `Edam.Studio` points `Edam.Application.AppData` at `Windows.Storage.ApplicationData.Current.LocalFolder`. Initialization copies the packaged seed into `LocalFolder/Edam.Studio`; subsequent launches copy only missing files and never overwrite user files.

## Consequences

- Settings, project data, and generated app data are writable and package-scoped without depending on Documents/OneDrive.
- Package updates can add new default files without destroying user changes.
- Existing settings in the former Documents location are not migrated automatically; migration can be added later as an explicit, separately validated feature.
- The sanitized seed must not contain machine-specific absolute paths or secrets.

## Validation

- `Edam.System` 1.0.1 built and was republished to the local feed.
- `Edam.Studio.sln` Debug build completed with zero errors after restore.
- Release Publish completed with zero errors; the signed x64 MSIX contains `ApplicationData/Edam.Studio/Edam.App.Data/Edam.Settings.json` and signature verification succeeded.
