# B-430–B-439 — Campaign v00.00.13 3D Infrastructure & Settlements

## Status

**Target version:** `v00.00.13`  
**State:** PLANLAGT  
**Depends on:** B-410 geodesy + B-420 terrain.

## B-430 — 3D road spline network
**Status:** PLANLAGT

Road edges become geographic splines draped/fitted to terrain.

Acceptance:
- explicit polyline geometry,
- road class/condition/capacity stored as data,
- spline renderer follows terrain safely,
- route movement uses edge data, not rendered mesh length,
- construction/repair stages can alter visual road state.

## B-431 — 3D railway spline network
**Status:** PLANLAGT

Rail edges become proper 3D routes for troop/freight trains.

Acceptance:
- explicit geographic rail geometry,
- track state/capacity/ownership represented in data,
- rail follows grade constraints from B-416,
- train visuals can travel continuously along full spline,
- unfinished/destroyed rail is visibly and functionally distinct.

## B-432 — Bridges, tunnels, causeways and ferries
**Status:** PLANLAGT

Crossings become first-class route assets.

Acceptance:
- crossing has stable ID/type/capacity/condition,
- bridge geometry references road/rail edge and hydrology,
- tunnel/cut data can bypass terrain where historically/data appropriate,
- ferry terminals connect land and sea/ferry transport lifecycle,
- destroyed crossing can invalidate/reroute movement.

## B-433 — Stations, rail yards and loading facilities
**Status:** PLANLAGT

Rail transport requires visible/functional station infrastructure.

Acceptance:
- station has capacity/loading hooks,
- yard size can reflect level/importance,
- B-381 troop-train lifecycle can assemble/load/unload there,
- congestion can be visualized without spawning unlimited rolling stock,
- stations are selectable at appropriate zoom.

## B-434 — Ports, harbors and quays
**Status:** PLANLAGT

Ports gain physical strategic-map representation.

Acceptance:
- port footprint tied to coastal node/location,
- embark/disembark points defined,
- quay/harbor capacity hooks supported,
- ships/ferries can approach/depart along defined lanes,
- blockade/damage/occupation can alter visible activity.

## B-435 — Settlement footprint and urban scale model
**Status:** PLANLAGT

Cities/towns are no longer identical node cylinders.

Acceptance:
- location has settlement class/footprint/importance,
- major cities visibly larger than minor towns,
- footprint follows terrain/coast constraints,
- population/economic size can later influence visual density,
- footprint is not a literal per-building population simulation.

## B-436 — Facilities and strategic buildings
**Status:** PLANLAGT

Place state-driven facilities inside/around settlements.

First set:
- barracks,
- depot/warehouse,
- factory/workshop,
- farm/rural facility,
- hospital,
- station,
- port facility,
- telegraph office,
- recruitment/training facility.

Acceptance:
- facility ID/state is authoritative,
- visual prefab/stage is derived,
- B-360 construction framework can build/repair them,
- facilities use semantic LOD/icons at far zoom.

## B-437 — Fortifications and strategic defensive works
**Status:** PLANLAGT

Forts, batteries, redoubts and fieldworks become visible 3D state.

Acceptance:
- fortification has level/type/controller/condition,
- terrain placement respects local slope/position,
- construction/damage stages supported,
- map representation can hand off relevant context to tactical battles later,
- no tactical fort AI introduced here.

## B-438 — Telegraph and communication infrastructure
**Status:** PLANLAGT

Telegraph network becomes visible infrastructure with selective rendering.

Acceptance:
- line/station data distinct from roads/rail,
- command/communication systems can query connectivity,
- cut/damaged line state supported,
- far zoom uses overlay rather than thousands of physical poles,
- near zoom may show sparse pole/wire visuals for living-world effect.

## B-439 — 3D infrastructure/settlement QA gate
**Status:** PLANLAGT

Acceptance:
1. representative road and rail corridors follow terrain,
2. bridges/crossings align with hydrology,
3. stations and ports can host transport lifecycle visuals,
4. settlement size differences are visible,
5. facilities/fortifications reconstruct from state,
6. destroyed/incomplete infrastructure is not treated as operational,
7. infrastructure layers can be independently toggled/debugged,
8. route distance/ETA remains geospatial and independent of mesh tessellation.