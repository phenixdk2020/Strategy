# PROJECT 1864 — Campaign v00.00.14 Design Supplement

## Milestone

`GOVERNMENT, RESEARCH, DOCTRINE & STRATEGIC AI`

This supplement records the approved design direction for the national decision layer planned after the 3D/geospatial foundation.

## Core design decisions

### Delegation instead of forced micromanagement
Every major national portfolio can be controlled through `MANUAL`, `ADVISORY`, `ASSISTED` or `AUTO`. Delegation is granular: the player may automate economy and logistics while personally controlling General Staff plans, or the reverse.

### AI uses player-equivalent rules
Minister/staff AI may not receive free money, manpower, equipment, horses, rolling stock, construction, research, transport capacity or hidden enemy intelligence. AUTO is convenience/delegation, not a difficulty cheat.

### Government is a set of semantic portfolios
Historical titles differ by nation and year, so engine roles are semantic portfolios and scenario content supplies period-appropriate office names and people.

Initial portfolios include War, General Staff, Quartermaster, Finance, Public Works, Rail/Transport, Industry/Trade, Agriculture, Interior, Foreign Affairs and Intelligence.

### Player intent remains sovereign
National policy defines guardrails such as defensive/offensive posture, theatre priority, treasury reserve, recruitment severity, infrastructure weights, reserve manpower floor, risk tolerance and protected/forbidden projects. AI may recommend breaking a guardrail but cannot silently do so.

### Research is capability-driven
Research should unlock equipment, processes, institutions and doctrine rather than rely primarily on generic percentage bonuses. Completion is separate from prototype, adoption, procurement and production.

### Historical plausibility is explicit
Research/technology/doctrine data can carry earliest plausible dates, prerequisites and scenario restrictions. Alternate history is allowed when explicitly supported, but the default 1864 campaign should not drift into implausible technology jumps.

### Doctrine has three levels
- Strategic: national war posture, concentration, fortress/rail strategy, attrition/manoeuvre.
- Operational: corps/division concentration, reserves, marches, crossings, supply and pursuit.
- Tactical: skirmisher use, artillery preparation, line/column transitions, flanking, defence and cavalry roles.

Doctrine influences planning and preferences; it does not grant hidden information or magical combat bonuses.

### General Staff AI plans rather than teleports
Strategic planning uses real campaign geography, route distance, march/forced-march ETA, finite rail capacity, rolling stock, depots, supply, intelligence confidence and mobilization state.

### Development AI manages real projects
Public Works/Rail/Industry/Agriculture AI acts through the same construction queues, production systems, resource pools and route constraints as the player.

### Military build-up is physically constrained
War Ministry AI must account for manpower, training time, weapons, artillery, horses, wagons, ammunition, depots, barracks and transport capacity. Artillery expansion without draught teams or ammunition support is an incomplete force plan.

### AI must explain itself
Significant autonomous decisions produce an auditable decision record showing actor, reason factors, constraints, cost, ETA and result. Player should be able to ask why a road, rail line, regiment, depot or research project was prioritized.

## Planned backlog

- B-470–B-479 — Government, Ministers & Delegation
- B-480–B-489 — Research, Technology & Historical Plausibility
- B-490–B-499 — Military Doctrine & General Staff AI
- B-500–B-509 — Economy, Industry & Development AI
- B-510–B-519 — Military Build-up, Recruitment & Mobilization AI
- B-520–B-529 — Strategic Planning, Theatre AI & War Direction
- B-530–B-539 — AI Explainability, Historical Research & Final QA

## Version gate

v00.00.14 remains PLANNED until v00.00.13/B-469 is accepted. B-539 is the planned final v00.00.14 promotion gate.