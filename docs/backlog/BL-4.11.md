# BL-4.11 — Schema-validation library + schema/conformance registry (i1 #9)

| Field | Value |
|---|---|
| **ID** | BL-4.11 |
| **Area** | Area E — Enterprise / AI (spec-schema-first) |
| **Type** | Capability / tooling |
| **Priority** | Medium (i1 #9) |
| **Effort** | M |
| **Status** | New |

## Description
If EDAM produces ISL-style conformance artifacts, it needs a **validator** in-stack (e.g., JsonSchema.Net / FluentValidation) plus a **registry** to host versioned canonical schemas. Complements the governance/conformance registry in BL-4.3.

## Scope
- Validation library for JSON-schema conformance artifacts (`<Reference>JsonSchema.Net</Reference>` or equivalent; behind an interface).
- Versioned registry for canonical schemas + conformance evidence (align with ADR-0002 spec/schema-first pillar).

## Acceptance criteria
- [ ] Schemas validated headlessly; conformance evidence recorded per version.
- [ ] Registry stores/retrieves versioned schemas; ready as a governed resource.

## Related
ADR-0002 (spec/schema-first), BL-4.3 (registry), `docs/CONTEXT.md` §3.
