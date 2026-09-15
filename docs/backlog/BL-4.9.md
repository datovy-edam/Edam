# BL-4.9 — Diagnostics: crash/telemetry for desktop + backup/DR for data stores (i1 #7)

| Field | Value |
|---|---|
| **ID** | BL-4.9 |
| **Area** | Area E — Enterprise |
| **Type** | Reliability / ops |
| **Priority** | Medium (i1 #7) |
| **Effort** | M |
| **Status** | New |

## Description
Two operational reliabilities absent from the stack:
1. **Crash reporting / diagnostics telemetry for the WinUI desktop shell** (e.g., Serilog + WER / local crash capture), building on the MEL work (BL-4.1).
2. **Backup / disaster-recovery** for the asset + metadata stores (the RPO/RTO currently listed TBD).

## Scope
- Crash/telemetry channel for the desktop shell; correlate with MEL logs.
- Backup/replication strategy for PostgreSQL metadata + blob asset store; define + validate RPO/RTO (honestly marked pending-confirmation).

## Acceptance criteria
- [ ] Desktop crash captured + correlated to logs.
- [ ] Backup/DR procedure defined; RPO/RTO validated or marked explicit-pending.

## Related
BL-4.1 (MEL), standards baseline S5 (observability); tech-stack persistence row.
