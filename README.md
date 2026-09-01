# zune-retro

A safe, reversible, Zune-inspired Start experience for Windows 11.

## Status

Planning and technical validation. The first release is scoped as a per-user Start-menu companion that runs alongside Windows Explorer and RetroBar. A custom taskbar is a separately gated, post-v1 project.

- [Council-reviewed implementation plan](docs/PROJECT_PLAN.md)
- Target: Windows 11 x64, dual-monitor and mixed-DPI support
- Proposed stack: C# / WPF / .NET 10 LTS
- Working product name: **Ember Start**

## Safety baseline

The project will not inject into Explorer, patch Windows system files, install a driver or service, or require administrator privileges at runtime. Native Windows Start and RetroBar remain recovery fallbacks until replacements pass the documented decision gates.

## Repository roadmap

Code will be added only after Gate G0 is recorded and the Phase 1 feasibility sprint begins. See the project plan for architecture, milestones, acceptance thresholds, testing, security, and rollback criteria.
