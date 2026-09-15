# BL-4.6 — Identity & access infrastructure (OAuth2 / OIDC) (i1 #4)

| Field | Value |
|---|---|
| **ID** | BL-4.6 |
| **Area** | Area E — Enterprise |
| **Type** | Infrastructure / security |
| **Priority** | High (i1 #4) |
| **Effort** | M |
| **Status** | New |

## Description
The standards matrix lists OAuth2/OIDC + RBAC + separation-of-duties as a standard (S3), but the tech stack has **no identity row**. Add the identity/access infrastructure so the future MCP/API/CLI/web/desktop shells share one auth model.

## Scope
- OAuth2/OIDC token handling + validation; auth middleware for API/MCP; least-privilege RBAC; separation of duties (map to governance roles from ADR-0002 §5.3).
- Identity provider integration (external/cloud IdP, or a local identity service component behind an interface).
- **Honest validation status:** model-level implemented; live tenant confirmation pending (S3).

## Acceptance criteria
- [ ] Token validation shared across shells.
- [ ] RBAC + SoD enforceable; roles map to governance roles.
- [ ] Live-tenant confirmation captured as pending, not overclaimed.

## Related
Standards baseline S3; ADR-0002 §5.3; BL-4.5 (secrets), BL-4.3 (governance/audit).
