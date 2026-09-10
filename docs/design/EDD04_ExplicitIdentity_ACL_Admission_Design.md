# EDD-04 Design — Explicit Identity / ACL / Admission Control (SUPERSEDED DRAFT)

> Reviewed 2026-09-05: retained for provenance. EDD-04 remains deferred. Exact APIs, ACEs, rights, mandatory-label behavior, and recovery semantics require primary design and live verification.

Status: DESIGN / TEST-DRAFT (no production edits). Owner per contract (line 244, EDD-04): **primary GPT reviewer ONLY**. Local agent may produce this doc + pure Core property draft; MAY NOT implement non-inheriting ACL descriptors, security descriptors on named objects, or pipe/registry ACL changes.

Source contract: docs/architecture/PHASE_1_EVIL_DRIVEN_EXECUTION.md lines 102-107, 117-135 (EDD-04 wave B). References ProcessIntegrityGuard.cs (lines 52-54 integrity allow-list: Untrusted/Low/Medium only; High/System/Protected rejected); ProcessIntegrityGuardTests.cs (minimal).

## 1. ACL Descriptor Format (user-only, non-inheriting)

For every named object created (mutex + named pipe):
- Set SECURITY_ATTRIBUTES.bInheritHandle = false (non-inheriting).
- Security descriptor = explicit DACL, NO inheritance from parent process.
- Grant only minimum rights required by the current interactive user; prohibit broad principals and inherited ACEs. Verify owner, access masks, mandatory-integrity behavior, and compatibility before fixing an exact ACE recipe.
- Named-object creation order: named object created with descriptor FIRST; readiness published ONLY after creation succeeds and descriptor is verified (read-back of DACL / handle rights). Pre-existing object = rejection, not takeover of unknown object.
- Integrity gate in parallel: allowed only Untrusted/Low/Medium (line 52-54). Elevated (High/System/Protected) = rejected at admission.

Descriptor fields (design schema — NOT added to SingleInstanceCoordinator):
- OwnerSID = current interactive user SID (string form, not token reference).
- DACLEntry = ACE_ACCESS_ALLOWED with UserSID only; no GROUP_ACE, no INHERITED_ACE.
- Inherit = false on both mutex (CreateMutexEx) and pipe (CreateNamedPipe) security attributes.
- IntegrityRequired = Medium or lower (enforced by ProcessIntegrityGuard.Evaluate).

## 2. Admission Rules (both endpoints validated; same session; readiness after creation)

Endpoint validation (client + server at activation/ACK time):
- SID same: both endpoints' WindowsIdentity.User SID match (exact equality, not group overlap).
- Session same: both in same Terminal Services session (WTS session ID, not just process parent).
- Integrity same: both return allowed (Medium-or-below); elevated client = reject.
- Executable identity: where available, validate installed path or publisher as defense in depth. This does not prevent impersonation by a same-user, same-session, same-integrity process, which is outside the specified security boundary.

Sequence (readiness after pipe creation):
1. Create named mutex + named pipe with descriptor (non-inheriting, user-only).
2. Verify descriptor read-back (DACL contains only user SID; inherit false).
3. Only then publish readiness / respond ACK.
4. Client connects: validates server endpoint SID/session/integrity; if any mismatch → reject, do NOT send ACK.

Rate / bound (design values, NOT wired to production):
- Active connection rate limit: 20 requests/sec sustained.
- Burst: 40 (leaky bucket / token bucket, bounded, not unbounded queue).
- Bounded retry must fit the separate 500 ms protocol deadlines. Never delete or take over an unknown named object; same-user squatting may remain a detectable availability failure.
- Elevated clients: rejected at admission (integrity > Medium OR token elevated); no privilege-crossing permitted.
- Cross-user: blocked (SID mismatch); cross-session: blocked (session mismatch).

## 3. Recovery Design (no unknown-object deletion)

- Legitimate standard user recovers WITHOUT logout/reboot.
- Recovery path: verify current endpoint identity (SID/session/integrity) matches expected descriptor; if yes, reconnect to existing validated named object; if no, refuse connection (do NOT delete an unknown object to "clean up").
- Stop/redesign trigger (contract line 107): recovery must NOT depend on deleting an unknown named object. If recovery requires unknown-object deletion, redesign.

## 4. Pure Core Property Draft (tests ONLY — not wired to coordinator)

File: `docs/design/EDD04_AdmissionControlProperties.cs.draft` (preserved conceptual sketch, not compiled or executed).

Properties to assert (pure, no Win32 dependency in assertion logic; use mock token/identity fixtures in Core):
- ElevatedClientRejected: given token integrity = High (0x3000+), admission = false; message indicates elevation rejected.
- CrossUserBlocked: given server SID != client SID, admission = false.
- CrossSessionBlocked: given session IDs differ, admission = false.
- PreCreatedObjectRejected: when named object already exists with different descriptor (unknown object / different SID / inherited), new take-over = false; do NOT delete unknown object.
- ReadinessAfterCreation: readiness flag only true after pipe-creation verification; false if descriptor read-back fails.
- NonInheritingDescriptor: descriptor Inherit = false; DACL contains exactly one user SID entry.
- RateLimitEnforced: 20/s sustained, burst 40, bounded queue (no unbounded retry).

The preserved sketch contains placeholders and a Windows-layer type reference; it is not a genuine pure-Core test or production proof.

## 5. Non-Delegatable Pieces (per contract — must stay with primary reviewer)

Per line 244 (EDD-04) "primary GPT reviewer only" and line 105-107 acceptance/stop conditions:

- Actual non-inheriting user-only ACL descriptor construction (CreateMutexEx / CreateNamedPipe SECURITY_ATTRIBUTES + DACL edit via SetSecurityDescriptorDacl / AddAccessAllowedAce). Local agent must NOT add descriptors to production source.
- Named-object creation order validation (pipe created before ready reported; descriptor verified before ACK) — requires real Win32 object lifetime; not delegable to pure Core.
- SID + session + executable-identity endpoint validation at admission — requires real WindowsIdentity + WTS session + process-image checks; not pure-Core-delegable.
- Rate-limit / burst implementation on live pipe connections (token-bucket on named-pipe handles) — production-side, primary only.
- Recovery path that touches real named objects (mutex/pipe open/close, descriptor read-back) — primary only; must prove no unknown-object deletion.
- Decision to allow/reject High/System/Protected integrity (line 52-54 is design; actual enforcement on named-object admission requires primary approval).

Local agent MAY produce: this design doc, the pure Core property draft (synthetic fixtures), checklist, zero-edit confirmation.
