# Ember Start persistent work queue

Updated 2026-09-10. User authorized Hermes CLI workers through online OpenRouter free models to keep progressing toward project completion. Hourly Codex heartbeat: `ember-start-hermes-free-model-work`. This is supervised recurring work, not proof of completion or a release authorization.

## Resume protocol

Read git status and this queue before editing. Existing uncommitted R3b activation-fixture changes belong to the primary and must be preserved. Check `artifacts/hermes-online/*/*/run.json` and ticket locks for active jobs before dispatch. Use `tools/hermes/Invoke-HermesTicket.ps1` with a currently listed open-model `:free` ID. The runner checks live zero pricing, uses explicit OpenRouter, records hashes and keeps each run bounded. No local or paid fallback. No credentials or private evidence in prompts. Drafting workers have tools disabled and receive only checked-in self-contained prompts. Returned code is untrusted until reviewed/tested. A returned CLI process is not a completed ticket.

## Current assignments

| Ticket | Owner | State / acceptance |
|---|---|---|
| H1a manifest/performance validator | Hermes online | BLOCKED: OpenRouter credentials missing, CLI exit 1 on 2026-09-10; no inference or code generated. Attempt used nvidia/nemotron-3-ultra-550b-a55b:free, zero pricing verified. Prompt ready; retry after configuration. |
| H2 campaign adversarial oracles | Second Hermes online model | BLOCKED: same credentials issue, CLI exit 1; no inference. Attempt used nvidia/nemotron-3-super-120b-a12b:free, zero pricing verified. Synthetic edge-case prompt ready. |
| H1b activation campaign validator | Hermes after H1a/H2 review | Implement exact campaign counts, transitions, identities, ordering and included placement subset against a primary-reviewed schema. |
| H3 documentation references | Hermes draft / primary integration | Resolve packaging/readiness ADR numbering collision while preserving existing ADR-006 and historical evidence. |
| R3b cold-process contract/fixture | Primary | Partial untested fixture and project references exist from interrupted work. Finish contract, controlled process harness and tests before application startup changes. |
| R3b recovery integration | Primary after fixture | Bounded recovery, close secondary mutex before reacquisition, canonical newly-created ownership only; never replay after uncertain transmission. |
| Integrity, cancellation, admission fairness | Primary separate slices | Resolve each contract, reproduce failures and make small tested patches. |

## Following work

Follow docs/PROJECT_PLAN.md and current decision evidence for the Start-only v1 scope. G0 passed; DG-1/DG-2 remain ITERATE and G1a/G1b are not qualified. R1/R2/R3a are already complete. After dependencies pass, implement process/window pilot, missing Shell containment/launch fixtures, monitor/focus and accessibility qualification, stable route/packaging experiments, canonical 1000 activation cases (100 placement observations included), and separate 24-hour soak. Use exact current acceptance gates; do not equate tests or model output to owner gate approval. Defer post-v1 taskbar work. Never invent hardware observations.

## Persistent-run boundaries

Each heartbeat resumes one useful bounded slice, records tested results and next state, and stays quiet unless progress/failure/action matters. On free-tier limits or provider outages, retain the failure and wait for a later cycle instead of tight retry loops. Continue independent local work. No automated publication, push, paid fallback, deployment, installation, registry/startup/RetroBar changes or account creation. Ask only for actual blockers requiring owner input. Pause the heartbeat when all agreed gates are genuinely complete or the user cancels. Host availability, account quotas and physical qualification can block unattended progress.

Known credential blocker: Hermes reported no OpenRouter API key; neither its environment file nor current process contains the key entry. User was asked to configure it with `hermes model`, never paste it into chat. Until configuration changes, skip inference attempts and do independent local work; do not repeat the same notification each cycle. The two first manifests predate runner failure classification and say returned-needs-review, but exitCode=1 and response prove failure; this dated correction is authoritative.
