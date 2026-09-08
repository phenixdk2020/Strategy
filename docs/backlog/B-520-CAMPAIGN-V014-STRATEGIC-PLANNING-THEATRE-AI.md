# B-520–B-529 — Campaign v00.00.14 Strategic Planning, Theatre AI & War Direction

## Status

**Target version:** `v00.00.14`  
**State:** PLANLAGT  
**Scope:** national strategy, theatres, operational planning and strategic execution.

## B-520 — National war aims and strategic posture
Represent national objectives, protected areas, acceptable losses, diplomatic constraints and offensive/defensive posture.

Acceptance: plans reference explicit war aims; AI does not invent hidden victory conditions.

## B-521 — Theatre definition and command scope
Create named theatres with geographic bounds, assigned HQ/commander, objectives and force responsibility.

Acceptance: formations belong to at most one primary theatre at a time; reassignment is logged and can require command/transport time.

## B-522 — Strategic objective scoring
Score cities, rail junctions, bridges, ports, depots, fortifications, enemy formations and terrain corridors.

Acceptance: score factors include military value, logistics, political value, risk, distance and intelligence confidence; player priorities can override weights.

## B-523 — Force allocation between theatres
Allocate divisions/brigades, artillery, cavalry, reserves and logistics according to threat and objectives.

Acceptance: no duplicated formations; allocation accounts for real travel/rail capacity and current commitments.

## B-524 — Operational course-of-action generation
Generate candidate plans such as hold, delay, concentrate, reinforce, flank, seize crossing, relieve fortress, raid logistics or withdraw.

Acceptance: each COA includes objectives, forces, route concept, logistics estimate, ETA and major risks.

## B-525 — Logistics-aware plan validation
Before approval, plans check supply sources, route capacity, rail/sea transport, depots, forage/food and transport assets.

Acceptance: unsupported plan is flagged, modified or rejected rather than receiving free supply.

## B-526 — Strategic reserve management
Maintain national/theatre reserves and release them based on explicit triggers or player orders.

Acceptance: reserve remains a real formation/location; commitment requires actual movement and cannot teleport to crisis points.

## B-527 — Contingency and branch plans
Plans can contain conditional branches for enemy movement, failed crossing, rail disruption, loss of node, weather or reinforcement arrival.

Acceptance: trigger uses known/observable campaign state; branch changes are audit-logged.

## B-528 — Reassessment and replanning cycle
Theatre AI periodically reassesses intelligence, losses, supply, objectives, fatigue and enemy activity.

Acceptance: prevents per-frame oscillation through planning cadence/hysteresis; plan changes show reason and expected consequence.

## B-529 — Theatre command UI and QA
Map/UI shows theatre boundaries, objectives, active plan, force allocation, reserve, supply concerns and decision history.

Acceptance: deterministic QA covers two theatres, competing objectives, bad intelligence, disrupted rail, reserve commitment, retreat/replan, save/load and no tactical hidden-state access.