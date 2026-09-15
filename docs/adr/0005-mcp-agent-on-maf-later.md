# ADR-0005 — MCP / AI-agent shaped later on Microsoft Agent Framework (MAF)

- **Status:** Accepted — direction pinned (2026-09-15, user decision)
- **Context:** The MCP / AI-agent / coder / chat surfaces are **not** being shaped in this iteration. The tech stack governs AI via **Microsoft Agent Framework (MAF)**, not Semantic Kernel. The existing feed package `Edam.Mcp` currently resolves `Microsoft.SemanticKernel.Core 1.68.0` (critical advisory GHSA-2ww3-72rp-wpp4) transitively through `Edam.AgentFramework.Core 1.0.1` (→ `Microsoft.SemanticKernel.Connectors.Qdrant`) and `Edam.Format.SemanticKernel 1.0.1`.
- **Decision:**
  1. **Pin** MCP / AI-agent / coder / chat work — no further shaping now. Revisit later built on **MAF**.
  2. When the MCP/AI surfaces are built, they must use **MAF**; the feed-side packages (`Edam.AgentFramework.Core`, `Edam.Mcp`, `Edam.Format.SemanticKernel`) must be republished on MAF (drop the SemanticKernel dependencies) before any production MCP/AI work.
- **Source location (note, 2026-09-15):** `Edam.AgentFramework.Core` is **owned in-house** — source is a sibling solution at `C:\Users\esobr\source\repos\Edam.AgentFramework\Edam.AgentFramework.slnx` (`src\Edam.AgentFramework.Core\Edam.AgentFramework.Core.csproj`; sample `MAF_EdamFormat_Sample`). The MAF republish is implementable there and the feed (`c:\nugetlocalfeed`) republished from it.
- **Consequences:**
  - `Edam.Mcp.Shell` stays compiled (BL-6.3 acceptance) but is **pinned**; the `NU1904` SemanticKernel advisory is recorded as a dependency risk until the feed owner republishes on MAF.
  - Python (ADR-0004) is not resurrected as a runtime — the future MCP/AI layer manages functionality via MCP tools.
- **Related:** ADR-0004.
