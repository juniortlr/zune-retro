# R3a — Local activation readiness

Date: 2026-09-07, approximately 01:10 America/Sao_Paulo. Source: `d9bcef4` plus this commit's coordinator change and `ActivationReadinessTests.cs`. Previous checkpoint GitHub build run `34077749727` was independently confirmed successful before this work.

Decision: R3a locally verified; R3 overall and DG-1/DG-2 remain ITERATE. No installed app, RetroBar, registry, or startup configuration changes.

## Change and contract

Startup and claiming disposal now share a lifecycle lock. The listener task is published/read through that lock; cancellation and waiting occur outside it. A secondary completes its local readiness false immediately. `Ready` is historical startup success; new `IsReady` is a current local health snapshot, not remote/UI readiness or a guarantee against subsequent disposal. Cleanup is scheduled in finally even when cancellation/wait throws. See [ADR-006](../../decisions/ADR-006-local-activation-readiness.md) for limits and the next recovery slice.

## Evidence, including failures

- RED against unchanged `d9bcef4` production: four initial readiness tests, **2 passed / 2 failed / 0 skipped**. Secondary readiness was still pending; concurrent starts returned different listener task identities (12 of 16 did not match the final published task). Raw: `r3-red_net10.0_20260907010633.trx`.
- The complete suite after implementation initially passed **91/92**: the existing required Shell catalog test returned TimedOut. All readiness/IPC tests passed. The cause of this timeout was not established; it must not be attributed to the readiness patch or local inference without evidence. Raw: `r3-green_net10.0_20260907010758.trx`.
- A focused Shell rerun passed **3/3**, without changes to code or deadlines: `r3-shell-check_net10.0_20260907010822.trx`.
- Three subsequent full required-mode runs each passed **92/92**, zero skipped: 36 Core + 56 Windows. This does not erase the earlier timeout or prove Shell timing is universally stable.
- Five additional readiness-only runs each passed **7/7** (35 executions). Each start/disposal race test includes ten iterations; this is regression sampling, not a full process or resource-growth campaign.
- Full Release build: zero warnings/errors. Solution formatting verification and diff whitespace checks passed. Existing deadlines and test skip requirements were not relaxed.

Environment: Windows x64, SDK 10.0.400, .NET 10.0.11; both `EMBERSTART_REQUIRE_IPC_TESTS=1` and `EMBERSTART_REQUIRE_SHELL=1`. Build output and raw TRX files remain local/ignored under `artifacts/r3-readiness`, to avoid publishing runner profile metadata.

Commands:

```powershell
dotnet build EmberStart.slnx -c Release --artifacts-path artifacts/r3-readiness
$env:EMBERSTART_REQUIRE_IPC_TESTS = '1'
$env:EMBERSTART_REQUIRE_SHELL = '1'
dotnet test EmberStart.slnx -c Release --no-build --no-restore --artifacts-path artifacts/r3-readiness --logger 'trx;LogFilePrefix=r3-final-1' --results-directory artifacts/r3-readiness/results
dotnet format EmberStart.slnx --verify-no-changes --no-restore
```

The full-run prefixes were `r3-final-1` through `r3-final-3` (01:08:51–01:08:59). The Windows-only repeats used filter `FullyQualifiedName~ActivationReadinessTests`, prefixes `r3-repeat-1` through `r3-repeat-5` (01:09:01–01:09:09). Do not test stale outputs after a failed build.

SHA-256 of tested binaries:

- EmberStart.Windows.dll: `81E921992A214C3ECA4C71A811C65D3277B85DF1258DF37168895CC8507F175C`.
- EmberStart.Windows.IntegrationTests.dll: `FCE12D0A734857B447F57FD8C1719984BFE1E2562FC16C25B1DBDE3BDCB0E904`.

## Local-model advisory review

Hermes was invoked explicitly with `--provider custom:ollama -m qwen3.5:9b`, a self-contained read-only design question, max one turn and requested 90-second run budget. Session `20260907_010701_c44bce` returned three scenarios. Its first two assumed creation/task capture occurred outside the lock, contrary to the proposed and actual implementation; they were not adopted as findings. Its readiness caution reinforces the documented snapshot-not-lease limit. The primary authored/reviewed the actual patch and executed all tests; the local model neither ran tests nor approved a gate. No cloud fallback was used.

## Remaining scope

No cold-start process fixture, automatic takeover/retry, UI/window readiness, alternate-user/token matrix, full shutdown callback supervision, or admission-fairness change is included. R3b is the next separately bounded recovery contract/fixture. The original R1/R2 evidence remains historical; this record supersedes only the latest local test count and readiness implementation status.
