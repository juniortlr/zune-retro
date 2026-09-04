# Phase 1 Foundation Status

> This is the foundation checkpoint captured before Shell catalog work. Continued evidence is recorded in [CATALOG_VERTICAL_SLICE.md](CATALOG_VERTICAL_SLICE.md).

**Recorded:** 2026-09-04

**Branch:** `codex/phase-1-foundation`

**Decision:** **ITERATE — foundation accepted for continued Phase 1 work; Gate G1 is not yet eligible for a PASS decision.**

## What this increment establishes

- A pinned .NET 10 solution with Core, Windows interop, WPF host, unit-test, and Windows integration-test boundaries.
- Central package versions, deterministic builds, current recommended analyzers, warnings as errors, formatting verification, and Windows CI.
- An `asInvoker`, `uiAccess=false`, Per-Monitor v2 application manifest plus startup rejection above medium integrity.
- Strict `--toggle`, `--show`, `--hide`, and versioned RetroBar activation parsing. Arbitrary paths, URIs, and command text are not accepted.
- A provisional `Ctrl+Alt+Space` registration using `MOD_NOREPEAT`, with a nonfatal unavailable state and no keyboard hook.
- Current-user/session single-instance naming and a length-prefixed JSON pipe protocol with fixed commands, a 4 KiB cap, and 500 ms operation deadlines.
- Client/server session checks and client SID/integrity validation by pipe impersonation. The runtime smoke test exposed a missing client impersonation level; the implementation now requests it explicitly and has an end-to-end regression test.
- Pure monitor-selection and menu-clamping policies for physical pixels, including negative coordinates, plus a two-stage WPF placement adapter.
- A runnable 704 × 640 DIP Ember Fusion fake-data surface with search filtering, keyboard dismissal, 44-DIP targets, accessible names, and visible focus styling.

## Verification snapshot

| Check | Result |
|---|---|
| `dotnet format EmberStart.slnx --verify-no-changes --no-restore` | PASS |
| Release solution build | PASS — 0 warnings, 0 errors |
| Core unit tests | PASS — 20/20 |
| Windows integration tests | PASS — 6/6 |
| Real-process single-instance/IPC smoke | PASS — secondary exit 0, exactly one resident, primary remained healthy |
| Native WPF visual/keyboard capture | PENDING — the attempted capture was stopped when live user input was detected |

The runtime smoke starts a hidden primary with `--hide`, starts a hidden secondary with the same command, requires the secondary to exit within five seconds with code 0, verifies exactly one process from the tested repository build remains, and then terminates only the test process after validating its executable path.

## Backlog coverage

| Phase 1 item | Foundation status |
|---|---|
| ES-001 solution, SDK, analyzers, CI, license | Implemented |
| ES-002 runtime and DPI manifest | Implemented |
| ES-003 single-instance and activation IPC | Partial; bounded happy path is working |
| ES-004 focus/dismiss state machine | Partial; runnable skeleton, full behavioral evidence pending |
| ES-005 mixed-DPI monitor placement | Partial; policies and initial adapter exist |
| ES-006 Shell catalog | Not started |
| ES-007 icons and safe Shell launch | Not started |
| ES-008 supported hotkey and native fallback | Foundation implemented |
| ES-009 Ember tokens and fake-data UI | Foundation implemented |
| ES-010 unit and integration fixtures | Partial; 26 tests currently pass |

## Known gaps before Gate G1a

- No AppsFolder, Programs, or CommonPrograms catalog; no AUMID/PIDL deduplication; no real classic or packaged app launch.
- No icon extraction, worker timeout/circuit-breaker behavior, or catalog performance evidence.
- `WindowsMonitorPlacement.GetSelectionSnapshot` currently supplies only the primary monitor. Full monitor enumeration, topology changes, `WM_DPICHANGED`, `WM_DISPLAYCHANGE`, `WM_SETTINGCHANGE`, monitor removal, and focus restoration remain.
- Named objects do not yet apply the required explicit non-inheriting user-only security descriptor. IPC queue bounds, token-bucket rate limiting, complete client/server SID/integrity/path validation, and the abuse matrix remain.
- No 1,000-cycle activation campaign, latency percentiles, mixed-DPI hardware matrix, Accessibility Insights/Narrator evidence, touch/localization checks, recovery campaign, or packaging comparison.
- No installed RetroBar button proof. Gate G1b remains separately pending.
- The surface is a feasibility preview with fake entries. It does not replace Windows Start, alter Explorer, or ship a public binary.

## Next decision gate

Continue the Phase 1 vertical slice through real Shell enumeration/launch, complete monitor and IPC hardening, then execute the reproducible evidence protocol. Only that evidence packet can support separate owner decisions for G1a and G1b.
