# BL-4.8 — Security tooling in-stack: SAST / dependency vulnerability / SBOM (i1 #6)

| Field | Value |
|---|---|
| **ID** | BL-4.8 |
| **Area** | Area E — Enterprise |
| **Type** | Security / tooling |
| **Priority** | Medium (i1 #6) |
| **Effort** | M |
| **Status** | New |

## Description
The standards baseline requires "applicable standards, mechanically enforced" for security (S2), but lists **no tools that do the enforcing**. Add in-stack security analysis.

## Scope
- **SAST:** static analysis (analyzers) wired into the build/CI gate.
- **Dependency vulnerability scanning:** on the local NuGet feed + packages (e.g., `dotnet list package --vulnerable`).
- **SBOM:** software bill-of-materials generation for artifacts/packages.

## Acceptance criteria
- [ ] SAST runs as part of the build/CI gate (0 blocking findings).
- [ ] Dependency vulnerability scan runs; known-vulnerable packages flagged.
- [ ] SBOM produced for built artifacts.

## Related
Standards baseline S2; BL-4.7 (CI); ADR-0002 (enforcement).
