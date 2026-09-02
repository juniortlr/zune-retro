# zune-retro

A safe, reversible, Zune-inspired Start experience for Windows 11.

## Status

Gate G0 passed on 2026-09-02 and Phase 1 feasibility work is authorized. The first release is scoped as a per-user Start-menu companion that runs alongside Windows Explorer and RetroBar. A custom taskbar is a separately gated, post-v1 project.

- [Council-reviewed implementation plan](docs/PROJECT_PLAN.md)
- [Gate G0 owner decision record](docs/decisions/GATE_G0_CHARTER.md)
- [Phase 1 feasibility specification](docs/architecture/PHASE_1_FEASIBILITY_SPEC.md)
- [Visual and accessibility baseline](docs/design/VISUAL_ACCESSIBILITY_BASELINE.md)
- [Final council validation](docs/council/2026-09-02-phase-0-round-3.md)
- Target: Windows 11 x64, dual-monitor and mixed-DPI support
- Proposed stack: C# / WPF / .NET 10 LTS
- Working product name: **Ember Start**

## Safety baseline

The project will not inject into Explorer, patch Windows system files, install a driver or service, or require administrator privileges at runtime. Native Windows Start and RetroBar remain recovery fallbacks until replacements pass the documented decision gates.

## Repository roadmap

All eight Gate G0 decisions are ratified and the project is licensed under Apache-2.0. Phase 1 may now implement the non-release feasibility spike; public binaries remain blocked until the later release gates. See the project plan for architecture, milestones, acceptance thresholds, testing, security, and rollback criteria.
