# Visual and Accessibility Baseline

**Status:** Provisional Phase 1 baseline; not owner-approved design

**Canonical for:** Tokens, control states, layout units, and the Phase 1 accessibility protocol

**Date:** 2026-09-01

## Concepts

- **Ember Fusion** is the restrained feasibility baseline: solid dark surfaces, concise typography, a bright orange accent, minimal bevel, and original icons.
- **Ember Classic** is the denser, more beveled alternative to compare at G2.

The names are internal design concepts. “Zune-inspired” may describe the historical influence, but neither concept copies Microsoft bitmaps, logos, sounds, wordmarks, or exact historical assets.

## Canonical tokens

All dimensions are WPF device-independent pixels (DIPs). Colors are original project values.

| Token | Value | Required role |
|---|---:|---|
| `Canvas` | `#101010` | Main window background |
| `Surface` | `#1B1A18` | Panels and default controls |
| `SurfaceRaised` | `#25221E` | One part of hover/raised state; never the only cue |
| `Accent` | `#F5841F` | Accent rail, glyph, and principal emphasis |
| `OnAccent` | `#101010` | Text/icons placed on Accent |
| `Selection` | `#AD4D00` | Selected-row fill |
| `TextPrimary` | `#F5F5F5` | Main text |
| `TextSecondary` | `#BDBDBD` | Supporting text |
| `Focus` | `#FFD166` | Keyboard focus ring |
| `Divider` | `#3A3834` | Decorative separators only |
| `OutlineStrong` | `#6F6B64` | Essential control boundaries |

Measured reference contrast ratios:

- TextPrimary/Canvas: 17.45:1.
- TextSecondary/Canvas: 10.13:1.
- Accent/Canvas and OnAccent/Accent: 7.43:1.
- Selection/Canvas: 3.48:1; TextPrimary/Selection: 5.02:1.
- White on Accent is only about 2.35:1 and is prohibited.
- Focus directly against Accent is only about 1.78:1. When an accented control has focus, draw a 2-DIP Focus outer ring separated from Accent by a 1-DIP Canvas-colored gap. A focus ring must never rely on color adjacency that falls below 3:1.
- Divider cannot communicate essential state or boundaries; use OutlineStrong for those roles.

Typography uses Segoe UI Variable: 14 DIP-equivalent body, 12 secondary, and 20 semibold heading. Use a 4/8-DIP spacing grid, 2–4-DIP radii, 44-DIP minimum rows/targets, and 48 DIPs in touch density.

## Control state matrix

Every interactive control must implement and capture these states with more than a subtle background-only distinction:

| State | Visual and programmatic requirement |
|---|---|
| Default | Surface/Canvas with readable label and correct UIA enabled state |
| Hover | SurfaceRaised plus OutlineStrong, an Accent rail/glyph, or another ≥3:1 non-text cue |
| Pressed | Persistent ≥3:1 boundary/cue and a visibly depressed fill; no content shift that clips focus |
| Selected | Selection fill, Accent rail or glyph, TextPrimary, and UIA selected state |
| Focus | 2-DIP Focus ring; use the 1-DIP dark separation rule on Accent/Selection; focus is never hover-only |
| Disabled | Non-actionable, removed from action patterns, and still readable; opacity alone cannot erase the label |
| Contrast theme | Replace project colors with Windows system resources while preserving roles, boundary, focus, and selection |

The Phase 1 state packet covers Search, one application row, a principal button specimen, and a context-menu specimen. Power controls are omitted from the interactive spike rather than presented as enabled controls that do nothing.

## Layout contract

- Initial window: 704 × 640 DIPs.
- Clamp to the active work area with an 8-DIP margin.
- Below 640 available DIPs, use a single-column layout.
- Use 16-DIP outer padding, a 40-DIP minimum/Auto heading, a 44-DIP minimum/Auto Search edit, a flexible scrollable body, and a 48-DIP minimum/Auto footer in the product design.
- Phase 1 may omit the footer because real power and an in-product native-Start command are not part of the spike. `Ctrl+Esc` is the tested native fallback.
- At high text scale or on a short work area, heading, search, and footer measure to content; the body takes remaining height and scrolls.
- The first and last realized or virtualized items must expose their complete label, selection cue, and focus ring.
- At 200% Windows text size, scrolling is permitted; clipping, overlap, truncated actionable labels, or clipped focus are not.

## Minimal Phase 1 search

Phase 1 implements a deterministic, case-insensitive installed-application name filter:

1. exact prefix;
2. token prefix;
3. ordinal substring;
4. display name and stable identity as tie-breakers.

An empty query displays the first deterministic catalog page. A nonempty query with no match displays a named “No apps found” status. Search waits 150 ms after the last edit, applies one immutable snapshot, and raises one settled result-count announcement; provider-by-provider chatter is prohibited.

## Phase 1 UI Automation contract

| Element | UIA control type/pattern | Required accessible data |
|---|---|---|
| Menu surface | Window | Name `Ember Start`; not modal; no focus trap |
| Search | Edit / Value | Name `Search installed apps`; current value |
| Results | List / Selection | Name `Installed apps`; item count available through children |
| Application | ListItem / Invoke, SelectionItem | Localized app name; selected/enabled state |
| Empty/loading state | Status / LiveRegion | `No apps found` or `Loading installed apps` |
| Settled count | Status / LiveRegion | Announce once: `<n> apps found` |

Tab order is Search → Results. Down from Search moves to the first result; arrows move in the list; Enter invokes; Escape dismisses. After dismissal, focus returns to the previously foreground window only when it remains valid and no other application has since become foreground.

## Reproducible Narrator journey

With Narrator running and the menu hidden:

1. Invoke the supported hotkey. Expected: “Ember Start” and Search are announced; focus is in Search.
2. Type the fixed fixture query. Expected: the query is echoed according to Narrator settings, then exactly one settled `<n> apps found` announcement.
3. Press Down. Expected: the first fixed fixture application name and its selected state are announced.
4. Press Enter on the non-destructive fixture. Expected: one invocation is recorded and the menu dismisses.
5. Reopen and press Escape. Expected: the menu dismisses and focus returns according to the focus contract.
6. Press `Ctrl+Esc`. Expected: native Windows Start opens independently of Ember Start.

Accessibility Insights automated inspection must report zero unnamed actionable controls and zero critical findings. The evidence packet records the UIA tree, Narrator transcript/checklist, and focus HWNDs before, during, and after the journey.

## Display, locale, motion, and touch evidence

- Capture default/hover/pressed/selected/focus/disabled/contrast states at 100%, 150%, and 200% display scale.
- Separately run Windows 200% text size at each layout mode; display scale and text size are not interchangeable tests.
- Use English, Portuguese (Brazil), and a fixed long German-label fixture.
- Verify high-contrast/contrast-theme resources, plus reduced motion with zero entrance translation or fade.
- G1a includes a tap-to-focus/invoke and touch-scroll smoke test on 44-DIP targets. Complete touch journeys are required at G2 for the prototype and G4 for the internal alpha.

## G2 comparison packet

Compare Ember Classic and Ember Fusion using identical content, monitor, DPI, window bounds, and control states. Include state-matrix captures, token contrast results, asset provenance, long-text/contrast examples, keyboard/Narrator results, and a dated owner selection. Phase 1 implementation does not count as G2 approval.
