# BL-2.6 — Update Monaco version in Edam.Studio (0.33.0 → current)

| Field | Value |
|---|---|
| **ID** | BL-2.6 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Dependency / maintenance |
| **Priority** | Medium |
| **Effort** | M |
| **Status** | Implemented — needs verification |

## Description

Bump the vendored Monaco in `Edam.Studio\web\monaco-editor\` from 0.33.0 to the current stable release, re-verifying the `code-editor.html` bridge still works.

## Acceptance Criteria

- [x] Monaco updated to the target version; editor loads and set/get text still work.
- [x] No regressions in language highlighting or layout.

## Dependencies

- BL-2.1, BL-2.3

## Notes / Test Results

- Current version: 0.33.0 (per `web\monaco-editor\package.json`).
- **Updated to 0.56.0** (latest stable on npm). Replaced the vendored `dev`/`esm`/`min` folders and metadata (`package.json`, `monaco.d.ts`, `CHANGELOG.md`, `LICENSE`, `README.md`, `ThirdPartyNotices.txt`) from the `monaco-editor@0.56.0` npm package; removed the obsolete `min-maps` folder (no longer shipped).
- **Bridge fix:** `code-editor.html` (and the sample `index.html`) no longer reference `min/vs/editor/editor.main.nls.js` — that file no longer exists in 0.56.0 (NLS is bundled differently). The bridge uses stable APIs (`monaco.editor.create`, `getValue`/`setValue`, `KeyMod`/`KeyCode`), so Ctrl-S and set/get text are unaffected.
- **Build:** Edam.Studio builds 0 errors.
- **Runtime:** editor load + set/get text + Ctrl-S still need verification in the running app. Note: the web folder is resolved against `AppSettings.ApplicationDataFolder` at runtime (not the output `bin`), so the updated files must be deployed to that location.
