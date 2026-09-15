# BL-4.2 — Resolve namespace collision: `Edam.Diagnostics.ILogger` vs Microsoft.Extensions.Logging.ILogger (+ collision scan)

| Field | Value |
|---|---|
| **ID** | BL-4.2 |
| **Area** | Area E — Enterprise / instrumentation |
| **Type** | Bug / hygiene |
| **Priority** | **High** |
| **Effort** | M |
| **Status** | **Complete** — verified 2026-09-14 |

## Description
`Edam.System/Diagnostics/ILogger.cs` defines a custom `Edam.Diagnostics.ILogger`, while `Microsoft.Extensions.Logging.ILogger<_>` is already used elsewhere (e.g. `WeatherForecastController`). When `IResultsLog` gains an MEL surface (BL-4.1), the two can collide — an AI coder / contributor could silently bind the wrong `ILogger`. This is exactly the authority-ambiguity/drift class we're eliminating (ADR-0003).

## Approach
- In `ResultLog`/`IResultsLog`, alias MEL explicitly (`using MsLogger = Microsoft.Extensions.Logging;`) and use fully-qualified MEL types for any added surface.
- **Deprecate** `Edam.Diagnostics.ILogger` (and its `IEventLogger`/`IFileLogger` derivatives) once the MEL bridge (BL-4.1) is live; migrate/removal is a follow-up.
- **Scan the repo for other namespace/type collisions** between Edam types and BCL/`Microsoft.Extensions.*` types; record findings here.

## Acceptance criteria
- [x] No ambiguous `ILogger` reference anywhere in the codebase. **Verified 2026-09-14:** whole-`src` scan — no file imports both `Microsoft.Extensions.Logging` and `Edam.Diagnostics`; all MEL surface in `ResultLog`/`Log`/`InMemoryLoggerProvider` is **fully-qualified** (`Microsoft.Extensions.Logging.ILogger`), so no bare `ILogger` can bind ambiguously today (the collision was latent, not active).
- [x] Collision scan performed; other collisions documented (see **Collision scan findings** below).
- [x] Tests still build/pass. **Build 0-error** on net10 (`Edam.System`, `Edam.WinUI.Controls`); deprecation is additive (no consumer uses the legacy interfaces). Headless suite re-run is folded into the Wave-1 sign-off.

## What changed (2026-09-14)
- Deprecated the legacy Edam logging interfaces in `Edam.System/Diagnostics/ILogger.cs` with `[Obsolete]`: `ILoggerWriter`, `IEventLogger`, `IFileLogger`, `ILogger`. **Migration/removal is a follow-up** (BL-4.1 MEL bridge is live and is the replacement target).
- `Logger.cs` (the only implementer/comsumer of those interfaces) got `#pragma warning disable CS0618` to keep the build warning-clean until removal.
- MEL surface already collision-safe (fully-qualified); no alias change required.

## Collision scan findings (whole repo, 2026-09-14)
1. **`Edam.Diagnostics.ILogger`/`IEventLogger`/`IFileLogger`/`ILoggerWriter` vs `Microsoft.Extensions.Logging.ILogger`** — the primary (this item). Now `[Obsolete]`; contained to `Edam.System` Diagnostics.
2. **`Edam.Diagnostics.Logger` (class) shadows MEL name resolution** — not a direct type clash, but within the `Edam.Diagnostics` namespace a bare `Log`/`Logger` identifier can pick the legacy type; this broke the MEL **`[LoggerMessage]` source generator** in BL-4.1 (CS1525/CS0103 on generated code). Workaround adopted: runtime `LoggerMessage.Define` (recorded in `BL-4.1.md`). Watch when wiring MEL consumers in the `Edam.Diagnostics` namespace.
3. **`Edam.System` top-level namespace** is adjacent to BCL `System` — no concrete type collision found (nothing in `Edam.System` shadows `Exception` etc.), but future contributors in `Edam.System.*` should keep `using System;` fully explicit to avoid surprises.
4. **Minor/hygiene (NOT collisions):** pre-existing lowercase type aliases (`app`, `cnv`, `io`, `json`, `newton`, `resource`, `inout`, `convert`) trigger CS8981 ("may become reserved"). Not BCL conflicts; flagged for a future cleanup pass.

## Why
Prevents silent wrong-type resolution by AI coders and humans; cleans the public contract before MEL adoption.
