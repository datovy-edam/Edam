# BL-4.12 — Caching / distributed state (i1 #10)

| Field | Value |
|---|---|
| **ID** | BL-4.12 |
| **Area** | Area E — Enterprise |
| **Type** | Infrastructure |
| **Priority** | Low (i1 #10) |
| **Effort** | M |
| **Status** | New |

## Description
Once there are multiple shells/services, shared caching + distributed session/state is needed. Adopt a distributed cache (e.g., Redis) and a consistent cache abstraction behind an interface, DI-bound.

## Scope
- `ICache` abstraction (extend existing `Edam.System\Application\Cache` component) + a distributed implementation for v2.0 services.
- Session/state handling across shells.

## Acceptance criteria
- [ ] Cache abstraction with in-memory (dev/test) + distributed implementations.
- [ ] Multi-shell state handled without shared mutable globals.

## Related
Tech-stack (cache/orchestration), BL-3.1 (.NET 10 first), ADR-0003 (no shared mutable globals).
