# ST-4 — The settings surface over the platform: read API now, authenticated writes later

| Field | Value |
|---|---|
| **ID** | ST-4 |
| **Area** | Area ST — Settings surface |
| **Type** | Feature / API |
| **Priority** | Medium |
| **Effort** | S (read) / M (writes) |
| **Status** | Read: Done (2026-10-12) · Authenticated writes: **Deferred** (needs an identity mechanism) |

## Description
"UI-agnostic" is a claim that has to be demonstrable, not asserted. ST-4 makes the settings model reachable from hosts with **no WinUI in the loop**, using the same catalog and the same service the desktop page drives.

## Delivered — read surface (`Edam.WebApi`)
- `GET /settings` → the catalog: `Groups` (contract order) + every `Items` row with state, effective value, **source**, problem, editability and apply scope.
- `GET /settings/{id}` → one row, or `404 { Found: false }`.
- Composed through `ProjectSettingsComposition` with a stated state root (`Edam:Settings:StateRoot`), because a service has no packaged per-user state of its own.

## Delivered — write surface (`Edam.Cli`)
`edam settings list | get <id> | set <id> <value> | reset <id|--all>` — the **same** `SettingsService`, with a local state root (overridable via `Edam:Settings:StateRoot` / `Edam__Settings__StateRoot`). Verified end to end: a set is stored as plain readable JSON in the per-user overlay, read back with the right **provenance** ("your value"), refused with the validator's reason when invalid, refused when the row is an installation fact, and `reset --all` forgets answers while keeping the installation markers.

## Why writes are NOT on the HTTP API yet
The Wave-1 API has **no authenticated caller**. A settings write endpoint without an identity would let any reachable client rewrite the deployment's answers — a security hole dressed as a feature. Writes are exercised where a **person** is behind them (desktop, CLI). Promoting writes to the API is gated on the platform's identity story, not on this feature's design.

## Acceptance criteria (deferred part)
- [ ] An authenticated caller can `PUT /settings/{id}` and `DELETE /settings/{id}`.
- [ ] The caller's identity is recorded with the change (audit), and a per-user state root is resolved for it.
- [ ] A write over HTTP applies through the host's `ISettingsApplier` where the platform host has one.

## Related
ADR-0016 (decision 11), ST-1, Wave-1 (BL-6.x), BL-4.3 (governance/audit).
