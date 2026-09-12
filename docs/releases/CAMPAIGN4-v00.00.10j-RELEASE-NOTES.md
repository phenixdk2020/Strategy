# CAMPAIGN4 v00.00.10j — DEM visibility and presentation cleanup

## Problem reproduced

The first Unity Play Mode screenshot of v00.00.10i confirmed that the new perspective camera was active, but Denmark itself was not visibly rendered as terrain. The view was dominated by the large blue sea plane, while city/zone labels appeared to float over it. The inherited v10g provider-lab panel also occupied a large part of the screen and the perspective city labels were too dense.

## Root cause

The inherited v10g DEM tile builder emits triangle winding that produces downward-facing normals for the X/Z campaign projection. With Campaign4's above-ground perspective camera and ordinary back-face culling, those land meshes can disappear from view.

The old city semantic-zoom code also only checked `Camera.orthographicSize`; once Campaign4 switched the camera to perspective, that condition no longer reduced the number of labels.

## Changes

### `Campaign4DemMeshRepairV010J.cs`

- Watches streamed `DEM_*` meshes as they appear.
- Recalculates normals and samples their average Y direction.
- Reverses triangle winding only when the mesh is facing downward.
- Recalculates normals and bounds afterwards.
- Logs repaired tile counts using `CAMPAIGN4-DEM-REPAIR`.

### `Campaign4PresentationV010J.cs`

- Waits for provider 1 and the 40-city layer to initialize.
- Hides the inherited v10g TRUE 11 BASEMAP LAB UI in Campaign4 after provider 1 is active.
- Disables the old perspective-unaware city-label `OnGUI` while preserving all city GameObjects.
- Adds perspective-distance semantic labels:
  - strategic range: top 5 cities;
  - operational range: top 15;
  - close range: all 40.
- Suppresses overlapping inherited zone labels at strategic and very-close zooms.
- Adds a small Campaign4 v10j status/control line instead of the large lab overlay.

## Expected result

After updating `channel-campaign4`, the Denmark overview should show actual 3D land terrain above the sea instead of the previous sea-only/trapezoid appearance. Labels should be substantially cleaner at strategic range.

## QA

1. Compile in Unity 6.6.
2. Enter Play mode.
3. Confirm green/brown DEM land is visible.
4. Confirm the large v10g provider-lab panel is gone.
5. Confirm only the top 5 city labels appear at the Denmark overview.
6. Use mouse wheel to zoom in; confirm additional labels appear progressively.
7. Press `F`; verify Aalborg/Limfjorden close view.
8. Confirm Limfjorden is visibly cut through the land terrain.
9. Confirm nearby city `Detail3D` activates.
10. Verify army/zone selection and right-click orders still work.

Promotion remains blocked until the new Denmark-overview and Aalborg/Limfjord screenshots pass visual QA.
