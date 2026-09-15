# ADR-0004 — Drop Python scripting; manage functionality through MCP

- **Status:** Accepted (2026-09-15, user decision)
- **Context:** Wave 1 proposed a Python-scripting avenue (BL-6.5) — a pythonnet / `Edam.Language.Python` execution engine exposed via CLI / MCP / server-side. Carrying a native Python runtime adds image/dependency burden (e.g. BL-4.15 container images) for an avenue that is not required once the platform exposes its capabilities to agents and automation directly.
- **Decision:** **Remove all Python references.** Functionality the platform needs will be managed through **MCP tools** instead of arbitrary Python execution. Semantic/analytics scoring, if later needed, becomes an MCP tool.
- **Consequences:**
  - Deleted `Edam.Services.Contracts/PythonExecution.cs` (`IPythonExecutionEngine`, `PythonRunRequest`/`PythonRunResult`) and `Edam.Services.Core/PythonExecution.cs` (`FakePythonExecutionEngine`).
  - Removed `IPythonExecutionEngine` registration from `AddWave1Services()`, the `Edam.Cli` `edam python` command, and the WebApi `/python` endpoint.
  - No native Python runtime / pythonnet requirement in Wave-1 images.
  - BL-6.5 is **Parked** (`docs/backlog/BL-6.5.md`).
- **Related:** ADR-0005 (MCP / AI-agent shaped later on Microsoft Agent Framework).
