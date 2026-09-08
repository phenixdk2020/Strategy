# PROJECT 1864 — Campaign v00.00.15 Planning

## Status

- **Version:** `v00.00.15` PLANNED
- **Entry condition:** v00.00.14/B-539 must pass before v00.00.15 becomes active.
- **Planned backlog:** B-540–B-599
- **Milestone name:** `MULTI-NATION AI, TRADE, DIPLOMACY & ALLIANCES`

## Milestone purpose

Every country in the scenario must operate under the same strategic design rules as the player country. A non-player nation is not a static backdrop: it develops infrastructure, manages economy and transport, builds and mobilizes armed forces, conducts research, allocates supply, reacts to threats and pursues national objectives through the same legal campaign actions available to the player.

The design must also support changing the player-controlled nation without changing the underlying simulation architecture. `PlayerControl` is therefore a controller assignment, not a special nation type.

Historical starting conditions define the initial world, but country development is not a fixed historical script. AI priorities, ministers, threat assessments, resource constraints, research choices, infrastructure projects and force-structure decisions include bounded, seeded variation so repeated campaigns can develop differently while remaining plausible for the period.

## B-540–B-549 — Generic nation controller & AI parity

- generic `NationState` / `NationController` contract,
- player / AI controller handoff,
- identical minister portfolios for AI nations,
- nation-specific policies and strategic priorities,
- AI country development using real construction/resources,
- AI military build-up using real manpower/equipment,
- AI research and doctrine selection,
- AI logistics and transport allocation,
- difficulty may change decision quality/reaction but never grant free resources or hidden information,
- multi-nation parity QA.

## B-550–B-559 — International trade & markets

- bilateral trade relations,
- import/export orders,
- tradable food, wood, stone, iron, coal/fuel hooks, weapons, horses and rolling stock,
- price/cost and transport requirement,
- port/rail/road capacity constraints,
- tariffs/customs policy hooks,
- shortages and substitution,
- embargo/blockade interaction,
- foreign arms and rolling-stock purchases,
- trade UI and QA.

Trade moves actual goods through transport capacity. A signed trade deal must not teleport stock between national inventories.

## B-560–B-569 — Diplomacy & treaties

- bilateral relations,
- diplomatic actions with cost/delay,
- non-aggression agreements,
- trade treaties,
- military access,
- transit rights,
- guarantees,
- influence/pressure hooks,
- treaty duration/breach/consequences,
- diplomacy UI and explainable AI.

## B-570–B-579 — Alliances, coalitions, neutrality & war/peace

- defensive alliances,
- offensive/limited coalition hooks,
- call-to-arms lifecycle,
- accept/refuse based on interests and commitments,
- neutrality policy,
- war declaration / casus-belli framework hooks,
- separate peace and negotiated peace hooks,
- coalition war aims,
- alliance military coordination without telepathic shared intelligence,
- alliance/war-state QA.

## B-580–B-589 — International military/economic cooperation

- arms contracts,
- foreign loans/financial assistance hooks,
- shared or leased rolling stock where treaty permits,
- expeditionary force framework,
- allied supply-access rules,
- port/rail transit agreements,
- intelligence-sharing agreements with explicit scope and delay,
- military mission/adviser hooks,
- sanctions/embargo interaction,
- cooperation QA.

## B-590–B-599 — World AI, replay variation, long-run balance & final gate

- all nations execute strategic planning cycles,
- AI evaluates friends, rivals, threats and opportunities from known information,
- national interests and historical scenario constraints,
- seeded/bounded variation in development, research, infrastructure and force-structure priorities,
- repeated campaigns may diverge without becoming arbitrary or technologically implausible,
- diplomatic AI explanation/audit trail,
- anti-exploit checks for circular trade/free resources,
- long-run economy and military-growth simulation,
- alliance cascade stress tests,
- save/load determinism across all nations,
- full multi-nation regression,
- **B-599 final v00.00.15 promotion gate**.

## Core parity rule

The same system executes both player and AI actions:

`Nation intent -> minister/staff portfolio -> legal campaign action -> resource/time/transport cost -> result`

AI nations may have different competence, doctrine, information, priorities and personalities, but they do not receive alternate rules.

## Historical plausibility and replay variation

The campaign uses historical data as the **starting condition and plausibility envelope**, not as a mandatory future script.

Variation must be weighted and explainable rather than pure random noise. A country's development decision can consider:

`Need + Threat + Geography + Economy + Resources + Ministers + Doctrine + PriorEvents + SeededVariation`

Consequences:

- the same country may emphasize railways, fortifications, industry, artillery, cavalry, reserves or logistics differently in separate campaigns,
- military force structure may diverge from history if resources, ministers, doctrine or threats develop differently,
- research and doctrine may follow different plausible paths,
- minister changes and major events may redirect national policy during a campaign,
- starting the same scenario with the same campaign seed must reproduce the same AI variation for QA/save-load purposes,
- variation must never bypass technology plausibility, resource costs, manpower, construction time or information limits.

Exact player-facing controls for the amount of historical variation can be decided later; the default design target is noticeable replay variation while preserving a recognizable 19th-century strategic world.

## Trade rule

International trade is a contract plus a physical flow. Goods remain owned at origin until the agreed transfer lifecycle executes; transport, blockade, congestion, route disruption and delivery time can affect the actual shipment.

## Diplomacy rule

Relations are not merely a single friendliness number. Decisions consider national interests, existing treaties, military balance estimates, trade dependence, territorial objectives, war state, government/minister preferences, trust and known prior behavior.

## Alliance rule

Alliances do not create perfect command or perfect intelligence. Allied forces remain owned and commanded by their nation unless an explicit expeditionary/command arrangement changes that. Intelligence sharing follows treaty scope and communication delay.