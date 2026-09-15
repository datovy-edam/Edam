# Wave 1 — Move EDAM legacy resources into the distributed platform

> **Status:** Active planning — the near-term initiative. **Scope:** platform + back-end + observability only; **UI components are explicitly NOT in Wave 1** (existing WinUI code is a reference model for a later decision, parked as D1).
> **North star:** make EDAM a distributed, observable platform — **metrics, diagnostics, and environment/services health are first-class citizens**, using **.NET Aspire** as the scaffold.

## What Wave 1 is for
Onboard EDAM's legacy "resources" (`Edam.Libraries` capabilities + the Catalog) into the Aspire mesh as distributable **components and services**, with observability wired in from day one. The browser/UI and the web-first decision (D1) are parked until the platform behind them is real.

## Python scripting — **DROPPED** (2026-09-15)
Python (`Edam.Language.Python`/pythonnet) was formerly scoped to a CLI / MCP / server-side avenue (BL-6.5). **Removed by decision** (ADR-0004): functionality the platform needs will be managed through **MCP** instead of arbitrary Python execution; **MCP/AI-agent surfaces are pinned and shaped later on Microsoft Agent Framework (MAF)** (ADR-0005). No native Python runtime in Wave-1 images.

## Backlog (Area W1 — `BL-6.x`)
| ID | Item | Priority |
|---|---|---|
| [BL-6.1](BL-6.1.md) | Aspire app-host + ServiceDefaults baseline (OTel, health, resilience) | **High** |
| [BL-6.2](BL-6.2.md) | Onboard legacy `Edam.Libraries` resources as components/services | **High** |
| [BL-6.3](BL-6.3.md) | Service boundary: minimal API + MCP + CLI shells over the core | **High** |
| [BL-6.7](BL-6.7.md) | Unified MEL/OTel diagnostics across Wave-1 services | **High** |
| [BL-6.4](BL-6.4.md) | Environment & services health as first-class (probes, metrics, dashboard) | **High** |
| [BL-6.6](BL-6.6.md) | Persistence + assets into the mesh (PostgreSQL + blob, containerized) | Medium |
| [BL-6.5](BL-6.5.md) | ~~Python scripting as a service~~ — **Parked/dropped** (functionality via MCP; ADR-0004) | — |

## Wave-1 prerequisites (land first)
- **BL-3.1** (.NET 10), **BL-4.1** (ResultLog→MEL), **BL-4.2** (name-collision), **BL-4.7** (CI foundation), **BL-4.15** (Docker where it makes sense — service deps are containerized).

## Definition of Wave-1-complete
- App Host + ServiceDefaults run on .NET 10; catalog/mapping/vocabulary surfaces onboarded behind interfaces.
- API + MCP + CLI shells live; each service has ready/live health + metrics, logs unified via MEL→OTel with correlation IDs.
- Environment overview shows ecosystem/health at a glance (Aspire dashboard dev; OTLP→dashboards prod).
- Persistence (Postgres + blob) round-trips via containers.
- Headless suite still green; HANDOFF + backlog current; ADRs recorded for adopted decisions (Aspire baseline; Python-via-MCP = ADR-0004; MCP-on-MAF = ADR-0005).

## Dependencies on other decisions
- D1 (web-first UI) is **parked** — independent of Wave 1.
- D3 (persistence model) partially touched by BL-6.6; D5 (MCP spec) touched by BL-6.3.
