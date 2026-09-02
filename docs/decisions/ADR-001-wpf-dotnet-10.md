# ADR-001 — WPF on .NET 10 LTS

- **Status:** Accepted for feasibility; production confirmation requires G1a
- **Date:** 2026-09-01

## Context

Ember Start needs a highly styled transient Windows surface, direct HWND access, mature keyboard/UI Automation behavior, and precise mixed-DPI interop. Cross-platform value is irrelevant. The comparison set is WPF, WinUI 3, native C++/Win32, and cross-platform UI stacks.

## Decision

Use C# WPF on .NET 10 LTS. Keep Win32 and Shell COM behind a Windows adapter and keep Core independent of WPF. Phase 1 uses three runtime projects: Core, Windows, and App.

WinUI 3 remains a comparator only if the WPF spike misses its latency, DPI, focus, or accessibility budgets and evidence identifies the framework as the cause.

## Consequences

- WPF resource dictionaries and control templates support the original black/orange visual system.
- HWND/message-loop access remains direct through `HwndSource` and supported Win32 APIs.
- The product is Windows-only.
- WPF defaults do not make Per-Monitor v2, focus restoration, or UI Automation correct automatically; those remain explicit gate evidence.

## Validation

G1a must prove warm/cold reveal budgets, correct physical-pixel placement, `WM_DPICHANGED`, keyboard/Narrator viability, and a clean forced-kill fallback.
