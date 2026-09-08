# PROJECT 1864 — Campaign v00.00.14 Planning

## Status

- **Version:** `v00.00.14` PLANNED
- **Active campaign version remains:** `v00.00.11 WORK`
- **v00.00.12 remains:** PLANNED, B-370–B-409
- **v00.00.13 remains:** PLANNED, B-410–B-469
- **Entry condition for v00.00.14:** v00.00.13/B-469 must pass before v00.00.14 becomes active.
- **Planned backlog:** B-470–B-539
- **Milestone name:** `GOVERNMENT, RESEARCH, DOCTRINE & STRATEGIC AI`
- **Tactical isolation:** this milestone may define doctrine and strategic intent consumed later by tactical systems, but it must not directly rewrite tactical battlefield navigation or give tactical AI hidden information/bonuses.

## Milestone purpose

v00.00.14 adds the national decision layer above the campaign map. The player can govern directly or delegate defined portfolios to AI ministers and staff officers. Research, doctrine, economy, infrastructure, military expansion, mobilization, logistics and theatre planning become explainable strategic systems rather than isolated buttons.

The central design rule is **delegation with guardrails**. AI ministers use the same legal actions, resources, construction times, transport limits and information constraints as the player. AUTO is not a cheat mode and MANUAL remains fully viable.

## Delegation modes

Every delegatable portfolio supports the same baseline modes:

- `MANUAL` — AI does not execute decisions.
- `ADVISORY` — AI produces recommendations with reasons, expected cost, risks and alternatives.
- `ASSISTED` — AI may execute routine actions inside explicit player policies, limits and budgets.
- `AUTO` — AI manages the portfolio inside national strategy, budget, legal rules and player-defined guardrails.

Player may override, pause, lock or reserve individual projects/resources regardless of delegation mode, subject to game rules.

## Planned work packages

### B-470–B-479 — Government, Ministers & Delegation

Government structure, minister portfolios, delegation modes, cabinet priorities, budgets, minister competence/personality, policy guardrails, conflict/arbitration, decision audit trail and government UI.

Source: `docs/backlog/B-470-CAMPAIGN-V014-GOVERNMENT-MINISTERS-DELEGATION.md`

### B-480–B-489 — Research, Technology & Historical Plausibility

Research institutions, research categories, project lifecycle, historical availability/validation, prototypes/adoption, doctrine research, industrial/civil research, knowledge diffusion, research AI and research QA.

Source: `docs/backlog/B-480-CAMPAIGN-V014-RESEARCH-TECHNOLOGY.md`

### B-490–B-499 — Military Doctrine & General Staff AI

Strategic/operational/tactical doctrine, general-staff plans, mobilization/deployment plans, theatre objectives, reserves, reconnaissance assumptions, planning cycles, military exercises/lessons, doctrine recommendations and doctrine/staff QA.

Source: `docs/backlog/B-490-CAMPAIGN-V014-MILITARY-DOCTRINE-GENERAL-STAFF.md`

### B-500–B-509 — Economy, Industry & Development AI

Finance ministry, public works, railway/transport administration, industry/trade, agriculture/horse economy, infrastructure scoring, construction portfolio optimization, resource/budget arbitration, regional development policy and economic-AI QA.

Source: `docs/backlog/B-500-CAMPAIGN-V014-ECONOMIC-DEVELOPMENT-AI.md`

### B-510–B-519 — Military Build-up, Recruitment & Mobilization AI

War ministry force planning, manpower/reserves, recruitment, replacements, artillery/horses/wagons/rolling stock requirements, equipment procurement, training/readiness, force-structure targets, mobilization execution and military-build QA.

Source: `docs/backlog/B-510-CAMPAIGN-V014-MILITARY-BUILDUP-MOBILIZATION-AI.md`

### B-520–B-529 — Strategic Planning, Theatre AI & War Direction

National war aims, theatre creation, force allocation, objective scoring, operational plans, strategic reserve, logistics-aware planning, contingency plans, plan execution/reassessment and theatre-AI QA.

Source: `docs/backlog/B-520-CAMPAIGN-V014-STRATEGIC-PLANNING-THEATRE-AI.md`

### B-530–B-539 — AI Explainability, Personalities, Historical Research & Final QA

Explainable AI decisions, minister traits, confidence/uncertainty, player constraints, historical data/research workflow, no-cheat validation, deterministic AI QA, stress/long-run tests, full regression and B-539 final promotion gate.

