# B-500–B-509 — Campaign v00.00.14 Economy, Industry & Development AI

## Status

**Target version:** `v00.00.14`  
**State:** PLANLAGT  
**Scope:** delegated national development, infrastructure and economy.

## B-500 — Finance Ministry AI
Manage treasury reserve, spending ceilings, borrowing hooks and ministry budgets.

Acceptance: AI cannot spend unavailable money; every approval/rejection records budget impact and reason; player-set reserve floor is respected.

## B-501 — Public Works AI
Score roads, bridges, ports, telegraph, depots and other public infrastructure using military/economic/civil value.

Acceptance: projects use the same construction queues/resources/times as player projects; score factors are visible.

## B-502 — Railway and Transport Administration AI
Manage rail expansion, station capacity, rolling-stock procurement/repair, train allocation and network maintenance.

Acceptance: AI distinguishes infrastructure from rolling stock; finite locomotives/cars remain binding; military and freight demand compete for capacity.

## B-503 — Industry and Trade AI
Allocate industrial investment, military production, imports/exports and key raw materials.

Acceptance: production uses actual capacity/resources; imports require valid trade/diplomatic hooks; AI cannot conjure strategic materials.

## B-504 — Agriculture, forage and horse-economy AI
Manage farms, food/forage priorities and horse-breeding investment.

Acceptance: cavalry/artillery/transport horse demand is visible to the portfolio; shortages influence recommendations and cannot be ignored by AUTO.

## B-505 — Regional development scoring
Locations/regions get explainable development scores based on population/economy hooks, strategic position, resources, transport connectivity, damage and risk.

Acceptance: AI does not always overbuild the capital; player can weight military/civil/regional-balance objectives.

## B-506 — Infrastructure portfolio optimization
Choose among competing projects under money/material/worker/construction-slot limits.

Acceptance: project selection is deterministic for same state/seed; dependencies and opportunity cost are logged; locked player projects are preserved.

## B-507 — Maintenance and repair prioritization
AI balances new construction against maintenance, damaged bridges/rail/roads, ports, depots and facilities.

Acceptance: critical broken routes can outrank expansion; maintenance backlog is visible; neglected infrastructure has explicit consequences.

## B-508 — Economic emergency policies
Shortage/war-emergency hooks for rationing, military procurement priority, emergency imports and temporary budget shifts.

Acceptance: emergency policies have costs/tradeoffs and explicit start/end state; AI cannot silently enable them outside guardrails.

## B-509 — Development AI dashboard and QA
Show proposed/active projects, scores, budgets, blocked resources, regional investment and expected completion.

Acceptance: QA covers treasury shortage, rail bottleneck, horse shortage, repair-vs-new-build conflict, player lock/override, save/load and AUTO/ASSISTED behavior.