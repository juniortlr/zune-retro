# zune-retro

A safe, reversible, Zune-inspired Start experience for Windows 11.

## Status

Gate G0 passed on 2026-09-02 and Phase 1 feasibility work is in progress. The first release is scoped as a per-user Start-menu companion that runs alongside Windows Explorer and RetroBar. A custom taskbar is a separately gated, post-v1 project.

- [Council-reviewed implementation plan](docs/PROJECT_PLAN.md)
- [Gate G0 owner decision record](docs/decisions/GATE_G0_CHARTER.md)
- [Phase 1 feasibility specification](docs/architecture/PHASE_1_FEASIBILITY_SPEC.md)
- [Phase 1 foundation evidence](docs/evidence/phase1/FOUNDATION_STATUS.md)
- [Shell catalog vertical-slice evidence](docs/evidence/phase1/CATALOG_VERTICAL_SLICE.md)
- [Visual and accessibility baseline](docs/design/VISUAL_ACCESSIBILITY_BASELINE.md)
- [Final council validation](docs/council/2026-09-02-phase-0-round-3.md)
- Target: Windows 11 x64, dual-monitor and mixed-DPI support
- Stack: C# / WPF / .NET 10 LTS
- Working product name: **Ember Start**

The current Phase 1 branch includes a runnable Ember Fusion window backed by the Windows AppsFolder and Start Menu catalogs, asynchronous Shell icons, argument-free Shell identity activation, strict activation commands, a provisional `Ctrl+Alt+Space` hotkey, current-user/session single-instance IPC, process-integrity checks, and physical-pixel placement policies. It is not yet a Start replacement and has not passed Gate G1.

## Build and run

Requirements: Windows 11 x64 and the .NET SDK selected by [`global.json`](global.json).

```powershell
dotnet restore EmberStart.slnx
dotnet build EmberStart.slnx --configuration Debug --no-restore
dotnet test EmberStart.slnx --configuration Debug --no-build --no-restore
dotnet run --project src/EmberStart.App -- --toggle
```

While the resident is running, use `--show`, `--hide`, or `--toggle` from another process. `Ctrl+Alt+Space` is the provisional global hotkey; registration failure is nonfatal. `Ctrl+Esc` remains the native Windows Start fallback.

## Safety baseline

The project will not inject into Explorer, patch Windows system files, install a driver or service, or require administrator privileges at runtime. Native Windows Start and RetroBar remain recovery fallbacks until replacements pass the documented decision gates.

## Repository roadmap

All eight Gate G0 decisions are ratified and the project is licensed under Apache-2.0. Phase 1 may now implement the non-release feasibility spike; public binaries remain blocked until the later release gates. See the project plan for architecture, milestones, acceptance thresholds, testing, security, and rollback criteria.
