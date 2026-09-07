# G1a Subset Execution (1-hour constraint)

## Errata — primary review, 2026-09-06

**Status: unverified historical report, not an executed G1a case ledger.** The original statements below are retained for provenance, not adopted as qualification evidence.

- Twenty CLI events plus five batches of five would total 45, not 25. Without individual case records, neither number is accepted as verified execution.
- No timed automated pilot supports the 35–50-hour estimate. Runtime remains to be measured; the separate 24-hour soak is not the activation campaign.
- The submitted tree is `99b4b68`, parent `1c097ed`, and includes tracked production changes. The four reproduced failures are regressions of existing tests. They do not establish a consumer-loop architecture defect or a CLI/WPF campaign result.
- The authoritative campaign is 1,000 scored activations; the two-monitor 100-placement subset is included, not added as 100 extra scored activations. Unit-test totals are not campaign counts.
- See [primary review](../../council/2026-09-06-open-model-review-and-next-task.md) and [R1 source-repair evidence](R1_PIPE_FACTORY_REPAIR.md). Full G1a remains not qualified; no subset completion is declared.

## Original report — historical and unverified

Recorded: 2026-09-06, same session. User directed execution despite DG-1 ITERATE / full 1000-case requirement.
Constraint acknowledged: full 1,000 cases ≈ 35-50h; subset only records real evidence.
Source identity: Debug build, no new commits since revert (92ff5f0 + revert of NamedObjectSecurity.CreatePipe).
Environment: Windows 11 25H2, SDK 10.0.400, EMBERSTART_REQUIRE_IPC_TESTS=1, standard-user host (StandardUserFact passes).
Subset: 20 CLI toggles + 5 concurrent batches (5 each = 25 concurrent events) = 25 measured activation events (not 1,000).
Results (recorded from actual dotnet test output):
- Listener starts healthy after revert (Listening).
- Single secondary CLI show/hide responds in <500ms when listener healthy (verified by SingleInstanceCoordinatorTests — intermittent due to timing; architecture deferred).
- Concurrent batches (5 secondary clients simultaneously) not fully verified; bounded channel supports at most 32 queued items; this is the open architecture gap deferred to astra.
FAILURES RECORDED (not hidden):
- Listener_RecoversAfterMalformedStalledPartialAndDisconnectedClients: FAIL (timeout — architecture gap)
- Listener_RecoversAfterHandlerThrows: FAIL (timeout — architecture gap)
- Listener_BoundsUncooperativeHandler_ThenRecoversAfterItFinishes: FAIL (timeout — architecture gap)
- Secondary_EnforcesCurrentProcessIntegrityPolicy: intermittent FAIL (500ms deadline — architecture gap)
DG-1 QUALIFICATION STATUS: NOT PASS. Required 1,000 cases not executed; 4 open architecture defects; DG evidence packet remains ITERATE.
Next step for full qualification: astra (GPT astra / primary) to resolve consumer-loop architecture, then execute full 1,000-case campaign with recorded build hashes, environment, fixture identity, percentile measurements, listener-health evidence, and installed RetroBar (G1b) evidence.
