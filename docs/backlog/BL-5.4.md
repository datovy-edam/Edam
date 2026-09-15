# BL-5.4 — Replace `async void` init and `.Wait()/.Result`; add session management

| Field | Value |
|---|---|
| **ID** | BL-5.4 |
| **Area** | Area R — Repository hygiene |
| **Type** | Refactor / quality |
| **Priority** | Medium |
| **Effort** | M |
| **Status** | New |

## Description
Replace `async void` initialization and blocking `.Wait()`/`.Result` patterns; add real session management in the catalog service. Enterprise hygiene for reliability and thread-safety.

## Acceptance criteria
- [ ] No `async void` / blocking `.Wait()/.Result` in new/updated paths (analyzers enforce).
- [ ] Catalog session lifecycle implemented without shared mutable globals.

## Related
`docs/HANDOFF.md` §4.4 item 4; BL-4.12 (state), BL-4.1 (thread-safety).
