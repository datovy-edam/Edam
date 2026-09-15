# EDAM — Sprint S1 (Next Sprint) plan

> **Status:** Proposed next-sprint plan. Per `docs/backlog/README.md`, items live in files by ID; this file groups the immediate work and sequencing. **Authority:** approved requirements + AGENTS.md remain the current authority; this is a planning aid.

## Sprint S1 — "Platform foundation + enterprise alignment"

Primary goal: land the .NET 10 runtime move and stand up the first enterprise-alignment increments (diagnostics→MEL, name-collision, the top i1 governance gap), plus quick repository-hygiene wins.

### Priority order

1. **BL-3.1 — .NET 10 runtime upgrade** (move off .NET 9). *Do first — unblocks EF/WinAppSDK and everything else. Day 1 = spike (retarget libs + test project, confirm RID/WinAppSDK, keep 26-pass baseline green), then roll out.*
2. **BL-4.1 — Diagnostics modernization (ResultLog → MEL bridge)** — additive, non-breaking; precursor to OTel.
3. **BL-4.2 — Name-collision (Edam.Diagnostics.ILogger vs MEL) + collision scan** — explicit user priority; unblocks BL-4.1 cleanliness.
4. **BL-4.3 — Governance/enforcement runtime + immutable audit log (i1 #1)** — prototype the top stack gap behind interfaces.
5. **BL-5.1 — Fix stale HintPath refs** | **BL-5.2 — nuget.config / local feed doc** — reproducible builds on a fresh machine.
6. **BL-5.5 — Align EF Core + migrations** (with BL-3.1) | **BL-5.4 — async-void/.Wait() cleanup + session management.**

### Carried / next
- Area E: BL-4.4 (codebase semantic index + AI telemetry), BL-4.5 (secrets), BL-4.6 (identity) — high-priority enterprise capabilities after the foundation.
- Area E: BL-4.7 (CI/CD + deployment), BL-4.8 (security tooling), BL-4.9 (crash/DR), BL-4.11 (schema validation/registry) — medium.
- Core/product + testing: BL-4.10 (import/connectors), BL-4.14 (contract + UI tests), BL-4.15 (containerized testing — Docker where it makes sense), BL-4.12 (caching), BL-4.13 (i18n).
- Area R quick wins: BL-5.3 (dead code), BL-5.6 (catalog tests).

### Sprint gates (Definition of Sprint-complete)
- Build 0 errors on .NET 10 across all solutions; headless suite green (baseline 26, re-verified).
- `doc-health-checklist` A–D pass; HANDOFF + backlog current.
- ADRs/decisions recorded (e.g., a diagnostics→MEL ADR; governance-runtime ADR) before binding.
- No stale .NET 9 claims anywhere.
