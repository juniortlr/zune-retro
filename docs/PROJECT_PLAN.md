# Zune-Inspired Windows Shell Project Plan

**Working product name:** Ember Start  
**Plan status:** Gate G0 passed — Phase 1 feasibility implementation authorized
**Prepared:** 2026-08-31  
**Target platform:** Windows 11 x64, beginning with the owner's dual-monitor AMD PC  
**Product direction:** A safe, reversible Start-menu companion first; an optional custom taskbar only after a separate feasibility gate

## 1. Executive decision

Build a per-user, non-elevated C#/.NET application that runs alongside Windows Explorer and the existing RetroBar installation. Version 1 owns only the Start-menu experience: app discovery, pinned and recent apps, search, common places, settings shortcuts, and session/power actions. RetroBar continues to own the taskbar. Explorer continues to own the desktop, File Explorer, native shell services, recovery behavior, and the native Start fallback.

The recommended technology is **WPF on .NET 10 LTS**, with a narrow, testable Win32/Shell interop layer. Microsoft generally recommends WinUI 3 for new Windows applications, but WPF is the better engineering fit here: it has mature custom control templating, direct HWND interop, and is already proven by RetroBar, Cairo, Flow Launcher, and ManagedShell. .NET 10 is an active LTS release supported through November 2028. Sources: [Windows development platform overview](https://learn.microsoft.com/en-us/windows/apps/get-started/), [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy), and [ManagedShell](https://github.com/cairoshell/ManagedShell).

This project will not patch Windows system files, inject code into Explorer, replace `explorer.exe`, install a driver or service, or promise to reskin every third-party window. Windows does not expose a supported API for universally changing other applications' title bars, buttons, and internal controls. The safe product can still deliver a cohesive black-and-orange Start menu, taskbar, launcher, icons within our UI, wallpaper coordination, and supported Windows accent settings.

### Council conclusion

| Council role | Recommendation | Main reason |
|---|---|---|
| Windows/.NET engineering | WPF/.NET 10, Start-only v1, Explorer coexistence | Best custom styling and Win32 interoperability with the lowest maintenance risk |
| Product/visual design | “XP Zune shell, Zune-software restraint” | Match RetroBar's dark/orange chrome while preserving fast, readable Windows behavior |
| Security/QA/release | Per-user `asInvoker`, fail-open recovery, signed package | A shell enhancement must never impair login, Explorer, or the native recovery path |
| Integration/technical program | Gates before every scope expansion | Windows updates, focus, hooks, DPI, and multi-monitor behavior require evidence, not assumptions |

## 2. Product charter

### 2.1 Problem statement

RetroBar reproduces the Windows XP Zune taskbar but intentionally opens the modern Windows Start menu. The resulting experience is visually inconsistent. The project will supply an original, Zune-inspired Start experience that visually and behaviorally complements RetroBar without destabilizing Windows.

### 2.2 Version 1 goals

Version 1 must provide:

- A black-and-orange Start menu aligned to the RetroBar Start button area.
- For the stable v1 product—not merely the technical preview—the installed RetroBar Start button opens Ember Start on the correct monitor. If this integration does not pass G1/G6, the build remains explicitly labeled a hotkey launcher and does not satisfy the original product promise.
- Pinned applications, local recent/frequent applications, and an All Apps view.
- Fast search across installed apps, Windows settings, and named common places.
- Correct discovery and launch of classic Win32 and packaged applications.
- Documents, Downloads, Pictures, Music, File Explorer, Settings, and Network shortcuts.
- Lock, sign out, sleep, restart, and shut down actions with unambiguous labels and configurable confirmation for destructive power actions.
- Pointer, keyboard, touch, screen-reader, contrast-theme, and text-scaling support.
- Correct behavior on either monitor, including mixed DPI, negative coordinates, portrait layouts, hot-plugging, and primary-monitor changes.
- Per-user startup, single-instance behavior, local settings, export/reset, clean uninstall, and an explicit native Start fallback.
- Stable external activation contracts: simple `--toggle`/`--show`/`--hide` for command and hotkey fallback, plus a strict versioned integrated form through which RetroBar can supply physical anchor/edge context without knowing menu internals.

### 2.3 Explicit non-goals for version 1

- Replacing Explorer as the Windows shell.
- Replacing RetroBar or recreating the notification area.
- Patching Explorer, StartMenuExperienceHost, DWM, or Windows resource DLLs.
- System-wide icon replacement or third-party window-frame skinning.
- Bare-Windows-key interception as a required architectural dependency.
- Web search, cloud accounts, advertising, telemetry, a plug-in marketplace, arbitrary command execution, or PowerShell integration.
- Reading browser history, document contents, or Windows-wide recent-document history.
- Shipping Microsoft Zune logos, wordmarks, copied bitmaps, or other unlicensed historical assets.

### 2.4 Later optional scope

After the Start menu is stable, the project may evaluate:

- Local file search through the Windows Search index, disabled by default.
- A configurable upstream RetroBar integration that invokes the strict versioned integrated form defined by ADR-005; plain `--toggle` remains the context-free fallback.
- An original icon pack used only inside Ember Start.
- A separate dual-monitor AppBar/taskbar process using documented APIs and, preferably, ManagedShell.
- A companion Windows `.theme` file containing wallpaper, supported accent settings, and sounds, with a one-click restore path.

## 3. Safety and product principles

1. **One owner per shell region.** Ember Start owns Start; RetroBar owns the taskbar; Explorer owns the desktop and recovery shell.
2. **Fail open.** If Ember Start fails, native Start and Explorer remain usable.
3. **No elevation at runtime.** The application runs as the signed-in user with `requestedExecutionLevel=asInvoker` and `uiAccess=false`.
4. **Documented APIs first.** Unsupported hooks are isolated, optional, and gated.
5. **Local and private by default.** No network access or telemetry is required for the product to work.
6. **Accessibility and mixed DPI are architecture requirements.** They are not post-release polish.
7. **Original assets only.** Historical inspiration is acceptable; copied branding and artwork are not.
8. **Reversibility is a feature.** Startup, integration, theme settings, updates, and uninstall all have tested rollback paths.
9. **Unknown Windows builds degrade safely.** Disable only an incompatible integration; never prevent Explorer or login.
10. **Scope expands only at a decision gate.** A working Start menu does not automatically authorize a taskbar or shell replacement.

## 4. Target users and core journeys

### Primary user

A Windows 11 user who wants the Windows XP Zune visual character while retaining modern application compatibility, two-monitor behavior, gaming compatibility, and easy recovery.

### Five acceptance journeys

1. Open Start on either monitor and launch a pinned browser.
2. Type immediately, find an unfamiliar installed app, and launch it.
3. Open Downloads or Windows Settings without searching.
4. Pin, reorder, and unpin an app, then verify persistence after reboot.
5. Lock, sleep, restart, or shut down without confusing one action for another.

Every journey must work by pointer, keyboard, and Narrator or another UI Automation client, including confirmation and cancellation of every power/session action.

## 5. Visual and interaction specification

### 5.1 Design direction

The default direction is **“XP Zune shell, Zune-software restraint.”** The surrounding chrome should match the installed RetroBar preset, but the content layout should use simple typography, negative space, clear grouping, and short motion rather than reproducing every XP bevel.

Use a neutral public product name such as **Ember Start**. “Zune-inspired” can appear descriptively with a non-affiliation notice; do not name the distributed application “Zune Shell” or use the Zune wordmark. Microsoft's current guidance recommends avoiding product names that imply affiliation and using third-party product names only descriptively. Source: [Microsoft trademark and copyright guidance](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/trademark-and-copyright-protection).

### 5.2 Proposed original design tokens

These are new project values, not copied Microsoft assets. The canonical state rules, additional semantic tokens, exact layout units, and reproducible accessibility protocol are maintained in [the visual and accessibility baseline](design/VISUAL_ACCESSIBILITY_BASELINE.md).

| Token | Value | Use |
|---|---:|---|
| `Canvas` | `#101010` | Window background |
| `Surface` | `#1B1A18` | Primary panels |
| `SurfaceRaised` | `#25221E` | Hovered/raised areas |
| `Accent` | `#F5841F` | Accent rail, glyph, and principal emphasis |
| `OnAccent` | `#101010` | Text/icons on bright Accent |
| `Selection` | `#AD4D00` | Selected-row fill |
| `TextPrimary` | `#F5F5F5` | Main text |
| `TextSecondary` | `#BDBDBD` | Supporting text |
| `Focus` | `#FFD166` | Keyboard focus ring |
| `Divider` | `#3A3834` | Decorative separators only |
| `OutlineStrong` | `#6F6B64` | Essential control boundaries |
| Typography | Segoe UI Variable | Windows-native legibility |
| Spacing | 4/8 effective-pixel grid | Layout rhythm |
| Row/touch target | 44 effective pixels minimum | Pointer and touch access; 48 in touch density |
| Corner radius | 2–4 effective pixels | Restrained retro geometry |
| Motion | 120–170 ms | Fast open/close and selection |

Normal text must meet at least 4.5:1 contrast and large text at least 3:1. In Windows contrast themes, system theme resources replace the palette. Microsoft recommends Segoe UI Variable for current Windows typography and requires keyboard, UI Automation, and contrast support. Sources: [Typography in Windows](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography), [Windows iconography](https://learn.microsoft.com/en-us/windows/apps/design/iconography/), and [Windows accessibility guidance](https://learn.microsoft.com/en-us/windows/apps/develop/accessibility).

### 5.3 Information architecture

The home view uses a familiar two-column arrangement:

- **Header:** user display name, optional local avatar, optional clock.
- **Left column:** pinned apps, recent/frequent apps launched through Ember Start, and All Apps.
- **Right column:** File Explorer, Documents, Downloads, Pictures, Music, Settings, and Network.
- **Footer:** search affordance and a visually separated Power/Session menu.
- **Search state:** replaces the menu body with grouped results for Apps, Settings, and Places. Local Files is a later opt-in provider.
- **Settings:** a separate normal window, never nested into the transient Start surface.

### 5.4 Interaction contract

- Only one menu exists per user session. It moves to the invoking monitor rather than creating one process/window per display.
- Authorization derives from the connected process token and active Windows session, never a payload session claim. The strict integrated form carries a fixed source/edge enum and validated physical anchor rectangle; a plain `toggle` without placement context uses resident-side foreground→pointer→primary fallback.
- Pointer invocation anchors to the selected taskbar edge and Start-button region.
- Keyboard invocation opens on the monitor containing the foreground window; if no suitable foreground window exists, use the pointer's monitor, then the primary monitor.
- Typing from the home view immediately enters search.
- Arrow keys move through results; Enter launches; Escape dismisses; Shift+F10 opens context actions.
- Clicking outside, launching an item, removing the active display, or invoking Start a second time dismisses the menu.
- `Ctrl+Esc` or an explicit “Open Windows Start” command remains the native escape hatch. All operating-system Windows-key shortcuts remain untouched.
- Restart and shut down use clear confirmation by default during alpha and beta. The user may later opt out.
- Animations stop when Windows reduced-motion settings request it.

### 5.5 Multi-monitor and DPI behavior

Declare Per-Monitor DPI Awareness v2 in the application manifest. Placement uses the invoking monitor's work area and supports negative virtual-screen coordinates. The menu flips or constrains its expansion direction near an edge and is remeasured on DPI and display changes; the entire window is never bitmap-scaled.

Required scale-pair tests include 100/100, 100/150, 125/175, and 150/200 percent. Required display events include docking, undocking, sleep/resume, RDP reconnect, primary-monitor changes, portrait rotation, taskbar edge changes, and removal of the monitor currently showing the menu. Microsoft recommends setting process DPI awareness in the manifest and documents Per-Monitor v2 behavior. Sources: [DPI awareness contexts](https://learn.microsoft.com/en-us/windows/win32/hidpi/dpi-awareness-context) and [WPF per-monitor DPI guidance](https://learn.microsoft.com/en-us/windows/win32/hidpi/declaring-managed-apps-dpi-aware).

## 6. Technical architecture

### 6.1 Technology decision

| Option | Decision | Rationale |
|---|---|---|
| WPF + .NET 10 LTS | **Selected baseline** | Mature custom XAML styling, good Win32 interop, design-time support, proven in comparable shell tools |
| WinUI 3 | Spike comparator only | Microsoft's modern default, but its Fluent defaults and deployment complexity provide little immediate value for this retro surface |
| C++/Win32 | Rejected for v1 | Maximum control but substantially slower and riskier to implement and maintain |
| MAUI/Avalonia/Electron | Rejected | Cross-platform value is irrelevant and Windows shell integration still requires Win32-specific work |

Use the Windows App SDK incrementally only if a specific lifecycle, notification, or packaging API justifies it. Use [Microsoft CsWin32](https://github.com/microsoft/CsWin32) to generate strongly typed bindings for the small Win32 API surface instead of maintaining hand-written signatures.

### 6.2 Process model

- One resident, per-user, medium-integrity WPF process. Because `asInvoker` can inherit a high-integrity parent token, startup rejects integrity above medium rather than retaining elevation.
- No Windows service, scheduled task, driver, broker, or runtime administrator process.
- A single-instance coordinator redirects later activations to the resident process.
- Instance names, mutexes, and named pipes include both the current user's SID and Windows session ID so concurrent console/RDP sessions cannot collide.
- A named pipe with explicit non-inheriting ACLs restricted to that interactive user SID accepts a small versioned command set such as `toggle`, `show`, `hide`, and `settings`. Both endpoints validate the peer SID, Windows session, and integrity from its process token rather than trusting payload fields. LocalSystem and elevated clients are not admitted to the normal UI pipe. Same-user/same-session/same-integrity malware remains outside the security boundary, so the protocol minimizes capability rather than claiming application identity.
- Search scoring and managed queue work are cancellable background operations. Shell discovery and icon COM calls are not assumed cancellable: UI waits and queues are bounded, a timed-out worker opens its circuit, and the UI degrades to cached/empty results.
- The UI thread only renders state and performs brief shell activation calls.
- Icon extraction moves to a constrained worker process only if soak tests demonstrate COM hangs or unbounded memory growth.
- Crash-loop protection disables auto-restart after three crashes within five minutes.

### 6.3 Proposed solution layout

```text
EmberStart/
├── EmberStart.slnx
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── docs/
│   ├── architecture/
│   ├── decisions/
│   ├── design/
│   ├── security/
│   └── testing/
├── src/
│   ├── EmberStart.App/              # WPF lifecycle, menu and settings windows
│   ├── EmberStart.Core/             # Models, use cases, ranking, settings contracts
│   ├── EmberStart.Platform.Windows/ # Win32, COM, monitor, Shell and power adapters
│   ├── EmberStart.Catalog/          # Installed-app and Settings catalog providers
│   ├── EmberStart.Theming/          # XAML dictionaries and original assets
│   └── EmberStart.Integration/      # Named-pipe and optional RetroBar adapter
├── tests/
│   ├── EmberStart.UnitTests/
│   ├── EmberStart.IntegrationTests/
│   ├── EmberStart.UiAutomationTests/
│   └── EmberStart.FuzzTests/
├── packaging/
└── tools/
```

### 6.4 Component responsibilities

| Component | Responsibilities | Must not do |
|---|---|---|
| App host | Lifecycle, single instance, window creation, recovery | App discovery or arbitrary shell parsing |
| Menu presentation | Render view state, focus, keyboard/pointer input, accessibility peers | Direct COM enumeration on UI thread |
| Core | Search ranking, pins, recency, settings validation, commands | Reference WPF or Win32 |
| Windows platform | Shell folders, icons, activation, monitors, DPI, documented power APIs | Inject, patch, or construct command strings |
| Catalog | Normalize/deduplicate apps and Settings entries, cache metadata | Crawl the whole disk |
| Theming | Resource dictionaries, semantic tokens, contrast fallback | Load executable XAML or code from untrusted themes |
| Integration | Versioned IPC and activation adapters | Depend on RetroBar internals |

### 6.5 Application discovery and launch

Use the Shell's supported model rather than inventing a package scanner:

- Enumerate `FOLDERID_AppsFolder` for the unified application view.
- Reconcile `FOLDERID_Programs` and `FOLDERID_CommonPrograms` for Start-menu shortcuts and folder hierarchy.
- Normalize by Shell identity, AppUserModelID, or resolved shortcut target; preserve localized display names.
- Obtain icons through Shell item image/icon interfaces and cache bounded raster sizes for the active DPI.
- Launch traditional shell items through Shell execution.
- Activate packaged applications by AppUserModelID through `IApplicationActivationManager` where appropriate.
- Reconcile inventory on startup, package/install notifications where available, and a low-frequency bounded refresh; never block opening the menu on a full refresh.

Microsoft documents the virtual Apps folder and Start-menu known folders in [KNOWNFOLDERID](https://learn.microsoft.com/en-us/windows/win32/shell/knownfolderid), and packaged-app activation in [IApplicationActivationManager](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-iapplicationactivationmanager-activateapplication).

For Gate G3, the denominator is the set of unique launchable entries visible in native **Start > All apps** for the test account. Collapse duplicate shortcuts that resolve to the same Shell identity. Exclude only group/folder headings, native recommendations or web content, and entries that native Windows itself cannot launch during the same test. Include packaged apps, PWAs, execution aliases, classic shortcuts, uninstall shortcuts, and every launchable child of an All Apps folder. Record every exclusion and classification in the gate evidence so the 99% result is reproducible.

### 6.6 Search model

MVP providers are internal, not third-party plug-ins:

1. Installed Apps.
2. Windows Settings and Control Panel entries maintained in a versioned internal catalog.
3. Common Places.

Each provider returns immutable candidates to a central ranker. Ranking considers exact prefix, token prefix, substring/fuzzy score, pinned state, and local launches through Ember Start. Search is cancellable and generation-tagged so late results cannot overwrite newer queries. Query text is never logged.

Local file search is a post-v1 provider using the Windows Search index, not a custom disk crawler. Web search remains out of scope.

### 6.7 Settings and state

Store user state beneath `%LocalAppData%\EmberStart`:

- `settings.json`: versioned schema and product preferences.
- `pins.json`: ordered app identities, not fragile display names.
- `usage.json`: bounded launch counts and timestamps only for items launched through Ember Start.
- `cache/`: regenerable icon and catalog data.
- `logs/`: bounded, redacted diagnostics.

Writes are transactional: create a new file, validate it, atomically replace the current file, and retain one last-known-good copy. A failed migration restores the previous version or safe defaults without deleting the original.

## 7. RetroBar and keyboard integration strategy

Perfect invocation is the hardest integration problem and must be solved in this order:

| Priority | Approach | Risk | Decision |
|---:|---|---|---|
| 1 | A configurable supported chord (initial candidate `Ctrl+Alt+Space`) plus `EmberStart.exe --toggle` | Low | Ship in the first technical alpha; registration failure is nonfatal |
| 2 | Propose an upstream RetroBar option to run a configured Start command | Low–medium | Preferred click-integration route |
| 3 | Maintain a RetroBar fork | High maintenance | Avoid unless upstream integration is rejected and the user accepts the maintenance cost |
| 4 | Replace RetroBar with our taskbar | Very high | Separate post-v1 program and gates |

`RegisterHotKey` cannot be the basis for replacing the bare Windows key because Microsoft reserves Windows-key shortcuts for the operating system. No low-level keyboard hook is part of the approved v1 roadmap or Gates G1–G7. Any future hook research requires a new post-G7 owner scope decision, ADR, threat review, and decision gate; it cannot inherit approval from this plan. Source: [RegisterHotKey documentation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey).

The v1 architecture is acceptable only if the user agrees that a configurable supported chord is the temporary fallback when a clean RetroBar click integration is not yet available. `Win+Z` is not used because Windows 11 owns it for Snap Layouts. `Ctrl+Esc` remains the native Start fallback.

## 8. Comparable projects and lessons learned

| Project | Relevant approach | Lesson adopted | Lesson deliberately avoided | License |
|---|---|---|---|---|
| [RetroBar](https://github.com/dremin/RetroBar) | WPF taskbar using ManagedShell and XAML themes; delegates Start | Keep shell ownership narrow and theme via resource dictionaries | Rebuilding its stable task/tray behavior in v1 | Apache-2.0 |
| [ManagedShell](https://github.com/cairoshell/ManagedShell) | .NET/WPF tasks, tray, AppBar, and shell helpers; can coexist with Explorer | Strong candidate for the optional taskbar and a model for service separation | Reimplementing the notification area from undocumented behavior | Apache-2.0 |
| [Open-Shell](https://github.com/Open-Shell/Open-Shell-Menu) | Mature Start menu, skins, custom button, keyboard mapping | Separate menu model from skinning; preserve native fallback and exportable settings | Depending on deep Explorer integration and decades of compatibility hooks | MIT |
| [Cairo](https://github.com/cairoshell/cairoshell) | Full C#/WPF desktop environment | WPF can deliver a complete alternative desktop; use isolated services | Expanding v1 into desktop, file navigation, tray, and shell replacement | Apache-2.0 |
| [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher) | WPF launcher with Core/Infrastructure/Plugin separation and async search | Provider separation, cancellation, local settings, keyboard-first search | Plug-in store and elevated shell commands in v1 | MIT |
| [PowerToys](https://github.com/microsoft/PowerToys) | Modular runner, named-pipe IPC, hotkeys, privacy and release discipline | Versioned IPC, settings separation, diagnostics, specifications | Suite-level process and plug-in complexity | MIT |
| [ExplorerPatcher](https://github.com/valinet/ExplorerPatcher) | Build-sensitive shell modification and Explorer restart | Treat it as evidence that update compatibility needs explicit gates | Injection, private symbols, system-level patching | GPL-2.0 |

### Specific failure patterns to convert into tests

- RetroBar has had reports involving multi-monitor always-on-top state and DPI alignment. These become mandatory dock, hot-plug, autohide, and mixed-DPI regression tests: [multi-monitor issue](https://github.com/dremin/RetroBar/issues/1162) and [DPI alignment issue](https://github.com/dremin/RetroBar/issues/1380).
- Open-Shell's issue history shows that Windows updates, Explorer restarts, focus, drag/drop, and screen-reader behavior can regress independently: [Start unavailable until Explorer restart](https://github.com/Open-Shell/Open-Shell-Menu/issues/2348), [Windows 11 drag/drop regression](https://github.com/Open-Shell/Open-Shell-Menu/issues/2189), and [NVDA regression](https://github.com/Open-Shell/Open-Shell-Menu/issues/2476).
- Cairo demonstrates the difficulty of launching and integrating modern packaged apps and Windows Settings when Explorer is replaced. Explorer therefore remains running: [Cairo packaged-app/shell discussion](https://github.com/cairoshell/cairoshell/issues/936).
- ExplorerPatcher's current releases include Windows-build-specific fixes and warnings that Explorer or Start may fail when injected files are incompatible or blocked. This validates the no-injection rule: [ExplorerPatcher releases](https://github.com/valinet/ExplorerPatcher/releases).
- Flow Launcher's monitor-placement discussions show that “window appears on the correct monitor” is not equivalent to “launched experience belongs to the correct monitor.” Monitor ownership must be an explicit use-case test: [Flow Launcher monitor discussion](https://github.com/Flow-Launcher/Flow.Launcher/discussions/2351).
- PowerToys uses a runner, separate settings process, named pipes, and a low-level hook message loop. The useful lesson is isolation and fast callback dispatch, not copying its suite architecture: [PowerToys architecture](https://github.com/microsoft/PowerToys/blob/main/doc/devdocs/core/architecture.md) and [runner architecture](https://github.com/microsoft/PowerToys/blob/main/doc/devdocs/core/runner.md).

Do not copy GPL-licensed ExplorerPatcher code into a differently licensed product. Apache-2.0 and MIT dependencies may be considered after dependency and notice review. Every dependency gets a pinned version, license record, security review, and explicit architectural justification.

## 9. Delivery phases

Durations are full-time solo-engineer estimates with part-time design and QA support. Part-time evening work may take roughly two to three times the calendar duration.

### Phase 0 — Charter and clean repository (2–3 days)

**Deliverables**

- Create a new repository, recommended path `C:\Users\EG\Documents\ChatGPT\EmberStart`.
- Do not add the project to the current workspace's unrelated Rust game repository.
- Record product scope, supported Windows versions, license, naming decision, and non-goals.
- Create ADR-001 through ADR-005 listed in section 16.
- Create a risk register, threat-model skeleton, and Definition of Done.

**Gate G0:** Product/legal scope.

### Phase 1 — Technical feasibility spikes (5–7 working days)

Build disposable or minimal vertical spikes for:

- WPF versus WinUI menu-window creation and HWND access.
- Per-Monitor v2 positioning on both monitors at mixed scale factors.
- Focus acquisition, outside-click dismissal, Escape, and foreground restoration.
- `FOLDERID_AppsFolder` enumeration, localized names, icons, and Win32/packaged app launch.
- Single-instance activation and current-user-only named-pipe IPC.
- A configurable supported activation chord (initial candidate `Ctrl+Alt+Space`) and external `--toggle` command. Registration failure must degrade to the command/native fallback without installing a hook.
- An experiment for a clean RetroBar command integration. Bare-Windows-key behavior remains outside Phase 1 and requires a later separate gate.
- An early MSIX/full-trust versus unpackaged activation spike. Prove a stable version-independent entry point—such as a registered protocol, execution alias, or package activation route—that RetroBar can call after package upgrades.

**Deliverables:** Feasibility report, benchmark data, screenshots, API inventory, and updated ADRs.

Use the minimal Phase 1 assembly boundary in [the feasibility specification](architecture/PHASE_1_FEASIBILITY_SPEC.md); defer the full production assembly split until the spike establishes real boundaries.

**Gate G1:** Record G1a for supported hotkey/command launcher feasibility and G1b separately for verified RetroBar Start-button invocation. G1a alone authorizes only a hotkey-launcher preview.

### Phase 2 — Product and accessibility prototype (5 working days)

Create two moodboards:

1. **Ember Classic:** more bevel, compact density, and a closer original visual conversation with RetroBar.
2. **Ember Fusion:** restrained dark chrome, larger typography, and simpler layout.

Produce static designs for home, All Apps, search, context menu, power, long text, high contrast, and two-monitor placement. Capture the installed RetroBar Zune theme and compare default, hover, pressed, focus, and inactive states side by side while using only original project-owned assets. Build a keyboard-operable prototype using fake catalog data.

For a personal/private build, the owner completes all five journeys without assistance in two separate sessions. Before public distribution, run a fixed protocol with five participants × five journeys and require at least 23/25 unassisted completions, with zero power-action errors.

**Gate G2:** Design, information architecture, and accessibility prototype.

### Phase 3 — Foundation and inventory vertical slice (1–2 weeks)

- Establish solution structure, analyzers, formatting, tests, and CI.
- Implement lifecycle, single instance, IPC, DPI manifest, monitor placement, focus state machine, and bounded logging.
- Implement app discovery, normalization, icon cache, and launch adapters.
- Build the basic menu against real installed applications.
- Add automated geometry and catalog fixtures.

**Gate G3:** Inventory and launch completeness.

### Phase 4 — Start-menu MVP (3–4 weeks)

- Pinned apps with reorder and persistence.
- Local recency/frequency.
- All Apps hierarchy and alphabetical navigation.
- Search across Apps, Settings, and Places.
- Common folders and Settings URIs.
- Power/session commands and confirmation behavior.
- Complete keyboard navigation, UI Automation peers, visible focus, contrast-theme resources, reduced motion, and 200% text-scale behavior.
- Settings window with startup, motion, density, native fallback, export/reset, and diagnostics controls.
- Startup hidden, single-instance redirect, corrupt-config recovery, and crash-loop handling.

**Gate G4:** Safe internal alpha.

### Phase 5 — Hardening and compatibility (2–3 weeks)

- Run the full display, DPI, sleep, RDP, Explorer restart, fullscreen, virtual desktop, and locale matrices.
- Add UI Automation and fuzz tests.
- Profile cold/warm open, search, memory, CPU, GPU activity, and icon-cache growth.
- Run 24-hour soak and forced-crash/recovery campaigns.
- Validate against the current Windows cumulative update and a clean VM.
- Document supported and unsupported combinations.

**Gate G5:** Preview/beta readiness.

### Phase 6 — Packaging and preview release (1–2 weeks)

- Finalize the packaging decision proven in Phase 1 and implement the selected MSIX or signed WiX/MSI route.
- Implement user-controllable StartupTask or per-user startup fallback.
- Generate SBOM, license notices, hashes, and signed artifacts.
- Test install, upgrade, downgrade, rollback, uninstall, retained settings, and complete removal.
- Publish preview notes, privacy statement, recovery instructions, and known limitations.

**Gate G6:** Signed Start-menu release candidate.

### Phase 7 — Stable Start-menu release (minimum 2 weeks of dogfooding)

- Seven consecutive days of normal owner use after the latest cumulative Windows update.
- If publicly released: staged rollout through preview before stable.
- Triage all Severity 1 and Severity 2 defects before stable.
- Tag a reproducible release and retain N−1 for rollback.

**Gate G7:** Start-menu stable.

### Phase 8 — Optional taskbar feasibility (2–3 weeks)

This phase begins only after Gate G7 and a new scope decision.

- Prototype a separate taskbar process, not a mode inside the Start process, plus an independent per-user recovery watchdog.
- Use documented AppBar behavior through `SHAppBarMessage` and make ManagedShell/task/tray feasibility a required spike outcome rather than an assumed Phase 9 capability.
- Use a reversible transaction to suspend RetroBar only during an individual test. RetroBar and the prototype never claim the same AppBar edge simultaneously.
- Demonstrate one bar per monitor, work-area reservation, window tracking, packaged apps, Explorer restart, display hot-plug, mixed DPI, fullscreen behavior, and forced-kill recovery.
- On forced taskbar termination—even when cleanup code never runs—the independent watchdog must restore native/RetroBar visibility within three seconds. Do not hide or disable RetroBar permanently during the spike.

Microsoft's AppBar API is the supported way for an application-defined desktop toolbar to coordinate screen work area with the shell: [Application desktop toolbars](https://learn.microsoft.com/en-us/windows/win32/shell/application-desktop-toolbars) and [SHAppBarMessage](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shappbarmessage).

**Gate G8:** Taskbar feasibility.

### Phase 9 — Optional taskbar product (8–14 weeks)

Only proceed if G8 passes. Build task buttons, pinned/quick-launch items, clock, tray/overflow, input language, thumbnails, drag ordering, autohide, edge placement, multi-monitor policy, and crash restoration. RetroBar remains installed and is the production fallback until the new taskbar passes a separate 14-day qualification and two monthly Windows update validations. Every ownership transition is transactional: release the current AppBar, confirm work-area restoration, then enable the next owner; the watchdog reverses an incomplete transition.

**Gate G9:** Permission to replace RetroBar for daily use.

### Schedule summary

| Scope | Full-time solo estimate | Part-time/evening estimate |
|---|---:|---:|
| Technical proof and design prototype | 2–3 weeks | 4–7 weeks |
| Reliable Start-menu v1 | 8–12 weeks total | 4–7 months |
| Optional production taskbar | Additional 10–17 weeks | Additional 5–9 months |
| Start menu plus taskbar, polished | Roughly 4–7 months | Roughly 9–16 months |

These estimates exclude unpredictable code-signing identity verification and public-store review time.

## 10. Decision gates

Every gate produces an evidence packet: build hash, environment, automated results, manual checklist, benchmarks, screenshots/video where relevant, open defects, and a written Go / Iterate / Stop decision.

| Gate | Pass criteria | If it fails |
|---|---|---|
| G0 — Charter/legal | Owner ratifies all eight choices in the [G0 charter](decisions/GATE_G0_CHARTER.md), including distribution/license, Start-only v1, original assets, neutral name, Explorer/RetroBar coexistence, invocation fallback, local recency, power confirmation, and no universal window skinning | Redefine the product; do not code |
| G1a/G1b — Feasibility/invocation | G1a: 1,000 activation cases with no duplicate menu; packaged and unpackaged candidates have recorded rehearsals and at least one ADR-006-selected stable route passes all required tests; simple hotkey/CLI activation uses resident-side foreground→pointer→primary placement and 50 invocations per monitor have zero wrong-monitor opens; `Ctrl+Esc` native Start remains accessible; WPF meets initial latency/DPI targets. G1b: the installed RetroBar Start button invokes the selected strict integrated entry point and supplies its fixed source/edge plus validated physical anchor context. Without G1b, the build remains a hotkey-launcher preview. | Retain the supported command/hotkey launcher, redesign RetroBar integration, or stop the Start-replacement claim; reconsider framework only if evidence identifies WPF as the blocker |
| G2 — Design/accessibility | Owner approves one moodboard and side-by-side RetroBar state match; personal build: owner completes 5/5 journeys twice; public build: ≥23/25 unassisted completions across five fixed participants, with zero power-action errors; all five journeys pass keyboard and Narrator; normal text ≥4.5:1, non-text controls/focus ≥3:1, contrast themes pass, and no clipping at 200% text | Iterate before platform work expands |
| G3 — Inventory/launch | At least 99% of the explicitly defined native All Apps denominator appears and launches on the target PC; duplicates <2%; icons/names correct; app install/uninstall reconciles without restart; below 95% is automatic no-go | Fix Shell catalog architecture before MVP features |
| G4 — Internal alpha | Per-user `asInvoker`; no injection/system modification; forced termination leaves native Start usable; atomic config recovery; no admin prompt; all P0 unit/integration/UI tests pass | No external build |
| G5 — Preview/beta | Full target matrix passes, including standard-user, offline, application-control, and unsupported-build fail-open cases; zero critical accessibility findings; 500 mixed-monitor invocations without focus/placement failure; 24-hour soak within budgets; zero Explorer crashes attributable to the product | Continue internal hardening |
| G6 — Release candidate | Signed artifacts; clean SBOM/scans; tampered update rejected; startup user-controlled; 100 install/update/rollback/uninstall cycles; recovery procedure independently verified | Block distribution |
| G7 — Stable Start | At least seven days, 50 local sessions, and 1,000 privacy-preserving counted invocations on the current Windows update; no more than one app crash, zero unrecovered/native-shell failures, no Sev-1/2 defects, and native fallback verified. Counters contain no queries, paths, app inventory, or titles. | Remain preview or roll back to N−1 |
| G8 — Taskbar feasibility | Exclusive AppBar ownership; ManagedShell/task/tray feasibility resolved; two mixed-DPI monitors, hot-plug, Explorer restart, sleep/resume, fullscreen, and 100 forced kills; independent watchdog restores native/RetroBar visibility within three seconds; no injection/private symbols | Permanently remain Start-only under this architecture |
| G9 — Replace RetroBar | Feature parity for task buttons, packaged apps, tray/overflow, clock, input language, thumbnails, drag ordering, multi-monitor/DPI, and recovery; 14-day qualification plus two cumulative-update validations | Keep RetroBar as production taskbar |

## 11. Quality strategy

### 11.1 Automated test layers

**Unit tests**

- Search normalization, ranking, cancellation, and stable ordering.
- Pin ordering, migration, missing-app behavior, and local recency limits.
- Settings schema validation, migration, atomic recovery, and defaults.
- DPI conversions, monitor selection, edge placement, and negative coordinates.
- Shortcut and URI validation, launch policy, and redaction.
- Crash-loop and fallback state machines.
- IPC authentication, versioning, size limits, and malformed messages.

**Integration tests on clean Windows VMs**

- Enumerate and launch Win32, packaged, alias, protocol, and Settings entries.
- App install/uninstall/upgrade reconciliation.
- First run, startup, single-instance redirection, and Explorer restart.
- Corrupt settings/cache, missing icon, hung icon provider, and forced process kill.
- Install, upgrade, downgrade, uninstall, and signature rejection.

**UI Automation tests**

- Open, dismiss, search, launch, pin/unpin/reorder, All Apps, Settings, and power confirmation.
- Focus restoration and no focus traps.
- Tab, arrows, Enter, Space, Escape, Shift+F10, and access keys.
- Automation names, roles, states, groups, selection, and result-count announcements.

**Fuzz/property tests**

- Settings and theme JSON.
- Named-pipe messages.
- Search strings, Unicode, long text, invalid surrogate sequences, and RTL text.
- Shortcut metadata and icon dimensions.

### 11.2 Manual hardware matrix

| Area | Required coverage |
|---|---|
| Windows | Windows 11 24H2 and 25H2 x64 with current monthly stable updates; 26H1 as compatibility canary only until stable support is declared |
| Displays | One, two, and three monitors; 100/125/150/175/200%; mixed DPI; negative coordinates; portrait and landscape; primary swaps |
| Display events | Hot-plug, dock/undock, sleep/resume, HDR/SDR, display-driver reset, RDP connect/reconnect |
| Shell | Explorer alone; Explorer + RetroBar; documented Open-Shell compatibility check |
| Unsupported combinations | ExplorerPatcher, Windhawk shell mods, or other taskbar patchers are detected/warned and not part of the supported matrix |
| Security posture | Standard user; UAC transitions; Smart App Control or equivalent application-control policy where available; signed and deliberately tampered artifacts |
| Connectivity/build fallback | Fully offline operation; unknown/unsupported Windows build disables incompatible integration and leaves native Start available |
| Interaction | Mouse, keyboard-only, touch, high-precision touchpad |
| Accessibility | Narrator, UI Automation inspection, contrast themes, 200% text, reduced motion |
| Locale | English, Portuguese (Brazil), one long-label LTR locale such as German, one CJK locale, and one RTL locale |
| Workloads | Fullscreen game, borderless game, video playback, virtual desktops, UAC secure desktop, lock/unlock |
| Hardware | Owner's AMD/x64 PC; at least one Intel system/VM host; ARM64 only after a separate full-matrix gate |

### 11.3 Performance budgets

Measure on the owner's actual PC and a clean VM. A regression exceeding a budget by more than 10% blocks release unless the council approves a documented exception.

| Metric | Target |
|---|---:|
| Resident warm reveal | p95 ≤150 ms; stretch target ≤100 ms |
| Cold first reveal | p95 ≤1 second |
| Keystroke to updated results | p95 ≤50 ms |
| First useful search result | p95 ≤300 ms |
| Initial catalog | ≤2 seconds without blocking menu display |
| Logon readiness | p95 ≤5 seconds without slowing foreground startup |
| Idle CPU | p95 <0.2% over a five-minute sample |
| Steady working set | Target <120 MB; hard beta ceiling 150 MB |
| UI thread stalls | No stall >100 ms during index/icon work |
| GPU | No continuous activity while hidden or idle |
| Soak | No unbounded growth; <10% working-set drift after stabilization over 24 hours |

## 12. Security, privacy, and recovery

### 12.1 Threat boundaries

Treat the following as untrusted:

- `.lnk`, `.url`, package metadata, icon resources, and display names.
- Search input and imported settings.
- Theme and image files.
- IPC clients and message payloads.
- Installer, update feed, and downloaded artifacts.

### 12.2 Required controls

- Runtime uses `asInvoker`; no service or scheduled task.
- IPC instance and pipe names include a SID-derived identifier plus Windows session ID. Explicit non-inheriting mutex/pipe ACLs permit only that interactive user SID; endpoints validate connected process SID, session, and integrity, and the protocol has a version, 4 KiB message cap, 500 ms I/O deadlines, queue bound, and rate limit. LocalSystem and elevated clients are not trusted by the normal UI channel.
- Use Shell identities/PIDLs and argument-safe activation APIs; never concatenate a command line from catalog metadata or user input.
- No arbitrary shell, PowerShell, or plug-in execution.
- Theme input is declarative and schema-limited. Do not load arbitrary loose XAML because XAML can instantiate types and expand the attack surface.
- Restrict DLL search paths and prefer packaged/read-only binaries.
- Dependencies are pinned and scanned; release output includes an SBOM and third-party notices.
- Logs exclude queries, arguments, file/document paths, app inventory, usernames, and window titles. Diagnostics are local, bounded, redacted, previewable, and user-clearable. Raw catalog ledgers and monitor captures are separate owner-controlled gate artifacts and are sanitized before any public commit or export.
- Telemetry is off by default. Any future health telemetry requires a separate opt-in and published data dictionary.
- Power and session actions use documented APIs and explicit labels. Never imitate a credential or UAC dialog.

### 12.3 Crash and shell recovery

- If Start integration does not receive a ready acknowledgment within 500 ms, expose native Start or a recovery action.
- A forced Ember Start termination must not alter Explorer or RetroBar state.
- Three crashes within five minutes suppress automatic restart and bare-Windows integration until the user opens recovery settings.
- Persist only a reversible “enabled” preference. Do not permanently hide native shell components in the Start phase.
- The installer includes a signed recovery command that removes Ember Start from startup and resets integration without starting the main UI.
- Retain the prior stable package and last-known-good settings during upgrade.

## 13. Packaging, signing, startup, and updates

### Development

- Use x64 self-contained debug/dev builds on the owner PC.
- Self-signed certificates are acceptable only on controlled test machines.
- Startup is opt-in and clearly visible in Windows Startup Apps.

### Preview and public distribution

Prefer a per-user full-trust MSIX package if the packaging spike confirms all Shell and startup behavior. MSIX offers atomic installation, clean uninstall, package identity, and App Installer/Store update paths. Use signed WiX/MSI only if MSIX blocks a required documented shell behavior.

For a packaged desktop application, use a manifest `StartupTask` so the user can control launch-at-login from Windows Settings or Task Manager. Sources: [StartupTask](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.startuptask) and [desktop StartupTask manifest element](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-desktop-startuptask).

Sign every public EXE, DLL, and installer using one consistent publisher identity, SHA-256, and trusted timestamping. Store distribution is the simplest way to avoid download SmartScreen warnings because Microsoft re-signs Store packages. For non-Store distribution, use an eligible trusted signing service or an OV certificate; new publishers can still encounter reputation warnings. Sources: [SmartScreen reputation](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation), [distribution choices](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/choose-distribution-path), and [MSIX signing](https://learn.microsoft.com/en-us/windows/msix/package/sign-msix-package-guide).

Because the owner is in Brazil, verify signing-service eligibility at release time. If individual cloud signing is unavailable in the region, prefer Microsoft Store signing for public distribution or a CA-issued OV certificate. Personal local builds need no public-signing expenditure.

Do not build a custom network updater in v1. Use Store/App Installer or manual signed releases. Stable updates are notify-then-install, retain N−1, and require a health check after first launch. For direct MSIX preview distribution, configure and test the App Installer downgrade route (`ForceUpdateFromAnyVersion`) using the same package identity, and prove that settings schemas work in both directions. If Store policy does not permit an arbitrary downgrade, stop rollout and publish a signed N+1 emergency roll-forward instead. Merely retaining N−1 does not count as a tested rollback. Failed migrations or health checks restore last-known-good settings and present recovery instructions.

## 14. Release operations

### Channels

1. **Developer:** local unsigned/self-signed builds on controlled machines.
2. **Canary:** signed owner/internal builds after every accepted change.
3. **Preview:** signed, explicit opt-in package.
4. **Stable:** promoted only after G7.

If public telemetry is not implemented, rollout is manual: preview for at least seven days, then stable. If consented coarse health telemetry is later added, stage 5% → 25% → 100% with minimum observation windows of 48 hours, 72 hours, and seven days.

Pause or roll back for:

- Any Explorer crash attributable to Ember Start.
- Login impairment or missing native recovery path.
- Wrong or unrecoverable power action.
- Lost settings or pins without recovery.
- Signature/update validation failure.
- Privacy violation.
- Crash-free rate below 99.9% in a measured beta.
- A critical accessibility regression.

Validate the supported stable build after every monthly Windows cumulative update before enabling any future taskbar feature on that build.

## 15. Risk register

| Risk | Likelihood | Impact | Mitigation and trigger |
|---|---|---:|---|
| Windows update changes Start/focus behavior | High | High | Explorer coexistence, native fallback, monthly canary matrix; any native Start lockout pauses release |
| Bare Windows key cannot be intercepted safely | High | Medium | Ship a configurable supported chord and stable command contracts; no hook exists in the approved v1 gates; preserve `Ctrl+Esc`; prefer upstream RetroBar integration; reject Start-replacement claim if verified RetroBar click cannot ship |
| Mixed-DPI menu appears on wrong monitor or offset | High | High | Per-Monitor v2 manifest, physical/effective coordinate tests, negative-coordinate fixtures, mandatory hardware matrix |
| Focus is stolen or menu immediately closes | Medium | High | Explicit focus state machine, foreground event tests, 1,000-cycle gate, Explorer-restart and fullscreen cases |
| Packaged apps or Settings do not enumerate/launch | Medium | High | Shell AppsFolder vertical slice before MVP; ≥99% gate on real catalog |
| Icon extraction hangs or leaks | Medium | Medium | Bound UI waits and queues, open a circuit on a wedged STA worker, use a bounded cache, and isolate a worker process only if profiling proves necessary; do not claim in-flight COM cancellation |
| RetroBar update breaks click integration | Medium | Medium | Versioned external command, no dependency on internals, supported hotkey fallback |
| Antivirus/SmartScreen reduces trust | Medium | High for public release | Consistent signing, SBOM, official distribution, no injection or self-modifying code |
| Historical branding infringes IP | Medium | High | Original assets, neutral name, non-affiliation notice, license inventory, legal review before public launch |
| Scope expands into full shell too early | High | High | Enforce gates G7–G9 and separate taskbar process/backlog |
| MSIX restricts needed behavior | Medium | Medium | Packaging spike before beta; documented signed WiX fallback |
| Shutdown/restart action is triggered accidentally | Low | Critical | Separated UI, explicit labels, confirmation default, automated and usability tests |
| Logs expose personal data | Low | High | Redaction tests, query/path exclusion, bounded local-only diagnostics |

## 16. Architecture decision records required before implementation

- **ADR-001: WPF on .NET 10 LTS.** Record the comparison spike and why WPF wins this specific shell surface.
- **ADR-002: Explorer coexistence and Start-only v1.** Record prohibited injection/system modification techniques.
- **ADR-003: Shell AppsFolder as catalog authority.** Record identity, icon, deduplication, and activation rules.
- **ADR-004: Local-only, plugin-free MVP.** Record privacy, logging, search providers, and absence of network/update code.
- **ADR-005: Invocation contract.** Define `--toggle`, current-user/current-session IPC, configurable supported hotkey, `Ctrl+Esc` native fallback, and RetroBar integration order.
- **ADR-006: Packaging and stable activation choice.** Complete during Phase 1 after comparing full-trust MSIX with the fixed unpackaged per-user launcher; record the selected version-independent route and downgrade/roll-forward policy. If unpackaged wins, WiX is a later packaging implementation for that proven route, not a third Phase 1 activation candidate.
- **ADR-007: Optional taskbar architecture.** Create only after G7; decide ManagedShell dependency and recovery design.

## 17. Initial implementation backlog

### P0 — Feasibility and skeleton

- `ES-001` Create separate repository, solution, pinned SDK, analyzers, CI, and license.
- `ES-002` Add `asInvoker` and Per-Monitor v2 manifests.
- `ES-003` Implement single-instance coordinator and current-user named-pipe `--toggle`.
- `ES-004` Build borderless Start window with deterministic focus/dismiss state machine.
- `ES-005` Implement monitor selection, work-area anchoring, DPI changes, and negative coordinates.
- `ES-006` Enumerate AppsFolder/Programs/CommonPrograms and normalize identities.
- `ES-007` Extract/cache icons and launch Win32/packaged/Settings entries.
- `ES-008` Implement a configurable supported chord (initial candidate `Ctrl+Alt+Space`), nonfatal registration failure, and `Ctrl+Esc` native Start fallback.
- `ES-009` Create Ember token dictionaries and fake-data home/search prototypes.
- `ES-010` Build unit fixtures for geometry, catalog, search, settings, and IPC.

### P0 — MVP behavior

- `ES-011` Pins, reorder, missing-app handling, and atomic persistence.
- `ES-012` Local launch recency/frequency with bounded retention.
- `ES-013` All Apps hierarchy and keyboard navigation.
- `ES-014` Async Apps/Settings/Places search and ranking.
- `ES-015` Common places and session/power action adapters.
- `ES-016` Settings window, startup, motion, density, export/reset, diagnostics.
- `ES-017` UI Automation peers, Narrator flows, contrast themes, text scale, reduced motion.
- `ES-018` Crash-loop recovery, last-known-good configuration, bounded redacted logs.

### P1 — Hardening and release

- `ES-019` UI Automation suite and Accessibility Insights checklist.
- `ES-020` Mixed-DPI hardware matrix runner/checklist.
- `ES-021` Fuzz/property tests for IPC, settings, search, and shortcuts.
- `ES-022` Performance benchmarks and 24-hour soak harness.
- `ES-023` Final packaging implementation, SBOM inputs, and 100-cycle downgrade/roll-forward qualification. The initial MSIX-versus-fixed-unpackaged activation comparison and ADR-006 belong to Phase 1/P0; WiX may implement the selected unpackaged route later.
- `ES-024` SBOM, dependency/license scanning, Authenticode verification, checksums.
- `ES-025` Recovery command and 100-cycle install/update/rollback/uninstall test.
- `ES-026` RetroBar configurable-command proposal or documented integration adapter.

### P2 — Post-v1

- Windows Search-backed local file provider.
- Optional additional original themes.
- ARM64 build and full compatibility matrix.
- Taskbar feasibility spike, ManagedShell evaluation, and ADR-007.

## 18. Repository and CI policy

- Protect the main branch; all changes arrive through reviewed pull requests.
- Require formatting, analyzers with warnings as errors, unit tests, and deterministic build before merge.
- Run Windows integration/UI tests on a dedicated Windows runner, not only a generic hosted job.
- Pin SDK and package versions centrally; automate dependency update proposals but never auto-merge them.
- Generate an SBOM, third-party notice report, hashes, and signature verification for release candidates.
- Enable secret scanning, dependency vulnerability scanning, license checks, Defender scan, and static analysis.
- Require a test or explicit rationale for every bug fix.
- Keep platform APIs behind interfaces so Core tests run without Windows COM/Win32.
- Keep the menu and future taskbar in separate processes and packages or independently recoverable features.

## 19. Definition of Done for Start-menu v1

The Start-menu project is complete only when:

- Gates G0 through G7 have written passing evidence.
- The owner can complete all five journeys on either monitor with pointer and keyboard.
- Apps/Settings/Places catalog meets the G3 completeness threshold.
- The full supported display and Windows matrix passes.
- All user-visible controls expose correct UI Automation data and no critical accessibility finding remains.
- Performance stays within the stated budgets, including the 24-hour soak.
- The application never requires runtime administrator privilege and never modifies a Windows system file or another process.
- Killing the process immediately returns the user to a usable native Windows state.
- Startup is user-controlled and cleanly removed on uninstall.
- Install, update, rollback, and uninstall pass 100 consecutive automated/assisted cycles.
- Settings and pins survive normal upgrades and recover from deliberate corruption.
- All distributed assets and dependencies have documented licenses.
- Public binaries, if any, are signed and accompanied by hashes, SBOM, privacy notice, known limitations, and recovery instructions.
- RetroBar remains available as the taskbar and is not replaced by an unqualified prototype.

## 20. Staffing and practical budget

Minimum practical council for a serious release:

| Role | Typical allocation during Start v1 |
|---|---:|
| Senior .NET/Windows engineer | 1.0 FTE for 8–12 weeks |
| Product/visual designer | 0.2 FTE, concentrated in weeks 1–4 |
| QA/accessibility engineer | 0.25–0.5 FTE from week 3 onward |
| Security/release engineer | 0.1–0.2 FTE, concentrated at G4–G6 |
| Owner/product decision maker | Gate reviews and daily dogfooding |

For a personal-only build, software tooling can be free: Visual Studio Community or compatible tooling, .NET SDK, GitHub, and local/Hyper-V test VMs. Public distribution may add certificate or store-account costs. Hardware coverage can begin with the owner PC and VMs, but stable public claims require at least one additional physical system because mixed-DPI, graphics, sleep, docking, and fullscreen behavior cannot be fully validated in a VM.

## 21. Decisions ratified at Gate G0

On 2026-09-02 the owner approved:

1. **Distribution:** public open-source development; personal/developer binaries until G6.
2. **License:** Apache-2.0.
3. **Visual direction:** Ember Fusion for Phase 1, compared with Ember Classic at G2.
4. **Scope:** Start-only v1 while retaining RetroBar and Explorer.
5. **Invocation fallback:** configurable supported chord, initial candidate `Ctrl+Alt+Space`, plus stable command forms; preserve `Ctrl+Esc` for native Start.
6. **Name:** Ember Start.
7. **Recent apps:** local tracking only for launches performed through Ember Start.
8. **Power confirmations:** restart and shutdown confirmations enabled by default.

The ratified decision record is [the G0 charter](decisions/GATE_G0_CHARTER.md). G0 authorizes the non-release Phase 1 feasibility implementation; later gates continue to control binaries and product claims.

## 22. Recommended immediate next step

Do not start with the taskbar or system-wide theming. Create the clean Ember Start repository and run the one-week Phase 1 feasibility sprint. Its first demonstrator should do exactly four things:

1. Open a black/orange WPF menu on the correct monitor through a configurable supported chord and `--toggle`.
2. Remain sharp and correctly anchored across the owner's two scale factors.
3. Enumerate and launch both a traditional desktop app and a packaged Windows app.
4. Exit or crash without affecting Explorer, RetroBar, or native Start.

If that demonstrator passes G1a, the project has retired its highest architectural risks before investing in the full visual design and feature set. It remains a hotkey-launcher preview until G1b proves the RetroBar Start-button route.

