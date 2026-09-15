# BL-4.4 — Codebase semantic index + AI coder-compliance telemetry (i1 #2)

| Field | Value |
|---|---|
| **ID** | BL-4.4 |
| **Area** | Area E — Enterprise / AI |
| **Type** | Capability / infrastructure |
| **Priority** | Medium (i1 #2) |
| **Effort** | L |
| **Status** | New |

## Description
Two complementary pieces that make AI coders effective and *observable*:

1. **Codebase semantic index** — chunk + embed the EDAM code/docs, make them searchable through the existing `IRetriever`/`IVectorStore` interfaces (from `docs/EDAM-v2.0-Tech-Stack.md`). This is the retrieval layer applied to *code*, complementing the doc-level anti-drift (ADR-0003).
2. **AI coder-compliance telemetry** — record what each AI coder loaded and which gates/DoD items it satisfied, so drift is visible instead of silent. Feeds the AI-metrics honesty requirement (AGENTS.md §5) and the anti-drift feedback loop (ADR-0003).

## Approach
- Build on the AI interface set: `IEmbeddingProvider`, `IVectorStore`, `IRetriever` — with a deterministic fake for tests (also unblocks headless semantic scoring, cf. BL-1.9).
- Define a compliance/telemetry record shape (artifact → loaded → gates satisfied → evidence) writing to the audit/log substrate (cf. BL-4.3, BL-4.1).

## Acceptance criteria
- [ ] Repo code+docs embeddable/searchable via `IRetriever` behind DI.
- [ ] Compliance events recorded (or marked unavailable) per AGENTS.md §5.
- [ ] Tests inject fake retrieval; headless suite green.

## Related
`docs/EDAM-v2.0-Tech-Stack.md` (§3), BL-4.3 (governance/audit), ADR-0003.
