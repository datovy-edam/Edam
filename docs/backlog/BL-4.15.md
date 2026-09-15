# BL-4.15 — Containerized testing: use Docker where it makes sense

| Field | Value |
|---|---|
| **ID** | BL-4.15 |
| **Area** | Area E — Enterprise / testing (extends BL-4.7 CI/CD, BL-4.14 testing) |
| **Type** | Testing / infrastructure |
| **Priority** | Medium |
| **Effort** | L |
| **Status** | New |

## Description
Adopt the principle: **"where it makes sense, use a Docker container"** — for testing the EDAM *web-app and service* ecosystem (and their dependencies). Deliberately **not** a blanket directive: desktop/WinUI and the fast headless unit/model tier stay out of Docker.

## Tiered strategy

- **Tier 0 — headless unit/model (`Edam.Test.Studio`, etc.):** no Docker. Runs everywhere incl. CI; stays fast. Baseline 26-pass suite remains the no-Docker regression gate.
- **Tier 1 — containerized service integration tests:** the core "use Docker" tier. Spin up real dependencies on demand per run via **Testcontainers** — PostgreSQL, MinIO/S3, Redis, vector store (pgvector), an OIDC provider. Real integration coverage in CI with no shared-state flakiness.
- **Tier 2 — Aspire/Compose full-stack environment:** `docker compose` / .NET Aspire app-host (its default local run) to bring up API + MCP + web shell + dependencies together for end-to-end smoke. Consistent with the v2.0 Aspire/OTel observability pillar.
- **Desktop (WinUI):** out of Docker — Windows-host UI automation (cf. BL-4.14) covers the UI-host cases.

## Considerations / risks
- **pythonnet / `Edam.Language.Python`** must be installed in any image that runs the Python-dependent services/tests.
- **Local NuGet feed (`c:\nugetlocalfeed`)** must be reachable inside containers/CI (volume mount or feed endpoint) or restore fails in a clean container.
- Keep Tier 0 unwrapped in Docker to preserve test speed.
- Validate behavior on this Windows host + CI with a small spike before finalizing (Aspire + one service + Postgres via Testcontainers).

## Acceptance criteria
- [ ] Tier 1 Testcontainers integration test for at least one service + Postgres (validated on this setup/CI).
- [ ] Tier 2 Compose/Aspire full-stack smoke runs.
- [ ] Tier 0 unaffected; headless suite green.
- [ ] Desktop explicitly documented as Windows-host (no container).

## Related
BL-4.7 (CI/CD + containerization), BL-4.14 (contract + UI tests), tech-stack orchestration/testing rows; recommended ADR: "Docker where it makes sense for service/web testing; desktop on Windows host."
