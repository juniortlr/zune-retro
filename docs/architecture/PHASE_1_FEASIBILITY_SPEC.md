# Phase 1 Feasibility Specification

**Status:** Round 2 corrections applied; candidate for final council validation

**Timebox:** 5–7 working days after G0 owner ratification

**Goal:** Determine whether a safe WPF launcher can meet the activation, placement, Shell-catalog, focus, accessibility-skeleton, and stable-entry-point requirements.

## Scope boundary

Phase 1 proves technology; it does not build the complete Start product. Pins, usage ranking, real power actions, daily-use startup, updater logic, polished themes, a bare-Windows-key hook, and taskbar behavior are out of scope. Phase 1 code begins only after G0 passes.

### Minimal solution and dependency direction

```text
EmberStart.slnx
├── src/EmberStart.Core/                    # net10.0; pure models, policies, contracts
├── src/EmberStart.Windows/                 # net10.0-windows; Win32, Shell, IPC, DPI
├── src/EmberStart.App/                     # net10.0-windows; WPF host and spike UI
├── tests/EmberStart.Core.Tests/
├── tests/EmberStart.Windows.IntegrationTests/
└── packaging/EmberStart.Package/           # isolated MSIX experiment
```

`Windows → Core` and `App → Core`; App composes Windows, and Windows never references App. App owns `HwndSource` and passes handles to Windows adapters. The larger production split remains a target architecture, not a spike requirement.

## Runtime integrity policy

- Manifest: `asInvoker`, `uiAccess=false`, Per-Monitor v2.
- At startup inspect the process token. A process above medium integrity refuses to become the resident or connect to the normal UI pipe, reports the condition, and exits. It does not silently retain elevation or install anything.
- Standard-user and normal Explorer launches must run at medium integrity without UAC.
- Certificate trust or VM preparation that requires administration is test-environment setup, never an Ember runtime requirement.

## Activation contracts

### Simple command and hotkey

- Stable simple grammar: exactly one of `--toggle`, `--show`, or `--hide`; `show` and `hide` are idempotent.
- Simple command source is `CommandLine`. The resident chooses placement from the captured foreground window, then pointer, then primary monitor.
- Provisional hotkey: configurable `Ctrl+Alt+Space` registered with `MOD_CONTROL | MOD_ALT | MOD_NOREPEAT`. Registration failure is visible but nonfatal; no hook is installed.
- Native Start fallback is `Ctrl+Esc` plus untouched operating-system Windows-key shortcuts.

### Versioned RetroBar integration form

The stable launcher also accepts a strictly parsed v1 integration form:

```text
--integrated-toggle-v1 --source retrobar \
  --anchor-left <int32> --anchor-top <int32> \
  --anchor-right <int32> --anchor-bottom <int32> \
  --taskbar-edge <left|top|right|bottom>
```

This form accepts no path, URI, free text, shell arguments, or serialized handle. `source` affects diagnostics and placement only; it grants no privilege and another same-user process can spoof it. Invalid or stale geometry falls back to the simple placement policy.

The launcher captures foreground context, starts or locates the resident, and—only when Windows grants the launcher foreground permission—spikes `AllowSetForegroundWindow(residentPid)` before IPC. Failure is recorded as Iterate; do not use Topmost, `AttachThreadInput`, hooks, or focus-stealing retries. G1b must prove this contract through the actual installed RetroBar button, not a synthetic source flag.

## Single instance and IPC

- Names include a SHA-256-derived SID identifier, Windows session ID, and protocol major version.
- Apply explicit non-inheriting security descriptors to mutex and pipe: current interactive user SID only; no Everyone, Authenticated Users, or LocalSystem access.
- Server pipe options: `Asynchronous | CurrentUserOnly | FirstPipeInstance`; client: `Asynchronous | CurrentUserOnly`.
- After connection, inspect the peer process/token. Both ends require the expected user SID, Windows session ID, and medium-or-lower integrity; elevated same-SID clients are rejected. Payload SID/session fields are never trusted as authorization.
- Where available, the client checks server PID/token/session and installed path or publisher. This does not establish cryptographic application identity in an unsigned development build.
- A malicious process already running as the same user, session, and integrity is outside the security boundary. It may send the small fixed UI command set or squat on the named objects. `FirstPipeInstance` makes squatting detectable; the result is an availability failure and native Start remains available. Do not claim to prevent same-user impersonation.
- Messages are length-prefixed, versioned JSON, at most 4 KiB, and accept only fixed command/source/edge enums plus validated physical rectangles. No paths, URIs, shell text, or arbitrary arguments cross IPC.
- Connect, read, write, and acknowledgment deadlines are each 500 ms. The server queue holds at most 32 requests and uses a per-client token bucket of 20 requests/second with a burst of 40. Excess work receives a bounded Busy/RateLimited result.
- The primary owns the mutex and creates the pipe before reporting ready. A secondary sends one command, waits for its result, and exits. Hotkey activations do not have IPC acknowledgments.
- Repeated `show`/`hide` is intentionally idempotent; repeated `toggle` intentionally changes state. No anti-replay security claim is made.

