# B-280–B-289 — Campaign Map / Strategic Layer v00.00.11

## Purpose

v00.00.11 samler flere relaterede campaign-opgaver i **samme version** i stedet for at øge versionsnummeret for hvert enkelt backlogpunkt.

Versionen bygger videre på v00.00.10 med 50 strategiske locations og fokuserer på at gøre campaign-kortet læsbart, brugbart og egnet til den første sammenhængende strategiske test.

**Versionregel:** `v00.00.11` beholdes, indtil hele work package B-280–B-289 har bestået sin acceptance-gate. En ny backlogopgave i denne work package giver derfor ikke automatisk `v00.00.12`.

Tactical AI er eksplicit uden for scope på denne branch.

## B-280 — v00.00.11 campaign work package

**Status:** AKTIV

Samler B-281–B-289 og er færdig, når alle blocking acceptance-kriterier er bestået.

### Exit

- Unity compiler uden blocking errors.
- CampaignMap åbner og viser `PROJECT 1864 CAMPAIGN | v00.00.11 WORK`.
- 50 strategiske locations er tilgængelige ved runtime.
- Tactical AI-filer er ikke ændret som del af campaign-work package.

## B-281 — 50-node integration og runtime-validering

**Status:** AKTIV

De eksisterende 32 locations plus node-packens 18 ekstra locations skal fungere som ét sammenhængende network.

### Krav

- Runtime total = 50 nodes.
- Ingen duplikerede IDs.
- Alle 50 locations får gyldig latitude/longitude og projected map position.
- Alle links skal referere til eksisterende nodes.
- Ingen node må blive usynlig på grund af manglende region/material mapping.
- Runtime diagnostics skal rapportere node count og validation-resultat.

### Acceptance

`CAMPAIGN-NODES` telemetry viser 50 nodes uden validation warning.

## B-282 — Location labels og læsbarhed

**Status:** PLANLAGT

Campaign-kortet skal kunne aflæses uden at spilleren skal kende markørerne på forhånd.

### Krav

- Synligt by-/location-navn ved relevante zoomniveauer.
- Labels følger korrekt node-position.
- Labels må ikke ligge nede i terrænet eller bag node-marker.
- Grundlæggende overlap/clutter-håndtering.
- Labels kan reduceres/skjules ved meget stort zoom-out.
- Fortified, port og depot locations kan senere få små sekundære symboler uden at ændre navnelayoutet.

### Acceptance

Mindst de større byer og alle valgte locations kan identificeres direkte på kortet.

## B-283 — Strategiske linktyper: vej, jernbane og sø

**Status:** PLANLAGT

Det nuværende generiske link-net skal kunne skelne mellem transporttyper.

### Krav

- Link-type skal kunne repræsentere mindst `Road`, `Rail`, `Sea/Ferry` og senere special crossing.
- Great Belt, Øresund og andre vandlinks må ikke behandles som almindelig march til fods.
- Rail capability på nodes skal kunne bruges af senere movement/supply logic.
- Visuel route rendering skal kunne skelne linktyper uden at ændre den geografiske projection.

### Acceptance

En route kan identificere, hvilke legs der er land, rail-kandidater eller søtransfer.

## B-284 — Strategic pathfinding, route preview og ETA

**Status:** PLANLAGT

Spilleren skal kunne vælge en formation og give en reel strategisk movement order gennem node-nettet.

### Krav

- Pathfinding finder en gyldig kæde af links fra start til destination.
- Route preview/ghost vises før eller umiddelbart efter ordren.
- ETA beregnes pr. route leg og samlet.
- Movement fortsætter korrekt over flere legs.
- Formationens current node opdateres kun, når et leg faktisk er gennemført.
- Pause stopper movement progression.
- Speed controls ændrer simulationstid, ikke den beregnede militære marchhastighed.
- Ugyldig destination giver tydelig feedback og ingen korrupt route-state.

### Acceptance

En dansk QA-formation kan beordres gennem mindst 4 sammenhængende nodes, og ETA/progress følger simulationstiden.

## B-285 — Node selection og location information panel

**Status:** PLANLAGT

Et klik på en strategisk location skal give et brugbart informationspanel.

### Minimumsdata

