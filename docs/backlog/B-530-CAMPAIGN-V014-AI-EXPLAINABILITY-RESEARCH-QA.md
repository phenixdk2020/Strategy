# B-530–B-539 — Campaign v00.00.14 AI Explainability, Personalities, Historical Research & Final QA

## Status

**Target version:** `v00.00.14`  
**State:** PLANLAGT  
**Scope:** explainability, historical grounding, minister behavior and full-version acceptance.

## B-530 — Unified AI decision record
Every significant minister/staff AI action writes a structured record with actor, action, timestamp, inputs, positive/negative factors, constraints, score, budget/resource impact and result.

Acceptance: UI/debug can reconstruct why a decision happened; records survive save/load within retention policy.

## B-531 — AI recommendation explanation UI
Player can inspect `WHY`, expected benefits, risks, cost, ETA, alternatives and blocking constraints before approving advisory proposals.

Acceptance: no important recommendation is a black box; reasons use human-readable labels plus diagnostic values.

## B-532 — Confidence, uncertainty and intelligence-aware planning
AI outputs confidence when decisions depend on uncertain enemy/economic information.

Acceptance: AI cannot inspect hidden true enemy state to improve confidence; stale reports visibly reduce certainty.

## B-533 — Minister/staff personality profiles
Add bounded data-driven traits and preferences for office holders.

Acceptance: personality changes weighting/initiative/caution, not resource rules; same person behaves consistently under same state/seed.

## B-534 — Player doctrine/policy constraints and protected decisions
Central registry of player locks, minimum reserves, spending caps, protected theatres/assets and forbidden action classes.

Acceptance: all AUTO portfolios query this registry; violation becomes failed/escalated proposal with explicit reason.

## B-535 — Historical research/source workflow
Define project workflow for researching 1864-era ministries, government structures, railway administration, military organization, doctrine, weapons, industrial capacity, mobilization and infrastructure.

Acceptance: factual data entries can carry source/provenance notes, confidence, scenario date and `historical / estimated / QA-placeholder` status; unsourced QA data is clearly marked.

## B-536 — Historical plausibility validator
Automated/data-level checks for date-inappropriate technology, impossible office/nation combinations, invalid doctrine prerequisites and implausible scenario configuration.

Acceptance: validator reports warnings/errors without silently rewriting scenario data; alternate-history overrides are explicit.

## B-537 — No-cheat and fairness validator
Test AI access to money, manpower, transport, knowledge, construction, research and orders against player-equivalent rules.

Acceptance: detect negative pools, instant construction/research, free transport, hidden enemy knowledge and bypassed capacity gates.

## B-538 — Long-run deterministic AI simulation
Headless/synthetic campaign runs covering months of government AUTO management.

Acceptance: run fixed seeds; detect budget death spirals, project spam, oscillating mobilization, deadlocked ministries, impossible force build-up, runaway queues and save/load divergence.

## B-539 — Final Campaign v00.00.14 acceptance gate
B-539 is the planned final promotion gate.

Acceptance:
1. Unity compiler has 0 blocking errors for campaign build.
2. v00.00.13/B-469 geospatial/3D baseline remains intact.
3. All four delegation modes behave according to contract.
4. Ministry budgets/resources cannot double-spend or go negative.
5. Public Works/Transport AI uses real construction and finite capacity.
6. War Ministry AI uses real manpower/equipment/horse/rolling-stock constraints.
7. General Staff plans use real route ETA, supply and intelligence state.
8. Research respects historical/plausibility prerequisites and adoption lifecycle.
9. Doctrine changes preferences/capabilities without hidden combat cheats.
10. Player locks/guardrails cannot be silently overridden.
11. Significant AI decisions are explainable through decision records.
12. No-cheat validator passes blocking checks.
13. Deterministic long-run AUTO test has no blocking state corruption/deadlock.
14. Save/load produces equivalent government/research/AI state.
15. Tactical battlefield navigation/formation steering has not been silently changed by this milestone.
16. Deferred items are documented before promotion.

When B-539 passes, v00.00.14 can be promoted and the next campaign milestone can be created.