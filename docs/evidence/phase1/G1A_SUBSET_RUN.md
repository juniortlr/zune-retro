# G1a Subset Execution (1-hour constraint)
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
