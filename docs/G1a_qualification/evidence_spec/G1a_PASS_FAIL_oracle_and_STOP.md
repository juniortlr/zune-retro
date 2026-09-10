# G1a Oracles and Stop Conditions — Reviewed 2026-09-05

Status: planned, not executed. Authority: [Phase 1 feasibility specification](../../architecture/PHASE_1_FEASIBILITY_SPEC.md).

## Activation campaign

Require the exact five 200-request groups in the [collection specification](G1a_evidence_collection_spec.md). Preconditioning is separate. Every case must reach its expected state within 500 ms, retain exactly one resident and at most one Ember menu top-level HWND, and preserve native Start. Every secondary exits within one second; each IPC request has one matching bounded result.

Require 50 placement observations per monitor within the campaign, correct monitor selection, final bounds within two physical pixels of the computed clamp, and observed DPI within 0.01 of the target. The prescribed two-monitor topology matrix includes negative coordinates.

A missing, failed, skipped, or excluded required case prevents PASS. Ordering must come from observed execution, not sorting planned sequence numbers or counting acknowledgments.

## Performance and broader qualification

Evaluate warm and cold-process performance using the separate reproducible samples and first-presented-frame oracle. Warm p95 ≤150 ms; cold-process p95 ≤1 second. Do not apply the activation campaign's 500 ms expected-state oracle to the separate cold-process latency measurement.

G1a additionally requires every prescribed accessibility, DPI/text/locale, launch fixture, IPC abuse/identity, recovery, privacy, and packaging criterion. Outstanding Critical/High defects or missing evidence keep the status ITERATE or STOP.

## Stop and failure handling

Stop the affected campaign and preserve evidence for any native-Start lockout, attributable Explorer crash, retained elevation, injection/system modification, duplicate or wrong-monitor open, unbounded IPC/UI wait, or unsupported stable activation route. Stop publication if private data enters an artifact; isolate and redact it before review.

A finite latency miss fails its criterion and requires investigation; do not relabel it an unbounded wait. Re-run only as a new recorded attempt after a correction. Do not hide failing samples.

The owner records separate G1a and G1b decisions. Passing G1a alone permits only the hotkey-launcher-preview claim. Actual installed RetroBar integration and its stable upgrade route are required for G1b.
