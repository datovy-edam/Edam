# BL-5.3 — Remove dead / duplicate code

| Field | Value |
|---|---|
| **ID** | BL-5.3 |
| **Area** | Area R — Repository hygiene |
| **Type** | Cleanup |
| **Priority** | Low |
| **Effort** | S |
| **Status** | New |

## Description
Remove dead/duplicate code: commented-out REST block in `Program.cs`, `WeatherForecast` boilerplate, unused Sizers source copy, empty READMEs. Reduces context noise for AI coders (ADR-0003).

## Acceptance criteria
- [ ] Listed dead/duplicate code removed; builds + tests still pass.
- [ ] No silent deletions of used code (grep-verify each removal).

## Related
`docs/HANDOFF.md` §4.4 item 3; ADR-0003 (context noise).
