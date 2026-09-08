# PROJECT 1864 — Campaign v00.00.13a 3D POLISH DEV

**Branch:** `work/v00.00.13a-campaign-visual-polish`  
**Base:** clean `work/v00.00.13-campaign-rebuild`  
**State:** IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA  
**Promotion:** none; active formal campaign gate remains v00.00.11.

## Why this revision exists

Video QA of v00.00.13 showed three presentation problems:

1. inherited/tactical UI and campaign UI could visually overlap and make it look like multiple campaign runtimes were active;
2. the initial camera did not foreground Denmark even though Denmark is the current detailed campaign focus;
3. the first v13 3D slice still looked like a technical prototype rather than a cleaner strategic world.

## Implemented in v00.00.13a

- `PrototypeBuildVersionOverlay` now creates only in `PrototypeBattle`; the inherited `v00.00.10e` tactical marker is not drawn on `CampaignMap`.
- campaign layer panel is collapsed by default; `F5` toggles it.
- strategic overlay layer (`F9`) is OFF by default to reduce line/outline clutter.
- search panel is hidden by default; `F3` toggles it.
- default/Home camera is centered on Denmark rather than the generic map centre.
- Natural Earth 1:50m Denmark geometry is instantiated as a dedicated v13a visual surface and draped onto the procedural strategic terrain.
- detailed Denmark coastline is lifted over the terrain and rendered thinner/cleaner.
- terrain and sea palette is darker and less neon.
- directional lights use soft shadows where supported.
- strategic route lines and optional outlines are thinner.
- route ghost is visually lighter/thinner.

## Isolation

This revision changes presentation only. It does **not** alter:

- authoritative campaign coordinates;
- geodesic/route distance;
- march ETA;
- campaign state;
- tactical navigation;
- Officer AI;
- battle movement/formation logic.

## QA checklist

1. Unity 6.6 compile: 0 errors.
2. CampaignMap displays only `PROJECT 1864 CAMPAIGN | v00.00.13a 3D POLISH DEV` as campaign build marker.
3. No `v00.00.10e CAMPAIGN TEST` box appears on CampaignMap.
4. Default camera opens over Denmark; `Home` returns to Denmark.
5. Denmark coastline/islands are clearly recognizable compared with v13.
6. `F5` opens/closes the layer panel.
7. `F9` can explicitly enable the strategic overlay layer; it starts OFF.
8. `F3` opens/closes search; it starts hidden.
9. roads/rail remain visible and do not change route simulation.
10. pause/speed/movement/contact continue to work as before.
