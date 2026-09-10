# Open-model handoff review and next task — 2026-09-06

Reviewed at approximately 18:00 America/Sao_Paulo. Request: verify the open models' work and plan their next step, not implement or deploy the next phase.

## Decision

**ITERATE. Give the next worker a small pipe-factory repair, not a listener rewrite or G1a campaign.** A two-site candidate repair already passes the existing suite in an isolated diagnostic copy. It has NOT been applied to the main source tree, committed, pushed, or installed.

DG-1 and DG-2 remain ITERATE. G1a is not qualified. G1b/installed RetroBar evidence remains pending. Passing the repair gate below does not upgrade any project decision gate.

## What was actually submitted

- Reviewed source: `99b4b68b4e7358018d60b7169ca1957f6e6672cc`, branch `codex/phase-1-foundation`. The remote branch matched this SHA at review time. The worktree was clean before this review report.
- The handoff identifies its runtime as `nvidia/nemotron-3-ultra-550b-a55b:free` via OpenRouter. That is document-reported provenance, not independently authenticated model identity.
- This commit contains four new evidence/plan documents, the pipe-factory revert, and the earlier unfinished EDD-04 source/test draft. It is not a test-only commit. The unfinished draft originated in the preceding primary/Sol work; do not attribute all 746 added lines to the open model.
- Useful material retained: deterministic bucket/admission tests, explicit acknowledgment of failed tests, and no DG-1 GO declaration.
- The original `ReadPermissions` mistake was present in the primary's unfinished ACL draft. The subsequent revert introduced a separate factory/listener incompatibility. Both require correction; this is not solely an open-model defect.

## Independent checks

Fresh Release builds used SDK 10.0.400 and runtime .NET 10.0.11. Both `EMBERSTART_REQUIRE_IPC_TESTS=1` and `EMBERSTART_REQUIRE_SHELL=1` were set. All builds below had zero warnings/errors; all test runs had zero skips.

| Exact input | Core | Windows integration | Meaning |
|---|---:|---:|---|
| Archived `1c097ed` baseline, rebuilt now | 27/27 | 41/41 | Previous checkpoint still passes on this host |
| Actual `99b4b68` checkout | 36/36 | 42/46 | Four real regressions reproduced |
| Isolated `99b4b68` copy with the two-site repair below | 36/36 | 46/46 | Minimal candidate restores all 82 existing tests |

The candidate's six listener/coordinator tests also passed five consecutive additional runs (30/30 executions, zero skips). This supports the bounded repair recommendation; it is not a load or soak test.

Current failing tests:

1. `Listener_RecoversAfterMalformedStalledPartialAndDisconnectedClients` — broken pipe.
2. `Listener_RecoversAfterHandlerThrows` — broken pipe.
3. `Listener_BoundsUncooperativeHandler_ThenRecoversAfterItFinishes` — timeout waiting for the handler exit signal; the earlier failure is masked by cleanup.
4. `Secondary_EnforcesCurrentProcessIntegrityPolicy` — broken pipe.

### Confirmed defect 1: one pipe instance cannot support overlapping replacement

`SingleInstanceCoordinator.cs:200` opens a replacement server while the accepted server remains open. The reverted factory at `:392` sets `maxNumberOfServerInstances: 1`. On the first accepted connection, creating the replacement fails before the request can reach `ServeConnectionAsync`; listener shutdown breaks the client's pipe. This is not evidence that the channel consumer needs rewriting.

An independent .NET 10 diagnostic using randomized temporary pipe names produced:

```text
maximum=1; overlapping replacement=FAIL; HRESULT=0x800700E7; All pipe instances are busy.
maximum=66; overlapping replacement=PASS
```

The instance limit applies across handles of the same pipe name and must match on each instance. [Microsoft CreateNamedPipe contract](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createnamedpipea).

### Confirmed defect 2: invalid additional access flag, not a descriptor/inheritance conflict

The dormant `NamedObjectSecurity.CreatePipe` passes `PipeAccessRights.ReadPermissions` as `additionalAccessRights` at `NamedObjectSecurity.cs:47`. The same .NET 10 diagnostic changed only this argument, keeping the protected owner-only descriptor and `HandleInheritability.None`:

```text
additionalAccessRights=ReadPermissions; creation=FAIL; HRESULT=0x80070057
additionalAccessRights=0; creation=PASS; ownerMatch=True; protected=True; ACEs=1;
mask=1F019F; expected=1F019F
```

The allowed extra Win32 security access flags are WRITE_DAC, WRITE_OWNER, and ACCESS_SYSTEM_SECURITY; ReadPermissions/READ_CONTROL is not an extra open-mode flag. The duplex handle permits the tested descriptor read-back without adding that flag. [Microsoft open-mode contract](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createnamedpipea), [.NET factory API](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes.namedpipeserverstreamacl.create?view=net-10.0).

