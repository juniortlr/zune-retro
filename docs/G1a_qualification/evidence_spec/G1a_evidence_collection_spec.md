# G1a Evidence Collection — Reviewed 2026-09-05

Status: specification only. No qualification campaign is reported as executed.

The authoritative requirements are [Phase 1 feasibility](../../architecture/PHASE_1_FEASIBILITY_SPEC.md#reproducible-g1a-protocol). This document corrects the Hermes handoff's counts, source/build identity, and performance methodology.

## Storage and identity

Keep raw evidence under the ignored `artifacts/g1a/` directory. Publish only a reviewed aggregate summary under `docs/evidence/phase1/`. Raw TRX files and screenshots can contain account paths or private app data.

Record source commit, dirty/clean status, patch hash when dirty, SHA-256 hashes of tested binaries and fixtures, SDK/runtime versions, OS build, process integrity, and timestamp. A Git commit hash is not a binary hash. Do not assert that source is unchanged: bind results to the source actually built.

Record physical monitor count, signed bounds/work areas, display DPI, Windows text scale, and topology identifier. Negative-coordinate placement is a topology, not a third monitor.

## Activation cases

Use exactly 1,000 scored requests:

- 200 hotkey toggles: 100 hidden→visible and 100 visible→hidden.
- 200 CLI toggles with the same split.
- 200 sequential shows: 100 hidden→visible and 100 visible→visible.
- 200 sequential hides: 100 visible→hidden and 100 hidden→hidden.
- 200 concurrent IPC requests in 40 five-request batches: 20 show batches from hidden and 20 hide batches from visible.

Log preconditioning separately and exclude it from these counts. Include 50 placement cases per monitor within the 1,000; on each of two monitors, 25 hotkey and 25 CLI/integrated cases. Include a negative-coordinate topology.

Each row records case ID, category, batch/sequence, expected/observed state, matching request/result identity for IPC, timing, resident count, menu HWND count, selected monitor, expected/actual physical bounds, target/observed DPI, and outcome. Use bounded enums for failure reasons instead of free-text notes. Hotkeys have state observations, not fabricated IPC acknowledgments.

## Separate performance runs

Do not derive warm/cold reveal budgets from arbitrary activation or hide cases. Follow the canonical performance protocol: warm 20 discarded warm-ups then 200 recorded samples; cold-process five warm-ups then 50 fresh-process samples. Record all failures and missing samples. Do not purge the OS file cache.

Measure from received hotkey/validated command (warm) or external process creation (cold) to the first presented WPF frame using the specified render/QPC/harness synchronization. Record raw samples and calculate nearest-rank p95 with `ceil(0.95 * n)`, one-based. No post-hoc exclusions. Warm p95 ≤150 ms; cold-process p95 ≤1 s. A failed or missing sample invalidates the run rather than disappearing from the denominator.

## Complete qualification packet

The 1,000-case campaign is only one section. Include the prescribed mixed-DPI, text/locale, UIA/Narrator, contrast, reduced-motion, touch, classic/packaged launch, IPC abuse/identity, forced-kill recovery, privacy, and both packaging rehearsals. Record open defects and explicit not-run/skipped results.

G1b additionally needs the installed RetroBar route, version/configuration, stable entry point and upgrade evidence, anchor/edge, selected monitor, foreground transfer, and native fallback. A manual report without the associated environment/build/result record stays unverified.

The owner decides G1a and G1b separately after the complete evidence packet is reviewed. Neither a green unit suite nor a successful 1,000-case campaign alone is a G1a PASS.
