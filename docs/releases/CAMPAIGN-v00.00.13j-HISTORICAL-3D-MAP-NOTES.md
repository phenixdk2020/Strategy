# CAMPAIGN v00.00.13j — HISTORICAL 3D MAP FOUNDATION DEV NOTES

## Status

**IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA**

## Hovedændring

v13j er første renderer-pass, der går væk fra den synlige v13i tile-grid præsentation. v13i bruges stadig til at hente/cache Terrarium DEM, men v13j bygger derefter ét samlet, glattere mainland Denmark mesh med global cartographic UV/material.

## Implementeret

- `CampaignHistorical3DMapV013J`.
- `CampaignHistoricalMapLabelsV013J`.
- Smooth terrain grid `520 x 340` med UInt32 mesh indices.
- Bilinear Terrarium sampling fra eksisterende v13i cache.
- To elevation smoothing passes og lavere vertical exaggeration.
- Ét seamless cartographic terrain material.
- Optional local historical raster: `PROJECT1864/HistoricalMap/denmark_1864.png` under Unity persistentDataPath.
- Procedural seamless cartographic fallback hvis historisk raster ikke er til stede.
- Oversized v13i city dioramas skjules; små Denmark-only city miniatures bygges.
- Aalborg harbour/barracks og Aarhus farm genbygges i markant mindre map scale.
- Gamle oversized QA construction renderers skjules.
- V13i road/ferry lines bevares men gøres tyndere.
- Yellow GIS coast-line presentation skjules; coast læses fra land/water silhouette.
- Campaign camera får terrain-aware cursor zoom, middle mouse pan, `nearClipPlane 0.02` og close terrain clearance.
- MAP-ONLY gate bevares.

## Kendte begrænsninger

- v13j mainland envelope omfatter ikke Bornholm i den nye smooth global mesh endnu.
- Coastline mask bruger stadig Natural Earth scaffold og er ikke endnu final GeoDanmark coastline.
- Hydrology er fortsat v13i pilot vectors, ikke endelig authoritative water dataset.
- Procedural cartographic fallback er ikke et autentisk 1864-kort.
- Den rigtige historical raster skal tilføjes lokalt/konfigureres senere og må ikke have secrets/tokens committed.
- Input Manager deprecation warning kan fortsat vises, fordi v13j bevidst ikke laver en risikabel project-wide input migration.

## QA

1. Ingen compile errors.
2. V13i DEM cache gennemfører først.
3. Status skifter til `Historical 3D map klar`.
4. `V013J_SmoothTerrain` er synligt, mens gamle `GIS_Terrain_*` renderers/colliders er skjult.
5. Ingen store tile-farveblokke/seams som på v13i screenshot.
6. Terrain virker blødere/lavere og mindre facetteret.
7. City miniatures er markant mindre.
8. Aalborg harbour/barracks og Aarhus farm er små og lyse nok til close zoom.
9. Scroll zoomer mod terrain under cursor; middle mouse pans.
10. Kun danske labels vises.
11. Ingen battle/contact UI.
