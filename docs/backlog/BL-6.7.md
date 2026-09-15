# BL-6.7 — Unified MEL/OTel diagnostics across Wave-1 services

| Field | Value |
|---|---|
| **ID** | BL-6.7 |
| **Area** | Area W1 — Wave 1: distributed platform migration |
| **Type** | Observability / diagnostics |
| **Priority** | **High (Wave 1)** |
| **Effort** | L |
| **Status** | Implemented — Core + WebApi built 0-err; verified running live — 2026-09-14 |

## Description
Make **diagnostics first-class and unified** across the distributed platform: every Wave-1 service logs via **Microsoft.Extensions.Logging** (MEL, from BL-4.1), emits **OpenTelemetry** traces/metrics, and shares **correlation/trace IDs** so a request is followable end-to-end across API → service → store.

## Scope
- Adopt the BL-4.1 ResultLog→MEL bridge across service surfaces; services use `ILogger` with structured fields + UTC + `[LoggerMessage]`.
- Trace propagation (OTel activity/correlation IDs) across services.
- Unified metrics (Meter/counters) per service; aligned with BL-6.4 dashboards and BL-6.1 OTel.
- Resolve the diagnostics name-collision (BL-4.2) as part of this.

## Dependencies
- BL-4.1 (MEL), BL-4.2 (name-collision), BL-6.1 (OTel/ServiceDefaults), BL-3.1.

## Acceptance criteria
- [x] Service logs flow through MEL → OTel; structure + UTC + correlation IDs present. → Every Wave-1 surface logs through injected `ILogger<T>` (MEL) with structured fields; the WebApi `CorrelationIdMiddleware` sets/echoes `X-Correlation-ID` and tags the ambient OTel `Activity`. **Verified live**: `/wave1` returned `correlationId=edam-test-corr-12345` (echoed) + `traceId=381f7d1a…`; MEL console logs from `InMemoryCatalogService`/`Booklet`/`Vocabulary` all carried the same `trace=381f7d1a…`.
- [x] A cross-service request is traceable end-to-end from one trace ID. → OTel `Activity` spans API request → each onboarded service's `Describe()` (trace ID threaded through). Correlation ID follows the HTTP contract end-to-end.
- [x] Per-service metrics emitted to the OTel collector/dashboard. → `AddServiceDefaults()` (BL-6.1) configures the meter + HTTP/runtime instrumentation (AspNetCore/Http/Runtime exporters) and OTLP. **Pending**: collector/dashboard wiring verified during an Aspire host run (user shell) — BL-6.4 dashboards.

## Progress (2026-09-14)
- `Wave1Services.cs` — all three in-memory services now take `ILogger<T>` and emit structured `Describe … trace={Trace}` logs.
- `Edam.WebApi/CorrelationIdMiddleware.cs` — inbound `X-Correlation-ID` (or generated), echoed on response, OTel tag + structured request log.
- `Program.cs` — `AddHttpContextAccessor()`, `UseMiddleware<CorrelationIdMiddleware>()`, `/wave1` returns `correlationId` + `traceId` alongside the service descriptors.
- Verification: VS MSBuild build 0-error; ran live; probed `/wave1` — see acceptance evidence above.

## Why Wave 1
Realizes "diagnostics first-class"; unified, correlated telemetry is the observable backbone of the distributed platform.
