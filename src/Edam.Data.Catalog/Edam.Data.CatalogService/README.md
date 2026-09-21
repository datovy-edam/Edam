# Edam Data Catalog Service

The Wave-1 catalog REST API (BL-7.x) — an ASP.NET Core minimal-API host over the
provider-agnostic `ICatalogStore`/`IContentStore` seams (ADR-0006 / ADR-0007).
The storage provider is selected by configuration and never named by the host;
the default target is **PostgreSQL**.

## Endpoints

Every catalog route is mounted under `/catalogservice/` (see `CatalogServiceMap`):
`container/*`, `catalog/item*`, `catalog/data*`, `catalog/branch/*`,
`catalog/content/*`, plus the session `*/info` routes.

Path-addressed **content** (the `IContentStore` blob/binary seam, ADR-0007) is also on the
wire — mapped only when a content provider resolved for the configured target:

| Route | Purpose |
|---|---|
| `GET /catalogservice/content/info?resourcePath=` | descriptor (existence) at a resource path |
| `GET /catalogservice/content/item?resourcePath=` | payload, base64 in `ContentInfo` (absent → `Exists=false`) |
| `POST /catalogservice/content/item` | write/replace (body: `ContentInfo` with `ContentBase64`) |
| `DELETE /catalogservice/content/item?resourcePath=` | delete |

Clients use it through the existing seam — `ICatalogClient.Content` is an `IContentStore`
(`OpenReadAsync`/`WriteAsync`/`DeleteAsync`/`ExistsAsync`); base64 is a wire detail.

Operational endpoints (from `Edam.ServiceDefaults`):

| Route | Purpose |
|---|---|
| `GET /health` | readiness (all checks; mapped in Development) |
| `GET /alive` | liveness (`live`-tagged checks) |

> **Known follow-up — OpenAPI:** the service used to map `/openapi/v1.json`, but the
> only `Microsoft.AspNetCore.OpenApi` package available offline is **9.0.5** (net9),
> which faults on the net10 runtime. Re-add `AddOpenApi()`/`MapOpenApi()` together with
> the **10.x** package once it can be restored online.

## Run it standalone

```powershell
# 1. PostgreSQL (repo root) — the default target
docker compose up -d --pull always

# 2. the API
dotnet run --project src/Edam.Data.Catalog/Edam.Data.CatalogService
# -> http://localhost:5194/catalogservice/
```

> Run it as a project (`dotnet run --project`) or from the app directory: the host
> uses the current directory as its content root, so `appsettings.json` must be in it.

## Run it under the Aspire AppHost (default stack)

```powershell
dotnet run --project src/Edam.AppHost
```

The AppHost starts `edam-catalog-service` next to the `catalogdb` PostgreSQL
container, waits for the database, and injects `ConnectionStrings__catalog`
(double-underscore maps to `ConnectionStrings:catalog`) plus
`Edam__Catalog__Target=postgres`. The dashboard shows the catalog API with its
`/health` state.

## Configuration

| Key | Meaning | Default |
|---|---|---|
| `Edam:Catalog:Target` | `postgres` \| `filesystem` \| `service` | `postgres` |
| `ConnectionStrings:catalog` | PostgreSQL DSN (or `Edam:Catalog:ConnectionString`) | compose DSN |
| `Edam:Catalog:FileSystemRoot` | root folder when target = `filesystem` | — |
| `Edam:Catalog:ServiceBaseUri` | remote catalog service URI (registers `ICatalogClient`) | — |

## Verify

`Edam.Data.Catalog.HttpConformance` hosts this service **in-process** (via
`Program.BuildApp`) and drives it through the real `CatalogHttpClient` over the
REST wire:

```powershell
Edam.Data.Catalog.HttpConformance filesystem
Edam.Data.Catalog.HttpConformance postgres
```
