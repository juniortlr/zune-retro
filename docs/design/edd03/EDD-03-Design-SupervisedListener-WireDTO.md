# EDD-03 — Supervised Listener + Validated Wire DTO (SUPERSEDED DRAFT)
> Reviewed 2026-09-05: retained for provenance. The primary-owned implementation is authoritative; this draft supplies requirements, not test evidence or approval.

> Continuation note: the owner authorized continued implementation. The primary implemented the strict Windows-layer wire DTO and listener recovery; see [current evidence](../../evidence/phase1/EDD03_LISTENER_RECOVERY.md). The original draft below is historical, including its delegation restrictions. It does not require a new user approval. Invalid but structurally complete geometry uses the canonical fallback; valid negative coordinates are preserved. EDD-04 remains open.
STATUS: DESIGN / DRAFT ONLY — NOT AUTHORIZED FOR PRODUCTION EDIT. Primary (Sol-level) reviewer must approve before any change to `ActivationPipeProtocol.cs`, `SingleInstanceCoordinator.cs`, or ACL descriptors.
OWNER PER CONTRACT (PHASE_1_EVIL_DRIVEN_EXECUTION.md line 244 / §8): primary GPT reviewer retains IPC authorization / ACLs / token & process validation / COM/native handle ownership; local Qwen/Hermus review-only for pure- model/test drafts. This document is the permitted draft; it does NOT authorize production edits.
SOURCE COMMIT CONTEXT: the handoff claimed DG-1 GO. The 2026-09-05 review supersedes that claim with DG-1 ITERATE; G1a remains not run/not declared.

## 1. What EDD-03 requires (spec lines 93-115)
- Deserialize into a wire DTO first (before constructing `ActivationRequest`).
- Reject unknown `ProtocolVersion`, unknown `ActivationCommand` enum values, unknown `ActivationSource`, unknown edge / geometry, and extra capability-bearing fields — BEFORE constructing `ActivationRequest`.
- Per-connection timeouts (bounded, per-accept not global).
- Catch + classify connection-local failures; do NOT let a bad frame terminate the accept loop.
- Listener-completion supervised (not fire-and-forget); health exposed.
- 10,000 generated frames => zero listener loss + bounded allocation.
- STOP / REDESIGN: untrusted frame reaches WPF or can terminate the accept loop.

## 2. Wire DTO design (pure Core, no Win32 / COM / native handle)
```
WireActivationFrame (record, pure Core only; NO native handles inside)
  ProtocolVersion : byte
  RequestId       : Guid
  Command         : byte  (mapped to ActivationCommand; unknown => reject before ActivationRequest)
  Source          : byte  (mapped to ActivationSource; unknown => reject)
  AnchorGeometry  : PhysicalRect?  (valid geometry only; unknown/invalid => reject)
  Edge            : byte? (TaskbarEdge; unknown => reject)
  ExtraFields     : rejected by strict schema (any unrecognized key => reject)
```
Validation order (before ANY `ActivationRequest` construction):
1. ProtocolVersion == 1 (CurrentProtocolVersion) else reject.
2. Command byte maps to defined `ActivationCommand`; else reject.
3. Source byte maps to defined `ActivationSource`; else reject.
4. Geometry: require checked ordered bounds, positive dimensions no larger than the virtual screen, and intersection with a current monitor/work area. Negative virtual-screen coordinates are valid.
5. Edge: if present, must be defined `TaskbarEdge`; else reject.
6. No extra fields permitted — strict deserialization.
7. Only then construct `ActivationRequest` (using validated values).

## 3. Supervised listener (design, not implementation)
- Per-connection `CancellationTokenSource` with bounded timeout (design only; duration requires primary review).
- Catch/classify expected connection-local schema, I/O, validation, timeout, and handler failures, then continue. Requested shutdown terminates normally; do not indiscriminately swallow fatal runtime failures.
- Observe unexpected listener completion promptly and bound active/queued work. `Task.WhenAll` only during shutdown is not sufficient supervision.
- Health exposed via a pure-Core status property only (no WPF/plateform dependency; primary reviews exposure contract).

## 4. Forbidden by contract (NOT done by this draft)
- No edit to `src/EmberStart.Windows/Instance/ActivationPipeProtocol.cs` by the original delegated draft.
- No edit to `SingleInstanceCoordinator.cs`.
- No ACL descriptor edit.
- No token / process validation logic edited (stays primary-only).
- No COM / native handle ownership change.
- No listener-loop production code edited.
- No commit / move / gate claim.

## 5. Evidence / verification (test-only, not production proof)
See preserved sketch `docs/design/edd03/WireDtoValidationTests.cs.draft`. It contains conceptual assertions/placeholders and is not executable evidence.

## 6. Primary-review checklist (EXACTLY what requires Sol-level / primary approval before any production edit)
Per operating contract (§8) and EDD-03/EDD-04 specs, these are NON-DELEGABLE from primary:
- [ ] Token / process validation (who may send a wire frame; identity of sender).
- [ ] ACL descriptors for named pipe / mutex / all named objects (explicit non-inheriting user-only).
- [ ] COM / native handle ownership: listener loop holds accept handles? Who owns / disposes per-connection native handles?
- [ ] Listener loop itself: exception handling, timeout values, bounded concurrency, loop termination conditions.
- [ ] Per-connection timeout duration (design proposes bounded; primary chooses value and verifies bounded allocation).
- [ ] Health-exposure contract: what is exposed (count? state? identity?) and to which callers; must not leak token/SID/handle.
- [ ] Geometry validation rules (what counts as "valid" `PhysicalRect`? negative? oversized? out-of-monitor?) — requires primary because it affects placement / WPF interop.
- [ ] Unknown-version / unknown-command / unknown-geometry rejection logic — semantics must match primary's security model (not just "reject" but HOW: close connection? log? rate-limit attempt?).
- [ ] Wire DTO deserialization library / strict-mode setting (primary approves dependency / parsing strategy to avoid deserialization attacks).
- [ ] Integration with `ActivationPipeProtocol.cs` and `SingleInstanceCoordinator.cs`: any production edit to those requires separate authorization, not implied by this design.
- [ ] Any change to `ActivationRequest` construction path that touches WPF / native paths.

## 7. Confirmed zero source modifications
Verified via `git status` / file-check (see checklist in outputs); only new files under `docs/` and `tests/` created.
