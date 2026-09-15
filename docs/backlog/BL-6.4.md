# BL-6.4 — Environment & services health as first-class

| Field | Value |
|---|---|
| **ID** | BL-6.4 |
| **Area** | Area W1 — Wave 1: distributed platform migration |
| **Type** | Observability / reliability |
| **Priority** | **High (Wave 1)** |
| **Effort** | M |
| **Status** | Implemented — built 0-err, verified live (ready/live/report endpoints) — 2026-09-14 |

## Description
**Environment and services health are first-class citizens.** Make every Wave-1 service expose **ready/live probes** + health reports, publish metrics, and aggregate into a **health dashboard** so the health of the whole Edam ecosystem is visible at a glance.

## Scope
- Readiness + liveness probe endpoints per service (from ServiceDefaults, BL-6.1).
- Structured health reports: service name/version, dependency status (DB, blob, vector), last-seen, error rate.
- Metrics aggregation via OTLP → collector; health dashboard (Aspire dashboard in dev; OTel→Grafana/prometheus in prod as the target).
- An **environment overview** that answers "is the ecosystem healthy?" — the operational heartbeat.

## Dependencies
- BL-6.1 (Aspire + OTel), BL-6.3 (services to probe), BL-4.9 (crash/telemetry alignment).

## Acceptance criteria
- [x] Every service has ready/live probes + a health report callable. → WebApi `WaveOneServiceHealthCheck<T>` per Wave-1 service; `/health` (ready) + `/alive` (live) from ServiceDefaults; **`/health/report`** structured environment overview (always available, not Development-gated). **Verified live**: `/health`→200 Healthy, `/alive`→200 Healthy, `/health/report`→200 `{overall=healthy, timestamp, services[…]}`.
- [x] Metrics aggregated OTLP; health visible on a dashboard. → ServiceDefaults (BL-6.1) configures OTel metrics + HTTP/Runtime instrumentation + OTLP export; the per-service checks feed the Aspire dashboard /health graph. **Pending**: collector/dashboard confirmed during a full Aspire host run (user shell) — Grafana/Prometheus noted as prod target.
- [x] Environment overview page/query shows per-service + dependency health. → `/health/report` returns per-service name/version/kind/status/health + overall verdict + last-seen timestamp. Dependencies (DB/blob/vector) flow through the service descriptor once BL-6.6 lands.

## Progress (2026-09-14)
- `Edam.WebApi/HealthChecks.cs` — generic `WaveOneServiceHealthCheck<T>` (drives ASP.NET health result from the service's descriptor).
- `Program.cs` — `AddHealthChecks()` with a check per Wave-1 service (tags `wave1`); new always-on `/health/report` heartbeat endpoint.
- Verification: VS MSBuild 0-error; ran with `Development` env; probed `/health`, `/alive`, `/health/report` — all 200 (see evidence above).

## Why Wave 1
Directly realizes "metrics, diagnostics and environment/services health are first-class" — the operational heartbeat of the distributed platform.
Directly realizes "metrics, diagnostics and environment/services health are first-class" — the operational heartbeat of the distributed platform.
