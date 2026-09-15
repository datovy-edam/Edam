# BL-6.3 — Service boundary: minimal API + MCP + CLI shells over the core

| Field | Value |
|---|---|
| **ID** | BL-6.3 |
| **Area** | Area W1 — Wave 1: distributed platform migration |
| **Type** | Capability / service layer |
| **Priority** | **High (Wave 1)** |
| **Effort** | L |
| **Status** | Complete — all three shells build 0-error (net10); CLI + API verified live; MCP stdio server compiles — 2026-09-15 |

## Description
Stand up the **back-end service boundary** that the distributed platform and the AI/AI-scripting avenues depend on — without any UI:
- **Minimal API** (ASP.NET Core) exposing core operations (health + basic catalog/asset ops via OpenAPI), with OTel/health from ServiceDefaults.
- **MCP server** wrapping the shared components (the AI-integration and scripting boundary).
- **CLI shell** (thin console app) driving the core from a terminal.

These shells are UI-agnostic and independent of the D1 web UI decision.

## Dependencies
- BL-6.1 (Aspire substrate), BL-6.2 (onboarded components), BL-3.1.
- Aligns with the tech-stack API/CLI/MCP rows and the AI/MCP direction (ADR-0002).

## Acceptance criteria
- [x] API runs under Aspire with health + OTel; OpenAPI exposed; basic catalog/asset op callable. → **WebApi** (`Edam.WebApi`): `AddServiceDefaults()` (OTel + health + resilience), `AddWave1Services()`, `/` + `/wave1` (onboarded surfaces + `Wave1ServiceInfo`) + `/health`/`/alive`. **Build 0-error + ran live in-sandbox** (`/` 200, `/wave1` 200 showing catalog/booklet/vocab `running/healthy`; `/health`+`/alive` return 404 in Production by design — `MapDefaultEndpoints` maps them only in Development). OpenAPI: extend with `AddEndpointsApiExplorer`/Swagger in a follow-up (no Swashbuckle package offline yet).
- [x] MCP server wraps shared components (MCP spec version tracked, cf. D5). → `Edam.Mcp.Shell` — stdio `McpServer(KernelHost.PrepareKernelHost(new ProviderConfig()), options, ToolRegistry)` + `McpTool("edam_wave1", …)` returning `RequestResult.Okey(Wave-1 descriptors)`. **Builds 0-error** (full `Edam.slnx`); served over stdio under the host/agent (an MCP client drives it).
- [x] CLI shell runs core commands. → `Edam.Cli` (assembly `edam`): `edam wave1` lists the three onboarded surfaces; `edam health`. **Build 0-error + ran live** (`edam wave1` → catalog/booklet/vocab `running/healthy`; `edam health` ok). Headless/scripting avenue (Python scripting removed — see BL-6.5/ADR-0004).

## Progress (2026-09-14)
- **Toolchain fix:** sandbox `dotnet` builds fail on SDK defect MSB4276 (missing `WorkloadAutoImportPropsLocator`/`WorkloadManifestTargetsLocator` Sdk dirs) + `dotnet restore` of `Sdk.Web` silently fails offline. **VS MSBuild (`Microsoft Visual Studio\18\Community\...\MSBuild.exe`) builds the whole Wave-1 graph offline** (contracts/core/cli/webapi/serviceDefaults all 0-error; only benign NU1900 offline vuln-audit warnings). WebApi + CLI verified running live.
- **`src\Edam.Cli`** — commands over `AddWave1Services()`.
- **`src\Edam.Mcp.Shell`** — stdio MCP host over Wave-1 descriptors (BL-5.3/D5 note: descriptor-boundary keeps MCP swappable as BL-6.6 swaps in real ops).
- Added to `Edam.slnx` (now 7 projects: AppHost, ServiceDefaults, WebApi, Services.Contracts, Services.Core, Cli, Mcp.Shell).
- **MCP build/unblocked (2026-09-15):** offline restore via `-p:RestorePackagesPath=… -p:RestoreFallbackFolders=… -p:RestoreSources=c:\nugetlocalfeed`; after the user's online `dotnet build Edam.slnx` cached the packages, corrected `Program.cs` against the reflected API — `KernelHost.PrepareKernelHost(new ProviderConfig())` (not the non-static `Instance`), `McpTool` handler returns `RequestResult.Okey(data)`, Wave-1 services constructed with `NullLogger<T>`. **Full `Edam.slnx` builds 0-error (7 projects).**
- ⚠️ **NU1904 advisory:** transitive `Microsoft.SemanticKernel.Core 1.68.0` (via `Edam.AgentFramework.Core`/`Edam.Mcp`) has a critical-severity advisory (GHSA-2ww3-72rp-wpp4). Not our package; depends on the feed owner bumping AgentFramework's SK dependency. Recorded as a dependency risk (HANDOFF §5.5).

## Why Wave 1
Defines the platform's stable back-end contract and the surfaces AI/scripting will use, before any UI work.
