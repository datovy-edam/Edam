# BL-5.1 — Fix stale `HintPath` references in Edam.Data.Catalog.WinUI

| Field | Value |
|---|---|
| **ID** | BL-5.1 |
| **Area** | Area R — Repository hygiene |
| **Type** | Bug / build hygiene |
| **Priority** | High |
| **Effort** | S |
| **Status** | New |

## Description
`Edam.Data.Catalog.WinUI` has stale `HintPath` references; repoint them to the `Edam.Libraries` projects (or use `PackageReference`s per the established local-feed pattern) so a fresh checkout builds.

## Acceptance criteria
- [ ] Catalog solution builds 0 errors on a fresh checkout.
- [ ] References consistent with the package-feed pattern used by Edam.Studio.

## Related
`docs/HANDOFF.md` §4.4 item 1; BL-3.1 (do before/with the .NET 10 retarget).
