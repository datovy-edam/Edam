# ST-3 — Arguments "Domain Details" as a stated default (and the seeded template defect)

| Field | Value |
|---|---|
| **ID** | ST-3 |
| **Area** | Area ST — Settings surface |
| **Type** | Feature / content fix |
| **Priority** | Medium |
| **Effort** | S |
| **Status** | Done (2026-10-12) — headless verified |

## Description
The arguments contract (§2.3 step 2) defines `Domain` as `DomainId` (required) plus an optional `Description`, and the specification says the application *"looks for it when an arguments file is selected and adds the domain to the EDAM database when it is missing"*. Two registry items now state the domain a **new** arguments document is written with — and the **shipped starter template states it too**, which fixes a real defect.

## The defect this closes
`app-data/Edam.App.Data/Templates/ToAssets.Args.json` had **no `Domain` section at all**. Every project the default-project flow (DP-4) creates seeds that template into `/Arguments/<project>.ToAssets.Args.json`, so the application's **own** validator immediately reported `domain-missing` on the project it had just created for the person. A starter artifact that fails its first check teaches the wrong lesson.

## Delivered
- `Edam:Arguments:Domain:DomainId` — default **`Edam.Sample`** (organization-first and dotted, per ADR-0013 decision 11), validated by `ConfigurableItems.ValidateDomainId` (letters/digits/`.`/`-`/`_`, must start with a letter, no leading/trailing dot/space, ≤ 128 chars, **no separators** so it cannot describe a location).
- `Edam:Arguments:Domain:Description` — default *"The asset domain of the starter project."*, bounded at 512 chars; **empty is a stated value** and is respected (CF-1).
- Both are **`SilentDefault`** policy: documented, overridable, **never asked** — a sane value ships, so the first run is not interrupted.
- **Two items, not one object**, because the overlay stores key/value pairs and each field keeps its own validator (ADR-0016 decision 10).
- The shipped `ToAssets.Args.json` now states `Domain` with exactly those values.

## Verified
`arguments (§2.3)` **12/12 ALL CONFORM** (the template still validates), `default project (DP-5)` **8 + 8 ALL CONFORM** (the seeded document is unchanged in structure and still resolves its `./Files` reference), and `registry (CF-2)` **17/17** with the two new entries.

## Deliberately NOT changed
`Namespace.OrganizationDomainId` in the shipped template still reads `cjust.ise` (legacy seed data that predates this work). Changing what a shipped template says about an organization could alter how `XsdToAssets` resolves the domain at run time — that is a behavior change, not a settings surface, and is left as a tracked follow-up rather than made silently. Making the two settings **rewrite documents** (rather than describe the default) is ST-5.

## Related
ADR-0016 (decision 10), `docs/arguments-schema.md` §1 step 2, ADR-0013 decision 11, DP-4.
