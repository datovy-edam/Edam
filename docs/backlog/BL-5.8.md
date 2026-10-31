# BL-5.8 — `catalog (remote HTTP)` fails its own delete semantics check

| Field | Value |
|---|---|
| **ID** | BL-5.8 |
| **Area** | Area R — Repository hygiene / recommended fixes |
| **Type** | Bug (provider contract) |
| **Priority** | Medium |
| **Effort** | S |
| **Status** | New — **found by the settings work (2026-10-12), not caused by it** |

## Description
The projects conformance suite has one **pre-existing** failing check, unrelated to settings:

```
--- catalog (remote HTTP): 1 FAILED (16 checks)
[FAIL] Exists / Delete semantics: existed=True deleted=False gone=True
```

at `src/Edam.Data.Projects/Edam.Data.Projects.Conformance/ProjectScenario.cs:126`. The **delete really happened** (`gone=True`) but the provider **reported `false`**. Every other target (file system, catalog local, catalog MS-SQL) passes the same check.

## Why it matters
`DeleteAsync` returning `false` is how the contract says *"I did not delete it"*. A caller that trusts the return value — a confirmation dialog, a retry loop, a `ResultLog` line, or a future *"nothing left behind"* assertion (LM-9) — will be told the wrong thing about a destructive operation. A "deleted but reported not deleted" is the mirror image of this repository's recurring *silent success* defect, and it is worse to trust because the operation really did happen.

## Likely cause (to confirm, not assumed)
The HTTP-backed catalog store maps a delete to an HTTP call and probably derives its boolean from the **response body** or a status the server does not send for an idempotent delete (e.g. `204 No Content` → no body → parsed as `false`), rather than from the **status code**. Alternatively the remote path deletes via a *container* endpoint that answers `200` with an empty payload. Confirm before changing anything.

## Acceptance criteria
- [ ] The remote provider's `DeleteAsync` returns `true` when the item is gone (idempotent delete of an absent item must still be distinguishable from a failed one, per the contract's own wording).
- [ ] `catalog (remote HTTP)` reports **ALL CONFORM (16 checks)** with the rest of the suite, so the runner's final line is clean for every target.
- [ ] A check proves the *absent-item* case explicitly, so "false because it was never there" and "false because it failed" cannot be confused again.

## Notes
While this fails, the conformance runner's summary line always shows one failure, which is how a real regression could hide. Fixing it is small and is the cheapest way to restore the suite's signal.

## Related
`ProjectScenario.cs:126`; LM-9 (storage provenance, "nothing left behind"); ADR-0016 (recorded as a separate defect so it is not absorbed into the settings work).
