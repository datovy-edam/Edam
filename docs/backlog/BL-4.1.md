# BL-4.1 — Diagnostics modernization: ResultLog → Microsoft.Extensions.Logging (MEL) bridge

| Field | Value |
|---|---|
| **ID** | BL-4.1 |
| **Area** | Area E — Enterprise / instrumentation |
| **Type** | Refactor / enterprise alignment |
| **Priority** | **High** |
| **Effort** | L |
| **Status** | **Complete** — verified 2026-09-14 |

## Description
Bring the `Edam.Diagnostics` subsystem (in `Edam.System`) onto **Microsoft.Extensions.Logging (MEL)** as its logging substrate, without breaking the `ResultLog`/`ResultsLog<T>` return-envelope contract used across `Edam.Libraries`.

**Keep** the result envelope (`.Messages`, `.Success`, `.Data`, `.LastException`, `.ReturnValue`) — it is the return contract of many methods. **Do not** replace `ResultLog` with `ILogger` wholesale.

## Approach (additive, non-breaking)
- `ResultLog` holds an injected MEL `ILogger` (`ILoggerFactory.CreateLogger(<category>)`), defaulting to `NullLogger` when absent.
- Forward every `Add`/`Write`/`Trace` to MEL, mapping `Edam.Diagnostics.SeverityLevel`/`Verbosity` → MEL `LogLevel`.
- Extend `IResultsLog` with an MEL logging surface using a **default interface method (DIM)** so existing implementers (`PythonResults`, test fakes) keep compiling — e.g. `Microsoft.Extensions.Logging.ILogger Logger { get; }` with DIM `=> NullLogger.Instance`.
- Adopt `[LoggerMessage]` source-generated structured logging for hot paths (`Trace`).
- Use UTC timestamps and structured fields (no hand-rolled XML, no hard-coded `windows-1252`).

## Related / subtasks
- **BL-4.2** — namespace collision resolution (`Edam.Diagnostics.ILogger` vs MEL).
- Resolve the `EVENT_LOG_SUPPORT` file/event-log no-op (currently `Log.Write` returns `false` unless the constant is defined).
- Replace the mutable static `ResultLog.DefaultLog`/`LogMessageHandler` with a MEL **in-memory `ILoggerProvider`** that raises the WinUI `DiagnosticsLogViewModel` event (thread-safe).

## Acceptance criteria
- [x] Envelopes preserved; all current call sites compile unchanged. **Verified: `Edam.System` builds 0 errors on net10.**
- [x] MEL surface on `IResultsLog` via DIM (`Logger => NullLogger.Instance`); `ResultLog`: injectable `Logger`, `ToLogLevel` mapping, `LogMEL` forwarding; `Add` forwards to MEL.
- [x] Log entries/traces surfaced through MEL providers at composition roots (hooks: `ResultLog.UseLogging`, `Log.UseLogging`, WinUI `InMemoryLoggerProvider`); full app-root factory wiring lands at the Edam.Studio composition root; UTC + structured fields.
- [x] High-performance structured `Trace` logging via a precompiled `LoggerMessage.Define` template (`ResultLog.s_TraceMEL`). *Note:* the source-generated `[LoggerMessage]` form is **not viable** here — Edam.System ships its own `Edam.Diagnostics.Logger` type, and that name collision (BL-4.2) breaks the generator's name resolution; the runtime `LoggerMessage` equivalent is used instead.
- [x] WinUI diagnostics view via MEL in-memory `ILoggerProvider` (`Edam.WinUI.Controls.Logging.InMemoryLoggerProvider`; thread-safe, UTC) replacing the static `DefaultLog`/`LogMessageHandler` subscription in `DiagnosticsLogViewModel`.
- [x] Resolve the `EVENT_LOG_SUPPORT` no-op: `Log.Write` now forwards to a MEL logger bound at a composition root (`Log.UseLogging`) instead of silently returning `false`.
- [x] Headless suite green. **Verified 2026-09-14:** vstest run → **26 passed / 0 failed / 2 not-executed (AddControl UI tests), Total 28** — matching the BL-3.1 baseline.

### Progress / verification (2026-09-10)
Implemented the additive, non-breaking MEL bridge on `Edam.System` `Diagnostics/`: `IResultsLog` gained a DIM `Logger` (collision-safe aliasing, BL-4.2); `ResultLog` added injectable `Logger`, `ToLogLevel(SeverityLevel→LogLevel)`, `LogMEL(IMessageLogEntry)` forwarding, `Add` now forwards to MEL, and `UseLogging(ILoggerFactory)` binds by category at a composition root. **Verified: `Edam.System` builds 0 errors on net10** (offline/cached).

**Runtime harness (new, offline): `src/Edam.Libraries/Tests/Edam.Test.ResultLogMel/`** (net10 console) injects a capturing MEL `ILoggerFactory`/`ILogger` into a `ResultLog` and asserts forwarding. **PASS: `MEL bridge forwarded (level=Critical, msg="bridge works")`** (exit 0). The harness also reproduced the exact BL-4.2 `ILogger` ambiguity in practice and confirmed the fully-qualified-MEL disambiguation. Note: the harness's `DotNetRun` must pass `-p:GeneratePackageOnBuild=false` to avoid triggering `Edam.System`'s pack (blocked feed).

### Completed 2026-09-14 — BL-4.1 remainder implemented
- **`[LoggerMessage]` high-perf trace:** precompiled `LoggerMessage.Define<string>(4001, Debug, "{message}")` (`ResultLog.s_TraceMEL`), used by `LogMEL` for the Debug path (see collision note above).
- **WinUI in-memory MEL provider:** new `Edam.WinUI.Controls.Logging.InMemoryLoggerProvider` (thread-safe, UTC, ring-buffer) + `DiagnosticsLogViewModel` rewired off the static `ResultLog.LogMessageHandler` onto the provider's `EntryLogged`. `Edam.WinUI.Controls` builds 0-error.
- **`EVENT_LOG_SUPPORT` no-op:** `Log.Write(LogSettings, SeverityLevel, String, String)` now forwards to `Log.UseLogging(factory)`-bound MEL logger; falls back to legacy `false`.
- **Package refresh:** `Edam.System 1.0.0` republished to `c:\nugetlocalfeed` with the bridge + new edits so package consumers (Edam.WinUI.Controls) see `ResultLog.Logger`. (Global cache cleared to re-extract the same-version package.)
- **Verification:** `Edam.System`, `Edam.WinUI.Controls`, `Edam.Test.Studio` all build **0-error** on net10.
- **Verified 2026-09-14 (headless sign-off):** user ran vstest in a normal shell → `EdamTest-BL41.trx` shows **Total 28 / Passed 26 / Failed 0 / 2 not-executed** (the `AddControl` WinUI-host tests), unchanged from the BL-3.1 net10 baseline. **BL-4.1 is Complete.**
- **Deferred (recorded):** full app-root `LoggerFactory` composition wiring at the Edam.Studio root (BL-6.1/BL-4.3 territory); the source-generated `[LoggerMessage]` form (blocked by the pre-existing `Edam.Diagnostics.Logger` type — BL-4.2) — runtime `LoggerMessage.Define` used instead.

## Why
Precursor to v2.0 observability (MEL is the substrate OpenTelemetry/OTLP providers plug into); removes custom logging and mutable static state before the multi-shell/API era.
