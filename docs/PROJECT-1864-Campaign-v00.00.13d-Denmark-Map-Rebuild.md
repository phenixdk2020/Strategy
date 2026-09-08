# PROJECT 1864 — Campaign v00.00.13d Denmark Map Rebuild

State: **IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA**

Branch: `work/v00.00.13d-denmark-map-rebuild`

This document is the designmanual supplement for the clean Denmark map rebuild. It supersedes the v13c projection/remap experiment for presentation, while preserving the v11+ strategic simulation contract.

## Why v13d exists

Runtime QA of v13c showed that removing tactical contamination helped, but the Denmark presentation still contained stretched/offset land geometry, large dark bands and all-Europe label clutter. The remaining root problem was presentation architecture: the Natural Earth Denmark layer had originally been created through the legacy Denmark-local projection and later remapped into the broad strategic projection.

v13d removes that two-step path.

## B-13d-01 — Direct broad Denmark projection

- Natural Earth Denmark longitude/latitude rings remain the physical coastline source.
- Denmark is projected directly with `CampaignGeoProjection.Project3D(latitude, longitude, y)`.
- No `CampaignGeoProjection.Unproject()` conversion/remap step is allowed in the new Denmark render path.
- Strategic node WGS84 coordinates remain authoritative.

## B-13d-02 — Dedicated Denmark render surface

- The v13 coarse global terrain renderer remains hidden during Denmark QA.
- v13a/v13c Denmark roots are disabled and destroyed at runtime before the v13d surface is shown.
- New render root: `GEO_Denmark_V013D_BROAD_DIRECT`.
- Each Denmark island/land part is triangulated directly from the Natural Earth ring in broad campaign coordinates.

## B-13d-03 — Low-relief Denmark rule

- Denmark uses intentionally low presentation relief.
- Relief is broad and shallow; no cliffs/plateaus are created by design.
- This is not a DEM claim. Final authoritative elevation remains future GIS/DEM work.
- Unity Y is presentation only and must not change movement or ETA.

## B-13d-04 — Remove residual v13b artifacts during geography QA

- `V013B_DenmarkLandCover` is hidden.
- `V013B_Vegetation` is hidden.
- These layers are not deleted from the project; they can be rebuilt later against the validated v13d geography.
- Construction dressing is retained separately.

## B-13d-05 — Settlement grounding and scale

- Denmark settlement miniatures are grounded to the same v13d Denmark height function.
- Settlement scale is reduced for strategic-map readability.
- Node markers and political-control markers are likewise reduced and grounded.
- Non-Danish settlement/node/control renderers are hidden for this QA slice.

## B-13d-06 — Denmark label rebuild

- The legacy CampaignMapController 50-node label style is suppressed during v13d QA.
- v13d draws Denmark-focus labels only.
- Danish-controlled Flensburg and Schleswig remain visible as immediate southern context.
- A simple rectangle overlap-avoidance pass shifts labels vertically when required.
- V011 diagnostic/political/search UI is disabled during this QA slice to reduce clutter.

## B-13d-07 — Camera ownership

- `CampaignMapCameraController` remains the authoritative camera controller.
- Its Denmark-first Home position remains active.
- The V011 generic Home reset is removed from active runtime by disabling the V011 usability helper during v13d QA.

## B-13d-08 — Infrastructure re-drape

- Strategic links touching the Denmark focus are reconstructed visually from their authoritative endpoint latitude/longitude values.
- Road/rail links use the v13d Denmark presentation height when inside Denmark.
- Sea-ferry links remain at water level.
- Route graph, link ownership and strategic distance are unchanged.

## B-13d-09 — Construction QA preservation

- `QA-BARRACKS-AALBORG` remains active.
- `QA-FARM-AARHUS` remains active.
- Both are grounded/scaled close to their authoritative campaign nodes.
- Their staged campaign-time construction behavior is preserved.

## B-13d-10 — Runtime isolation

- CampaignMap remains isolated from tactical PrototypeBattle bootstrap.
- The v13c scene-aware `GrandCampaignBootstrap.CampaignModeEnabled` fix remains mandatory.
- `CampaignDenmarkV013DLegacyGate` disables the obsolete v13c projection/remap component before it can execute.
- No Strategy-Test tactical navigation or Officer-AI fixes are introduced by v13d.

## Simulation invariants

v00.00.13d must not change:

- campaign node latitude/longitude,
- strategic route graph,
- route-distance calculation,
- movement speed,
- ETA,
- campaign time progression,
- logistics state,
- formations/regiments,
- tactical AI/navigation.

## Unity acceptance gate

1. Unity 6000.6.0f1 compiles with 0 errors.
2. Overlay reads `PROJECT 1864 CAMPAIGN | v00.00.13d DENMARK MAP REBUILD DEV`.
3. Only one strategic time-control bar is visible.
4. Tactical helper text/regiments/battlefield content do not appear on CampaignMap.
5. Denmark is immediately recognizable and aligned with Aalborg/Aarhus/København strategic nodes.
6. The large stretched dark bands/plates seen in v13c are gone.
7. Danish land reads as low, gentle terrain without artificial cliff walls.
8. Non-Danish labels no longer fill the Denmark view.
9. Danish settlements no longer hang over broad terrain edges.
10. Aalborg barracks and Aarhus farm remain visible with Living World enabled and continue to progress with campaign time.
11. Denmark-focus infrastructure remains visible and aligned.
12. Campaign movement/ETA behavior is unchanged.

## Known limits

- v13d is a clean strategic presentation rebuild, not final GIS terrain.
- v13b vegetation/land-cover is intentionally hidden until the geography is accepted.
- Final historical road/rail spline geometry remains future work.
- Sweden/Norway/Finland/Germany 3D presentation is intentionally outside this Denmark QA pass.