The diagnostic is current-user evidence only. It does not prove alternate-user, alternate-session, elevated/low-token rejection or universal ACL compatibility.

### Evidence corrections required

- `EDD04_EVIDENCE_REVERTED.md` repeatedly uses `92ff5f0` as the build baseline. The direct parent of this submission is `1c097ed`; the committed submission includes substantial production changes.
- The failing recovery tests are called new EDD-04/post-DG-1 tests. They already exist at `1c097ed`, and the fresh baseline run passes them. Do not exclude regressions from qualification or extend timeouts to hide them.
- The ACL failure is attributed to a security-descriptor/non-inheritance conflict. The controlled single-argument probe above contradicts that explanation.
- `G1A_SUBSET_RUN.md` claims 20 CLI events plus five batches of five equals 25 measured events. That arithmetic would be 45 if all described events ran. The document provides no per-case records demonstrating either total. Unit-test output is not proof of a CLI/hotkey/WPF campaign.
- No checked-in executable G1a harness or raw case ledger substantiates a completed subset. Treat it as an unverified report, not 25 completed acceptance cases.
- `EDD04_LOOP_TRACK.md` records two timestamps about four seconds apart, not a documented hour. The commit message's completed-hour claim is not supported by that file. This does not establish that no additional work happened outside the recorded evidence.
- The asserted 35–50-hour duration for 1,000 cases is not a measured estimate. Benchmark an actual automated pilot before estimating the campaign. Keep the separate 24-hour soak requirement separate.
- Statements that draft files are untracked/uncommitted describe a pre-commit state; they are tracked in `99b4b68`. Preserve historical notes, but label them and add a current correction.

## Next worker assignment: R1 — restore the secured pipe factory

This is the next executable task for Hermes/local Qwen or an available open model. Do not start R2/R3 automatically.

### Allowed changes

- `src/EmberStart.Windows/Instance/NamedObjectSecurity.cs`: replace only the invalid extra access argument with zero/default.
- `src/EmberStart.Windows/Instance/SingleInstanceCoordinator.cs`: route `CreateServer(bool first)` back through `NamedObjectSecurity.CreatePipe(_identity, MaximumConnections + 2, first)`.
- Add `tests/EmberStart.Windows.IntegrationTests/Instance/NamedObjectSecurityTests.cs` and narrowly scoped pipe-replacement regression tests.
- Add one evidence file for the repair, recording exact source/diff identity, commands, environment, counts, failures, and artifact hashes.

### Required tests and acceptance gate R1

1. Add a real Windows regression proving a connected first pipe can coexist with a waiting replacement using the production factory, while the name remains owned. Verify `FirstPipeInstance` collision behavior independently.
2. Exercise the production factory and inspect the actual owner SID, protected DACL, allowed ACE/mask, and non-inheritable handle. No constants standing in for observed descriptors.
3. Retain all existing tests and their deadlines. The current four failures are the RED baseline. The repaired full suite must pass all 82 existing cases plus the new tests, zero skips in required standard-user mode.
4. Repeat the listener/coordinator subset at least five times, recording every run, not just the final success. This is a regression check, not stress/soak qualification.
5. Zero build warnings/errors, formatting verification, `git diff --check`, and no unrelated source edits.
6. Stop and report any remaining failure with raw output. Do not rewrite channels, broaden permissions, remove endpoint validation, silently skip tests, or change timeouts to force green.

### Copy/paste prompt

> Work in the zune-retro checkout at C:\Users\EG\Documents\ChatGPT\EmberStart. Read docs/council/2026-09-06-open-model-review-and-next-task.md fully. Implement only R1 and its tests/evidence. Check git status and HEAD first; the reviewed baseline is 99b4b68. Preserve later or unrelated changes and report overlapping edits. Use apply_patch and isolated artifacts/r1-repair build output. Keep CurrentUserOnly on the client; use the explicit descriptor on the server without CurrentUserOnly overriding it. Retain the protected current-user-only DACL, owner/read-back checks, non-inheriting handles, first-handle collision detection, and finite connection limit. Do not modify the installed app, startup configuration, RetroBar, Explorer, registry, or user accounts. Do not weaken/skip existing tests or increase timeouts. Do not commit the whole dirty tree, push, merge, or declare any project gate GO. Deliver the scoped diff, actual test output locations, hashes, remaining risks, and stop for primary review. If local Qwen is unavailable, report the failure; do not claim it executed or silently switch providers.

### Verification commands

Run from the main checkout after the proposed repair; do not reuse old binaries:

