# ADR-003 — Shell AppsFolder as Catalog Authority

- **Status:** Accepted for feasibility; completeness confirmation requires G3
- **Date:** 2026-09-01

## Context

Windows applications include classic shortcuts, packaged apps, PWAs, aliases, protocol entries, localized names, and per-user/common duplicates. Filesystem crawling and package-only enumeration both miss supported entries and create unsafe launch reconstruction.

## Decision

Use `FOLDERID_AppsFolder` as the unified catalog authority. Merge `FOLDERID_Programs` and `FOLDERID_CommonPrograms` only to preserve classic hierarchy and coverage.

Enumeration, property access, shortcut resolution, and icon extraction run on a bounded STA Shell worker. Preserve current-session Shell identity/PIDL and localized display names. Deduplicate by AUMID and then canonical Shell identity, never display name.

Invoke default Shell behavior with Shell/PIDL execution. Use `IApplicationActivationManager` only for packaged entries with a valid AUMID. Never reconstruct a command line from display or shortcut metadata.

## Consequences

- Shell COM behavior and icon extraction require timeout, bounded-queue, and soak evidence.
- Phase 1 proves one classic and one packaged launch and begins a reproducible native All Apps ledger.
- A separate icon worker process is introduced only if measurements demonstrate hangs or unbounded growth.

## Validation

G3 compares against the explicitly defined native Start > All apps denominator: at least 99% appears and launches, duplicates remain below 2%, and below 95% is an automatic no-go.