## Monitor, DPI, and focus

- Use `GetForegroundWindow`, `GetCursorPos`, `MonitorFromWindow/Point/Rect`, `GetMonitorInfo`, `GetDpiForWindow`, and `SetWindowPos` behind Windows adapters.
- Cross-process rectangles use physical screen pixels. Never serialize `HMONITOR`; resolve a current monitor inside the resident.
- Validate rectangles with checked arithmetic: ordered bounds, positive width/height, dimensions no larger than the current virtual screen, and nonempty intersection with a current monitor/work area. Then clamp. Invalid data uses fallback placement.
- First show is two-stage: create the HWND hidden, move it without activation to the target monitor, process the resulting `WM_DPICHANGED`/target `GetDpiForWindow`, measure WPF in DIPs, set final physical bounds, then show. Do not infer a target monitor's DPI from the HWND before it moves there.
- Handle `WM_DPICHANGED`, `WM_DISPLAYCHANGE`, `WM_SETTINGCHANGE` including `SPI_SETWORKAREA`, primary swaps, negative coordinates, and monitor removal.
- If the visible monitor disappears, dismiss instead of jumping displays.
- Restore prior focus only if its HWND remains valid and no other application has since become foreground.

## Shell catalog, launch, and minimal search

- On a bounded STA worker, obtain `FOLDERID_AppsFolder` with the Shell known-folder APIs and enumerate its Shell items; merge Programs/CommonPrograms only for classic hierarchy/coverage.
- Preserve localized names and Shell identities. Deduplicate by AUMID, then canonical Shell parsing identity—not display name.
- Use `ShellExecuteExW` with `SEE_MASK_IDLIST` for current Shell PIDLs. Use `IApplicationActivationManager` only for packaged entries with a valid AUMID. Never reconstruct a command line from metadata.
- Resolve shortcuts and extract icons away from the WPF UI thread. The UI stops waiting after two seconds for initial catalog work and after 250 ms for an individual incremental result, then uses cached/empty state.
- A timed-out COM call may leave the worker wedged; no cancellation claim is made. Open its circuit, stop queuing work to it, keep the UI responsive, and record whether a worker process is required in the next phase.
- Fixed test specimens are a benign classic Start shortcut and a benign packaged/AUMID fixture that each write a nonce to their private test output and exit. Also record one non-destructive real classic and one real packaged launch on the owner PC.
- Minimal search follows the deterministic filter and empty/no-results behavior in [the visual and accessibility baseline](../design/VISUAL_ACCESSIBILITY_BASELINE.md).

## Feasibility UI

The canonical design, state, sizing, search, UI Automation, Narrator, locale, contrast, motion, and touch protocol is [the visual and accessibility baseline](../design/VISUAL_ACCESSIBILITY_BASELINE.md).

Phase 1 uses Ember Fusion at 704 × 640 DIPs, clamped to the work area with an 8-DIP margin and a single-column fallback below 640 DIPs. It omits interactive Power/Session and Open Windows Start footer controls. `Ctrl+Esc` is the native fallback under test.

## Packaging and stable-entry-point rehearsal

Use a disposable standard-user test account or VM. Development certificate trust is recorded separately from product behavior.

### MSIX candidate

1. Install signed same-publisher package N (`0.1.0.0`) with a `windows.appExecutionAlias`.
2. Verify alias activation with the resident stopped and running.
3. Smoke-test opt-in `StartupTask`, confirm hidden start, disable it, and verify the previous state is restored. Phase 1 never enables daily-use startup.
4. Upgrade to N+1 (`0.1.1.0`); validate alias, Shell COM, hotkey, settings migration, and both resident states.
5. Rehearse N+1→N only if the chosen App Installer policy supports `ForceUpdateFromAnyVersion`; otherwise install signed N+2 (`0.1.2.0`) as the documented emergency roll-forward.
6. Uninstall/reinstall and record retained/removed per-user files.
7. Disable or collide the alias. If RetroBar lacks another version-independent tested entry point, MSIX fails G1b and the unpackaged candidate is evaluated; “use another path” is not a pass.

Record package identity, publisher, versions, hashes, alias/StartupTask state, settings schema, and filesystem result at every step. The 100-cycle qualification remains G6 work.

### Unpackaged candidate

Use a fixed per-user launcher at `%LocalAppData%\Programs\EmberStart\EmberStart.Launcher.exe`, versioned payload directories, and an atomically replaced validated `current.json` selector. Stop the resident before payload switch; retain N−1 until health validation. Constrain the selector to signed/hashed child version directories. Restrict native DLL search to the application and System32 directories; never search the current working directory or plug-in paths.

Rehearse N→N+1→N, launcher invocation with resident stopped/running, interrupted selector replacement, uninstall, and last-known-good recovery. ADR-006 selects a route only after both candidates' evidence is recorded. No custom network updater is built.

