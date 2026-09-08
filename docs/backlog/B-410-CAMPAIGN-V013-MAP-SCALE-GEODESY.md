# B-410–B-419 — Campaign v00.00.13 Map Scale, Geodesy & Distance

## Status

**Target version:** `v00.00.13`  
**State:** PLANLAGT  
**Depends on:** v00.00.12/B-409  
**Tactical AI:** outside scope.

## Core rule

Unity-space is visual. Geographic coordinates and explicit route geometry are authoritative for strategic distance, movement and ETA.

## B-410 — Authoritative map extent and projection contract
**Status:** PLANLAGT

Lock the canonical geographic bounds and projection abstraction.

Acceptance:
- canonical lat/lon bounds documented,
- projection is replaceable behind one service,
- data remains stored in geographic coordinates,
- projection distortion is measured at representative locations,
- visual aspect ratio may not redefine real distance.

## B-411 — Geospatial coordinate service
**Status:** PLANLAGT

Central service for LatLon <-> projected map/world coordinates.

Acceptance:
- deterministic forward/inverse transform,
- node/route/terrain/settlement layers use the same service,
- no duplicate ad-hoc coordinate formulas,
- diagnostics can show lat/lon and projected/world position.

## B-412 — Unity scale decoupling
**Status:** PLANLAGT

Formalize visual scale independently from kilometers.

Acceptance:
- changing `MapWidth/MapDepth` does not alter travel time,
- strategic ETA never uses raw `Vector3.Distance` as km,
- visual scale factor is configurable,
- train/formation visuals interpolate by normalized route progress.

## B-413 — Elevation data contract
**Status:** PLANLAGT

Define canonical elevation storage in meters above reference level.

Acceptance:
- true elevation stored separately from render Y,
- elevation source can be replaced without changing movement API,
- sea level/reference datum documented,
- missing-data fallback is explicit and diagnosable.

## B-414 — Visual vertical exaggeration
**Status:** PLANLAGT

Allow strategic relief exaggeration without corrupting simulation.

Acceptance:
- separate `ElevationMeters` and visual Y,
- configurable exaggeration factor,
- slope/mobility calculations do not use exaggerated height,
- zero/low exaggeration mode available for QA.

## B-415 — Route geometry and authoritative edge distance
**Status:** PLANLAGT

Strategic edges receive explicit geographic polylines instead of straight screen-space links.

Acceptance:
- road/rail/sea edge can contain ordered geographic vertices,
- `DistanceKm` derives from geodesic/polyline geometry,
- route distance is sum of traversed edge geometry,
- straight node-to-node Haversine remains only fallback/diagnostic,
- geometry can later be historically refined without changing formation state IDs.

## B-416 — Slope, grade and passability model
**Status:** PLANLAGT

Compute terrain grade along explicit route geometry.

Acceptance:
- grade derived from true elevation,
- road/rail slope can differ from raw terrain slope where cuts/embankments exist,
- steepness affects appropriate mobility classes through data-driven modifiers,
- impassable states are explicit, not gigantic hidden penalties.

## B-417 — Map ruler and route-distance measurement tool
**Status:** PLANLAGT

Player/QA can measure geographic distance.

Acceptance:
- point-to-point geodesic measurement,
- selected route distance measurement,
- UI clearly distinguishes straight-line km from route km,
- results independent of zoom and Unity scale.

## B-418 — World origin, precision and chunk coordinates
**Status:** PLANLAGT

Prepare the 3D map for larger extents without float precision drift.

Acceptance:
- stable geographic coordinates remain authoritative,
- world/chunk local coordinates are derived presentation state,
- design supports floating origin or local chunk origins if required,
- saved formation/route state never depends on floating-origin offsets.

## B-419 — Geospatial/map-scale validator
**Status:** PLANLAGT

Automatic validation of the geographic contract.

Acceptance:
- bounds violations reported,
- invalid lat/lon/elevation reported,
- route polyline distance sanity checks,
- projection round-trip tolerance measured,
- representative known routes can be regression-tested,
- no strategic movement calculation references Unity world-distance as km.