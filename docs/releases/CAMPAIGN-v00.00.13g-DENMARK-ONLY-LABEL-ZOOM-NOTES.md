# Campaign v00.00.13g — Denmark-only label + close zoom fix

## QA findings addressed
- duplicate Danish city labels
- Swedish/Norwegian/Finnish/German city labels still visible in Denmark-first milestone
- Flensburg/Schleswig still shown despite current Denmark-only scope
- maximum zoom remained too far from the clean map

## Changes
- CampaignMapController legacy all-Europe labels are suppressed.
- v13f label GUI is disabled after the corrected landmesh has been created.
- v13g is the single visible label owner.
- visible city labels are restricted to `CampaignMapRegion.Denmark`.
- foreign legacy node/control/settlement renderers are continually suppressed.
- v13e clean strategic links are visible only when both endpoints are Danish-region nodes.
- close zoom no longer uses hidden CampaignTerrainV013 as its camera floor.
- Denmark QA camera uses MinHeight 8 and TerrainClearance 5.5.
- Aalborg barracks and Aarhus farm remain in the build.

## Simulation
Presentation-only change. No intentional changes to campaign movement, ETA, route geometry, time progression, tactical AI or tactical navigation.

## QA
Awaiting local Unity compile/runtime verification.
