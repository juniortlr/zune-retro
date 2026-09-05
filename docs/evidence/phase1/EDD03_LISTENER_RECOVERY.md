# EDD-03 — Wire Validation and Listener Recovery

Recorded: 2026-09-05, 20:32 America/Sao_Paulo. Baseline: `92ff5f0` plus the source changes delivered with this evidence file. The tested worktree was dirty; these are local build results, not a release qualification.

Decision: **continue Phase 1; DG-1 and DG-2 remain ITERATE; G1a not run; G1b pending reproducible evidence.**

## Implemented and reviewed

- `ActivationPipeProtocol` uses private Windows-layer DTOs before constructing `ActivationRequest`. Six request fields and four nested rectangle coordinates are required. Missing, extra, duplicate, wrong-type, malformed, unknown-version/enum, and empty-ID input is rejected with a fixed `InvalidDataException` message. Raw parser exceptions/payloads are not retained.
- Requests remain capped at 4 KiB and read/write operations at 500 ms. Responses also use a required-field schema and bounded result codes.
- Negative coordinates remain valid. Unordered or greater-than-Int32-sized rectangles lose their placement context and use resident fallback. Current topology validation remains in the Windows placement adapter; this increment does not complete EDD-07.
- The CLI integrated grammar rejects numeric/composite taskbar-edge spellings that `Enum.TryParse` previously accepted.
- Connection-local parse, I/O, impersonation, and deadline failures release their pipe and continue listening. Object-creation failures produce terminal `Faulted` health instead of retrying in a tight loop. Shutdown produces `Stopped`.
- Handler faults return `HandlerFailed`; a deadline returns `HandlerTimedOut` where the client is still waiting. The handler receives cancellation and executes off the accept-loop thread. At most one handler remains in flight: if it ignores cancellation, subsequent valid requests receive `Busy` until it completes. No automatic retry/replay occurs.
- Listener completion, health, and a payload-free rejection count are exposed. The WPF host observes terminal listener completion and reports unavailable activation with the native Start fallback.

The wire reader uses the pinned .NET 10 APIs for [duplicate-property rejection](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions.allowduplicateproperties?view=net-10.0), [unmapped-member rejection](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/missing-members), and required DTO properties. No package was added.

## Test evidence

The Sol test reviewer reported RED on genuine listener fixtures before the listener fix: a stalled connection prevented the next connection and a throwing handler faulted the listener/disposal. The primary independently executed the resulting GREEN suite below. Historical Hermes memory-stream claims were not treated as listener proof.

| Check | Result |
|---|---|
| Formatting verification | PASS |
| Complete Release solution build, isolated artifact path | PASS — 0 warnings, 0 errors |
| Core suite | PASS — 27/27 |
| Windows integration suite | PASS — 41/41 |
| Skipped tests in this local run | 0 |
| Deterministic invalid-input corpus | PASS — 10,000 invalid UTF-8 decoder frames, one test |
| Real listener malformed → stalled → partial/disconnected → valid sequence | PASS |
| Throwing handler followed by valid activation | PASS |
| Cancellation-ignoring handler, ten Busy responses, release and recovery | PASS |
| Pre-created pipe detection and idempotent listener disposal | PASS |
| Existing required Shell catalog and controlled classic-launch tests | PASS |

The 10,000 frames exercise the decoder in memory, not 10,000 live pipe connections, broad semantic fuzzing, or measured heap/handle growth. Real pipe fixtures use randomized names and current-process coordinators. They do not prove process cold-start races, WPF HWND counts, cross-user/session security, or a G1 campaign.

Environment: Windows 11 25H2, build 26200.9168, x64; SDK 10.0.400; test runtime .NET 10.0.11. IPC and Shell required-mode environment flags were enabled. Standard-user IPC tests explicitly skip on unsuitable elevated hosts unless required mode is set; such a skip is not gate evidence.

Commands:

```powershell
dotnet format EmberStart.slnx --verify-no-changes --no-restore
dotnet build EmberStart.slnx --configuration Release --artifacts-path artifacts/edd03-review
$env:EMBERSTART_REQUIRE_IPC_TESTS = '1'
$env:EMBERSTART_REQUIRE_SHELL = '1'
dotnet test EmberStart.slnx --configuration Release --no-build --no-restore --artifacts-path artifacts/edd03-review --logger 'trx;LogFilePrefix=edd03-final' --results-directory artifacts/edd03-review/results
```

Raw local results: `artifacts/edd03-review/results/edd03-final_net10.0_20260905203230.trx` and `edd03-final_net10.0_20260905203231.trx`. These files remain ignored because runner metadata can include profile paths. Only aggregate results are recorded here.

SHA-256 of the tested DLLs:

| Artifact | SHA-256 |
|---|---|
| EmberStart.App | `88e0264b926ab86e2354abc3babd27d8d52ef078ab850c65624b880c92f4021a` |
| EmberStart.Core | `9a8f7ae78264022fc864fcb4fc4accc07057e143b3f8d1cc7f3bf8f62a6810fd` |
| EmberStart.Windows | `3ccc8823a91ecd636d9a269b263ff9e1b7f9395bd43c225a51bd514bed61c404` |
| EmberStart.Core.Tests | `65ca578ed1e2fae8b9fa108e9d86fadba2e54b198a8ba48040c5264be948acbe` |
| EmberStart.Windows.IntegrationTests | `125687a4da18273366c429f3594f300162847f1c45ae7a15ebc3e1fbec9efe69` |

## Preserved Hermes work

The non-executing wire/admission placeholders, conceptual cold-start harness, and elevated-parent sketch are preserved as `.cs.draft` files under `docs/design/`, outside the compiled test projects. The actual decoder and listener tests replace the old EDD-01 claims. [The handoff review](../../council/2026-09-05-hermes-handoff-review.md) and [reviewed gate packet](../../../DG_EVIDENCE_PACKET.md) explain the corrections.

## Limits and next slice

EDD-04 remains next: explicit object security/read-back, both-endpoint SID/session/integrity validation, cold-start readiness, bounded connection admission/queue, and rate limiting. The current listener handles one connection at a time; it does not satisfy the complete flood/fairness specification.

A timed-out handler can still complete later. Cancellation is cooperative; `HandlerTimedOut` is an uncertain outcome, not proof that no effect happened. An indefinitely stuck handler leaves later requests Busy and requires resident recovery; this does not earn DG-1 GO.

Full process/window cold-start tests, mixed-DPI/focus, packaged-success activation, accessibility, stable packaging, and installed RetroBar qualification are still pending. Terminal-health UI presentation was compiled but not visually exercised in this increment.

The already-running Ember Start instance was left in place. Its locked default build output caused the initial ordinary build to fail; all final checks used isolated output. This change is source/build work, not deployment to the running resident. Restart all development instances before testing the tightened integrated wire schema; it is not an advertised cross-version release contract.
