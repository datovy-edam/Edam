# BL-4.14 — Contract tests (API/MCP) + UI automation (i1 #12)

| Field | Value |
|---|---|
| **ID** | BL-4.14 |
| **Area** | Area E — Enterprise / testing |
| **Type** | Testing |
| **Priority** | Medium (i1 #12) |
| **Effort** | M |
| **Status** | New |

## Description
The testing strategy jumps from headless unit tests to "nothing." Add the missing layers:
1. **Contract tests** for the future API and MCP surfaces (OpenAPI/contract-driven), so the API/MCP shells are a stable boundary.
2. **UI automation**: Playwright for the future web shell; WinUI Appium/TestStack for desktop.

## Scope
- Define contract-test project/schema for API + MCP; run as part of the gates.
- UI-automation scaffolding for desktop; reuse the web shell testing once a web shell exists.

## Acceptance criteria
- [ ] API/MCP contract tests exist and run in CI.
- [ ] Desktop UI automation smoke tests run (for UI-host-dependent cases currently deferred, e.g., the `AddControl` tests).

## Related
Standards baseline §4 (gates), BL-4.7 (CI), tech-stack testing row; unblocks the deferred WinUI UI-host integration tests.