Source: `docs/backlog/B-530-CAMPAIGN-V014-AI-EXPLAINABILITY-RESEARCH-QA.md`

## Proposed minister / senior-office portfolios

Initial portfolio model should support at least:

- **Head of Government / Cabinet** — national priorities and arbitration.
- **Minister of War** — force structure, recruitment, procurement, mobilization and military facilities.
- **Chief of the General Staff** — strategic plans, theatres, operational objectives, reserves and deployment.
- **Quartermaster General** — depots, supply priorities, army transport and sustainment.
- **Minister of Finance** — treasury, budgets, debt/borrowing hooks and spending limits.
- **Minister of Public Works** — roads, bridges, ports, telegraph and major infrastructure.
- **Railway / Transport Administration** — rail expansion, rolling stock, train allocation, maintenance and transport capacity.
- **Minister of Industry & Trade** — production, military industry, imports/exports and raw-material allocation.
- **Minister of Agriculture** — food, forage, farms and horse-breeding capacity.
- **Minister of the Interior** — public order, occupation, reconstruction, refugees and domestic administration.
- **Foreign Minister** — diplomacy, trade agreements, military access, arms purchases and peace hooks.
- **Intelligence / Reconnaissance Chief** — strategic intelligence priorities, estimates and reporting policy.

Exact historical office titles can vary by nation and year; the engine should use semantic portfolios while scenario data supplies historically appropriate names/titles.

## National strategy / guardrails

AI portfolios should receive explicit player intent such as:

- national posture: defensive / balanced / offensive,
- theatre priorities,
- minimum treasury reserve,
- maximum debt/spending thresholds,
- military vs civil investment weights,
- rail / road / fortification priorities,
- recruitment severity,
- reserve manpower floor,
- supply priority hierarchy,
- prohibited regions/projects,
- protected resources/rolling stock,
- risk tolerance,
- desired force composition.

AI decisions outside guardrails require advisory escalation rather than silent override.

## Research direction

Research must be grounded in the 19th-century setting. The preferred design is not a generic arcade tech tree of flat percentage bonuses. Research should primarily unlock or improve **capabilities, procedures, equipment, institutions and doctrines**.

Candidate fields:

- infantry weapons and drill,
- artillery technology and fire-control procedures,
- cavalry/dragoon doctrine,
- staff work and command procedures,
- reconnaissance/intelligence,
- logistics and depot practice,
- railways and railway administration,
- telegraph/communications,
- fortification/engineering,
- medicine/sanitation,
- industrial production,
- metallurgy/machining,
- agriculture/forage/horse breeding.

Scenario/year gates and historical plausibility data should prevent implausible technology leaps while still allowing believable alternate development.

## Military doctrine direction

Doctrine is split into three levels:

- **Strategic doctrine** — national war posture, mobilization, concentration, fortress strategy, railway concentration, attrition vs manoeuvre.
- **Operational doctrine** — corps/division concentration, reserves, marches, screening, supply bases, crossing operations and pursuit.
- **Tactical doctrine** — deployment preferences, skirmisher use, artillery preparation, line/column transitions, flank attacks, defensive positions, counterattack reserves and cavalry roles.

Doctrine changes AI preferences and available plans; it does not grant hidden vision, free movement or arbitrary damage bonuses.

## Explainability principle

Every significant AI action must be auditable. Example decision record:

`Project=Upgrade Aarhus-Fredericia Rail | RequestedBy=GeneralStaff | ExecutedBy=PublicWorks | MilitaryRoute=+38 | Congestion=+26 | DepotConnection=+21 | Cost=-11 | Score=74 | BudgetApproved=True`

Player UI should be able to show the major positive/negative factors, expected cost, ETA, constraints and which minister/staff office made the decision.

## Relationship to previous versions

- v00.00.12 supplies real economy/logistics/mobility state.
- v00.00.13 supplies the 3D/geospatial world and route/infrastructure representation.
- v00.00.14 supplies institutions and strategic AI that can operate those systems.

Examples:

- Public Works AI can propose/build real B-430 infrastructure using the same construction state as the player.
- Transport AI allocates finite rolling stock from B-380 instead of spawning trains.
- War Ministry AI must recruit from real manpower/reserve pools.
- General Staff AI plans using real route distance, march/forced-march ETA and supply constraints.
- Research cannot instantly create equipment; adoption/procurement/production remain separate lifecycle stages.

## Promotion principle

Planning files may exist now, but v00.00.14 must not become active before v00.00.13/B-469 is accepted. B-539 is the planned final v00.00.14 promotion gate.