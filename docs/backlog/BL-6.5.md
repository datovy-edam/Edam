# BL-6.5 — (Parked) Python scripting as a service

| Field | Value |
|---|---|
| **ID** | BL-6.5 |
| **Area** | Area W1 — Wave 1: distributed platform migration |
| **Type** | Capability |
| **Priority** | Parked — dropped by decision |
| **Effort** | M |
| **Status** | **Parked (2026-09-15)** — Python scripting removed. Needed functionality will be delivered through **MCP** tools (shaped later on **Microsoft Agent Framework / MAF**), not a Python runtime. See ADR-0004. |

## Decision (2026-09-15, user)
Remove all Python references. Functionality the platform needs will be managed through MCP instead of arbitrary Python execution; a pythonnet / `Edam.Language.Python` runtime is intentionally **not** offered in Wave 1.

## What was removed
- `Edam.Services.Contracts/PythonExecution.cs` (`IPythonExecutionEngine`, `PythonRunRequest`/`PythonRunResult`) — **deleted**.
- `Edam.Services.Core/PythonExecution.cs` (`FakePythonExecutionEngine`) — **deleted**.
- `AddWave1Services()` `IPythonExecutionEngine` → `FakePythonExecutionEngine` registration — **removed**.
- `Edam.Cli` `edam python` command + usage line (`Usage: edam <wave1|health>`) — **removed**.
- `Edam.WebApi` `/python` endpoint — **removed**.
- Wave-1 inventory + backlog + handoff + ADR references — **updated**.
- **Legacy `Edam.Language.Python` library (`src\Edam.Libraries\AI\Edam.Language.Python\`, pythonnet)** — **kept dormant** by user decision (not in `Edam.slnx`; unreferenced by Wave-1). It is intentionally *not* resurrected as a runtime; future functionality goes through MCP.

## Why
Python no longer justifies a dedicated runtime/surface when the platform will expose needed functionality to agents and automation via **MCP tools** (per the MCP/MAF direction — ADR-0005). Removing it reduces surface area (no native-runtime dependency, no pythonnet image requirement in BL-4.15).

## Acceptance (superseded)
Former BL-6.5 acceptance (execution-engine interface + CLI/MCP/server avenues + headless semantic scoring) is **revoked** by this decision. Semantic/analytics scoring, if later needed, becomes an MCP tool.
