# BL-4.5 — Secrets & configuration management (i1 #3)

| Field | Value |
|---|---|
| **ID** | BL-4.5 |
| **Area** | Area E — Enterprise |
| **Type** | Infrastructure / security |
| **Priority** | High (i1 #3) |
| **Effort** | M |
| **Status** | New |

## Description
Add a secrets + configuration-management tier so the multi-shell/API/MCP/components era has a single, secure way to source configuration and secrets. Currently absent from the stack (flagged in `docs/EDAM-v2.0-Tech-Stack.md` review).

## Scope
- **Secrets:** Azure Key Vault (or equivalent) in cloud; User Secrets/DPAPI for local dev; never secrets in code or config.
- **Configuration:** layered config-as-code (appsettings + environment + a small config provider), consistent across WinUI shell, future API/CLI/MCP.
- Behind an interface + DI (`IConfigurationProvider`/`ISecretStore`), swappable per environment.

## Acceptance criteria
- [ ] No secrets in source; secrets sourced from a secure store.
- [ ] Consistent configuration loading across shells.
- [ ] Interfaces + DI with environment-specific implementations.

## Related
Tech-stack §5 (open decisions), BL-4.6 (identity), security posture S2 in the standards baseline.
