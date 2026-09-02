# ADR-002 — Explorer Coexistence and Start-Only v1

- **Status:** Proposed; owner ratification required at G0
- **Date:** 2026-09-01

## Context

The desired visual system extends beyond RetroBar, but replacing or patching the Windows shell creates login, update, security, recovery, and accessibility risks far beyond a Start-menu companion.

## Decision

Version 1 owns only the Ember Start window. Explorer continues to own the Windows shell, desktop, File Explorer, native Start fallback, and recovery behavior. RetroBar continues to own the taskbar.

The Start program will not inject into Explorer, patch private symbols, replace system files, install a service/driver, claim an AppBar edge, hide another shell surface, or require runtime elevation. Because `asInvoker` can inherit an elevated parent's token, startup explicitly rejects integrity above medium rather than retaining it. Universal skinning of other applications is outside the supported Windows model and outside scope.

Taskbar feasibility begins only after G7 and a new owner decision at G8. It uses a separate process and independent recovery design.

## Consequences

- Killing Ember Start returns immediately to native Windows behavior.
- v1 cannot reskin arbitrary title bars, third-party controls, the notification area, or Explorer windows.
- Stable-product language requires a verified RetroBar Start-button integration; otherwise the result remains a hotkey launcher preview.

## Validation

Every gate checks native fallback. Any login impairment, Explorer crash attributable to Ember Start, unrecoverable native-Start lockout, or system modification is a stop condition.
