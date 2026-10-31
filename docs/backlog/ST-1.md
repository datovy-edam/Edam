# ST-1 — The settings platform: contract, catalog, editor, provenance, apply

| Field | Value |
|---|---|
| **ID** | ST-1 |
| **Area** | Area ST — Settings surface |
| **Type** | Feature / architecture |
| **Priority** | High |
| **Effort** | M |
| **Status** | Done (2026-10-12) — headless verified |

## Description
ADR-0013 decided how the application **asks** for values it cannot invent. It did not provide a way to **review and change** them. ST-1 delivers the UI-agnostic half: a dependency-free contract and one service that any host (WinUI, HTTP API, CLI, MCP) drives, so no surface owns its own store, validator or write path.

## Delivered
- **`src/Edam.Settings/Edam.Settings.Contracts`** (dependency-free): `ISettingsCatalog` + `SettingsItemInfo` (state · effective value · **source** · problem · editable · apply scope · default · choices · advanced · reason), `SettingsGroups`, `ISettingsEditor`, `ISettingsApplier`, `SettingsChangeResult`.
- **`src/Edam.Settings/Edam.Settings.Core`**: `SettingsService` (the one read/change/apply path) + `SettingsText` (the wording every host reuses).
- **`ProjectSettingsCatalog` / `ProjectSettingsEditor`** (`Edam.Data.Projects.DependencyInjection`): the projects module's implementation over the existing `ConfigurableItems` registry and `UserConfiguration` — no second reader, no second store, no second validator.
- **Registry extensions** on `ConfigurableItemInfo`: `Group` · `Kind` · `Tier` · `ApplyScope` · `Choices`, so a surface is generated and a setting is added without touching UI code.
- **`ProjectSettingsComposition`**: composes the surface for a host with no per-user app data (API/CLI), with a stated state root.

## Behaviors proven (headless)
- An item that does **not apply** is absent; every applicable item appears exactly once.
- **Provenance is honest**: user · installation · deployment · environment (**wins**) · documented default · unset. An unstated binding reports **default**, not deployment.
- An **action-backed** value is editable while pending, read-only once completed (with a reason).
- Installation facts and markers are read-only and say why.
- One write path; validation happens **before** storage.
- `ApplyScope`: only **immediate** items invoke the host applier; a save that failed to apply is reported as *stored but did not take effect*.
- Reset restores inherited values; *forget all* spares the installation markers.

## Verification
`settings catalog (ST-1)` 8 · `settings provenance (ST-2)` 6 · `settings apply (ST-3)` 9 — **ALL CONFORM**; CF-1 (15), CF-2 (17), CF-3 (14) unchanged and green.

## Related
ADR-0016 (decisions 1–8), ADR-0013, ADR-0011, ADR-0014. `Edam.Data.Projects.Conformance/SettingsSurfaceScenario.cs`.