- navn,
- region,
- politisk controller,
- terrain type,
- depot,
- port,
- rail,
- bridge/crossing,
- tilstedeværende egne/fjendtlige formationer når de er kendte,
- direkte links til nabonodes.

### Acceptance

Spilleren kan vælge mindst 10 forskellige nodes og få korrekte data uden console errors.

## B-286 — Political control visualization

**Status:** PLANLAGT

Politisk kontrol skal kunne aflæses uden at forveksle geografisk region med nation/controller.

### Krav

- `CampaignMapRegion` og `CampaignNation Controller` forbliver separate datafelter.
- Node/control-indikation viser mindst Danmark, Preussen, Sverige-Norge, Russiske Imperium og øvrige tyske områder korrekt efter den aktuelle prototype-model.
- Valgt node viser controller eksplicit i UI.
- Et tactical battle-resultat skal senere kunne skifte node controller uden at ændre node geography.

### Acceptance

Finland kan fx være geografisk `Finland`, men politisk `RussianEmpire`, uden at UI eller rendering blander de to begreber sammen.

## B-287 — Formation tokens, stacking og selection

**Status:** PLANLAGT

Flere formationer skal kunne befinde sig ved samme location uden at tokens bliver ubrugelige.

### Krav

- Stabil formation ID bevares.
- Flere formationer på samme node skal kunne vises som stack eller offset tokens.
- Selected formation fremhæves tydeligt.
- Friendly og enemy token state skal kunne skelnes.
- Selection panel viser formationens regimenter, total strength, ammo summary og movement state.
- Token-position følger formationens aktuelle campaign node/movement state.

### Acceptance

Mindst tre formationer kan vises på samme node og vælges entydigt uden overlap, der gør selection umulig.

## B-288 — Campaign camera og navigation polish

**Status:** PLANLAGT

Kortnavigation skal være hurtig nok til et stort Danmark–Tyskland–Finland/Norge view.

### Krav

- WASD pan.
- Q/E rotation.
- Mouse wheel zoom.
- Kamera bounds forhindrer, at hele kortet mistes.
- Focus selected formation/location command.
- Home/reset view til en kendt standardposition.
- Kamera-bevægelse må ikke ændre simulation state.

### Acceptance

Spilleren kan gå fra København til Stockholm/Berlin/Norge og tilbage til valgt formation uden at miste orienteringen.

## B-289 — v00.00.11 diagnostics og acceptance gate

**Status:** PLANLAGT

Denne opgave lukker versionen og afgør, hvornår `v00.00.12` må oprettes som næste campaign-version.

### Required diagnostics

- Campaign version/channel.
- Node count.
- Invalid/duplicate node IDs.
- Broken links.
- Selected node/formation ID.
- Active route og destination.
- Route leg progress / ETA.
- Campaign date/time og speed state.

### Samlet acceptance-gate

1. Unity compiler: 0 blocking errors.
2. CampaignMap åbner korrekt.
3. Version overlay viser v00.00.11 WORK.
4. 50 nodes valideres.
5. Labels gør centrale locations aflæselige.
6. Node selection/info fungerer.
7. Formation selection/stacking fungerer.
8. Multi-leg pathfinding og movement fungerer.
9. Route preview og ETA fungerer.
10. Pause/speed fungerer under strategic movement.
11. Controller/geographic region holdes korrekt adskilt.
12. Kamera pan/zoom/rotation/focus er stabilt.
13. Ingen campaign-ændring har modificeret tactical AI adfærd.

Når ovenstående gate er bestået, er næste normale campaign-version **v00.00.12**.

## Ikke blocking for v00.00.11

Følgende kan ligge i senere campaign-versioner:

- fuld supply simulation,
- rail capacity/timetables,
- weather og seasonal movement,
- fatigue/stragglers på campaign map,
- strategic fog of war/intelligence depth,
- diplomacy,
- production/economy,
- recruitment/training,
- fuldt naval campaign system,
- auto-resolve,
- save-game completeness,
- high-resolution final coastline/DEM.

## Version management principle

En version er en **testbar feature-gate**, ikke et enkelt commit eller et enkelt backlogpunkt. Derfor kan v00.00.11 indeholde mange commits og B-281–B-289, så længe de tilsammen udgør den samme campaign-milepæl og kan testes som én sammenhængende build.