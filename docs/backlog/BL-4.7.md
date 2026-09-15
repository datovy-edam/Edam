# BL-4.7 — CI/CD + deployment + containerization (i1 #5)

| Field | Value |
|---|---|
| **ID** | BL-4.7 |
| **Area** | Area E — Enterprise |
| **Type** | Infrastructure |
| **Priority** | Medium (i1 #5) |
| **Effort** | L |
| **Status** | New |

## Description
Build gates exist (build 0 errors, headless tests, `doc-health-checklist`), but there is **no pipeline, no containerization, no deployment path**. Add the release/deployment tier for the v2.0 services (Aspire/API/CLI/MCP) and desktop packaging.

## Scope
- **CI:** pipeline running the existing gates (WinUI via VS18 MSBuild; libs via `dotnet build`; headless tests; doc-health).
- **CD/deployment:** containers (Docker) for services; deploy target for Aspire output; win/installer packaging for the WinUI shell.
- Keep honest: pipeline is infrastructure; validate each gate as it lands.

## Acceptance criteria
- [ ] CI runs the standard gates and reports results on PR/commit.
- [ ] Services can be containerized; desktop can be packaged.
- [ ] Deployment path documented for Aspire output.

## Related
Tech-stack build/toolchain + orchestration rows; BL-3.1 (.NET 10) must land first.
