# B-380–B-389 — Campaign v00.00.12 Logistics & Transport

## Status

**Target version:** `v00.00.12`  
**State:** PLANLAGT  
**Depends on:** explicit infrastructure/edge data and B-369 living-world foundation  
**Tactical AI:** outside scope.

## Retning

Målet er at gøre transport og supply til synlige, kapacitetsbegrænsede strategiske systemer. Tog, vogne og skibe skal så vidt muligt afspejle reel transport-state i stedet for kun at være pynt.

Jernbanetransport må aldrig være gratis teleportation. En formation der flyttes med tog skal gennem et faktisk lifecycle med tildeling af materiel, loading, transit langs den valgte rail-route, eventuelle stop/ventetid og unloading. Det visuelle tog på campaign-kortet følger samme authoritative transport-state hele vejen og må culles/LOD-reduceres uden at ændre transportresultatet.

## B-380 — Strategic transport asset pools og rolling stock
**Status:** PLANLAGT

Nationer/regioner får transportkapacitet som persistente pools: heste, vogne, lokomotiver, passagervogne, godsvogne, militære flatcars/specialvogne og transportskibe.

Rolling stock er begrænset og kan senere produceres, købes/importeres, repareres, beskadiges, erobres eller gå tabt. Rail infrastructure og rolling stock er to separate ressourcer: skinner alene giver ikke ubegrænset transportkapacitet.

Acceptance:
- assets kan allokeres uden negative pools,
- unavailable/allocated/damaged assets reducerer ledig kapacitet,
- locomotive og rail-car pools er separate,
- troppetransport og godstransport konkurrerer om samme relevante materiel,
- pool-state er persistent og kan diagnosticeres,
- hooks findes til production/repair/import/capture uden at hardcode historiske tal,
- systemet kan køre med QA-defaults hvis historiske data mangler.

## B-381 — Troop-train lifecycle, rail capacity, loading og route-following visual
**Status:** PLANLAGT

Rail movement får kapacitet, materiel-allokering, loading/unloading delay og simple traffic slots.

Lifecycle:
`WAIT FOR STOCK -> ASSEMBLE TRAIN -> LOAD -> DEPART -> IN TRANSIT -> STATION/EDGE WAIT -> ARRIVE -> UNLOAD -> RELEASE STOCK`.

Når en formation flyttes med tog, skal et troop-train visual følge den faktiske rail-route fra startstation til slutstation. Togets position beregnes fra den samme transport progress som simulationen; det er ikke en løs ambient-animation.

Acceptance:
- formation kan ikke teleporteres ved valg af rail,
- nok lokomotiv-/vognkapacitet skal være allokeret før departure,
- boarding/loading og destination unloading tager campaign time,
- edge/station capacity kan skabe kø,
- toget følger hele den authoritative rail-route visuelt,
- pause stopper simulationens transit progress,
- campaign speed påvirker transport gennem campaign-time,
- visual train kan LOD-culles uden state loss,
- disrupted rail edge stopper eller rerouter transport sikkert,
- rolling stock frigives korrekt efter unloading eller failure handling.

## B-382 — Wagon convoy system
**Status:** PLANLAGT

Strategiske supply- og transportkolonner kan oprettes som convoy state mellem depoter/formationer.

Acceptance:
- convoy har origin, destination, cargo, capacity, route og ETA,
- convoy kan forsinkes/stoppes ved route disruption,
- arrival overfører cargo én gang idempotent,
- visuel wagon column må culles uden gameplayeffekt.

## B-383 — Sea transport and embarkation capacity
**Status:** PLANLAGT

Port-to-port transport kræver gyldige havne, transportkapacitet og embark/disembark state.

Acceptance:
- formation/cargo kan ikke krydse sea-link uden transport eligibility,
- embark/disembark tager tid,
- port capacity kan begrænse throughput,
- disrupted/blockaded route kan invalidere transfer.

## B-384 — Depot-to-depot stock transfer
**Status:** PLANLAGT

Stock kan flyttes mellem depoter via gyldige transportnet.

Acceptance:
- transfer bruger route og capacity,
- stock fjernes/ankommer deterministisk uden duplication,
- transfer kan være in-transit state,
- diagnostics viser cargo, route, ETA og bottleneck.

## B-385 — Supply demand and allocation priorities
**Status:** PLANLAGT

Formationer/facilities kan have supply demand og prioritet.

Acceptance:
- mindst Low/Normal/High/Critical priority,
- shortages fordeles efter eksplicit regel frem for tilfældig rækkefølge,
- player/AI kan senere ændre priority,
- allocation kan forklares i diagnostics.

## B-386 — Congestion and route capacity
**Status:** PLANLAGT

Veje, rail og ports kan blive belastet af samtidigt flow.

Acceptance:
- edge har usable capacity,
- flere samtidige movements/convoys kan øge delay,
- rail traffic slots kan belastes af troop trains og freight trains samtidigt,
- congestion kan vises som overlay,
- ingen permanent deadlock uden diagnostic/fallback.

## B-387 — Interdiction, sabotage and route disruption
**Status:** PLANLAGT

Cavalry raids, battle control, sabotage og ødelagt infrastructure kan forringe transportlinks.

Acceptance:
- disruption har cause, severity og duration/repair state,
- affected movements stoppes eller reroutes sikkert,
- fog of war respekteres i player UI,
- repair kobles til B-373 construction/repair.

## B-388 — Logistics efficiency, loss and attrition hooks
**Status:** PLANLAGT

Lang distance, dårlige routes og disruption kan reducere effektiv delivered supply.

Acceptance:
- losses/efficiency er bounded og forklarlige,
- ingen skjult direkte combat cheat,
- systemet kan slås fra i QA,
- tactical handoff kan senere modtage faktisk supply/fatigue state.

## B-389 — Logistics overlay, planner and QA
**Status:** PLANLAGT

Samlet strategic logistics view.

Acceptance:
- depots, active transfers, convoys, troop trains, rail/sea capacity og bottlenecks kan vises,
- rolling-stock availability/allocation kan vises,
- selected formation kan vise current supply source og route,
- player kan se hvorfor en formation er undersupplied eller venter på tog,
- deterministic QA tester road, rail, sea, rolling-stock shortage, disruption og reroute.
