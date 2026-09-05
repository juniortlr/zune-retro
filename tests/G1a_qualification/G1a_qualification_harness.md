# G1a Harness Contract — Reviewed 2026-09-05

Status: implementation backlog. This Markdown file is not an executable harness and supplies no runtime evidence.

Use the [canonical feasibility protocol](../../docs/architecture/PHASE_1_FEASIBILITY_SPEC.md#reproducible-g1a-protocol), [collection schema](../../docs/G1a_qualification/evidence_spec/G1a_evidence_collection_spec.md), and [oracles](../../docs/G1a_qualification/evidence_spec/G1a_PASS_FAIL_oracle_and_STOP.md).

## Required implementation

1. Create isolated controlled fixtures and bind every run to source, binary hashes, environment, and topology.
2. Launch and enumerate real processes; inspect real menu HWNDs and visible/focus state. Do not infer a HWND from a coordinator object.
3. Observe readiness and matching results with bounded deadlines. Observe hotkey behavior through state; no invented ACK.
4. Execute exactly 1,000 scored activation requests: 200 hotkey toggles, 200 CLI toggles, 200 shows, 200 hides, and 200 concurrent requests in 40 batches of five. Preserve all canonical sub-transition counts and record preconditioning separately.
5. Include 100 placement cases on the two-monitor target, 50 per monitor, including negative coordinates.
6. Record actual application order for concurrent events; never prove ordering by sorting the expected sequence.
7. Run separate warm/cold-process performance samples with first-presented-frame measurement.
8. Collect the remaining DPI, accessibility, launch, security, recovery, privacy, and packaging evidence. The activation campaign alone does not qualify G1a.

Tests of the current-process pipe coordinator and memory-stream decoder are useful lower-level tests. They cannot substitute for this process/window campaign, elevated-parent runs, or installed RetroBar evidence.
