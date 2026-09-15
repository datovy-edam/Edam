# ADR-0006 — AgentFramework access boundary: depend on interfaces, never concrete classes

- **Status:** Accepted — boundary mandate (2026-09-15)
- **Context:** EDAM will use the in-house **`Edam.AgentFramework`** solution (MAF-based) to expose AI/agent/chat functionality. The rule is fixed: **EDAM accesses AgentFramework functionality only through interfaces/contracts and never depends on concrete classes or provider-specific resources defined there.** MCP/AI work itself is pinned (ADR-0005); this ADR establishes the dependency boundary that governs it.
- **Grounding (from `Edam.AgentFramework.Core` source, net10):**
  - **Contract types EDAM may depend on:** `IKernelHost` (agent facade: `AIAgent Instance`, `Task<RequestResult> RunWorkflowAsync(JsonDocument, CancellationToken)`), `IKernelIO` (`Write`/`WriteLine`), `ITextChunker` (`TokenAwareChunks`, `ChunkByParagraph`), `IChunkData` (vector-store record: `Id`/`Text`/`Embedding`), and **`RequestResult`** — a `readonly struct` result DTO (`Ok`/`Data`/`Error`, factories `Okey(object?)`/`Fail(string)`). As a stable value-type DTO, `RequestResult` is acceptable to consume as a contract.
  - **Concrete types confined to an adapter/bootstrap layer** (never in EDAM domain code): `KernelHost` (implements `IKernelHost`), `KernelIO`, `TextChunker`, `StoreInstance`/`StoreConfig`, and provider/config classes `ProviderConfig`, `ChatModelConfig`, `EmbeddingConfig`, `ModelProvider`, `ProviderInfo`, plus the Semantic-Kernel `QdrantVectorStore` used inside `KernelHost`.
  - **Known seam gaps to close when MAF work resumes:**
    1. `KernelHost.VectorStore` returns the **concrete SK `QdrantVectorStore`** — there is no store interface yet. Introduce an `IStore`/`IVectorStore<T>` abstraction (e.g., `UpsertAsync`/`GetAsync`/search over `Microsoft.Extensions.VectorData`) so SK stays behind the seam.
    2. `Edam.Mcp`'s `McpServer(KernelHost …)`/`McpTool` currently bind to the **concrete `KernelHost`** — rebind them to `IKernelHost`/`IKernelIO`.
    3. Our pinned `Edam.Mcp.Shell` directly does `KernelHost.PrepareKernelHost(new ProviderConfig())` + `new McpServer(host, …)` + `new McpTool(…, → RequestResult.Okey(…))` — all concrete. It must be refactored so **all** concrete AgentFramework types are created only in a bootstrap/adapter (composition root) and the rest depends on interfaces.
- **Decision / rules:**
  - EDAM references `Edam.AgentFramework.Core` (or a future contracts assembly) **by its interfaces only** (`IKernelHost`, `IKernelIO`, `ITextChunker`, `IChunkData`, `RequestResult`).
  - Concrete AgentFramework + provider/SK types are created **only** in an adapter/bootstrapping assembly (e.g., `Edam.AgentFramework.Bootstrap` in the AgentFramework solution) and injected via DI/composition, never referenced in EDAM WebApi/Cli/Services domain code.
  - No `new KernelHost(...)`, no direct `ProviderConfig`/`StoreInstance`/`QdrantVectorStore` use outside the adapter.
  - **Recommended fixes to the AgentFramework source (its product):** add the store interface; make `KernelHost.VectorStore` return it; change `McpServer`/`McpTool` to `IKernelHost`; and on the MAF republish, **drop `Microsoft.SemanticKernel.Connectors.Qdrant`** (build the store on `Microsoft.Extensions.VectorData` already referenced).
- **Consequences:**
  - Testability and replacement without touching EDAM (swap provider/transport behind interfaces).
  - Enables the MAF republish and the SK advisory removal without impacting EDAM code.
  - A refactor of `Edam.Mcp.Shell` (pinned) is required before MCP/AI work resumes; EDAM domain code (WebApi/Cli/Services) stays interface-only.
- **ADDENDUM — Contract purity & dependency direction (2026-09-15).** The interface-boundary rule is a **platform-wide invariant**, codified:
  - **Direction of dependency:** every module depends *toward* a contracts assembly (`Edam.Data.Catalog.Contracts`, `Edam.Services.Contracts`, AgentFramework contracts); nothing depends back on a provider/implementation.
  - **Purity (no leakage):** provider, transport, and infrastructure concerns never leak into contracts or domain types — no DB-provider types (e.g. Npgsql), no `DbContext`/EF, no connection strings, and no provider-config/transport shapes inside interfaces or entity records. Contracts are expressed purely in catalog/service terms.
  - **Value types/records are the contract:** stable DTOs/records (`RequestResult`, `Wave1ServiceInfo`, catalog entity records) are contract members — not "leaks." They are the published surface and live in the contracts assembly.
  - **Seams appear where a real second target exists or is imminent** — not speculative interface-everywhere. Today the catalog has FileSystem + PostgreSQL (Wave 1.1).
  - **Enforcement:** a **provider-conformance test suite** runs identical behaviors against every provider of a contract (BL-7.2/7.4) — the mechanism that *proves* "swap a back-end without changing callers."
- **Related:** ADR-0002 (governance model), ADR-0003 (anti-drift), ADR-0004 (Python→MCP), ADR-0005 (MCP/agent on MAF).
  - Source: sibling solution `C:\Users\esobr\source\repos\Edam.AgentFramework\Edam.AgentFramework.slnx`.
