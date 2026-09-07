# R1 pipe-factory repair and R2 evidence corrections

Recorded: 2026-09-06 23:51 America/Sao_Paulo (2026-09-07 02:51 UTC).
Source baseline: `931a41f` (review committed on top of `99b4b68`) plus the two production edits and new test file committed with this record.

## Disposition

**R1 scoped repair accepted locally. R2 historical evidence corrected.** This is not DG-1 GO, complete EDD-04, or deployment. DG-1/DG-2 remain ITERATE; G1a is unverified/not qualified; installed G1b evidence remains pending.

## Implementation and review

- `NamedObjectSecurity.CreatePipe` now uses zero additional access flags, preserving its explicit owner-only protected DACL, owner/ACL read-back, and non-inheritable handles. It no longer passes invalid `ReadPermissions` as a Win32 extra open-mode flag.
- `SingleInstanceCoordinator.CreateServer` uses that secured factory with `MaximumConnections + 2` instances (66). This permits the accepted handle and waiting replacement to coexist while retaining the 64 active-connection limit. `FirstPipeInstance` remains first-handle-only. Client `CurrentUserOnly` and endpoint validation remain unchanged.
- No channel/handler rewrite, permission broadening, skipped regression, or timeout increase was used. The source delta is confined to these two factory sites.
- Three new real Windows tests inspect actual descriptors/handle flags, test exact first-instance collision (`UnauthorizedAccessException`, HRESULT `0x80070005`), and prove a waiting replacement retains the name after the original connection closes and then accepts a client.
- The tests assert a suitable medium-integrity host. Required-mode qualification cannot count elevated-host skips as passing IPC evidence.

The previous [independent review](../../council/2026-09-06-open-model-review-and-next-task.md) contains the rebuilt baseline, four reproduced regressions, controlled API probes, and authoritative API references. It is preserved as a pre-repair snapshot.

## Verification

Environment: Windows x64; SDK 10.0.400; .NET 10.0.11. Both `EMBERSTART_REQUIRE_IPC_TESTS=1` and `EMBERSTART_REQUIRE_SHELL=1` were set. Isolated `artifacts/r1-repair` output was used.

| Check | Actual result |
|---|---|
| Solution formatting verification | PASS |
| Full Release build | PASS, zero warnings/errors |
| Core tests | 36 passed, zero failed/skipped |
| Windows integration tests | 49 passed, zero failed/skipped |
| Total final suite | **85 passed, zero failed/skipped** |
| Listener/coordinator/security subset, repeated five times | **9/9 each run; 45/45 executions**, zero skips |

Test development was not uniformly green: the delegated Sol runner reported all three new tests failing at the invalid factory before repair. The first primary post-repair full run passed 84/85; its new collision test incorrectly expected IOException. That assertion was corrected to the documented/observed access-denied exception and exact HRESULT, not relaxed to accept arbitrary failures. The final runs above use that corrected test. The original four listener regressions passed after the production repair.

Final commands, with failure checks between stages:

```powershell
dotnet format EmberStart.slnx --verify-no-changes --no-restore
dotnet build EmberStart.slnx --configuration Release --artifacts-path artifacts/r1-repair
$env:EMBERSTART_REQUIRE_IPC_TESTS = '1'
$env:EMBERSTART_REQUIRE_SHELL = '1'
dotnet test EmberStart.slnx --configuration Release --no-build --no-restore --artifacts-path artifacts/r1-repair --logger 'trx;LogFilePrefix=r1-final' --results-directory artifacts/r1-repair/results
```

The five additional Windows-project runs used filter `FullyQualifiedName~ActivationListenerRecoveryTests|FullyQualifiedName~SingleInstanceCoordinatorTests|FullyQualifiedName~NamedObjectSecurityTests`, the same build, and `r1-repeat-1` through `r1-repeat-5` TRX prefixes. These are regression repeats, not cold-process, flood, UI, or soak evidence.

Raw local results are ignored under `artifacts/r1-repair/results/` because runner metadata can contain profile paths:

- `r1-final_net10.0_20260906235053.trx` — Core.
- `r1-final_net10.0_20260906235056.trx` — Windows.
- `r1-repeat-1_net10.0_20260906235059.trx` through `r1-repeat-5_net10.0_20260906235110.trx` — all five repeated runs.
- Earlier `r1_net10.0_20260906182103.trx` preserves the incorrect test-exception assertion failure.

SHA-256:

| Artifact | Hash |
|---|---|
| EmberStart.Windows.dll | `19E0E6C39D009711B9D9D985000E9AEF0C66CD37B3918D1ED87C079ECA6FAAD4` |
| EmberStart.Windows.IntegrationTests.dll | `DBEBBBABFBE5B6F429A7E075888EA05A10EE7EEA49138229FFC4246AB97F5292` |
| Final Core TRX | `8C971EFB150369EAB7B9A38A24B0A97A38439C2E0B9AE4ECBC21F9253E22911B` |
| Final Windows TRX | `5F080725E0068D7D8E50CF79040899EB800A287B4A59D144684B41E781170C34` |

## R2 and local-model provenance

Dated errata now precede the original EDD04 revert/loop and G1a plan/subset reports. They correct source identity, tracked production changes, regression scope, pipe failure cause, unsupported elapsed-time claims, unsupported case counts, and the included placement subset. Original text remains clearly labeled historical; no missing case records were invented.

Ollama was initially offline. The installed `ollama serve` was started hidden for this work, without startup/configuration changes; `/api/tags` then listed `qwen3.5:9b`. Hermes was explicitly invoked with `--provider custom:ollama -m qwen3.5:9b` for a bounded documentation-only task. No OpenRouter fallback was used.

- Session `20260906_182029_733f12`: returned a literal read_file expression rather than the requested errata. No successful file-read/tool execution or completed review is claimed for it.
- Session `20260906_182134_e5b7b5`: a self-contained prompt produced four draft errata bullets. The primary accepted the count/unsupported-timing observations but corrected muddled source-history statements and wording. Qwen did not execute tests, edit production files, or approve a gate.
- Local prompts are retained under `artifacts/r1-repair/hermes-r2-query.txt` and `hermes-r2-self-contained.txt`; the returned draft is preserved in `hermes-r2-output.md` there. Sol authored the tests; primary code changes, review, and final verification are recorded separately above.

## Remaining work and next gate

R3 is next: resolve the medium-only/spec mismatch, initial readiness/concurrent start, bounded cold-start recovery, queue/ack deadlines and no-replay behavior, shutdown supervision, and the global-attempt-versus-admission fairness contract. Then delegate small test-first slices with exclusive file ownership.

These tests do not prove cross-user/session or elevated/low-token rejection, full mutex security qualification, client/server process identity under PID races, exhausted 64-connection behavior, full 32-item queue behavior, G1a process/window counts, monitor/focus integration, or accessibility. A same-user/session/medium process remains outside the adversarial boundary. No release/install, registry, Explorer, RetroBar, or startup changes were made.
