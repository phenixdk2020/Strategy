# B-510–B-519 — Campaign v00.00.14 Military Build-up, Recruitment & Mobilization AI

## Status

**Target version:** `v00.00.14`  
**State:** PLANLAGT  
**Scope:** delegated force development and mobilization.

## B-510 — Force-structure target model
War Ministry can define target composition by infantry, cavalry, artillery, engineers, trains/support and reserves.

Acceptance: targets are goals, not free units; UI shows current vs target and shortages.

## B-511 — Manpower, reserve and recruitment AI
Manage recruitment/mobilization from real manpower/reserve pools.

Acceptance: AUTO respects reserve floors and recruitment policy; mobilization takes campaign time; no negative manpower.

## B-512 — Replacement allocation AI
Prioritize replacements among depleted formations based on theatre priority, readiness, distance, equipment availability and player rules.

Acceptance: replacement flow is explainable and uses real manpower/equipment stock.

## B-513 — Equipment procurement AI
Order small arms, ammunition, artillery, wagons, horses and other equipment based on force targets and losses.

Acceptance: procurement uses money, industry, imports and production lead time; no instant equipment creation.

## B-514 — Artillery and draught-team planning
AI plans gun types/numbers together with required horse teams, caissons and ammunition support.

Acceptance: artillery expansion cannot ignore horse/transport requirements; resulting mobility bottlenecks are visible.

## B-515 — Military transport and rolling-stock demand planning
War Ministry/Quartermaster estimate military demand for wagons, horses, locomotives, passenger cars, freight cars and military flatcars.

Acceptance: transport demand competes with civil/economic use; requested assets go through real procurement/production/repair.

## B-516 — Training and readiness pipeline
New/rebuilt formations pass through organization, training, equipment issue and readiness stages.

Acceptance: newly raised units cannot instantly reach veteran readiness; training consumes campaign time/resources.

## B-517 — Barracks, depots and military-facility planning
Military build-up can request barracks, depots, magazines, training grounds and fortifications.

Acceptance: requests are passed to construction/Public Works through normal cabinet/budget arbitration.

## B-518 — Mobilization execution and transport integration
Mobilized units receive assembly/deployment plans using march, forced march, rail or sea where valid.

Acceptance: rail deployment requires available rolling stock/capacity; march ETA uses B-400 mobility; bottlenecks delay assembly realistically.

## B-519 — Military build-up dashboard and QA
Show force targets, manpower, equipment gaps, training queues, mobilization state and procurement dependencies.

Acceptance: deterministic QA covers manpower shortage, artillery-without-horses, rolling-stock shortage, budget rejection, partial mobilization, save/load and player override.