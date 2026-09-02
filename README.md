# zune-retro

A safe, reversible, Zune-inspired Start experience for Windows 11.

## Status

Conditional planning baseline; owner ratification is pending at Gate G0. The first release is scoped as a per-user Start-menu companion that runs alongside Windows Explorer and RetroBar. A custom taskbar is a separately gated, post-v1 project.

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

Code will be added only after all eight Gate G0 decisions are ratified. Until a license is selected, this public-visible repository is not an open-source release and does not accept external contributions or distribute binaries. See the project plan for architecture, milestones, acceptance thresholds, testing, security, and rollback criteria.
