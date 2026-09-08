# PROJECT 1864 — Campaign v00.00.12 Planning

## Status

- **Version:** `v00.00.12` PLANNED
- **Active campaign version remains:** `v00.00.11 WORK`
- **Entry condition:** B-369 promotion gate must pass before v00.00.12 becomes active.
- **Planned backlog:** B-370–B-399
- **Tactical isolation:** no tactical Officer AI, battlefield navigation or tactical formation steering changes belong in this package.

## Milestone purpose

v00.00.11 establishes the 50-node campaign map, usability/system foundations, construction and living-world direction. v00.00.12 is planned as the first deeper strategic-world milestone: locations become developable places, infrastructure becomes persistent state, transport becomes capacity-limited, logistics becomes visible and explainable, and society/events begin reacting to the war.

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

- transport asset pools,
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

### B-390–B-399 — Society, Events & Final QA

- mobilization/reserves,
- civilian displacement,
- occupation/requisition,
- public order,
- war weariness/national morale,
- seasonal/weather/disease hooks,
- newspaper/event feed,
- generic historical event framework,
- strategic AI priority hooks,
- final v00.00.12 acceptance gate B-399.

Source: `docs/backlog/B-390-CAMPAIGN-V012-WORLD-EVENTS-QA.md`

## Promotion principle

Do not activate v00.00.12 merely because planning files exist. v00.00.11 must first reach its documented B-369 promotion gate. When v00.00.12 becomes active, individual tasks may be implemented in waves and remain `IMPLEMENTERET / AFVENTER QA` until batch Unity runtime acceptance.

## Living-world relationship

B-360–B-369 remains the visual/state-driven living-world foundation. v00.00.12 builds gameplay behind those visuals. Examples:

- a train can correspond to real rail transfer/capacity,
- a wagon can correspond to an actual convoy,
- construction workers can correspond to a real queued project,
- damaged bridges/rail can affect route availability,
- occupation can change flags/activity because controller state actually changed,
- refugees/civilian activity can represent aggregated campaign state rather than decorative fiction.
