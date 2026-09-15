# Wave 1 — Legacy-Resource Inventory (BL-6.2)

> Classification of `Edam.Libraries` + Catalog capabilities into **hosted service / in-process component / data store**, so Wave-1 onboards them behind interfaces into the Aspire mesh. UI is out of Wave-1 scope (WinUI = reference only).

## Hosted services (expose over the mesh — API / MCP / CLI)
| Capability | Source package(s) | Wave-1 surface |
|---|---|---|
| Minimal API shell (new) | `Edam.WebApi` | ASP.NET Core minimal API, `AddServiceDefaults()`, OpenAPI + catalog ops (BL-6.3) |
| Data catalog | `Edam.Data.Assets`, `Edam.Data.AssetDb`, `Edam.Data.Assets.Services` | `ICatalogService` (hosted) — deep ops + DB in BL-6.6 |
| MCP server (new / existing) | `Edam.Mcp` | MCP wrapper over shared components (BL-6.3; **pinned — shaped later on MAF**, ADR-0005) |
| CLI shell (new) | `Edam.Cli` (planned) | Thin console driving core (BL-6.3) |

## In-process components (DI-composed, run inside the mesh in-process)
| Capability | Source package(s) | Onboarded interface |
|---|---|---|
| Mapping / booklets | `Edam.Data.AssetMapping`, `Edam.Data.Assets` | `IBookletMappingService` (BL-6.2) |
| Vocabulary / lexicon | `Edam.Data.Lexicon`, `Edam.Data.Dictionary` | `IVocabularyService` (BL-6.2) |
| Data models / reference codes | `Edam.DataObjects` | — (BL-6.x follow-ups) |
| Schema / templates | `Edam.Data.Schema`, `Edam.Data.Templates` | — |
| JSON / XML / SQL / Graph | `Edam.Json`, `Edam.Xml`, `Edam.Gsql`, `Edam.GraphQl` | — |
| System / security / net / application | `Edam.System`, `Edam.Security`, `Edam.Net`, `Edam.Application`, `Edam.Help` | MEL bridge (BL-4.1) |
| B2B / connectors | `Edam.B2b`, `Edam.Connector.Atlas` | — |
| Data API Builder | `Edam.Api` | — |

## Data stores (persistence — wired in BL-6.6)
- **Catalog / asset store**: `Edam.Data.AssetDb` (SQLite today → planned **PostgreSQL** containerized) — the catalog's backing store.
- **Blob / asset storage**: planned containerized blob (BL-6.6).
- Reference/code data: via `Edam.DataObjects`/dictionary, persisted in the store above.

## DI composition (BL-6.2 deliverable)
- `src\Edam.Services\Edam.Services.Contracts` (net10) — `IWave1Service`, `ICatalogService`, `IBookletMappingService`, `IVocabularyService`, `Wave1ServiceInfo` (lifecycle/health descriptor).
- `src\Edam.Services\Edam.Services.Core` (net10) — in-memory implementations + `AddWave1Services()` DI extension (catalog = hosted service; booklet/vocab = in-process components).
- Wired into the WebApi in BL-6.3; health/metrics come from ServiceDefaults (BL-6.1).
