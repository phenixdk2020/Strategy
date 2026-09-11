# CAMPAIGN4 v00.00.10i — Playable 3D map foundation

Branch: `channel-campaign4`
Unity baseline: 6000.6.0f1
Status: implemented in GitHub, Unity compile/Play Mode QA pending

## Purpose

Turn Campaign4 from a mostly top-down basemap comparison into a real playable 3D campaign-map foundation while preserving Campaign3 as rollback/reference.

## Added

### Campaign4Camera3DController.cs

- Converts `Camera.main` to perspective rendering.
- Deep mouse-wheel zoom from Denmark overview to city scale.
- WASD/arrow pan.
- Middle-mouse orbit/tilt.
- Q/E rotation.
- Home resets Denmark view.
- F focuses Aalborg/Limfjorden QA area.
- Smooth pivot, rotation and zoom interpolation.

### Campaign4CityDetailLOD.cs

- Uses strategic markers at long range.
- Activates generated 3D settlement geometry near a city.
- Generates roads, houses and church geometry deterministically.
- Larger towns use denser geometry and longer LOD range.
- Hides the strategic marker while close detail is active.
- Restores marker and disables close geometry when zooming away.

## Existing 3D terrain reused

Provider 1 from v10g already builds a real streamed DEM mesh from Terrarium elevation data and applies an explicit hydrology cut including Limfjorden. v10i uses this as the first playable 3D terrain foundation instead of converting the concept image into a static background.

## Historical city layer retained

v10h top-40 city data remains unchanged. The city layer is based on the official 1 February 1850 population ranking and intentionally excludes Esbjerg from the 1851 start layer.

## Visual target

The approved generated Denmark/Aalborg concept is an art-direction target only. Production visuals must be assembled from real runtime terrain, water, vegetation, infrastructure, city prefabs and LOD/streaming so the player can freely pan, rotate and zoom.

## Required Play Mode QA

1. Scripts compile in Unity 6.6.
2. Provider 1 DEM loads.
3. Camera becomes perspective.
4. Country-to-city zoom works smoothly.
5. F focuses Aalborg/Limfjorden.
6. Limfjorden remains open water in the terrain mesh.
7. City close LOD activates near Aalborg and other towns.
8. Strategic marker returns after zooming out.
9. Existing army/zone selection and right-click movement remain functional.
10. No new warnings from deprecated Unity 6.6 object-search overloads.
