# EDAM — Tech Stack (Draft for review)

> **Status:** Draft. Separates what is **verified today (v1)** from what is **v2.0 target/proposed** (not yet adopted; each choice needs an ADR). AI components are modeled as **abstract interfaces instantiated via DI** — implementations are replaceable.
> **Last updated:** 2026-09-10
> **Companion:** `docs/EDAM-v2.0-Introduction.md`, `docs/EDAM-Engineering-Standards-and-Enterprise-Guidance.md`, ADRs 0001–0003.

## 1. Guiding constraints (applies to every layer)

1. **UI-agnostic, reusable components** — core behavior is portable and platform-independent; UI is one shell.
2. **Program-to-interfaces + DI** — components are replaceable; AI included (no framework lock-in).
3. **Spec/schema-first + governed** — the stack must support schema conformance and governed, gated AI work (ADR-0002/0003).

## 2. Layered stack

| Layer | Verified now (v1) | v2.0 target (proposed — ADR-tracked) |
| --- | --- | --- |
| **Language / runtime** | .NET 10 (`net10.0` libs; `net10.0-windows` WinUI) — **migration complete + verified 2026-09-13 (BL-3.1)**. | .NET 10 (current LTS); Aspire-compatible. |
| **Build / toolchain** | VS18 Community MSBuild (WinUI), `dotnet build` (libs), `vstest`, local NuGet feed `c:\nugetlocalfeed`, `.editorconfig`+analyzers, CPM. | Same gates + CI (build 0 errors, headless tests, `doc-health-checklist`). |
| **Shared core** | `Edam.Libraries` (plain .NET; mapping/booklets, JSONata, persistence round-trip). | Extend as reusable component libraries; keep UI-free. |
| **UI shells** | WinUI 3 / Windows App SDK (1.2); `CommunityToolkit.Mvvm 7.1.2`; Monaco (web) in WebView2. | WinUI desktop **+** web shell — **Uno-on-WASM vs Blazor** (open), UI-agnostic core. |
| **API** | — (desktop only) | ASP.NET Core Minimal API + OpenAPI; API versioning; error envelope (stability boundary). |
| **CLI** | — | `System.CommandLine` thin shell over shared components (small footprint). |
| **MCP server** | — | .NET MCP server wrapping shared components; the AI-integration boundary. |
| **Orchestration & observability** | — | .NET Aspire app-host + service-defaults (OpenTelemetry, health checks, resilience/Polly); OTLP ingest → dashboards (deployment separate). |
| **Persistence** | EF Core 6.0.25 + DB; Newtonsoft.Json 13.0.3; `Edam.Data.Assets`. *(Version skew risk: EF/WinAppSDK 2023-era vs .NET 9)* | PostgreSQL (+ EF or lighter access) for metadata; **blob/object storage (MinIO/Azure)** + index for assets; content-addressable, versioned, provenance. |
| **AI / Agents** | pythonnet + `Edam.Language.Python`; script-based text similarity (`ITextSimilarityService`). | AI behind **interfaces** (`ICoder`, `IChatAssistant`, `IRetriever`, `IEmbeddingProvider`, `IVectorStore`, `ITextSimilarityService`); implementations on **Microsoft Agent Framework / Semantic Kernel / AutoGen (candidate — verify)** + models via Azure OpenAI/OpenAI/local; vector store (pgvector/Azure AI Search/Qdrant). |
| **Scripting/extensibility** | pythonnet; `Edam.Language.Python`. | Keep as a component behind an interface (execution engines swappable). |
| **Testing** | MSTest 3.6.4, `Microsoft.NET.Test.Sdk 17.12.0`, VS18 vstest; headless suite (26 passing). | Keep headless model tests + add service/integration tests; AI components tested via interface fakes/stubs. |

## 3. AI components as abstract interfaces (DI-instantiated, replaceable)

Scope AI capabilities as interfaces; bind concrete implementations at each **composition root**. This is the "AI as pluggable component" principle — no framework/model lock-in.

| Interface (proposed) | Responsibility | Swappable implementations |
| --- | --- | --- |
| `IChatAssistant` | Conversational AI (chat) | Microsoft Agent Framework / Semantic Kernel dialog; model-backed or RAG |
| `ICoder` | AI coding/generation within the governed pipeline (readiness-gated) | Semantic Kernel skills; Agent Framework coder; custom |
| `IRetriever` | Semantic retrieval over EDAM assets/docs | Vector-store backed; keyword fallback |
| `IEmbeddingProvider` | Text → embedding vectors | Azure OpenAI / OpenAI / local embedding model |
| `IVectorStore` | Persist/query vectors | pgvector / Azure AI Search / Qdrant / in-memory (tests) |
| `ITextSimilarityService` | Text-similarity scoring (generalize the BL-1.9 script-based service) | Script-based; embedding-cosine; stub (headless tests) |

**Why behind interfaces:** AI is the fastest-moving dependency — the ability to swap the framework/model/library without touching consumers is a hard requirement, and it also lets tests inject deterministic **fakes** (which is how we finally unlock headless testing of the semantic score, per BL-1.9).

## 4. Microsoft Agent Framework / AI Chat & Coders — posture

- **Candidate** implementation behind `IChatAssistant`/`ICoder`. **Verify** the current product/version before pinning.
- Because it sits behind interfaces, adopting or replacing it is a **composition change** (an ADR), not a refactor of consumers.
- AI outputs feed the **governed readiness pipeline** (ADR-0002): Draft → Reviewable → Machine-Valid → Autonomous-Ready, with human gates and AI metrics — never un-governed autonomy.

## 5. Open decisions needing ADRs

- **Web shell:** Uno-on-WASM vs Blazor.
- **Aspire** version vs .NET SDK; prod deployment target (containers/K8s/cloud).
- **Persistence:** EF vs lighter access; blob provider; metadata DB.
- **Vector store + embedding provider** selection.
- **Agent framework** selection; pin `ICoder`/`IChatAssistant` contracts.
- **MCP spec version** and tool/resource contract.

## 6. Honesty note

The **v1 column is verified** (works today). The **v2.0 column is proposed direction** — not adopted, and must not be treated as fact until each item is ADR-tracked and validated (build + tests + live environment). AI framing is sound but the specific agent framework is unverified while web search is unavailable.

> **Runtime note:** **BL-3.1 (Area P) COMPLETE — verified 2026-09-13.** The codebase now targets **.NET 10 (current LTS)**: plain libs `net10.0`, WinUI `net10.0-windows10.0.26100.0`, with Windows App SDK 1.6. Headless test suite re-verified green (26 pass / 2 skip). Remaining dependency-hygiene skew (EF Core 6.0.25, WinAppSDK-era packages) tracked under BL-5.5. **Dependency-security cleanup (2026-09-18):** the `Edam.Studio.sln` NuGet Audit advisories were cleared with direct version pins — **5 of 6 fixed** (SqlClient 2.1.4→5.1.6, Caching.Memory 6.0.1→10.0.0, System.Drawing.Common 4.7.0→6.0.0, IdentityModel.Jwt/JsonWebTokens 6.8.0→8.19.2); the remaining `SQLitePCLRaw.lib.e_sqlite3` 2.0.4 (high) needs network access to obtain its fixed version. See **BL-5.7**.
