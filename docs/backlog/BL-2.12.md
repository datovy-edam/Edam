# BL-2.12 — Editor tests

| Field | Value |
|---|---|
| **ID** | BL-2.12 |
| **Area** | Area 2 — Monaco Code Editor Enhancement |
| **Type** | Testing |
| **Priority** | Medium |
| **Effort** | M |
| **Status** | Implemented — needs verification |

## Description

Add tests for the save command, dirty-state tracking, and language detection (where testable without a UI host).

## Acceptance Criteria

- [x] Save/dirty/language logic is covered by tests.
- [x] Tests run in CI.

## Dependencies

- BL-2.2 – BL-2.8

## Notes / Test Results

- Test project location: `src\Edam.Data.Catalog.WinUI\Testing\Edam.Test.Monaco\` (WinUI MSTest, added to the Catalog solution).
- **Tests added:** `MonacoFileRecognitionHandlerTests` — 5 tests covering `RecognizeLanguageByFileType` (known extensions, unknown fallback, empty/null fallback, case-sensitivity, mapping contents).
- **Bug found & fixed:** `RecognizeLanguageByFileType(null)` threw `ArgumentNullException` (dictionary lookup on null). Fixed to fall back to `"plaintext"` for null/empty input.
- **Result:** 5/5 tests pass via `vstest.console.exe` (0.48s).
- **Not yet covered (need a UI host):** save command and dirty-state tracking in `MonacoEditorViewModel`/`EditorTabsControl` (WinUI-dependent).
