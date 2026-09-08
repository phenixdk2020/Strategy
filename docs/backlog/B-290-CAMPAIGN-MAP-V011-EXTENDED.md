# B-290–B-299 — Campaign Map v00.00.11 Extended Work Package

## Purpose

Denne udvidelse fortsætter **samme campaign-version v00.00.11** efter B-280–B-289. Versionsnummeret ændres ikke for hvert backlogpunkt eller commit.

`B-289` behandles herefter som **core usability checkpoint**. Den endelige v00.00.11 promotion-gate flyttes til `B-299`, så hele B-280–B-299 kan udvikles og testes som én samlet campaign-milepæl.

Tactical AI er fortsat uden for scope på campaign-branchen.

## B-290 — Campaign search / find location

**Status:** PLANLAGT

Spilleren skal hurtigt kunne finde en kendt by eller formation på det store nordisk-tyske kort.

### Krav

- Søgning på location-navn og stabilt ID.
- Søgning på formation-navn og ID.
- Resultat kan centreres/fokuseres med kameraet.
- Delvis tekstmatch er tilladt.
- Ingen ændring af simulation state ved søgning.

### Acceptance

København, Berlin, Stockholm, Dybbøl og mindst én formation kan findes og fokuseres uden manuel panorering.

## B-291 — Map filters og layer toggles

**Status:** PLANLAGT

Kortet skal kunne aflastes visuelt ved at slå informationslag til/fra.

### Minimumslayers

- location labels,
- strategic links,
- political control,
- formation tokens,
- depots,
- ports,
- rail,
- fortifications.

### Acceptance

Hvert lag kan toggles uden at ændre authoritative campaign state.

## B-292 — Route-type legend og route cost breakdown

**Status:** PLANLAGT

Spilleren skal kunne forstå, hvorfor en route tager den tid, den gør.

### Krav

- Leg type vises som `Road`, `Rail` eller `Sea/Ferry`.
- Distance pr. leg.
- Terrain modifier.
- Beregnet travel time pr. leg.
- Total ETA.
- Senere hooks til weather, congestion, supply og fatigue.

### Acceptance

En multi-leg route viser en forståelig cost breakdown uden at ændre selve pathfinding-resultatet.

## B-293 — Strategic importance / node role

**Status:** PLANLAGT

Locations skal kunne få en gameplaymæssig rolle ud over navn og koordinat.

### Første roller

- capital/major city,
- regional centre,
- fortress,
- depot,
- port,
- rail junction,
- crossing/ferry point.

### Acceptance

Mindst 15 prototype-locations har en tydelig strategisk rolle, som kan vises i node-info uden at hardcode UI-logik pr. by.

## B-294 — Depot og supply overlay seed

**Status:** PLANLAGT

Før den fulde supply-simulation skal kortet kunne vise, hvor strategiske supply-kilder ligger.

### Krav

- Depot-noder kan fremhæves.
- Formation kan vise nærmeste kendte friendly depot som diagnostik.
- Dataarkitekturen skal kunne udvides med stock, capacity og route connectivity.
- Ingen gratis automatisk resupply implementeres i denne opgave.

### Acceptance

Depot-overlay kan vises, og mindst én dansk og én preussisk formation kan få identificeret en relevant friendly depot-kandidat.

## B-295 — Port, rail og fortification symbols

**Status:** PLANLAGT

De vigtigste infrastrukturegenskaber skal kunne aflæses hurtigere end via tekst alene.

### Krav

- separate symboler for port, rail og fortified,
- symbolerne følger node-position,
- symboler kan toggles via layer controls,
- symbolerne må ikke skjule location-navnet ved normal zoom.

### Acceptance

En spiller kan visuelt skelne fx havneby, rail-node og fæstning uden først at åbne info-panelet.

## B-296 — Formation hover/OOB summary

**Status:** PLANLAGT

Formation tokens skal give hurtig information uden altid at kræve fuldt selection-panel.

### Krav

- hover viser formation-navn,
- nation,
- total strength,
- regiment count,
- moving/engaged/idle state,
- destination hvis moving.

### Acceptance

Hover over alle QA-formationer viser korrekt summary uden console errors.

## B-297 — Label culling og performance

**Status:** PLANLAGT

50 nodes er kun første trin; label-systemet skal kunne skalere.

### Krav

- labels uden for viewport renderes ikke,
- labels kan reduceres efter zoomniveau,
- prioritet til major/selected locations,
- ingen per-frame allocation-spikes fra unødvendig rebuild,
- target: stabil campaign-map interaktion med mindst 100 locations som syntetisk test senere.

### Acceptance

50-node kortet kan panoreres/zoomes uden mærkbar label-stutter, og selected location-label forbliver synligt.

## B-298 — Campaign QA scenario presets

**Status:** PLANLAGT

Det skal være nemt at reproducere testcases for movement, stacking og contact.

### Presets

- multi-leg Danish movement,
- 3 formations stacked on one node,
- Danish/Prussian contact trigger,
- long-distance camera navigation,
- ferry/sea-link route,
- rail-capable corridor.

### Acceptance

Mindst fire presets kan reproduceres deterministisk efter campaign reset.

## B-299 — Final v00.00.11 acceptance / promotion gate

**Status:** PLANLAGT

B-299 er den endelige gate for at afslutte v00.00.11 og åbne næste campaign-version **v00.00.12**.

### Endelig acceptance

1. Unity compiler: 0 blocking errors.
2. Campaign version overlay viser `v00.00.11 WORK`.
3. Runtime network validerer 50 nodes uden broken links.
4. Location labels og node selection fungerer.
5. Political control kan aflæses separat fra geographic region.
6. Formation selection og stacking fungerer.
7. Multi-leg pathfinding, route ghost og ETA fungerer.
8. Strategic link types kan skelnes mindst som Road/Rail/Sea-Ferry.
9. Camera pan/zoom/rotate samt focus/reset fungerer.
10. Search/find location fungerer.
11. Map layers kan toggles uden state-ændring.
12. Route cost breakdown kan vises.
13. Depot/port/rail/fortification-information er visuelt tilgængelig.
14. Formation hover/OOB summary fungerer.
15. QA presets kan reproducere centrale campaign-tests.
16. Diagnostics rapporterer version, node count, links, selection, route, ETA og campaign-time.
17. Tactical AI-filer og tactical AI-adfærd er ikke ændret af campaign-work package.

Når B-299 er bestået, kan `v00.00.12` oprettes.

## Implementation status — first v00.00.11 usability wave

Følgende er nu implementeret på work-branchen gennem `CampaignMapUsabilityV011.cs`, men kræver Unity runtime QA før status kan ændres til godkendt:

- B-281: 50-node runtime validation + broken-link diagnostics.
- B-283: første link-type classification (`Road`, `Rail`, `SeaFerry`) som prototype-data/diagnostik.
- B-284: ETA-visning oven på eksisterende multi-leg route/pathfinding.
- B-285: node selection + information panel.
- B-286: separat political-control indicator layer.
- B-287: stationary formation stack offsets for multiple formations on same node.
- B-288: `F` focus selected og `Home` reset camera.
- B-289: udvidet runtime diagnostics panel/logging.

Disse punkter er **IMPLEMENTERET / AFVENTER QA**, ikke færdig-godkendte. Runtime-test i Unity er stadig promotion-gaten.
