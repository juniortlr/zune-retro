# EDD-03 PRIMARY-REVIEW CHECKLIST (EXACTLY what requires Sol / primary approval)
Source authority: PHASE_1_EVIL_DRIVEN_EXECUTION.md §8 (operating contract) + EDD-03/EDD-04 specs.
This checklist binds the permitted draft; none of these have been edited by this delegated task.
Reviewed 2026-09-05. Retained for provenance; unchecked items are not evidence of completion.

The primary's authorized continuation and actual results are recorded in [EDD-03 evidence](../../evidence/phase1/EDD03_LISTENER_RECOVERY.md). Historical delegation restrictions below do not request additional user permission. This checklist itself is not a gate decision.

## A. Token / process validation (primary only)
- [ ] Sender identity verified before wire DTO accepted (token, process, session integrity).
- [ ] Expected executable identity validated where platform permits.
- [ ] No anonymous / cross-session / elevated-parent spoof accepted.

## B. ACL descriptors (primary only — EDD-04 overlap)
- [ ] Named pipe / mutex / named objects have EXPLICIT non-inheriting user-only ACL.
- [ ] Descriptor creation reviewed (not inherited default); cross-user / cross-session denied.
- [ ] No ACL change made by this draft (verified: no file under src/ edited).

## C. COM / native handle ownership (primary only)
- [ ] Listener loop native accept handles: owned / disposed by which component?
- [ ] Per-connection handles: bounded, released on timeout / failure, never leaked.
- [ ] No native handle embedded inside pure-Core Wire DTO (design respects this; primary verifies integration).

## D. Listener loop (primary only — EDD-03 core)
- [ ] Exception-handling per accept: catch, classify, continue; loop never terminates on bad frame.
- [ ] Timeout value chosen and verified bounded (design proposes bounded; primary sets).
- [ ] Concurrent connections bounded (not unbounded); 10,000-frame test provisioned.
- [ ] Completion supervised (Task / cancellation); not fire-and-forget.
- [ ] Health exposure contract finalized (count / bounded state only; no token/SID/handle).

## E. Wire DTO / deserialization (design approved; implementation primary)
- [ ] Strict-mode parser selected / configured (primary approves to prevent deserialization attacks).
- [ ] Unknown version / command / source / edge / geometry rejection logic finalized (not just "reject" — how: close connection? log? rate-limit?).
- [ ] Geometry uses checked ordered bounds, positive dimensions, virtual-screen limits, and current-monitor/work-area intersection; valid negative coordinates are preserved.
- [ ] Extra-field / extra-capability rejection enforced by parser, not by after-check.
- [ ] Implementation path through `ActivationPipeProtocol.cs` / `SingleInstanceCoordinator.cs` requires separate authorization per file (not implied by this design).

## F. Production source files (forbidden from this draft — must stay untouched until primary approves separately)
- [ ] `src/EmberStart.Windows/Instance/ActivationPipeProtocol.cs` — original delegated draft made no edit.
- [ ] `src/EmberStart.Windows/Instance/SingleInstanceCoordinator.cs` — original delegated draft made no edit.
- [ ] `src/EmberStart.Core/Activation/ActivationRequest.cs` / `ActivationCommand.cs` / `ActivationSource.cs` — NOT edited.
- [ ] Any ACL descriptor file — NOT edited.

## G. Gate / commit / authority
- [ ] No commit made by this task (verified).
- [ ] No gate claim made (DG-1/DG-2 ITERATE; G1a not run; DG-3/G1b pending reproducible evidence).
- [ ] No "approved" / "merged" / "ready for production" claim in this draft.
- [ ] Primary reviewer must declare G1a / EDD-03 pass separately — agent cannot.
