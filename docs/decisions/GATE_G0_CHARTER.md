# Gate G0 — Product and Legal Charter

**Prepared:** 2026-09-01

**Status:** **CONDITIONAL GO — awaiting owner ratification**

**Permitted work:** Planning and architecture records only

**Not permitted by this gate:** Public binaries, stable-product claims, copied historical assets, Explorer replacement, or system modification

## Recommended decisions for owner ratification

| # | Decision | Recommended selection | Why |
|---:|---|---|---|
| 1 | Distribution | Public-visible, owner-only planning repository until the license decision; no external contributions or binaries | The current no-license repository is visible source under default copyright, not an open-source release. Phase 1 code begins only after G0. |
| 2 | Source license | Council recommendation: Apache-2.0 | Provides a permissive license with an explicit patent grant and aligns with RetroBar, ManagedShell, and Cairo. No license has been applied yet. |
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

G0 becomes **PASS** only after the owner records acceptance of every decision above. If an open-source path is chosen, a `LICENSE` file matching the selected license must be committed before Phase 1 code. If the owner instead chooses all-rights-reserved development, that status must be explicit and contributions and public binaries remain blocked.

Owner ratification:

- [ ] Distribution model accepted
- [ ] License selected
- [ ] Visual direction accepted
- [ ] Start-only/Explorer/RetroBar scope accepted
- [ ] Activation and native fallback accepted
- [ ] Ember Start working name accepted
- [ ] Local-only recency accepted
- [ ] Power confirmations accepted
- [ ] Non-negotiable constraints accepted

**Owner:** _pending_

**Decision date:** _pending_

**Gate result:** _pending_
