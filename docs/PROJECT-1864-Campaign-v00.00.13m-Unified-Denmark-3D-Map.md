# PROJECT 1864 Campaign v00.00.13m — Unified Denmark 3D Map

Status: DEV / MAP-ONLY / afventer Unity compile + runtime QA.

## Problem v13l exposed

The previous presentation stacked three independent visual systems: v13j smooth DEM terrain, v13k live map tiles and v13l close-detail tiles. At overview height the live map covered only the active tile neighborhood while the underlying generated terrain remained visible outside it. The result was the rectangular/trapezoid map patch visible in QA rather than one coherent 3D map of Denmark.

## v13m architecture

v13m introduces one visible map owner.

- The validated v13j MeshCollider is retained only as elevation reference.
- v13j, v13k and v13l visual renderers/status overlays are disabled after v13m initialization.
- A continuous 520x340 mesh covers the complete validated Denmark envelope.
- Land vertices sample the v13j DEM collider; water vertices are held at sea level.
- A z8 cartographic atlas is stitched across the complete mesh using Web-Mercator-correct UV coordinates.
- The atlas is permanent at all camera heights, so the renderer never exposes raw legacy terrain while detail tiles are loading.
- Medium/close zoom may add 5x5 z10-z13 detail tiles over the base atlas.
- Aalborg Kaserne and Aarhus Gård are owned by v13m as compact close-zoom terrain-anchored landmarks.

## Presentation rule

There must be exactly one visible campaign map stack during normal v13m QA. Old road/ferry debug lines, prototype city blocks and prior landmark renderers remain suppressed.

## Simulation isolation

v13m changes presentation only. Strategic lat/lon, route distance, ETA, campaign time, construction state, logistics and tactical code are not changed.

## Basemap note

OpenStreetMap Standard remains a temporary present-day development basemap and requires visible attribution. It is not an authoritative source for 1864 roads, settlements or infrastructure. The unified renderer is intentionally provider-agnostic enough to replace the temporary atlas later with approved historical cartography without changing the strategic simulation.
