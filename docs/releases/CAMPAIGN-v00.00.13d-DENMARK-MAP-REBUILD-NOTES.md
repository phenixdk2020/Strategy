# PROJECT 1864 Campaign v00.00.13d — Denmark Map Rebuild DEV

Status: **IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA**

## Runtime changes

- Replaces the v13c Denmark projection/remap experiment with a direct broad-projection Denmark surface.
- Uses the existing Natural Earth Denmark lon/lat rings as source geometry.
- Builds `GEO_Denmark_V013D_BROAD_DIRECT` directly with `CampaignGeoProjection.Project3D`.
- Keeps `CampaignTerrainSurface_v013` hidden during Denmark QA.
- Suppresses old v13a/v13c Denmark roots.
- Hides v13b land-cover and vegetation artifacts during geography validation.
- Keeps Aalborg barracks and Aarhus farm construction QA projects.
- Grounds/scales Denmark settlements, nodes and formations against one low-relief Denmark presentation height.
- Re-drapes Denmark-focus strategic links from authoritative endpoint lat/lon values.
- Suppresses the legacy all-Europe node label layer and draws Denmark-focus labels with simple overlap avoidance.
- Disables V011 diagnostic/political/search UI during the Denmark-focused QA slice.
- Retains the Denmark-first campaign camera and disables the conflicting V011 generic Home handler by gating that helper.

## Isolation

- CampaignMap remains separated from tactical PrototypeBattle bootstrap.
- No Strategy-Test tactical AI/navigation changes are part of this version.
- No strategic movement, ETA, distance, time, logistics or force-state rules are intentionally changed.

## Test target

Expected overlay:

`PROJECT 1864 CAMPAIGN | v00.00.13d DENMARK MAP REBUILD DEV`

Primary visual acceptance:

1. Denmark aligns with Aalborg/Aarhus/København nodes.
2. Large stretched dark plates/bands are absent.
3. Only Denmark/southern-context labels are visible in the Denmark QA view.
4. Denmark reads as low-relief terrain rather than cliffs/plateaus.
5. Aalborg barracks and Aarhus farm remain functional.

Unity baseline: 6000.6.0f1.
