# R3b cold-process recovery contract and fixture

Date: 2026-09-10. Scope: transport/process feasibility; no automatic application recovery is implemented by this fixture. ADR-006 remains the local readiness decision. DG-1/DG-2 stay ITERATE.

## Recovery boundary

1. Claim ownership only through the production secured canonical mutex factory's newly-created result. A process existing or exiting, a connect timeout, `Ready=false` on a secondary, or an abandoned mutex wait is not permission to become primary.
2. An existing mutex may precede pipe readiness. A secondary gets the existing 500 ms connect deadline; timeout keeps the request unapplied only if it is known no request transmission began. The current WPF application reports failure. This slice does not silently add retries.
3. Before a future recovery attempt, a secondary closes its own coordinator/mutex handle. Otherwise it can keep the old name alive after the original primary exits. Opening another handle while retaining the old one cannot establish fresh ownership.
4. Concurrent contenders may create only one new primary while its handle remains open. A losing contender is a secondary and must validate the endpoint normally. No deletion, takeover of an untrusted object, permission broadening, or forced termination of another application is permitted.
5. Never automatically replay a request after its transmission may have begun, including a read/ACK timeout, disconnect, malformed response or identity mismatch after connection. A timeout does not prove the handler did not apply a toggle. Request IDs correlate responses; no deduplication/exactly-once protocol is claimed.
6. Future production recovery needs explicit transmission-stage outcomes, a finite total startup budget, bounded contention/backoff, and cancellation handling before startup integration. Do not infer a safe retry from a general IOException alone. Security failures fail closed. Preserve the separate 500 ms transport deadlines unless an explicit later decision changes them.

## Executable observations

The new ActivationFixture apphost accepts fixed stdin control commands and emits typed JSON events. Every child derives actual current SID/session, uses the production coordinator and validators, and substitutes only randomized test object names. Server/client children use the same executable image so production image-path validation remains active. It does not launch the WPF UI, read application inventory, change startup, or touch the installed resident.

Four tests exercise: mutex-before-pipe timeout then a distinct successful operation; original process exit while the secondary still retains the mutex; five real secondary processes closing their handles before concurrent canonical creation; and a handler that applies once but delays its ACK beyond the client deadline. Observations include actual PID, ownership result, request IDs, handler events and elapsed send time. Tests do not infer events from constants or sort planned events to claim application order.

The contention test intentionally coordinates closure of every old handle. It proves uniqueness after release, not liveness under arbitrary unsynchronized real-world contention. The no-replay test records current single-send behavior and demonstrates uncertainty; it does not prove a future retry implementation safe.

Fixture orchestration uses an eight-second observation timeout and a thirty-second process watchdog; these are test infrastructure deadlines, not changes to transport budgets. Cleanup attempts graceful exit, then kills only an explicitly spawned fixture process if necessary. No global process-name termination occurs.

## Still required before recovery ships

Implement a bounded recovery state machine and stage-aware send contract, with fault injection proving no retransmission after partial write or lost ACK; test uncoordinated contenders and cancellation. Retain namespace-collision/security rejection tests. Then integrate into App startup and exercise actual WPF windows/foreground behavior. Cross-user/session/token tests, full shutdown supervision, admission fairness, G1 campaign/soak, packaging and installed RetroBar evidence remain separate work.
