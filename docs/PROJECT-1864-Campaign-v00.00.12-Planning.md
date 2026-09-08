# PROJECT 1864 — Campaign v00.00.12 Planning

## Status

- **Version:** `v00.00.12` PLANNED
- **Active campaign version remains:** `v00.00.11 WORK`
- **Entry condition:** B-369 promotion gate must pass before v00.00.12 becomes active.
- **Planned backlog:** B-370–B-409
- **Tactical isolation:** no tactical Officer AI, battlefield navigation or tactical formation steering changes belong in this package.

## Milestone purpose

v00.00.11 establishes the 50-node campaign map, usability/system foundations, construction and living-world direction. v00.00.12 is planned as the first deeper strategic-world milestone: locations become developable places, infrastructure becomes persistent state, transport becomes capacity-limited, logistics becomes visible and explainable, society/events begin reacting to the war, and strategic movement gets a real mobility model instead of one fixed map speed.

The campaign should continue to feel like a grand-strategy map rather than a city-builder. Detailed animation exists to communicate state; authoritative simulation remains in campaign data.

## Planned work packages

### B-370–B-379 — Infrastructure & Settlements

- location development slots,
- facility catalog/levels,
- construction queue,
- repair/reconstruction,
- bridges/crossings/pontoons,
- telegraph network,
- depot/warehouse expansion,
- strategic fortification projects,
- settlement growth/damage/occupation visuals,
- infrastructure planner UI/QA.

Source: `docs/backlog/B-370-CAMPAIGN-V012-INFRASTRUCTURE-SETTLEMENTS.md`

### B-380–B-389 — Logistics & Transport

- limited transport asset pools,
- rolling stock: locomotives/passenger/freight/military cars,
- troop-train lifecycle with full route-following visual,
- rail loading/capacity,
- wagon convoys,
- sea transport,
- depot-to-depot stock transfer,
- supply priorities,
- congestion,
- interdiction/sabotage,
- logistics efficiency/loss hooks,
- logistics planner/overlay/QA.

Source: `docs/backlog/B-380-CAMPAIGN-V012-LOGISTICS-TRANSPORT.md`

### B-390–B-399 — Society & Events checkpoint

- mobilization/reserves,
- civilian displacement,
- occupation/requisition,
- public order,
- war weariness/national morale,
- seasonal/weather/disease hooks,
- newspaper/event feed,
- generic historical event framework,
- strategic AI priority hooks,
- B-399 society/events integration checkpoint.

Source: `docs/backlog/B-390-CAMPAIGN-V012-WORLD-EVENTS-QA.md`

### B-400–B-409 — Formation Mobility & March

- data-driven mobility profiles for foot/mounted/horse-drawn/wagon elements,
- formation speed set by slowest required attached component,
- horses/draught teams/artillery mobility,
- road/terrain/weather modifiers,
- column length and passage congestion,
- march/rest schedule and daily distance,
- forced march and straggling hooks,
- detach/rejoin to remove mobility bottlenecks,
- explainable route ETA,
- B-409 final v00.00.12 acceptance gate.

Source: `docs/backlog/B-400-CAMPAIGN-V012-FORMATION-MOBILITY-MARCH.md`

## Core mobility rule

A strategic formation does not receive an arbitrary fixed map speed. Its effective speed is derived from the actual elements that must move together. As a baseline:

`FormationEffectiveSpeed = min(EffectiveSpeed(required attached components))`

The bottleneck is evaluated after movement mode, horses, artillery/transport burden, route, terrain, weather, fatigue and relevant delays. A detached slow train no longer limits the main body if it receives a valid separate movement state. Rail or sea transit temporarily uses the relevant transport lifecycle instead of normal land-march speed.

## Promotion principle

Do not activate v00.00.12 merely because planning files exist. v00.00.11 must first reach its documented B-369 promotion gate. When v00.00.12 becomes active, individual tasks may be implemented in waves and remain `IMPLEMENTERET / AFVENTER QA` until batch Unity runtime acceptance.

B-399 is now an intermediate society/events checkpoint. **B-409 is the planned final v00.00.12 promotion gate.**

## Living-world relationship

B-360–B-369 remains the visual/state-driven living-world foundation. v00.00.12 builds gameplay behind those visuals. Examples:

- a troop train corresponds to a real formation in rail transit and follows its route visually from station to station,
- rolling stock is limited, so rail use competes for locomotives and cars,
- a wagon can correspond to an actual convoy,
- construction workers can correspond to a real queued project,
- damaged bridges/rail can affect route availability,
- a marching formation's visual progress follows its calculated movement state and ETA,
- occupation can change flags/activity because controller state actually changed,
- refugees/civilian activity can represent aggregated campaign state rather than decorative fiction.
