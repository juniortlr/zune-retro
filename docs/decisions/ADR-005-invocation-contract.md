# ADR-005 — Invocation Contract

- **Status:** Accepted for feasibility; owner fallback ratification required at G0
- **Date:** 2026-09-01

## Context

Ember Start needs a stable invocation route independent of its installation version, a supported keyboard path, correct monitor context, one instance per user session, and a native fallback. Windows 11 already owns `Win+Z` for Snap Layouts, and `RegisterHotKey` cannot replace the bare Windows key.

## Decision

The canonical external contract has two strictly parsed forms. The simple form accepts exactly `--toggle`, `--show`, or `--hide` and lets the resident choose foreground→pointer→primary placement. The versioned RetroBar form accepts only an integrated toggle, fixed source/edge enums, and four signed physical-pixel anchor coordinates as specified in the Phase 1 feasibility specification. Invalid geometry falls back to simple placement. The source label grants no privilege.

The initial hotkey candidate is configurable `Ctrl+Alt+Space`, registered with `MOD_NOREPEAT`. Failure to register is nonfatal and never triggers a low-level hook in Phase 1. A foreground-eligible integration launcher may spike `AllowSetForegroundWindow(residentPid)` before IPC; failure is evidence to Iterate and never authorizes `AttachThreadInput` or focus-stealing retries.

Later invocations redirect through a versioned, explicitly ACL-protected current-user/current-session named pipe. Both endpoints validate peer user SID, session, and integrity from the process token rather than payload claims. Same-user, same-session, same-integrity malware remains outside the security boundary; capability is minimized to the fixed UI command set. Commands never carry arbitrary paths, URIs, or command text.

`Ctrl+Esc` and all untouched operating-system Windows-key shortcuts preserve native Start. A bare-Windows-key hook remains off by default and outside Phase 1.

G1 records separate decisions:

- **G1a:** supported chord/command hotkey-launcher feasibility;
- **G1b:** installed RetroBar Start-button invocation of the stable entry point with correct placement context.

## Consequences

- Passing G1a without G1b permits only the label **hotkey launcher preview**.
- The Phase 1 packaging spike must prove a version-independent packaged and unpackaged entry point across upgrade.
- RetroBar integration must call the stable public contract and never depend on Ember internals or a versioned WindowsApps path.

## Validation

G1a requires 1,000 duplicate-free activation cycles, 100 error-free two-monitor placement runs, IPC abuse tests, latency budgets, packaged/unpackaged upgrade evidence, and forced-kill native fallback. G1b remains pending until real RetroBar button evidence exists.
