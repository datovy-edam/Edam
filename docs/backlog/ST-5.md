# ST-5 — Settings follow-ups: make the values DRIVE documents, add application preferences, and prove the other hosts

| Field | Value |
|---|---|
| **ID** | ST-5 |
| **Area** | Area ST — Settings surface |
| **Type** | Feature backlog |
| **Priority** | Medium |
| **Effort** | M (collectively) |
| **Status** | New — deliberately out of the first increment (ADR-0016 decision 12) |

## Description
The first increment **surfaces** what the application already resolves; it does not invent new behavior. These are the follow-ups, recorded so they are not lost, each with the reason it was not done in v1.

## 5.1 Domain details should DRIVE new arguments documents
**Today:** the two Domain items state the default, and the shipped template states the same values, so a new project's first document is coherent. **Gap:** changing the setting does not rewrite the seeded document, so the value describes intent rather than acting on it. **Do:** have the default-project seeding (DP-4 / `IDefaultProjectHost.SeedAsync`) stamp the stated `Domain` into the seeded arguments file — a JSON patch through `IProjectResources` after the copy — and prove it with a DP-5 check. **Why deferred:** it changes the seeding path that DP-3/DP-5 cover, and the user chose *surfacing only* for the first increment.

## 5.2 Application preferences (each is NEW behavior, not a view of existing config)
- **Theme** (*System · Light · Dark*): nothing sets `RequestedTheme` today — the app follows the system. A `Choice` item would also flow into Monaco (its theme mapping already reads `Application.Current.RequestedTheme`). Needs an `Immediate` apply hook that sets `Application.Current.RequestedTheme`.
- **Diagnostics verbosity** (minimum level for the panel and `Edam.Diagnostics.log`): needs a level filter in `AppDiagnostics` / `InMemoryLoggerProvider` before the setting can mean anything.
- **Diagnostics panel open at start**: currently collapsed to a rail by construction in `NavigationControl`.

## 5.3 Prove the remaining hosts
- **MCP**: expose `settings.list` / `settings.get` (read) and, once ST-4's identity question is settled, `settings.set`.
- **Wave-1 API writes**: see **ST-4**.

## 5.4 Guardrails worth adding
- A conformance check that **no surface owns a store**: the projects editor is the only writer of the overlay, and the container dialog no longer writes it (already true; make it stay true).
- A check that every registry item declares a `Group` and an `ApplyScope` (a new item that forgot them would silently land in *Projects* / *Next use*).

## Related
ADR-0016 (decision 12), ST-1, ST-2, ST-3, ST-4, DP-4, BL-2.9 (Monaco theme).
