# PROJECT 1864 — Campaign v00.00.13c Denmark Cleanup

State: **HOTFIX IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA**

Branch: `work/v00.00.13c-denmark-cleanup`

This document is the designmanual supplement for the Denmark-cleanup pass. The authoritative strategic simulation remains the v00.00.11+/v13 campaign state model; v13c changes presentation/runtime isolation only.

## Runtime QA finding that changed the implementation

Video QA of the first v13c build exposed two coupled presentation defects rather than one simple terrain-smoothing issue:

1. `PrototypeBootstrap` was allowed to auto-run on `CampaignMap`, building the complete tactical battlefield on top of the strategic scene. That injected `Battlefield Ground`, tactical road/farm/trees/regiments, `BattleManager`, `PlayerCommander`, the second tactical time-control bar and the tactical helper banner.
2. The detailed Natural Earth Denmark mesh used the legacy Denmark-local projection while v11+ strategic nodes/routes/terrain use the broad Europe projection. The first v13c implementation incorrectly attempted to use that local-projection mesh as a mask against the broad global terrain and carved a coarse 112x136 mesh, producing stepped bands/walls.

The rejected destructive carving implementation is not the v13c rule anymore.

## B-13c-01 — Runtime isolation correction

- `GrandCampaignBootstrap.CampaignModeEnabled` is scene-aware: true only on `CampaignMap`.
- `PrototypeBootstrap` therefore cannot auto-build the tactical battlefield on `CampaignMap`.
- `PrototypeBattle` remains explicit and can still bootstrap tactical content when deliberately loaded.
- Campaign must show one campaign time-control system, not a second `BattleManager` control strip.

## B-13c-02 — Denmark projection correction

- Existing Natural Earth Denmark vertices/coastline points are converted from the legacy local render projection back to longitude/latitude with `CampaignGeoProjection.Unproject`.
- They are then projected into the broad campaign render system with `CampaignGeoProjection.Project3D(latitude, longitude, ...)`.
- Strategic WGS84 coordinates remain authoritative and unchanged.

## B-13c-03 — Terrain cleanup rule

- Do **not** carve the coarse global v13 terrain grid with the detailed Denmark coastline.
- For the Denmark-focus QA slice, the coarse global terrain renderer is hidden instead of destructively reshaped.
- The corrected detailed Denmark mesh becomes the visible Denmark land surface over the strategic sea.

## B-13c-04 — Denmark relief

- Denmark uses deliberately low-amplitude presentation relief.
- Relief is a visual proxy only, not claimed DEM elevation.
- Hills should read as gradual Danish terrain, not cliffs, plateaus or map walls.

## B-13c-05 — Settlement grounding and scale

- Danish settlement roots are placed at their authoritative node X/Z positions and grounded against the same Denmark presentation height function.
- Broad-map settlement miniatures are visually reduced so coastal cities do not overhang large portions of the coastline.
- Simulation node coordinates are unchanged.

## B-13c-06 — Denmark road/rail/ferry cleanup

- Strategic links touching Denmark are retained for the Denmark-focus QA slice.
- Land links are re-draped against the Denmark presentation height.
- Sea-ferry links remain at water level.
- Route graph and travel distance are unchanged.

## B-13c-07 — Barracks/farm grounding

- `QA-BARRACKS-AALBORG` and `QA-FARM-AARHUS` remain staged campaign-time construction projects.
- Both are visually repositioned close to their authoritative Aalborg/Aarhus nodes and scaled for broad-map presentation.
- Campaign construction start time/duration/stages remain unchanged.

## B-13c-08 — Denmark vegetation/land-cover cleanup

- v13b vegetation and field patches are retained but compacted around their nearest Danish node and re-grounded.
- Decorative vegetation remains presentation-only and must not intercept strategic selection.

## B-13c-09 — Denmark-focus context

- Non-Danish 3D settlement/node/control/formation renderers are temporarily hidden during the Denmark visual QA slice.
- This prevents floating foreign context from obscuring whether Denmark geometry, scale and grounding are correct.
- The underlying non-Danish simulation state is not deleted.

## B-13c-10 — UI/readability and isolation

- Existing campaign UI remains available.
- Tactical `BattleManager` time controls and tactical helper banner must not appear on `CampaignMap`.
- Tactical battlefield terrain, farm, road, tactical trees and tactical regiments must not be present on `CampaignMap`.
- Strategy-Test navigation/Officer-AI work remains isolated from Campaign.

## Simulation invariants

v00.00.13c must not change:

- WGS84 node coordinates,
- route graph,
- movement speed,
- ETA calculation,
- campaign time,
- logistics state,
- formation strength/state,
- tactical AI/navigation rules.

Unity world coordinates, model scale and render heights are presentation only.

## Unity acceptance gate

1. Unity 6000.6.0f1 compiles with 0 errors.
2. Campaign opens `CampaignMap` and overlay reads `v00.00.13c DENMARK CLEANUP DEV`.
3. There is **one** time-control bar: the campaign bar. The tactical `0.5/1/2/5/20` BattleManager strip is absent.
4. The tactical helper text (`Ctrl/Shift multi ... F9 kamp-setup`) is absent.
5. No `Battlefield Ground`, tactical farm/road or tactical regiments are rendered on CampaignMap.
6. Home/default view opens over Denmark.
7. Denmark coastline aligns with strategic nodes in the same broad projection.
8. No long rectangular terrain slabs/walls cross Denmark.
9. Danish settlements are visibly smaller and do not hang far over coast/water.
10. Aalborg barracks and Aarhus farm remain visible when Living World is enabled and progress with campaign time.
11. Danish roads/rails/ferry links remain readable.
12. No Strategy-Test tactical AI/navigation fixes are introduced into the campaign diff.

## Known limits

- Denmark relief is still procedural presentation relief, not DEM/GIS elevation.
- The Denmark-focus QA slice intentionally hides the coarse global terrain renderer and non-Danish 3D context while the Denmark foundation is validated.
- Final Europe terrain, historical road/rail splines and authored environment assets remain future work.
