# BL-5.2 — Add `nuget.config` / document the local feed

| Field | Value |
|---|---|
| **ID** | BL-5.2 |
| **Area** | Area R — Repository hygiene |
| **Type** | Build / onboarding |
| **Priority** | High |
| **Effort** | S |
| **Status** | New |

## Description
A fresh machine cannot restore packages because the local NuGet feed (`c:\nugetlocalfeed`) is not documented/configured. Add a `nuget.config` (or document the feed) so restoring is reproducible.

## Acceptance criteria
- [ ] Fresh machine restores + builds.
- [ ] Feed location documented in the handoff/onboarding.

## Related
`docs/HANDOFF.md` §4.4 item 2; BL-3.1.
