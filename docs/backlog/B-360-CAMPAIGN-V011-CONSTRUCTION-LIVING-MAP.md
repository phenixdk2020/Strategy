# B-360–B-369 — Campaign v00.00.11 Construction & Living Map

## Decision

PROJECT 1864 bruger **systemisk construction (valg B)** frem for ren dekorativ fake-animation.

Målet er, at campaign-kortet bliver mere levende, mens campaign-tiden går, og at de synlige byggeanimationer samtidig afspejler reel campaign-state.

Visuelle construction-loops må være lette og stiliserede, men selve progressionen skal være datadrevet og kunne kobles til manpower, resources, depots, supply, infrastructure og senere AI-prioritering.

Tactical AI er uden for scope.

## B-360 — Persistent construction project state
**Status:** PLANLAGT

Indfør et separat, versioneret construction-state-lag.

Minimum data:
- ConstructionId
- ConstructionType
- NodeId eller EdgeId
- Owner/controller
- StartTime
- RequiredHours
- Progress01
- Stage
- Paused
- Completed
- WorkersAssigned
- ResourceCost / reserved resources hook

Acceptance:
- progression følger `CampaignSession.CurrentDateTime`, ikke real-time direkte,
- pause af campaign-time stopper progression,
- speed 1/5/20 ændrer progression via campaign-tid,
- state er adskilt fra GameObject/prefab-state.

## B-361 — Construction stage visual framework
**Status:** PLANLAGT

Byggeprojekter får visuelle stages, fx 0–5:

0. byggeplads / survey markers
1. fundament / earthworks
2. træskelet / stillads
3. halvfærdig struktur
4. næsten færdig
5. completed asset

Acceptance:
- stage beregnes fra authoritative progress,
- visuelt stage-skift kan ske uden at ændre simulation state,
- stage kan rekonstrueres korrekt efter load/reset.

## B-362 — Barracks construction prototype
**Status:** PLANLAGT

Første fulde building prototype bliver en kaserne.

Visuelle elementer:
- fundament,
- træskelet/stillads,
- vægge,
- tag,
- materialestakke,
- 2–4 simple worker loops,
- let støv/hammer-activity ved tæt zoom.

Systemisk hook:
- manpower/workers,
- wood/stone/money hooks,
- build duration,
- pause ved manglende resources senere.

Acceptance:
- kasernen kan ses vokse gennem mindst 5 stages over campaign-tid,
- completed stage erstatter construction visuals uden pop/state-loss.

## B-363 — Farm construction prototype
**Status:** PLANLAGT

Farmen bruger samme construction framework men eget stage-set.

Visuelle elementer:
- markeringspæle,
- fundament,
- lade/bondegård-skelet,
- tag,
- hegn,
- høstak/materialevogn,
- små worker/farmer loops.

Systemisk hook:
- food-production hook,
- local manpower,
- wood resource,
- regional economy.

Acceptance:
- farmen bruger samme state-model som barracks uden special-case simulation code.

## B-364 — Road construction crews
**Status:** PLANLAGT

Vejbyggeri skal kunne foregå langs en strategic edge.

Visuelle elementer:
- survey stakes,
- jord/grus-sektioner,
- workers med skovle/hakker,
- materialevogn,
- gradvis ændring fra dårlig/local road til forbedret road visual.

Systemisk hook:
- edge construction state,
- road class upgrade,
- worker assignment,
- money/stone/wood hooks,
- senere movement/supply modifier.

Acceptance:
- progression sker sektion for sektion langs edge i stedet for kun ved en node,
- incomplete road giver ikke completed-road bonus før completion gate.

## B-365 — Railway construction crews
**Status:** PLANLAGT

Jernbane får sin egen levende construction-sekvens.

Visuelle stages:
- survey/earthworks,
- embankment,
- sleepers/sveller,
- rail sections,
- work crew/material wagon,
- completed rail connection.

Systemisk hook:
- iron/wood/money,
- workers,
- edge eligibility,
- station/endpoint requirement senere,
- rail transport bliver først aktiv efter completion.

Acceptance:
- sveller/skinner kan vokse progressivt langs en edge,
- en halvfærdig jernbane må ikke bruges som færdig rail route.

## B-366 — Living-map worker animation library
**Status:** PLANLAGT

Lav et lille genbrugeligt bibliotek af billige ambient loops.

Første loops:
- hammering,
- digging,
- carrying timber,
- pushing/pulling cart,
- placing sleepers,
- idle/talking/rest,
- walking short work path.

Principper:
- få keyframes,
- lav polygon/placeholder models accepteres i prototype,
- random phase offset så alle workers ikke bevæger sig synkront,
- animation må aldrig være authoritative simulation.

Acceptance:
- mindst 4 forskellige loop-typer kan bruges af flere construction projects.

## B-367 — Construction LOD / semantic animation
**Status:** PLANLAGT

Construction visuals skal skalere med zoom og performance.

Tiers:
- Far: kun construction icon/progress marker.
- Medium: stage mesh + evt. 1 simplificeret worker loop.
- Near: fulde stage meshes, flere workers, cart/materialer og små VFX.

Acceptance:
- ingen worker animation kræves på hele kortet samtidigt,
- selected construction må prioriteres,
- LOD må ikke ændre project progress/state.

## B-368 — Construction economy/supply integration hooks
**Status:** PLANLAGT

Construction bliver koblingspunkt til economy/logistics uden at gøre v00.00.11 afhængig af fuld økonomisimulation.

Hooks:
- money,
- manpower/workers,
- food for work crews,
- wood,
- stone,
- iron,
- nearest depot/supply source,
- construction efficiency,
- damaged/interrupted route state.

Acceptance:
- prototype kan køre med simple QA-defaults,
- resource hooks kan aktiveres senere uden at omskrive visual framework,
- manglende resources kan sætte project i paused state.

## B-369 — Construction/living-map QA and final v00.00.11 gate
**Status:** PLANLAGT

B-369 overtager rollen som endelig v00.00.11 promotion-gate efter tilføjelsen af construction/living-map work package.

Acceptance:
1. Unity 6.6 compiler: 0 blocking errors.
2. Existing B-359 systems/polish gate er bestået eller eksplicit deferred efter dokumenteret vurdering.
3. Mindst én barracks og én farm kan gennemføre stage progression over campaign-tid.
4. Mindst ét road project og ét rail project kan vise staged edge construction.
5. Pause stopper construction progression.
6. Campaign speed påvirker progression gennem campaign-time, ikke særskilt animation-clock.
7. Visual stages kan rekonstrueres fra project state.
8. Worker loops kan LOD-reduceres efter zoom.
9. Construction må ikke ændre tactical AI eller tactical formation/navigation.
10. Construction diagnostics viser ID, type, location/edge, progress, stage, ETA og paused/completed state.

Når B-369 er godkendt, kan næste campaign-version oprettes som `v00.00.12`.

## Visual direction

Construction skal læses som små levende dioramaer på strategikortet, ikke som et city-builder-spil inde i campaign-laget.

Animationerne skal være diskrete:
- langsom aktivitet,
- små carts/arbejdere,
- periodiske hammer/dig loops,
- gradvis stage progression,
- ingen konstant hektisk bevægelse.

Det vigtigste er, at spilleren ved et blik kan se: **her sker der noget, og verden ændrer sig med tiden**.
