# CAMPAIGN v00.00.13i — GIS TERRAIN FOUNDATION DEV NOTES

## Status

**IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA**

## Hovedændring

v13i er første CampaignMap-build, hvor Danmark-terrain ikke længere primært er en håndbygget polygonflade. En ny `V013I_DENMARK_GIS_FOUNDATION` loader public Terrarium elevation tiles, cacher dem lokalt og bygger 3D terrain direkte i campaign broad-map projection.

## Implementeret

- Mapzen/Terrain Tiles Terrarium DEM via AWS public dataset, zoom 8.
- Lokal terrain tile cache under `Application.persistentDataPath/PROJECT1864/TerrainCache`.
- Offline deterministic low-relief fallback.
- Natural Earth DNK coastline mask genbruges som midlertidigt land scaffold.
- Hydrology pilot med Limfjorden + udvalgte danske fjorde.
- Legacy v13 presentation/render stacks skjules/deaktiveres.
- Denmark-only semantic labels.
- Explicit Denmark road/ferry pilot; generic all-node graph lines skjules.
- Roskilde-Fredericia straight land-over-water visual er fjernet.
- Nyborg-Korsør og Fredericia-Middelfart renderes som ferry/water crossing.
- Aalborg harbour mini-diorama med kaj/pakhuse/både og dekorativ Nørresundby.
- Tydelig Aalborg barracks construction compound.
- MAP-ONLY battle gate fra v13h1 forbliver aktiv.
- Campaign clock/construction progression fortsætter.

## Kendte prototypebegrænsninger

- Hydrology er v13i pilot vectors, ikke endelig GeoDanmark water geometry.
- Natural Earth 1:50m coastline er stadig grovere end ønsket slutniveau.
- DEM source er global Terrain Tiles, ikke endnu Danmarks officielle DHM/Terræn.
- Terrain tiles downloades ved første run; offline første run bruger fallback.
- Rail er bevidst ikke tegnet fra generic `HasRail` flags før historisk 1864 validation.
- Bornholm/udvidet full-Denmark tile envelope kan forbedres efter første QA.

## Første QA

Kontroller især:

1. Unity compile errors.
2. GIS terrain progress nederst på skærmen.
3. Om terrain bliver mærkbart 3D.
4. Om Limfjorden skærer land omkring Aalborg.
5. Om Aalborg fremstår som havneby.
6. Om Roskilde-Fredericia fejllinjen er væk.
7. Om kun danske labels/settlements ses.
8. Om Aalborg barracks compound kan ses ved close zoom.
9. Om MAP-ONLY fortsat forhindrer kamp/contact.