```powershell
dotnet build EmberStart.slnx --configuration Release --artifacts-path artifacts/r1-repair
# Stop here if build fails; never test a stale output after a failed build.
$env:EMBERSTART_REQUIRE_IPC_TESTS = '1'
$env:EMBERSTART_REQUIRE_SHELL = '1'
dotnet test EmberStart.slnx --configuration Release --no-build --no-restore --artifacts-path artifacts/r1-repair --logger 'trx;LogFilePrefix=r1' --results-directory artifacts/r1-repair/results
dotnet format EmberStart.slnx --verify-no-changes --no-restore
git diff --check
git diff --stat
```

Use `--filter 'FullyQualifiedName~ActivationListenerRecoveryTests|FullyQualifiedName~SingleInstanceCoordinatorTests'` on the Windows test project for repeated regression runs. If the installed app locks normal Release output, keep using isolated artifacts; do not terminate the user's app.

## Following assignments, in order

### R2 — repair evidence, documentation only

After R1 results are known, a separate worker can correct the four new evidence/loop/G1a documents with clearly dated errata and links to raw local results. Do not erase original provenance or invent missing events. G1a remains unverified/not qualified. Gate: every count has a case ledger or test output; every source identity resolves; projected runtime is labeled an estimate. This documentation work can run beside R1 only with disjoint file ownership.

### R3 — primary architecture decision, then bounded implementation tasks

Leave these decisions to the primary Astra/Sol review rather than asking the next small worker to rewrite the IPC subsystem:

- Align the medium-only implementation with the authoritative medium-or-lower spec; record an ADR and real rejection-test requirements.
- Specify startup/initial readiness, serialize `StartListening`, and add bounded cold-start recovery. Releasing a secondary's own mutex handle before trying fresh creation is essential; never take over a live or untrusted object.
- Preserve the spec's separate 500 ms connect/read/write/ack deadlines unless explicitly amended. Define when queue/handler time starts and prevent canceled queued work from dispatching. A timed-out dispatched handler is uncertain and must not be replayed automatically.
- Audit disposal/child-task supervision and terminal-health reporting under concurrent start/stop, slow peers, ignored cancellation, and namespace conflicts.
- Decide whether the global limiter caps attempted authenticated requests or admitted work. It currently spends a global token before a per-peer rejection. Define and test fairness instead of assuming the five existing admission tests cover abusive-peer starvation.
- Add real cross-user/session/integrity and endpoint-identity tests. Image-path/PID checks are defense in depth, not cryptographic identity or protection against same-user/session/medium malware.

Gate: approved contracts, adversarial tests that fail for the actual missing behavior, small non-overlapping assignments, and independent review of each patch. No full DG-1 GO from unit tests alone.

### R4 — executable G1a pilot, only after the IPC repair and contracts

Build a controlled process/window harness and run a small recorded pilot with exact expected/actual state, process/HWND counts, request IDs, latency, listener health, and fixture/build identity. Estimate duration from that pilot. Then schedule the canonical 1,000 scored activation cases and the specified placement subset, preserving hotkey/CLI/concurrent distinctions. Full qualification still also requires the other architectural, accessibility, security, packaging, and soak evidence. Do not present a timer loop or unit-test count as campaign completion.

## Local evidence locations and reproducibility

Generated diagnostic copies and raw outputs stay ignored under `artifacts/open-model-review/`; they may contain local profile paths and are not automatically publishable.

- Current-source results: `results/open-model-review_net10.0_20260906175652.trx` (Core), `results/open-model-review_net10.0_20260906175654.trx` (Windows).
- Baseline rebuilt from `git archive 1c097ed`: `baseline/`; outputs/results: `baseline-build/`.
- Candidate rebuilt from `git archive 99b4b68`: `candidate/`, with only the two factory changes described in R1; outputs/results: `candidate-build/`.
- Standalone API probe: `PipeProbe.cs`; run with `dotnet run --file artifacts/open-model-review/PipeProbe.cs -p:EnableNETAnalyzers=false`. It creates randomized transient named pipes, prints no actual SID, and disposes its handles.
- Actual current-source Windows DLL SHA-256: `E66EACABEBB039E9227FA7974A52A8AC1D878DC7E4A960D01944ADFD717C085E`.
- Isolated candidate Windows DLL SHA-256: `B1001B7E3174583AC36444EED659D0248D1A1EBEFAC055F9E42ACE7B2B611372`.
- Current Core TRX SHA-256: `A6504E8202F084C82885182244175AE59AF1EEA906BF9502C6E74E415E1F96FC`.
- Current Windows TRX SHA-256: `2A30FA0EEFD5A00ADDA33D03F4992FF2FD370ED49B9C9DAD7186EE422CB4C793`.

This review did not change production/test files in the main checkout, alter installed software, push commits, or dispatch the next implementation assignment. Only this report and ignored diagnostic artifacts were created.
