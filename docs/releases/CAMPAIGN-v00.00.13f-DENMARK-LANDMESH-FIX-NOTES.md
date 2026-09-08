# Campaign v00.00.13f — Denmark Landmesh Fix DEV

## Summary

Targeted follow-up to v00.00.13e. The clean Denmark render foundation is kept, but the Denmark land surface is regenerated with validated upward triangle winding so the land is visible from the campaign camera.

## Changes

- Added `CampaignDenmarkLandmeshFixV013F`.
- Disables v13e runtime update/label ownership after v13e has built its clean sea, settlement and infrastructure foundation.
- Suppresses the v13e defective Denmark landmesh renderer.
- Rebuilds Denmark from the same Natural Earth lon/lat rings.
- Adds automatic X/Z-space winding correction.
- Adds runtime `AverageNormalY` validation.
- Adds restrained land material variation.
- Rebuilds coastline slightly above the corrected surface.
- Adds three-tier semantic label LOD with multi-direction overlap avoidance.
- De-emphasises Flensburg/Schleswig at close zoom.
- Slightly improves road/rail/ferry readability.
- Slightly tightens Denmark Home framing.
- Keeps Aalborg barracks and Aarhus farm grounded.

## Not changed

No intended changes to campaign movement, ETA, route-distance calculation, campaign clock, strategic state, logistics, tactical AI or tactical navigation.

## Expected runtime evidence

A successful build should log a line similar to:

`CAMPAIGN-V013F|Landmesh=True|Winding=Upward|AverageNormalY=...|LabelLOD=3Tier|SouthernContext=DeEmphasized|SimulationChanged=False`

`AverageNormalY` must be greater than `0.05`.

## QA status

IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA.
