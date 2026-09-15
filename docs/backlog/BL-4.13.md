# BL-4.13 — Internationalization / localization (i1 #11)

| Field | Value |
|---|---|
| **ID** | BL-4.13 |
| **Area** | Area E — Enterprise |
| **Type** | Feature / quality |
| **Priority** | Low (i1 #11) |
| **Effort** | M |
| **Status** | New |

## Description
Every enterprise deployment eventually needs i18n/l10n; architecting it now (WinUI resx / ResourceLoader + web) is far cheaper than retrofit. Not currently in the stack.

## Scope
- Resource-based localization for the UI shells (WinUI + future web).
- Culture-aware formatting/date-time handling (UTC-aligned, per BL-4.1).

## Acceptance criteria
- [ ] Shell strings moved to resources; a non-default culture renders.
- [ ] Dates/times handled correctly across cultures (UTC storage).

## Related
BL-4.1 (UTC), tech-stack (UI shells).
