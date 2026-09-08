# B-320–B-329 — Campaign v00.00.11 Movement & Logistics

## Scope

Strategisk movement og logistics foundation til campaign-laget. Opgaverne må implementeres trinvist under samme `v00.00.11 WORK`, men skal runtime-testes før promotion.

## B-320 — Explicit strategic edge data
**Status:** PLANLAGT

Erstat heuristisk linkklassifikation med eksplicit edge-data.

Acceptance:
- hvert link kan have type, distance, capacity, state og movement modifier,
- bidirectional og one-way modeller understøttes i datalaget,
- eksisterende 50-node net kan migreres uden tab.

## B-321 — Road movement classes
**Status:** PLANLAGT

Skeln mellem hovedvej, lokal vej og dårlig/sekundær forbindelse.

Acceptance:
- route cost bruger road class,
- UI kan vise road class,
- værdier kan senere historisk valideres uden kodeændring.

## B-322 — Rail movement foundation
**Status:** PLANLAGT

Rail bliver særskilt transportmode med eligibility og kapacitet.

Acceptance:
- kun rail-connected links kan vælges som rail,
- formation kan være rail-eligible eller ej,
- boarding/unboarding hooks findes,
- ingen gratis teleportation.

## B-323 — Ferry and sea-transfer foundation
**Status:** PLANLAGT

Sea/Ferry links får særskilt transfer state.

Acceptance:
- embark, transit og disembark kan repræsenteres,
- sea leg kan have ekstra delay,
- landformation kan ikke marchere direkte over vand uden gyldigt link.

## B-324 — Movement order queue
**Status:** PLANLAGT

Formation kan have mere end én planlagt strategic movement order.

Acceptance:
- waypoint queue kan ses og redigeres,
- næste destination er tydelig,
- cancel bevarer formationens nuværende gyldige state.

## B-325 — Formation march stance
**Status:** PLANLAGT

Strategisk movement får stance som normal march, forced march og rest.

Acceptance:
- stance påvirker movement speed hook,
- forced march opbygger fatigue hook,
- rest stopper/afbremser movement og understøtter recovery hooks.

## B-326 — Strategic fatigue model seed
**Status:** PLANLAGT

Formation får campaign fatigue separat fra tactical transient states.

Acceptance:
- fatigue lagres i campaign state,
- fatigue påvirkes af march duration/stance,
- tactical handoff kan modtage fatigue som input senere.

## B-327 — Supply-source association
**Status:** PLANLAGT

Formation kan identificere relevant friendly supply source/depot via netværket.

Acceptance:
- nearest depot er ikke kun euklidisk afstand men route-reachable,
- hostile-controlled links/nodes kan blokere kandidat,
- diagnostics viser valgt source og route distance.

## B-328 — Supply throughput prototype
**Status:** PLANLAGT

Links får prototype throughput/capacity til fremtidig supply flow.

Acceptance:
- depot -> formation route kan beregne bottleneck capacity,
- supply data ændrer endnu ikke ammunition automatisk uden eksplicit integration,
- diagnostics viser bottleneck edge.

## B-329 — Movement interruption and reroute
**Status:** PLANLAGT

En route skal kunne invalidere ved ændret control, battle/contact eller lukket link.

Acceptance:
- formation stopper sikkert ved sidste gyldige state,
- invalid route rapporteres tydeligt,
- spilleren/AI kan udstede ny route,
- ingen teleport eller state corruption.
