# ST-2 — The WinUI settings surface (and one home for the connection-string override)

| Field | Value |
|---|---|
| **ID** | ST-2 |
| **Area** | Area ST — Settings surface |
| **Type** | Feature / UI |
| **Priority** | High |
| **Effort** | M |
| **Status** | Implemented — needs user verification (F5) |

## Description
The shell showed a Settings item (`IsSettingsVisible="True"`) with **no handler** — the most obvious place a person looks was a dead end. ST-2 wires it to a page **generated from the catalog** (ADR-0016 decisions 1–6), and retires the second editor that had grown inside the container manager dialog.

## Delivered
- **`SettingsControl`** (`Edam.WinUI.Controls/Settings`): three tiers kept visibly apart — *your values* (editable, with per-row Save and *Reset to default* where the value is the person's), *set for this installation* (read-only + the reason), *recorded state* (advanced, read-only, explained as evidence). Each row states **where its value came from** and, when it is not immediate, **when the change takes effect**. Every save/reset/refusal is written to the **diagnostics panel**.
- **`NavigationControl`**: `ItemInvoked` + `IsSettingsInvoked` opens the page in the shell's content frame (WinUI has no separate settings event — the flag on the invoked event is the pattern).
- **`SettingsGateway`**: the Studio's single composition of the platform service, with a `StudioSettingsApplier` that recycles the projects storage when the catalog connection changes — the same effect the old dialog button had, now applied for **every** surface.
- **`ContainerManagerDialog`**: the connection-string editor and its two buttons are **removed**; the Storage tab *states* the storage and points to Settings → Catalog. **`ProjectServicesHelper.SetCatalogConnectionAsync` deleted** (one write path per key).

## Acceptance criteria
- [ ] (user, F5) the gear opens Settings and the page renders every group.
- [ ] (user, F5) a save is reflected immediately in the row's state and source.
- [ ] (user, F5) changing the catalog connection in Settings still rebuilds the storage (the projects view reloads with the new storage).
- [ ] (user, F5) *Forget my answers* keeps the starter project from being re-offered.

> **Unblocked (2026-10-25):** the application now starts — **packaged**, as the installed signed MSIX — so these four
> checks can be run by launching its Start-menu entry (`docs/HANDOFF.md` item 153; `BL-5.9` is Done). The blocker was
> the deployment itself, never this item. (The unpackaged fallback in item 150 also runs, but it is a development path,
> not the shipped one.)

## Notes
The row editors honour `SettingsKind` (`Text`, `Choice`, `Switch`, `ReadOnly`) so a future choice/switch setting needs no new code. The page renders imperatively, like `ConfigurationPrompt` and `ContainerManagerDialog`, so it needs no XAML plumbing.

## Related
ADR-0016 (decisions 1–9), ST-1, ADR-0013 (the ask surface this complements).