## Evidence privacy

Normal runtime diagnostics exclude queries, inventory, paths, titles, usernames, and window text. Raw catalog ledgers and monitor screenshots are separate owner-controlled local gate artifacts, not normal logs; do not commit or export them without explicit review. Public evidence contains aggregates, redacted screenshots, fixture identities, and hashes where useful.

## Reproducible G1a protocol

### Activation and placement

The 1,000-cycle campaign is exact:

- 200 hotkey toggles: 100 hidden→visible and 100 visible→hidden.
- 200 simple CLI toggles: 100 hidden→visible and 100 visible→hidden.
- 200 sequential CLI shows: 100 hidden→visible and 100 visible→visible.
- 200 sequential CLI hides: 100 visible→hidden and 100 hidden→hidden.
- 200 concurrent IPC requests: 20 batches of five `show` from hidden and 20 batches of five `hide` from visible.

Preconditioning commands are recorded but excluded from the 1,000. The placement subset is `50 × monitor_count`; on the owner's two-monitor target it totals 100 and is included in the campaign: per monitor, 25 hotkey and 25 CLI/integrated activations. At least one topology places a monitor at negative coordinates.

Expected state must appear within 500 ms. “Stuck” means the expected visible/hidden state is absent at that deadline or Escape cannot dismiss a visible menu. After each case there must be exactly one resident process and at most one Ember menu top-level HWND; transient secondary processes must exit within one second. Every IPC request receives one matching result within its deadline; hotkeys are scored by visible state, not ACK.

### Performance

- Run outside a debugger, on AC power with the recorded Windows power mode, after five idle minutes.
- **Warm:** resident initialized, catalog snapshot ready, menu hidden for two seconds; discard 20 warm-ups, record 200 samples split evenly between hotkey and IPC.
- **Cold-process:** resident absent; do not purge the OS file cache; discard five warm-ups, record 50 fresh-process samples.
- Use `QueryPerformanceCounter`. Start at received hotkey/validated command (warm) or external process launch (cold); end on the first presented visible WPF frame, synchronized through the render callback and `DwmFlush`/harness signal.
- Calculate p95 with nearest-rank `ceil(0.95 × n)` over all recorded samples; no post-hoc exclusions. Warm p95 ≤150 ms and cold-process p95 ≤1 second.

### DPI, accessibility, touch, and launch

- Placement final physical bounds differ by no more than two physical pixels from the computed clamp; `VisualTreeHelper.GetDpi` matches target scale within 0.01.
- Smoke scale pairs 100/100, 100/150, 125/175, and 150/200. Capture 100%, 150%, and 200% display scale plus separate 200% text size; no clipped label, action, selection cue, or focus ring.
- Complete the scripted UIA/Narrator protocol, contrast theme, reduced motion, English, Portuguese (Brazil), long German fixture, tap-to-invoke, and touch-scroll checks from the design baseline.
- Both fixed catalog fixtures must enumerate with correct identity/name, launch exactly once, write the expected nonce, and exit. Real-app cases must enumerate and visibly launch without reconstructed command text.

### Security, recovery, and packaging

- Abuse tests cover empty, partial, oversized, malformed, unknown-version/command, stalled, disconnected, flooded, cross-user/session, elevated-client, extreme rectangle, and pre-created-object cases.
- Force termination while hidden, visible, opening, enumerating, extracting an icon, handling IPC, and launching. Explorer and RetroBar remain running and unchanged; `Ctrl+Esc` opens native Start immediately; relaunch yields one resident.
- Normal standard-user and Explorer launches are medium integrity with no UAC. Launch from an elevated terminal must be rejected and must not leave an elevated resident or normal pipe connection.
- Complete the exact MSIX and unpackaged sequences above with hashes and state records.

## Gate decisions

**G1a PASS** requires every activation, placement, latency, DPI, accessibility, fixture-launch, IPC, integrity, recovery, and packaging criterion above. Any native-Start lockout, attributable Explorer crash, retained elevation, injection/system modification, duplicate/wrong-monitor open, unbounded IPC/UI wait, or unsupported stable activation route is a stop condition.

**G1b remains PENDING** until the installed RetroBar Start button invokes the stable integrated form on the correct monitor with a valid anchor and foreground transfer. Passing G1a without G1b authorizes only **hotkey launcher preview**, not **Start replacement**.

## Required evidence packet

- API inventory, source/build/package hashes, OS/hardware/display environment, and ADR updates.
- Raw activation/placement/latency result tables and percentile calculation.
- Redacted state/placement captures and private catalog ledger with an aggregate public summary.
- UIA tree, Accessibility Insights result, Narrator checklist, contrast/text/DPI/touch evidence.
- IPC abuse, standard-user/elevated-parent, foreground-transfer, and forced-kill reports.
- Packaging install/upgrade/rollback-or-roll-forward/uninstall record.
- Open defects and separate Go / Iterate / Stop decisions for G1a and G1b.
