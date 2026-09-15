# BL-6.1 — Aspire app-host + ServiceDefaults baseline (observability first)

| Field | Value |
|---|---|
| **ID** | BL-6.1 |
| **Area** | Area W1 — Wave 1: distributed platform migration |
| **Type** | Infrastructure / foundation |
| **Priority** | **High (Wave 1)** |
| **Effort** | M |
| **Status** | **Complete** — verified 2026-09-14 |

## Description
Stand up the **.NET Aspire** App Host and ServiceDefaults baseline for the distributed platform. This is the substrate the user's Wave 1 plan calls for: **metrics, diagnostics, and environment/services health are first-class citizens.**

## Scope
- **Aspire App Host** project wiring the Wave-1 services (API, MCP, CLI, execution/Python service, persistence) as resources.
- **ServiceDefaults** project with the standard defaults: **OpenTelemetry** (traces, metrics, logs), **health checks** (readiness/liveness), **resilience** (Polly), configuration binding — applied to every Wave-1 service.
- Aspire's built-in resource/dashboard for local environment + health view.
- OTLP export path (collector/dashboards) as the production observability destination, kept separate from the dev-time Aspire scaffold.

## Dependencies
- **BL-3.1 (.NET 10)** first — Aspire compatibility baseline.
- Alignment with `docs/EDAM-v2.0-Tech-Stack.md` (Aspire/orchestration + OTel), BL-4.7 (CI/CD).

## Acceptance criteria
- [x] App Host + ServiceDefaults build on .NET 10; at least one service runs under Aspire with OTel + health checks emitting. **Verified 2026-09-14:** all three projects build **0-error / 0-warning** on net10; user ran `dotnet run --project src\Edam.AppHost` and the `edam-web-api` resource launched under Aspire as expected.
- [x] Traces, metrics, logs visible in the Aspire dashboard (dev) and OTLP-collectable (prod). ServiceDefaults wires OTel 1.15 + OTLP exporter (dev-time via Aspire CLI bundle; prod path via `OTEL_EXPORTER_OTLP_ENDPOINT`). User confirmed the dashboard ran. **Deferred (separate item):** standing up the production OTLP collector/dashboards — kept separate from the dev scaffold per scope.
- [x] Health endpoint (readiness/liveness) present per ServiceDefaults host. `MapDefaultEndpoints()` maps `/health` (ready) + `/alive` (liveness, `live`-tagged) in development; `AddDefaultHealthChecks` adds the `self` liveness check.

## Verified 2026-09-14
Scaffold + build of the Aspire baseline is **Complete**. Independent check in-sandbox after the user's warm restore: `Edam.ServiceDefaults`, `Edam.WebApi`, `Edam.AppHost` each build **0 Error / 0 Warning** (net10, cache-resolved). User confirmed the AppHost runs and the dashboard/resource launched as expected.

## Why Wave 1
First-class observability means the distributed platform is observable from day one — metrics/diagnostics/health are not an afterthought.

## Blocker (environment) — RESOLVED / scaffolded 2026-09-14
- **Templates:** user installed `Aspire.ProjectTemplates` (on-PATH `dotnet` SDK 10.0.401) → `aspire-apphost`, `aspire-servicedefaults`, `aspire-starter`, … now available. (The in-SDK `dotnet workload` installer is still broken — the `InstallerBase` type-initializer exception — so the NuGet template package route was used instead.)
- **Scaffold (offline, in-repo):** root solution **`Edam.slnx`** + `src\Edam.AppHost` (SDK `Aspire.AppHost.Sdk/13.5.3`, `net10.0`) + `src\Edam.ServiceDefaults` (OTel 1.15.x + resilience + health; `AddServiceDefaults`/`MapDefaultEndpoints`) + `src\Edam.WebApi` (minimal API, wired to ServiceDefaults). `Edam.slnx` lists all three.
- **Remaining (network-bound — user shell):** `dotnet restore Edam.slnx` then `dotnet build Edam.slnx` (fetches `Aspire.AppHost.Sdk` + OTel instrumentation packages from nuget.org), then run the AppHost and verify the WebApi resource runs with OTLP + `/health`/`/alive` emitting. My sandbox is offline for this restore. **→ DONE 2026-09-14:** user restored/built and ran the AppHost; confirmed working.
- **Naming decision:** the Wave-1 API service is **`Edam.WebApi`** (not `Edam.Api`) to avoid colliding with the existing `src\Edam.Libraries\Data\Edam.Api` Data API Builder library.
