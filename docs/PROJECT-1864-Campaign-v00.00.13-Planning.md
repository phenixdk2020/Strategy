# PROJECT 1864 — Campaign v00.00.13 Planning

## Status

- **Version:** `v00.00.13` PLANNED
- **Active campaign version remains:** `v00.00.11 WORK`
- **v00.00.12 remains:** PLANNED, B-370–B-409
- **Entry condition for v00.00.13:** v00.00.12/B-409 must pass before v00.00.13 becomes active.
- **Planned backlog:** B-410–B-469
- **Milestone name:** `3D STRATEGIC WORLD & GEOSPATIAL FOUNDATION`
- **Tactical isolation:** this milestone must not modify tactical Officer AI, battlefield navigation or tactical formation steering.

## Milestone purpose

v00.00.13 turns the campaign map from a mostly flat projected strategic board into a layered, terrain-aware 3D strategic world. The version establishes a durable geospatial contract, real route geometry, elevation/terrain, hydrology, 3D infrastructure, settlements, state-driven moving entities, semantic overlays, weather/lighting and performance/streaming foundations.

The authoritative campaign simulation must remain independent of presentation scale. Unity world units are render coordinates, not authoritative kilometers. Strategic movement, railway transit, logistics and ETA use geospatial coordinates plus explicit route geometry and route state.

## Planned work packages

### B-410–B-419 — Map Scale, Geodesy & Distance Contract

Authoritative geographic extent, projection, coordinate service, decoupled Unity scale, elevation source contract, route polyline distance, slope/grade, distance ruler, precision/origin handling and map-scale validation.

Source: `docs/backlog/B-410-CAMPAIGN-V013-MAP-SCALE-GEODESY.md`

### B-420–B-429 — 3D Terrain, Hydrology & Land Cover

Terrain heightfield/mesh, sea/coast, rivers, lakes/wetlands, terrain materials, forests, agricultural land, terrain LOD, cuts/embankments and terrain/hydrology QA.

Source: `docs/backlog/B-420-CAMPAIGN-V013-TERRAIN-HYDROLOGY.md`

### B-430–B-439 — 3D Infrastructure & Settlements

Road and rail splines, bridges/tunnels/ferries, stations/yards, ports, settlement footprints, facilities, forts, telegraph lines and infrastructure QA.

Source: `docs/backlog/B-430-CAMPAIGN-V013-INFRASTRUCTURE-SETTLEMENTS-3D.md`

### B-440–B-449 — Strategic Entities & Living World in 3D

Formation representation, marching columns, troop trains, freight trains, ships/ferries, convoys/couriers/scouts, camps, construction workers and war-aftermath/civilian visual state.

Source: `docs/backlog/B-440-CAMPAIGN-V013-STRATEGIC-ENTITIES-LIVING-WORLD-3D.md`

### B-450–B-459 — Map Layers, Weather, Camera & Readability

Layer manager, political/logistics/infrastructure/intelligence overlays, weather, day/night/season, terrain-aware camera, depth-aware labels and player-selectable readability presets.

Source: `docs/backlog/B-450-CAMPAIGN-V013-LAYERS-WEATHER-CAMERA.md`

### B-460–B-469 — Streaming, Performance, Tooling & Final QA

Chunk streaming, pooling/instancing, culling/LOD budgets, save/load spatial state, deterministic visual reconstruction, GIS/data import tooling, editor/debug tools, stress tests, full regression and the final B-469 promotion gate.

Source: `docs/backlog/B-460-CAMPAIGN-V013-STREAMING-TOOLING-QA.md`

## 3D map principle

The map is layered, but not as a stack of unrelated flat planes. Most world layers conform to one common geospatial terrain surface:

1. Geospatial coordinate/reference layer.
2. Elevation/terrain surface.
3. Hydrology/coast/water.
4. Land cover/vegetation/agriculture.
5. Infrastructure: roads, rail, bridges, ports, telegraph.
6. Settlements/facilities/fortifications.
7. Strategic simulation entities: formations, trains, ships, convoys.
8. Living-world visuals: workers, camps, smoke, traffic, aftermath.
9. Political/logistics/intelligence overlays.
10. Weather/lighting/atmosphere.
11. UI, labels, selection, debug and measurement.

These are design/data/render layers. Only the subsets that benefit from collision filtering, raycasting or culling should become actual Unity `LayerMask` layers.

## Distance and visual-scale rule

Current prototype coverage uses latitude 47.0–71.5 N and longitude 4.0–32.5 E. At the midpoint this bounding rectangle is roughly 2,724 km north-south and 1,620 km east-west, while the current Unity presentation is 520 x 620 units. Therefore Unity unit distance must never be treated as real kilometers.

Authoritative strategic distance is:

`RouteDistanceKm = sum(geodesic/polyline distance of traversed route geometry)`

Movement time then applies mobility, road class, slope, terrain, weather, crossings, congestion, march/rest policy and other campaign state. Visual objects interpolate along the same route progress but do not determine the result.

## Vertical scale rule

Real elevation is stored separately from visual elevation. A configurable vertical exaggeration may be used for strategic readability, especially across Denmark/northern Germany where real relief is visually subtle at continental scale. Movement/slope calculations must use true terrain data or explicitly documented gameplay slope data, not the exaggerated render height.

## Relationship to v00.00.12

v00.00.12 defines the deeper simulation behind infrastructure, transport, society and formation mobility. v00.00.13 supplies the 3D geospatial world those systems operate on.

Examples:

- B-400 mobility uses real route distance and slope/road state from B-410+.
- troop-train state from B-381 follows a true 3D rail spline in B-431/B-442.
- rolling stock remains limited even if a rail line is visually present.
- construction state from B-360+ determines staged 3D assets, never the reverse.
- bridge destruction changes route state; the bridge mesh only reflects it.
- weather can alter mobility through campaign state while visual rain/snow is separately scalable.

## Promotion principle

Planning files may exist now, but v00.00.13 must not become the active campaign version until v00.00.12 passes B-409. B-469 is the planned final v00.00.13 gate.