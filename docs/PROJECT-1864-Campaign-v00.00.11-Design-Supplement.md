# PROJECT 1864 — Campaign v00.00.11 Design Supplement

## Status

- Campaign version: `v00.00.11 WORK`
- Branch: `work/v00.00.11-campaign-map`
- Tactical baseline: `v00.00.09 TEST`
- Geographic scope: Denmark, Sweden, Norway, Finland and Germany
- Runtime node target: 50
- Active campaign backlog: B-280–B-299
- Next campaign version after final gate: `v00.00.12`

## Version principle

Campaign-versionen er en samlet **testbar feature-gate**, ikke et versionshop pr. commit. Derfor kan B-280–B-299, kodeændringer, diagnostics og UI-polish alle fortsætte under `v00.00.11`, indtil B-299 er bestået.

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

## v00.00.11 usability layer

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

Den eksisterende `CampaignMapController` beholder authoritative campaign movement/pathfinding, route ghost, time progression and battle-contact logic. v00.00.11 layeren må ikke skabe en alternativ simulation-state.

## Strategic link principle

Link classification i v00.00.11 er første prototype-hook. `SeaFerry` bruges til kendte water-transfer legs i QA-nettet; links mellem rail-capable nodes kan klassificeres som rail candidates; øvrige links behandles som road/land links.

Senere versioner skal udvide dette til explicit infrastructure data, capacity, transport eligibility, military movement speed, supply throughput and disruption state. Prototype-classification må ikke behandles som endeligt historisk valideret 1864 transportdata.

## Political-control principle

`CampaignMapRegion` beskriver geografi. `CampaignNation Controller` beskriver politisk/militær kontrol. Disse må ikke slås sammen.

Eksempler:

- Finland kan være region `Finland` med controller `RussianEmpire`.
- Schleswig-Holstein locations kan ligge i region `Germany`, mens controller varierer efter campaign state.
- Et tactical battle-resultat kan ændre controller uden at flytte eller omdefinere locationens geografi.

## Campaign UI direction

v00.00.11 skal gøre kortet operationelt læsbart før næste store simulation layer. Prioriteten er:

1. location readability,
2. selection and information,
3. formation readability,
4. route/ETA understanding,
5. political/infrastructure overlays,
6. camera navigation,
7. diagnostics and reproducible QA.

## Expanded B-290–B-299 direction

Den udvidede v00.00.11 work package tilføjer plan for:

- search/find location,
- map/layer filters,
- route cost breakdown,
- strategic node roles,
- depot/supply overlay seed,
- port/rail/fortification symbols,
- formation hover/OOB summary,
- label culling/performance,
- deterministic campaign QA presets,
- final B-299 promotion gate.

## QA rule

En feature må gerne være implementeret i GitHub uden at blive kaldt godkendt. Status er `IMPLEMENTERET / AFVENTER QA`, indtil den er kompileret og runtime-testet i Unity 6.6. B-299 er den endelige gate for at åbne `v00.00.12`.
