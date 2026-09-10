# EDD-04 Primary-Review Checklist (before any production change — NOT performed by local agent)

Reviewed 2026-09-05; retained as a deferred primary-review checklist. No item is evidence of completion.

- [ ] Token handling: WindowsIdentity.GetCurrent(TokenAccessLevels.Query); SafeAccessTokenHandle disposal; TokenIntegrityLevel (25) read without leaking buffer; no token handle comparison across processes without validation.
- [ ] Object security: explicit non-inheriting user-scoped descriptor with minimum rights; no broad principals/inherited ACEs; owner, access masks, mandatory label, and compatibility verified by read-back and cross-token tests. Select exact APIs only after review.
- [ ] Named-object creation order: mutex/pipe created with descriptor FIRST; readiness / ACK published ONLY after descriptor verification passes; never report ready on pre-existing unknown object.
- [ ] Recovery without unknown-object deletion: recovery verifies current endpoint SID/session/integrity matches descriptor; reconnects to validated object; if mismatch, refuses (does NOT delete unknown named object); reboot/logout not required for legitimate user.
- [ ] Endpoint validation (both sides): SID, session, and permitted integrity checked; installed path/publisher checked where available as defense in depth, without claiming prevention of same-user/same-session/same-integrity impersonation.
- [ ] Rate / bound: queue at most 32; per-client 20 req/s with burst 40; bounded connections/retry compatible with each 500 ms protocol deadline; no unbounded queue.
- [ ] Rejection cases confirmed: elevated (High/System/Protected) → reject; cross-user SID mismatch → reject; cross-session → reject; pre-created object with different descriptor → reject / no takeover.
- [ ] Zero production edits verified: no changes to SingleInstanceCoordinator; no pipe security descriptor additions; no registry ACL changes.

Status (local agent, pre-primary): checklist written; no items checked against production (must remain with primary).
