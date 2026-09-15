# BL-2.8 — Language detection via file extension

| Field | Value |
|---|---|
| **ID** | BL-2.8 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Feature |
| **Priority** | Medium |
| **Effort** | S |
| **Status** | Implemented — needs verification |

## Description

Ensure the editor sets the correct language from the document's file extension (using `MonacoFileRecognitionHandler` in Catalog; equivalent mapping in Edam.Studio).

## Acceptance Criteria

- [x] Opening a file sets the correct Monaco language automatically.
- [x] Unknown extensions fall back to a default.

## Dependencies

- BL-2.6 / BL-2.7

## Notes / Test Results

- Related types: `MonacoFileRecognitionHandler.RecognizeLanguageByFileType`.
- **Already implemented (Catalog):** `MonacoEditorViewModel.CurrentPathItem` calls `FileRecognitionHandler.RecognizeLanguageByFileType(value.Extension)` to set `CurrentLanguage`; `RecognizeLanguageByFileType` falls back to `"plaintext"` for unknown extensions. The editor applies the language via `SetLanguageAsync` on load.
- **Build:** CatalogExplorer builds 0 errors.
- **Runtime:** language selection still needs verification in the running app.
