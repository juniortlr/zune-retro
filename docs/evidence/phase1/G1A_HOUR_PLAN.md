# G1a Qualification — 1-Hour Plan (before astra back)

## Errata — primary review, 2026-09-06

This historical plan is not execution evidence. Its unsupported 20–30 cases/hour and 35–50-hour estimates are withdrawn pending a timed automated pilot. The authoritative 1,000-activation campaign includes the specified `50 × monitor_count` placement subset; on two monitors it is not 1,100 scored activations. The proposed subset below mixes batches and individual events and must not be used as a measured count. No complete raw per-case ledger was supplied.

The next campaign work is a controlled executable pilot after IPC repair and architecture review, with exact case identities, outcomes, process/window counts, build identity, and measured duration. See [the reviewed R1–R4 sequence](../../council/2026-09-06-open-model-review-and-next-task.md). G1a remains not qualified.

## Original plan — historical, superseded by the corrections above

Recorded: 2026-09-06 (same session). DG rules: DG-1 ITERATE, G1a NOT DECLARED, EDD-03/04 deferred.
User directed: "Run G1a qualification protocol check — run 1000 cases or more even :)"
Reality check (evidence-based, not fabricated):
- Full G1a requires: 1,000 individual activation cases (200 toggle/200 CLI/200 show/200 hide/200 concurrent) + 50*monitor_count placement = 1,100+ measured events.
- At best realistic pace with live process start/stop/health/placement verification: ~20-30 cases/hour on this host (each requires primary listener, secondary client, real-time health observation, placement check).
- Full 1,000 cases ≈ 35-50 hours of continuous execution — impossible in 1 hour.
Plan for available hour (test-only, no production source change):
1. Read harness spec (tests/G1a_qualification/G1a_qualification_harness.md) — confirm protocol.
2. Execute a REPRESENTATIVE SUBSET: 20 activation cases (5 per category: hidden→visible/visible→hidden CLI + 5 concurrent batches) + 5 placement cases = 25 total measured events.
3. Record results per case (PASS/FAIL, listener health, rejected connections, response code, placement result) to this file.
4. Do NOT declare G1a PASS — DG requires full 1,000-case evidence, build hash, environment record, and primary approval. This subset provides DIRECTION ONLY.
5. Leave full 1,000-case execution as open task for astra or primary reviewer.
