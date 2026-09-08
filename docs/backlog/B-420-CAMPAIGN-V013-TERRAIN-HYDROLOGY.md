# B-420–B-429 — Campaign v00.00.13 3D Terrain, Hydrology & Land Cover

## Status

**Target version:** `v00.00.13`  
**State:** PLANLAGT  
**Depends on:** B-410–B-419 geospatial contract.

## B-420 — 3D terrain surface
**Status:** PLANLAGT

Replace/augment flat regional outlines with a real terrain surface driven by elevation data.

Acceptance:
- terrain covers canonical campaign bounds,
- node/world positions sample the same surface,
- terrain can be generated in chunks,
- visual vertical exaggeration is configurable,
- terrain generation does not modify strategic coordinates.

## B-421 — Coastline and sea surface
**Status:** PLANLAGT

Create stable coastline and sea geometry.

Acceptance:
- coastline follows geographic data rather than coarse hand-drawn outlines,
- sea surface has consistent reference level,
- land/sea masks support ports/ferries/naval overlays,
- coast LOD does not open visible gaps at normal camera heights.

## B-422 — Rivers, streams and canals
**Status:** PLANLAGT

Hydrology becomes a real map layer.

Acceptance:
- major strategic rivers receive geographic polylines,
- river width/class data can vary,
- crossings/bridges reference river IDs,
- rivers can affect route eligibility/mobility without the renderer being authoritative,
- visual water follows terrain without severe z-fighting.

## B-423 — Lakes, wetlands and marshes
**Status:** PLANLAGT

Add inland water and wet ground where strategically relevant.

Acceptance:
- lakes/wetlands use geographic polygons or classified zones,
- movement modifiers reference data zones,
- visuals can LOD/cull independently,
- wetland state can interact with seasonal/weather hooks later.

## B-424 — Terrain material and land-cover classification
**Status:** PLANLAGT

Introduce data-driven land-cover zones.

Initial classes:
- urban/built-up,
- cultivated/agriculture,
- grass/open,
- forest,
- heath/scrub,
- wetland,
- mountain/rock,
- coastal/sand.

Acceptance:
- terrain material blends from classified data,
- gameplay terrain classification remains explicit data,
- material detail does not change movement rules,
- debug mode shows source classifications.

## B-425 — Forest and vegetation zones
**Status:** PLANLAGT

Forests gain 3D representation with strategic-scale density.

Acceptance:
- forest zones come from data polygons/masks,
- tree instances use LOD/instancing,
- far zoom collapses to cheap canopy/texture representation,
- forest renderer can be disabled without changing terrain movement state,
- selected routes remain readable through vegetation.

## B-426 — Agricultural fields and rural pattern
**Status:** PLANLAGT

Create low-cost fields around suitable settlements/agricultural regions.

Acceptance:
- field blocks are aggregated rather than individual farm simulation,
- seasonal appearance can change by campaign date,
- B-366 living-world agriculture can reuse the same zones,
- near-zoom farm construction can sit inside/adjacent to valid agricultural areas.

## B-427 — Terrain LOD and horizon quality
**Status:** PLANLAGT

Terrain must remain readable from close operational zoom to continental overview.

Acceptance:
- chunk/mesh LOD avoids major silhouette popping,
- normals/material detail scale with distance,
- far terrain is significantly cheaper than near terrain,
- no cracks between neighboring LOD chunks,
- selected node/route remains visually stable during LOD changes.

## B-428 — Cuts, embankments and terrain conformity
**Status:** PLANLAGT

Infrastructure may locally modify visual terrain without changing canonical elevation source.

Acceptance:
- roads/rail can use visual cuts/embankments where needed,
- bridges remain above water/terrain,
- track/road splines do not clip through terrain under normal conditions,
- visual modifications are rebuildable from infrastructure data.

## B-429 — Terrain/hydrology QA gate
**Status:** PLANLAGT

Acceptance:
1. terrain renders across full campaign bounds,
2. coastline/sea align without major gaps,
3. representative major rivers/lakes can be displayed,
4. nodes sit on valid terrain/water-aware positions,
5. land-cover debug mode works,
6. terrain LOD has no blocking cracks/pop errors,
7. visual elevation/exaggeration changes do not alter route km or movement ETA,
8. terrain/hydrology layers can be individually disabled for diagnostics.