# B-460–B-469 — Campaign v00.00.13 Streaming, Performance, Tooling & QA

## Status

**Target version:** `v00.00.13`  
**State:** PLANLAGT

## B-460 — Geographic chunk/sector streaming
**Status:** PLANLAGT

Split the strategic world into manageable spatial chunks.

Acceptance:
- terrain/infrastructure/ambient content can load by visible/relevant chunk,
- canonical campaign state remains loaded independently as required,
- chunk unload never deletes simulation state,
- neighboring chunks have seam-safe terrain/infrastructure,
- selected/followed entity protects required chunks from premature unload.

## B-461 — Pooling, instancing and reusable ambient actors
**Status:** PLANLAGT

Avoid repeated allocation/spawn costs.

Acceptance:
- trees/vegetation use instancing where appropriate,
- workers/wagons/smoke/lights can use pools,
- train/ship visuals reuse components where possible,
- pool exhaustion has safe fallback,
- no unbounded ambient GameObject growth over long campaign time.

## B-462 — Frustum, distance, occlusion and semantic LOD budgets
**Status:** PLANLAGT

Define explicit rendering budgets by zoom/distance.

Acceptance:
- off-screen content culled,
- near/medium/far representations defined for key categories,
- selected/important entities may receive priority budget,
- visual detail can degrade before simulation tick rate,
- runtime diagnostics show active counts by category/LOD.

## B-463 — Save/load spatial reconstruction
**Status:** PLANLAGT

3D map state must rebuild from campaign data.

Acceptance:
- no requirement to serialize every renderer/GameObject transform,
- formations/trains/convoys reconstruct from IDs + route progress,
- construction rebuilds from project progress/stage,
- damaged infrastructure/aftermath rebuilds from state/events,
- visual chunk load order does not alter authoritative result.

## B-464 — Deterministic visual reconstruction and replay hooks
**Status:** PLANLAGT

Where random visual variation is used, it should be stable enough for QA/save-load.

Acceptance:
- deterministic seeds for settlement/field/vegetation placement where practical,
- ambient phase randomness can be non-authoritative but bounded,
- same save reconstructs equivalent strategic landmarks/routes,
- visual nondeterminism cannot alter simulation state.

## B-465 — GIS/geodata import pipeline
**Status:** PLANLAGT

Create tooling to ingest/normalize external geographic datasets later.

Target data classes:
- elevation/DEM,
- coastline/water,
- rivers,
- land cover,
- roads,
- rail,
- settlements,
- historical infrastructure corrections.

Acceptance:
- import output converted to project-owned normalized data,
- source/provenance/version metadata supported,
- coordinate conversion centralized,
- import can be rerun without manual scene editing,
- historical overrides can supersede modern geometry without destroying base tooling.

## B-466 — Campaign map editor/debug tooling
**Status:** PLANLAGT

Developer tools for inspecting/editing spatial data.

Acceptance:
- inspect node/edge/polyline IDs,
- show lat/lon/elevation/distance/slope,
- validate broken route geometry,
- toggle layer debug colors,
- inspect chunk/LOD/pool counts,
- export diagnostics without changing campaign state.

## B-467 — Large-map stress profiles
**Status:** PLANLAGT

Synthetic scalability tests beyond current canon.

Profiles:
- 250 locations,
- 500 locations,
- 100 moving formations,
- 50 simultaneous trains/convoys/ships,
- 1,000 ambient actors/instances equivalent,
- dense labels/overlays.

Acceptance:
- synthetic content clearly separated from canon,
- frame time, memory, object counts and simulation tick metrics captured,
- camera/navigation remains responsive,
- stress failures identify category/budget rather than silently degrading state.

## B-468 — Full 3D strategic-world regression presets
**Status:** PLANLAGT

Deterministic scenarios covering the complete 3D world stack.

Minimum presets:
1. Denmark march across road/terrain with normal vs forced march ETA.
2. Troop train follows multi-node rail route with loading/unloading.
3. Rail disruption stops/reroutes train safely.
4. Bridge destruction changes route availability.
5. Supply convoy and depot flow visible in logistics layer.
6. Construction progresses through 3D stages.
7. Night/weather transition while movement continues correctly.
8. Fog of war hides enemy living-world information.
9. Save/load during active movement reconstructs same state.
10. Layer/preset switching does not change simulation.

## B-469 — Final Campaign v00.00.13 promotion gate
**Status:** PLANLAGT

B-469 is the planned final gate for `v00.00.13`.

Acceptance:
1. Unity 6.6 compiler: 0 blocking errors.
2. v00.00.12/B-409 baseline preserved or explicitly migrated with documented tests.
3. Authoritative geospatial/map-scale contract validated.
4. Unity world scale can change without changing strategic km/ETA.
5. 3D terrain/elevation/hydrology render across canonical coverage.
6. Roads/rail/crossings/settlements align with terrain.
7. Route polylines provide authoritative route km.
8. Formation march uses B-400 mobility and real route data.
9. Troop train follows full 3D rail route and limited rolling-stock lifecycle.
10. Living-world/state visuals reconstruct correctly after save/load.
11. Political/logistics/intelligence layers remain readable on 3D terrain.
12. Weather/day-night visuals are scalable and do not become simulation state.
13. Camera/selection/labels work across relief and layer presets.
14. Chunking/LOD/pooling show no unbounded growth in long-run QA.
15. Synthetic large-map stress test completes with captured metrics.
16. No tactical Officer AI/navigation/formation steering files changed by this milestone.
17. All deferred items are explicitly recorded before promotion.

When B-469 passes, the next campaign milestone may be opened.