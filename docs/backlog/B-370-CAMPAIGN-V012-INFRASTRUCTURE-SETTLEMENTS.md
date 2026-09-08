# B-370–B-379 — Campaign v00.00.12 Infrastructure & Settlements

## Status

**Target version:** `v00.00.12`  
**State:** PLANLAGT  
**Depends on:** v00.00.11 promotion gate B-369  
**Tactical AI:** outside scope; må ikke ændres af dette work package.

## Retning

v00.00.12 skal gøre campaign-kortets steder mere funktionelle. B-360–B-369 leverer living-world/construction-retningen; B-370–B-379 gør byer, depoter, broer, telegraf, fæstninger og andre strategiske anlæg til egentlig persistent campaign-state.

## B-370 — Location development slots
**Status:** PLANLAGT

Hver strategisk location kan have datadrevne development slots/områder frem for hardcodede specialbygninger.

Acceptance:
- node kan have flere samtidige facility slots,
- slot har stable ID, type, level, owner/controller og state,
- UI kan læse slots uden by-specifik kode,
- save/load kan rekonstruere slot-state.

## B-371 — Facility catalog and upgrade levels
**Status:** PLANLAGT

Fælles facility catalog for fx barracks, farm, depot, workshop, rail station, port facility, hospital og fortification.

Acceptance:
- facility definitions er data frem for switch-kode i UI,
- mindst tre levels understøttes hvor relevant,
- level kan påvirke kapacitet/produktion gennem hooks,
- visuals kan vælge stage/variant ud fra facility type og level.

## B-372 — Construction queue and priority
**Status:** PLANLAGT

Locations kan have en kø af construction/upgrade projects oven på B-360 construction framework.

Acceptance:
- projekter kan køes, pauses, annulleres og prioriteres,
- kun tilladt antal samtidige projekter bruger aktiv workforce,
- annullering håndterer reserverede resources deterministisk,
- queue kan inspiceres i UI/diagnostics.

## B-373 — Repair and reconstruction
**Status:** PLANLAGT

Infrastruktur og facilities skal kunne blive damaged og repareres uden at blive slettet og genoprettet som nye objekter.

Acceptance:
- damage state er persistent,
- repair er et construction project med ETA,
- damaged level kan reducere kapacitet,
- visual state viser repair/ruin uden at blive authoritative.

## B-374 — Bridges, crossings and pontoon projects
**Status:** PLANLAGT

Strategiske crossings får explicit state for bridge, ferry, ford eller temporary pontoon.

Acceptance:
- crossing type er edge/node data,
- ødelagt bridge kan blokere eller kraftigt reducere movement,
- engineers kan senere starte pontoon/repair project,
- route/pathfinding bruger kun completed/usable crossing state.

## B-375 — Telegraph network foundation
**Status:** PLANLAGT

Telegraf bliver et strategisk kommunikationsnet koblet til locations og edges.

Acceptance:
- telegraph station og line-state kan repræsenteres,
- connected HQ/location kan få communication-speed hook,
- cut/damaged line mister bonus,
- kortet kan vise telegraph overlay uden at afsløre ukendt fjendestate.

## B-376 — Depot and warehouse expansion
**Status:** PLANLAGT

Depoter får fysisk/logisk capacity og kan udbygges.

Acceptance:
- depot har storage capacity og throughput hooks,
- upgrade kan øge capacity/throughput,
- destroyed/damaged depot reducerer effekt,
- living-world visuals kan afspejle aktivitet uden at styre stock.

## B-377 — Strategic fortification projects
**Status:** PLANLAGT

Fæstninger, skanser og større fieldworks kan være persistent campaign projects.

Acceptance:
- fortification level/state kan lagres på node/område,
- build/repair progress følger campaign time,
- tactical handoff kan senere modtage fortification context,
- completed fortification må ikke automatisk generere urealistisk tactical geometry uden særskilt mapperregel.

## B-378 — Settlement growth, damage and occupation visuals
**Status:** PLANLAGT

Locations kan visuelt ændre karakter ved development, damage og occupation.

Acceptance:
- growth/damage visuals afledes af state,
- control change kan skifte flag/checkpoints/activity,
- battle damage kan have recovery/repair lifetime,
- visual LOD følger B-369 performance-regler.

## B-379 — Infrastructure planner UI and QA
**Status:** PLANLAGT

Samlet UI til facilities, construction queue, crossings, telegraph, depots og fortifications.

Acceptance:
- selected location viser eksisterende facilities og projekter,
- player kan se cost, ETA, requirements og status,
- invalid project forklares med konkret reason,
- deterministic QA preset dækker build, pause, damage, repair og completion.
