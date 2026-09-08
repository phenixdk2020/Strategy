# PROJECT 1864 — Campaign v00.00.13c Denmark Terrain Cleanup

State: **IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA**

Branch: `work/v00.00.13c-denmark-cleanup`

This document is the designmanual supplement for the Denmark-cleanup pass. The authoritative strategic simulation remains the v00.00.11+/v13 campaign state model; v13c changes presentation only.

## Goal

Make Denmark immediately readable as the campaign focus and remove the visible terrain artifacts observed in v13b: steep rectangular terrain walls, coarse Denmark land overshoot, settlements hanging over terrain edges and construction sites sitting on uneven ground.

## B-13c-01 — Denmark terrain cleanup

- Detailed Natural Earth Denmark geometry is used as the visible Denmark land footprint.
- Legacy coarse Jutland/Funen/Zealand terrain outside the detailed Denmark footprint is carved back to water.
- Procedural seabed is moved close below the visible sea surface to remove deep rectangular trench/wall artifacts.

## B-13c-02 — Denmark coastline cleanup

- Detailed Denmark land/coast meshes are re-draped after the cleaned terrain mesh is created.
- Coastline width is reduced to avoid a heavy outline look.

## B-13c-03 — Denmark recognition

- Denmark-first Home/default camera from v13a/v13b is retained.
- Jutland, Fyn, Sjælland and the detailed island geometry remain the high-detail focus.

## B-13c-04 — Settlement grounding

- Danish node markers and settlement miniatures are re-grounded to the cleaned terrain after legacy v13 height helpers have run.
- Settlement level-ground pads prevent city dressing from reading as if it hangs off a steep terrain edge.

## B-13c-05 — Denmark city visual cleanup

- Existing v13b city/port/station/depot dressing is retained.
- The cleanup pass moves the complete settlement root to the corrected Denmark surface rather than changing simulation coordinates.

## B-13c-06 — Denmark road/rail/ferry cleanup

- Strategic links that touch Denmark are re-draped over the cleaned terrain.
- Sea-ferry segments remain visually above the water surface.
- Link type, route graph and authoritative travel distance are unchanged.

## B-13c-07 — Barracks/farm grounding

- `QA-BARRACKS-AALBORG` and `QA-FARM-AARHUS` remain staged campaign-time construction projects.
- Both are re-grounded to the cleaned terrain and receive level-ground pads.
- Existing v13b materials, carts, scaffold details and worker polish remain active.

## B-13c-08 — Denmark vegetation grounding

- v13b Denmark tree clusters are retained.
- Tree roots and land-cover patches are re-grounded to the cleaned mesh so they do not float after terrain cleanup.

## B-13c-09 — Denmark relief rule

- Denmark receives intentionally gentle visual relief: low-amplitude broad/fine variation plus a small central-Jutland rise.
- Three smoothing passes remove abrupt local slopes.
- This is a visual proxy only; final DEM/GIS elevation remains future work.

## B-13c-10 — Readability and isolation

- Existing v13b UI cleanup remains: overlays OFF by default, F5 layer panel collapsed, F3 search hidden.
- Campaign remains isolated from Strategy-Test tactical AI/navigation work.

## Simulation invariants

v00.00.13c must not change:

- WGS84 node coordinates,
- route graph,
- movement speed,
- ETA calculation,
- campaign time,
- logistics state,
- formation strength/state,
- tactical AI/navigation.

Unity world coordinates and render heights are presentation only.

## Unity acceptance gate

1. Unity 6000.6.0f1 compiles with 0 errors.
2. Campaign opens `CampaignMap` and overlay reads `v00.00.13c DENMARK CLEANUP DEV`.
3. Home/default view opens over Denmark.
4. The rectangular/vertical terrain artifacts visible in v13b around Denmark are gone or materially reduced.
5. Danish hills read as low, gradual terrain rather than cliffs/plateaus.
6. Detailed Denmark coastline remains recognizable after re-draping.
7. Danish settlements no longer hang over terrain edges.
8. Aalborg barracks and Aarhus farm remain visible with F8 Living World ON and progress with campaign time.
9. Roads/rails touching Denmark follow the cleaned surface; ferry links remain visible over water.
10. No Strategy-Test tactical AI/navigation changes appear in the campaign diff.

## Known limits

- This is not a DEM/GIS reconstruction.
- Sweden, Norway, Finland and Germany remain on the broader coarse v13 terrain representation.
- Historical 1864 political borders remain separate from physical coastline geometry.
- Final historical road/rail splines and final authored environment assets are still future work.
