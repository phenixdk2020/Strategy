# PROJECT 1864 — Campaign v00.00.13 3D DEV — Implementation Notes

## Status

**Branch:** `work/v00.00.13-3d-campaign-map`  
**State:** `IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA`  
**Promotion:** none. This development branch does not bypass the documented v00.00.12/B-409 dependency.

## Implemented in first 3D slice

- procedural campaign terrain mesh over the existing geospatial projection,
- coarse land/sea mask based on the existing country-outline geometry,
- stronger relief for Norway/Sweden and lower relief for Denmark/northern Germany,
- hydrology root with the existing sea base,
- architecture roots `L1` through `L10`,
- existing strategic links resampled over the rendered terrain surface,
- probable rail links visually separated from roads,
- country outlines lifted onto the 3D surface,
- node markers and moving formation tokens terrain-snapped in `LateUpdate`,
- small 3D settlement miniatures around campaign nodes,
- station/depot/port/fortification miniatures from existing node state,
- terrain-aware campaign camera,
- `PageUp/PageDown` camera pitch,
- campaign-time driven sunlight, ambient light and fog,
- layer visibility UI: F6 infrastructure, F7 settlements, F8 living world, F9 overlays, F10 hydrology,
- staged barracks construction QA project near Aalborg,
- staged farm construction QA project near Aarhus,
- construction stages follow `CampaignSession.CurrentDateTime`,
- two simple worker loops per active construction project,
- version overlay changed to `PROJECT 1864 CAMPAIGN | v00.00.13 3D DEV`.

## Important prototype limits

This is the first playable/renderable 3D slice, not the finished B-410–B-469 milestone.

Not yet authoritative/final:

- terrain is procedural/coarse, not imported DEM/GIS elevation,
- coastlines use the existing coarse outline data,
- strategic links still originate from the current node-to-node graph and are not yet historical route polylines,
- road/rail classification is still partly inferred from endpoint data,
- construction projects are explicit QA prototypes and are not yet connected to the full economy/resource construction queue,
- no production troop-train lifecycle is implemented in this slice,
- no chunk streaming/floating origin yet,
- no final land-cover/forest/agriculture system yet,
- no final weather simulation layer yet.

## Unity QA checklist

1. Close any older Unity instance using the project.
2. Checkout `work/v00.00.13-3d-campaign-map` in the Strategy test working copy.
3. Open the project in Unity 6.6.
4. Confirm **0 blocking compiler errors** before entering Play mode.
5. Open `CampaignMap` and press Play.
6. Confirm the top-left overlay says `v00.00.13 3D DEV`.
7. Confirm terrain has visible relief and Norway/Sweden are substantially more elevated than Denmark.
8. Confirm sea remains below the land surface.
9. Confirm node markers and formation tokens do not remain at the old flat Y level.
10. Confirm roads/rail links follow terrain rather than cutting through hills.
11. Confirm settlement miniatures appear around nodes.
12. Confirm F6–F10 toggle the documented map layers without affecting simulation state.
13. Confirm PageUp/PageDown changes camera pitch; Q/E still rotates and mouse wheel still zooms.
14. Let campaign time advance and confirm the Aalborg barracks and Aarhus farm change construction stages over time.
15. Pause campaign time and confirm construction stage progress and worker movement stop.
16. Resume/x5/x20 and confirm progress follows campaign time.
17. Confirm tactical battle scripts/Officer AI/navigation have not been modified by this branch.

## Expected diagnostics

`CAMPAIGN-V013|3DWorldBuilt=True|...`

and stage changes such as:

`CAMPAIGN-V013-CONSTRUCTION|Id=QA-BARRACKS-AALBORG|Type=Barracks|Progress=...|Stage=...`

## QA rule

Any compile/runtime defect found in this slice is fixed on the v00.00.13 3D development branch before it is treated as a candidate for a future campaign promotion. Visual success alone is not a promotion gate.
