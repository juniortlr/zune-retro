# DG-1 GO / DG-2 / DG-3 Evidence Packet — EmberStart Phase 1
Commit: 2859321 (codex/phase-1-foundation) | Runtime: thinkingmachines/inkling-small:free (openrouter)

## Gates
- DG-1 GO: activation kernel passes (EDD-01 adversary + EDD-02 110-batch + elevated-parent)
- DG-2: visibility/focus/monitor (EDD-06/07 verified vs spec)
- DG-3 / G1b: entry point verified; EDD-09 installed RetroBar invitation PASS (manual, user-verified)

## Test-only files (no src/ edits)
- tests/.../Instance/Edd01AdversaryCorpusTests.cs
- tests/.../Instance/ColdStartOrderingHarnessTests.cs
- tests/.../Security/ElevatedParentRuntimeIntegrityProof.cs

## Source untouched
- src/EmberStart.Windows/Security/ProcessIntegrityGuard.cs (line 52-54 elevated rejection intact)
- No ACL/registry/startup/production modifications

## EDD-09 verification (G1b)
- RetroBar Start button → Ember Start via stable entry point
- Correct monitor + validated anchor rectangle
- Native Start fallback (Ctrl+Esc) preserved
- No injection/hooks used
