# ADR-006 — Local activation readiness and lifecycle serialization

- Status: Accepted for the R3a feasibility slice; not a release or DG-1 approval.
- Date: 2026-09-07.

## Problem

Concurrent `StartListening` calls could each enter first-pipe creation and overwrite `_listener` with competing success/failure results. Disposal was not serialized with startup. A secondary's `Ready` task remained pending until disposal, while a successful primary's task stayed true after shutdown without an explicit historical-result contract.

## Decision

- One lifecycle lock protects startup, publication/reading of the listener task, and claiming disposal. A second start observes the first published task and does not create another listener. A start after disposal throws `ObjectDisposedException`.
- Pipe creation/security read-back still precede initial success. This lock does not serialize request handlers, alter the pipe ACL, change connection limits, or replace endpoint validation.
- `Ready` is a one-shot **local transport startup result**. On a primary it is pending before startup, true after successful initial creation, or false on creation failure/disposal before startup. A secondary completes false immediately because it does not own a listener. False says nothing about the remote primary's availability.
- `IsReady` is a current local snapshot: not disposed and listener health is Listening. It is not a lease, a promise that the next request succeeds, or evidence that the WPF UI/catalog is ready. Successful `Ready` remains true after shutdown; `IsReady` becomes false.
- `ListenerCompletion` remains a snapshot: completed before startup/on a secondary, the one published listener task afterward. Callers observing its resident lifetime must start first, as the current WPF host does.
- Disposal captures the listener under the same lock, then releases the lock **before cancellation callbacks and waiting**. Only the winning disposer releases resources. Cleanup is in a finally block; if the listener is still running, resource release remains deferred to its completion.
- The existing one-second listener wait is unchanged. This is not a hard bound on total Dispose duration: synchronous cancellation callbacks or waiting to enter the lifecycle lock can take longer. Comprehensive asynchronous shutdown supervision remains separate work.

## Validation and boundaries

Real tests first reproduced pending secondary readiness and conflicting listener task publication. New tests cover failed startup, disposal before start, historical/current readiness, concurrent disposers, name reacquisition, and start/dispose races. See [R3a evidence](../evidence/phase1/R3A_LOCAL_READINESS.md).

This decision does not claim interprocess readiness publication, cold-start retry/takeover, protection against same-user/session/integrity malware, UI readiness, or complete EDD-04. It does not change the existing 500 ms per-operation deadlines or authorize replay of an uncertain command.

## Next bounded slice

R3b must specify and test actual cold processes: mutex present before pipe readiness, primary exit before connect, and concurrent secondaries attempting recovery. Before attempting fresh creation a secondary must close its own mutex handle; it may become primary only when canonical creation reports newly created. Never modify/delete an untrusted named object or replay a request after transmission may have begun. Produce the recovery contract and a real process fixture before changing application startup flow. Medium-integrity/spec alignment, cancellation supervision and admission fairness remain separately tracked R3 decisions.
