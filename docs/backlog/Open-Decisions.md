# EDAM — Open Decisions & Backlog Triage

> **Purpose:** The heartbeat, per the decision-centric model. Shows (a) which **open decisions** gate the next phase, (b) which backlog items are **Ready** vs **Parked** (blocked by a decision) vs **Idea**. Advance phases by making a decision — a gating decision promotes a whole cluster of items to *Ready* at once. This is a planning aid; authority remains AGENTS.md + approved requirements.

## Triage legend

- **Ready** — executable now (unblocked, verifiable, has a clear done). *Run the Ready foundation set.*
- **Parked (blocked by <decision>)** — needs a decision/spike before it can move. *Do not schedule; resolve the blocker.*
- **Idea** — aspirational/shaping; not committed. *Keep in the sandbox.*

## Open decisions (make these → unlock clusters)

| # | Decision | Blocks (was Parked) → becomes Ready |
|---|---|---|
| **D1** | **Web platform (UI)** — direction: *web-first, .NET (Blazor WASM) primary shell; desktop thin/secondary*. **PARKED for Wave 1** — existing WinUI is a reference model; UI not yet migrated | Re-evaluate after Wave 1 → BL-4.6, BL-4.14 (web slice), web UI |
| **D2** | **AI framework + interface contracts** (`ICoder`, `IChatAssistant`, `IEmbeddingProvider`, `IVectorStore`) — verify **Microsoft Agent Framework** | BL-4.4 (semantic index + AI telemetry), BL-4.3 full scope (governance runtime integration), MCP/AI pillar |
| **D3** | **Persistence model** (EF vs lighter; blob provider; metadata DB) | BL-5.5 (EF align), BL-4.11 (schema-validation/registry), BL-4.9 (backup/DR), parts of BL-4.15 |
| **D4** | **Vector store + embedding provider** | BL-4.4 retrieval; generalizes BL-1.9 semantic scoring headlessly |
| **D5** | **MCP spec version + tool/resource contract** | BL-4.14 MCP contract tests; the AI-integration boundary |
| **D6** | **Observability substrate** (MEL adoption = BL-4.1; OTel/OTLP path) | part of BL-4.9 (crash/telemetry) |
| **D7** | **Distributed-platform scaffold — .NET Aspire** (decided direction for **Wave 1**: observability first-class) | BL-6.1, BL-6.2, BL-6.3, BL-6.4, BL-6.6, BL-6.7 |
| **D8** | **~~Python scripting avenue~~ — RESOLVED: dropped** (functionality managed through MCP instead; MCP/AI shaped later on MAF) | ~~BL-6.5~~ → ADR-0004 / ADR-0005 |

*Already decided/executing:* **.NET 10** (BL-3.1), diagnostics→MEL (BL-4.1), name-collision (BL-4.2).

## Active initiative — **Wave 1: move legacy resources into the distributed platform**
The near-term focus. Platform + back-end + **observability first-class (Aspire)**; **UI/deployed shells are out of Wave 1** (D1 parked). See `Wave-1-Migration.md`. Wave-1 items (Area **BL-6.x**): BL-6.1 Aspire baseline, BL-6.2 onboard legacy resources, BL-6.3 API+MCP+CLI boundary, BL-6.7 unified MEL/OTel, BL-6.4 services health, BL-6.6 persistence+assets, BL-6.5 Python-scripting-as-a-service.

## Ready now — the executable foundation set (do these first, incl. Wave-1 prerequisites)

- **BL-3.1** (.NET 10), **BL-4.1** (ResultLog→MEL), **BL-4.2** (name-collision), **BL-4.3*** (governance runtime — *prototype-able now behind interfaces*), **BL-4.5** (secrets/config), **BL-4.7** (foundation CI), **BL-4.8** (SAST/vuln/SBOM), **BL-4.10** (import/connectors), **BL-4.15** (Docker deps), **BL-5.1–5.6** (repo hygiene).
- **Wave-1 (once foundation + .NET 10 are in):** BL-6.1 → BL-6.2 → BL-6.3/BL-6.7 → BL-6.4 → BL-6.6/BL-6.5.

## Parked (waiting on a decision above) — do **not** schedule until the blocker resolves

- Parked **D1** (web shell): BL-4.6, BL-4.14, most of BL-4.15
- Parked **D2/D4** (AI): BL-4.4
- Parked **D3** (persistence): BL-5.5 (with BL-3.1), BL-4.11, BL-4.9, parts of BL-4.15
- Parked (sequence): BL-4.12 (needs .NET 10 + service layer)

## Ideas (sandbox — not committed)

- BL-4.13 (i18n/l10n)
- New ideas land here first (i-series / notes) and are **promoted to `Ready` only when a decision + definition-of-done matures.**
