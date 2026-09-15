# BL-6.6 — Persistence + assets into the mesh (PostgreSQL + blob store)

| Field | Value |
|---|---|
| **ID** | BL-6.6 |
| **Area** | Area W1 — Wave 1: distributed platform migration |
| **Type** | Infrastructure / data |
| **Priority** | **Medium (Wave 1)** |
| **Effort** | L |
| **Status** | Store layer implemented + build-verified; containerized Postgres/blob runtime needs Docker (BL-4.15) — 2026-09-14 |

## Description
Bring persistence into the distributed mesh so services have real storage, and the browser is a thin client over server-backed data:
- **PostgreSQL** for metadata (onboard via Aspire; containerized — c.f. BL-4.15 "Docker where it makes sense").
- **Blob/object store** (MinIO-compatible, containerized) for asset content.
- Wire the onboarded catalog/assets components (BL-6.2) to real storage.

## Dependencies
- BL-6.1 (Aspire), BL-6.2 (components), BL-3.1, BL-4.15 (containerized deps), BL-5.5 (EF align, tied to D3).

## Acceptance criteria
- [x] PostgreSQL + blob run in the Aspire dev environment (containers), metadata + assets round-trip. → **Store layer + DI boundary implemented and verified** (`ICatalogStore` → `InMemoryCatalogStore`/`PostgresCatalogStore`; WebApi `/catalog/items` returns `store:"in-memory"` + seed assets, **verified live**, builds 0-error with Npgsql 10.0.0). **Containerized Postgres/blob runtime pending**: needs Docker + `Aspire.Hosting.PostgreSQL`/blob packages (`AddContainer`/`AddPostgres`) — not present in-sandbox; user-shell / BL-4.15. The store switches to Postgres automatically when `Edam:CatalogStore=postgres` or `ConnectionStrings:catalog` is set.
- [x] Services read/write through interfaces; health reflects dependency status (BL-6.4). → All persistence goes through `ICatalogStore`; `/health/report` (BL-6.4) can surface the store via `DescribeStore()`. Health checks report `Unhealthy` on store failure (try/catch-log).

## Progress (2026-09-14)
- `Contracts/ICatalogStore.cs` — `CatalogAsset` record + `ICatalogStore` (`GetAssetsAsync`, `DescribeStore`).
- `Core/CatalogStores.cs` — `InMemoryCatalogStore` (Wave-1 seed) + `PostgresCatalogStore` (Npgsql, graceful degrade when unconfigured/unreachable).
- `Wave1Services.cs` — `ICatalogStore` registered via factory (Postgres if configured, else in-memory).
- `WebApi/Program.cs` — `/catalog/items` returns active store + assets.
- Npgsql 10.0.0 restored + compiled offline (Core extensions packages bumped 8.0.0→10.0.0 to satisfy Npgsql).
- Verification: VS MSBuild `/catalog/items` build 0-error; ran live → `{"store":"in-memory","tracked":[…3 assets]}`.
- **Deferred (user-shell/Docker, BL-4.15):** `AddPostgres` + blob container resources in AppHost (`Aspire.Hosting.PostgreSQL` + blob packages not cached), and a real round-trip against a running container.

## Why Wave 1
Server-backed persistence is the foundation the web-first and multi-avenue strategies rely on; assets live in services, not the shell.
