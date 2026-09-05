# Phase 1 Decision-Gate Evidence Status — Reviewed 2026-09-05

Branch: `codex/phase-1-foundation`

Provenance under review: commits `8511280`, `2859321`, and `92ff5f0`, plus the uncommitted Hermes handoff drafts. The earlier packet recorded runtime `thinkingmachines/inkling-small:free (openrouter)` and claimed DG-1 GO, DG-2 verification, and a user-verified RetroBar result. This review preserves those statements as provenance; it does not adopt them as evidence-backed decisions.

## Reviewed disposition

- **DG-1 — ITERATE.** The submitted EDD-01 and EDD-02 tests do not exercise all gate oracles against the live listener/process/window path. EDD-03 production validation and listener supervision are under primary review. EDD-04 remains deferred.
- **DG-2 — ITERATE.** EDD-05 is absent and EDD-07 requirements are not implemented or evidenced in full. Existing geometry work is useful foundation evidence, not DG-2 GO evidence.
- **DG-3 / G1b — PENDING reproducible evidence.** The handoff reports that the owner manually observed the installed RetroBar Start button invoke Ember Start on the correct monitor while preserving `Ctrl+Esc` and avoiding hooks/injection. No build hash, environment, fixture identity, raw result, stable upgrade-route record, or foreground-transfer record accompanies that report in the repository, so it remains an unverified report rather than a gate decision.
- **G1a — NOT RUN / NOT DECLARED.** The repository contains a draft protocol and evidence schema only.

## Why the earlier DG-1 claim is superseded

- The baseline deserializes directly into `ActivationRequest`; it does not reject every unknown enum, extra field, missing field, or invalid rectangle before constructing the trusted model.
- The baseline listener can fault permanently on malformed JSON, a per-operation timeout, or a handler exception because those failures are not all handled as connection-local failures.
- The EDD-01 recovery helper uses a new `MemoryStream`, not the resident listener after rejection. Its handler-exception case does not invoke the throwing handler.
- The EDD-02 harness does not launch cold processes, bind its fake server to the named pipe, count real resident processes or HWNDs, or verify actual event order. Its HWND result is a constant and its ordering check sorts before asserting monotonicity.
- The elevated-parent test includes synthetic/conditional assertions and a constant pipe result; it is not the required elevated runtime/process recovery evidence.

These files remain useful adversary-case inventories and draft scaffolding. They must not be cited as passing live acceptance criteria without replacement tests and recorded results.

## EDD-03 / EDD-04 status

- EDD-03: strict wire validation and listener recovery implemented in the primary continuation. See [measured results and limitations](docs/evidence/phase1/EDD03_LISTENER_RECOVERY.md). The deterministic 10,000-frame corpus tests the decoder; it is not a 10,000-connection listener/heap campaign or full DG-1 qualification.
- EDD-04: deferred. The baseline does not provide the full explicit ACL/readiness/admission contract, both-endpoint validation, bounded queue/rate limit, or recovery proof. Same-user/same-session/medium-integrity impersonation remains outside the security boundary under the authoritative feasibility spec and must not be claimed prevented.

## Exact G1a activation campaign

The campaign is **1,000 individual activation cases**:

- 200 hotkey toggles: 100 hidden→visible and 100 visible→hidden.
- 200 CLI toggles: 100 hidden→visible and 100 visible→hidden.
- 200 sequential CLI shows: 100 hidden→visible and 100 visible→visible.
- 200 sequential CLI hides: 100 visible→hidden and 100 hidden→hidden.
- 200 concurrent IPC requests: 40 batches of five, comprising 20 show batches from hidden and 20 hide batches from visible.

Placement is `50 × monitor_count`; on the specified two-monitor target this is 100 cases. At least one tested topology must place a monitor at negative coordinates. It is not a third monitor target.

## Evidence needed before any upgraded status

Record source/build identity, environment, fixture identity, raw case results, percentile calculations, listener-health and resource evidence, open defects, and privacy review. G1b additionally needs the installed RetroBar version/configuration, stable entry-point and upgrade route, physical anchor/edge and monitor result, foreground-transfer result, and native-fallback observation. Gate declarations remain owner decisions after the evidence is complete.
