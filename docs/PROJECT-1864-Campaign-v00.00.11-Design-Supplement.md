# PROJECT 1864 — Campaign v00.00.11 Design Supplement

## Status

- Campaign version: `v00.00.11 WORK`
- Branch: `work/v00.00.11-campaign-map`
- Tactical baseline: `v00.00.09 TEST`
- Geographic scope: Denmark, Sweden, Norway, Finland and Germany
- Runtime node target: 50 canonical nodes
- Active campaign backlog: B-280–B-359
- B-310–B-319 is an already occupied cross-layer regimental-standards namespace and is not redefined by the campaign expansion
- Intermediate core gate: B-299
- Final promotion gate: B-359
- Next campaign version after final gate: `v00.00.12`

## Version principle

Campaign-versionen er en samlet testbar feature-gate, ikke et versionshop pr. commit. Derfor kan mange backlogpunkter, kodeændringer, diagnostics og UI-polish fortsætte under `v00.00.11`, mens runtime QA samles i større batches.

Implementeret arbejde må stå som `IMPLEMENTERET / AFVENTER QA`, indtil Unity 6.6 compile/runtime er gennemført. Manglende QA alene udløser ikke en ny campaign-version.

## Current campaign architecture

Campaign-laget bygger på:

- real latitude/longitude for strategic locations,
- projection to a 3D campaign map,
- stable node and formation IDs,
- node-link based strategic movement,
- campaign time with pause/speed,
- campaign -> tactical battle context,
- tactical -> campaign result continuity,
- separate geographic region and political controller.

Det visuelle kort og navigation må udvikles uden at ændre tactical AI på campaign-branchen.

## Existing v00.00.11 usability layer

`CampaignMapUsabilityV011.cs` udbygger den eksisterende campaign prototype med en isoleret usability/diagnostic layer:

- network validation for 50 nodes,
- invalid-node and broken-link diagnostics,
- prototype classification of strategic links as Road/Rail/SeaFerry,
- node click selection and information panel,
- political-control indicators separate from geographic region,
- stationary formation stacking offsets,
- remaining-route ETA calculation,
- camera focus (`F`) and reset (`Home`),
- compact runtime diagnostics.

`CampaignMapSearchHoverV011.cs` tilføjer:

- partial-match search for locations and formations,
- camera focus to search result,
- formation hover/OOB summary.

Den eksisterende `CampaignMapController` beholder authoritative campaign movement/pathfinding, route ghost, time progression and battle-contact logic. v00.00.11 helper-layers må ikke skabe en alternativ simulation-state.

## Expanded campaign work package

### B-280–B-299 — Core map usability

50-node validation, labels, route types, ETA, node selection, political control, formation stacking, camera navigation, diagnostics, search, layer planning, route breakdown, node roles, depot/infrastructure display, hover/OOB, performance and QA presets.

B-299 er nu en intermediate core gate, ikke den endelige versionspromotion.

### B-300–B-309 — Map intelligence & UX

Semantic zoom, major-city priority, location hover, selected-node highlight, route emphasis, map legend, overview/minimap, bookmarks, formation list/OOB and contact/event markers.

### B-310–B-319 — Regimental standards namespace

Denne ID-range eksisterer allerede som tværgående standards/faner backlog. Campaign-integrationen kan bruge de eksisterende beslutninger, men range må ikke genbruges til andre campaign tasks.

### B-320–B-329 — Movement & logistics

Explicit edge data, road classes, rail, ferry/sea transfer, order queue, march stance, strategic fatigue, supply-source association, throughput and route interruption/reroute.

### B-330–B-339 — Warfare & command

Formation hierarchy, command relation, strategic order delay, enemy uncertainty, reconnaissance, battle initiation, reinforcement windows, retreat destination, battle aftermath and campaign battle history.

### B-340–B-349 — World, economy & manpower

Manpower, recruitment policy, replacements, equipment stockpiles, production, resources, treasury, regional/economic control split, trade hooks and economy diagnostics.

### B-350–B-359 — Save, QA & polish

Versioned campaign save snapshots, restore/load, autosave hooks, deterministic QA seed, integrity validation, 100+ node stress test, formation-count stress test, campaign error overlay, full regression preset and final B-359 promotion gate.

## Strategic link principle

Link classification i første v00.00.11 implementation er et prototype-hook. `SeaFerry` bruges til kendte water-transfer legs i QA-nettet; links mellem rail-capable nodes kan klassificeres som rail candidates; øvrige links behandles som road/land links.

B-320+ skal senere flytte dette til explicit infrastructure edge data med type, distance, capacity, state, transport eligibility, movement cost, supply throughput og disruption state. Historisk validering skal kunne ske gennem data uden at rewrite UI/pathfinding.

## Political-control principle

`CampaignMapRegion` beskriver geografi. `CampaignNation Controller` beskriver politisk/militær kontrol. Economic contribution/occupation state skal ligeledes kunne være separat.

Et tactical battle-resultat kan ændre controller eller military state uden at flytte eller omdefinere locationens geografi.

## Campaign UI direction

Prioriteten er operationel læsbarhed før kompleks simulation:

1. selection/search/focus,
2. location and formation readability,
3. route/ETA/infrastructure understanding,
4. political/contact overlays,
5. movement/logistics state,
6. OOB/command/intelligence,
7. economy and world-state hooks,
8. save/integrity/reproducible QA.

## Development isolation

Campaign-branchen må ikke bruges til at rette eller eksperimentere med tactical Officer AI, tactical formation steering eller battlefield navigation. Tactical battle modtager kun campaign context gennem definerede handoff-data.

Det er særligt vigtigt efter tactical navigation-regressionstest: campaign-udviklingen fortsætter parallelt og må ikke trække eksperimentelle tactical changes ind.

## QA rule

En feature må gerne være implementeret i GitHub uden at blive kaldt godkendt. Status er `IMPLEMENTERET / AFVENTER QA`, indtil den er kompileret og runtime-testet i Unity 6.6.

B-359 er den endelige gate for at åbne `v00.00.12`. Opgaver som ikke er nødvendige for promotion kan eksplicit deferred til v00.00.12+ med dokumenteret begrundelse, men de må ikke forsvinde fra backloggen.
