# Gate G0 — Product and Legal Charter

**Prepared:** 2026-09-01

**Status:** **PASS — owner ratified the recommended decisions**

**Permitted work:** Phase 1 feasibility implementation and evidence collection

**Not permitted by this gate:** Public binaries, stable-product claims, copied historical assets, Explorer replacement, or system modification

## Ratified decisions

| # | Decision | Recommended selection | Why |
|---:|---|---|---|
| 1 | Distribution | Public open-source development; personal/developer binaries until G6 | Allows transparent development without presenting an unsigned feasibility build as a release. |
| 2 | Source license | Apache-2.0 | Provides a permissive license with an explicit patent grant and aligns with RetroBar, ManagedShell, and Cairo. |
| 3 | Visual direction | Ember Fusion for Phase 1; compare Ember Classic at G2 | Keeps the feasibility UI legible and original while preserving black/orange character without implying copied historical fidelity. |
| 4 | Scope | Start-only v1; retain RetroBar and Explorer | Preserves a safe native recovery path and prevents taskbar scope from bypassing G7/G8. |
| 5 | Invocation fallback | Configurable supported chord, initial candidate `Ctrl+Alt+Space`, plus `--toggle`; `Ctrl+Esc` opens native Start | `Win+Z` is owned by Windows 11 Snap Layouts. Runtime registration may still fail, so command and native fallbacks remain mandatory. |
| 6 | Working name | Ember Start | Neutral and does not imply Microsoft affiliation. |
| 7 | Recent apps | Track only launches performed through Ember Start, locally and with bounded retention | Gives useful ordering without reading document or system-wide activity history. |
| 8 | Power behavior | Keep restart/shutdown confirmations enabled by default | Reduces the highest-impact interaction error. Phase 1 uses inert prototypes only. |

This charter records eight decisions. License is separate from distribution because a public repository and an open-source release are not the same decision.

## Non-negotiable constraints

- The manifest is per-user, `asInvoker`, and `uiAccess=false`. At startup, the process inspects its token and refuses to become the resident process if integrity exceeds medium; an elevated parent must not create an elevated Ember resident.
- Explorer remains the Windows shell and recovery path.
- RetroBar remains the production taskbar through G7; taskbar experiments require G8.
- No injection, private-symbol patching, system-file replacement, service, driver, or runtime elevation.
- No universal reskinning claim for third-party windows.
- All project artwork is original or separately licensed and recorded.
- The product is fully functional offline and has no telemetry, web search, arbitrary command execution, or plug-in loading in v1.
- Startup is opt-in, per-user, visible to Windows Startup Apps, and starts the menu hidden.
- Failure of Ember Start must expose native recovery; it must never impair login, Explorer, or Windows Start.

## Formal pass rule

G0 passed when the owner approved every decision above on 2026-09-02 and the Apache-2.0 `LICENSE` was added. This approval authorizes Phase 1 feasibility code, not public binaries, a stable-product claim, G1a/G1b, or the final G2 design.

Owner ratification:

- [x] Distribution model accepted
- [x] License selected
- [x] Visual direction accepted
- [x] Start-only/Explorer/RetroBar scope accepted
- [x] Activation and native fallback accepted
- [x] Ember Start working name accepted
- [x] Local-only recency accepted
- [x] Power confirmations accepted
- [x] Non-negotiable constraints accepted

**Owner:** Project owner; approval recorded in the Codex project thread

**Decision date:** 2026-09-02

**Gate result:** **PASS**
