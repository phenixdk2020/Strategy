# PROJECT 1864 — Campaign v00.00.13j Historical 3D Map Foundation

## Formål

v13j erstatter den synlige v13i tile-præsentation med en glattere, mere kortagtig 3D-foundation. Målet er at komme tættere på en Google Maps/Earth-lignende strategisk kortoplevelse uden at gøre moderne geografi til historisk 1864-state.

## Renderer

- v13i Terrarium DEM-cache bruges som elevationskilde.
- Mainland Denmark bygges som ét kontinuerligt `520 x 340` mesh i stedet for synlige `40 x 40` tile-meshes.
- DEM samples bilineært og højder glattes i to passes.
- Vertical exaggeration reduceres til et dansk low-relief look.
- Ét globalt UV-space dækker hele kortet.
- Ét fælles cartographic material fjerner skiftende tile-farver og tydelige tile-seams.

## Historisk raster contract

Hvis filen

`Application.persistentDataPath/PROJECT1864/HistoricalMap/denmark_1864.png`

findes lokalt, draperes den over v13j-terrainet. Filen skal være en georefereret/croppet Danmark-raster, som følger v13j map-envelope. Filen commits ikke til GitHub.

Hvis filen mangler, bruger v13j en procedural cartographic fallback. Denne fallback er kun visual QA og må ikke beskrives som et autentisk 1864-kort.

Den langsigtede datakildehierarki for historisk raster/vector forbliver: officiel dansk DHM/elevation, authoritative physical geography, historiske målebordsblade/kilder for scenario-date roads/settlements/land cover samt tydelig provenance/confidence.

## Byer og facilities

De store v13i diorama-byer skjules. v13j bygger væsentligt mindre town miniatures, så byerne ikke fylder flere kilometer visuelt. Major nodes får lidt større miniature, men semantic labels bærer identiteten ved overview/middle zoom.

Aalborg er fortsat initial harbour QA-case. Havnefront, kaj, warehouse og barracks visualiseres i map-scale størrelse ved Limfjorden. Aarhus farm visualiseres tilsvarende som et lille map-scale compound.

Legacy QA construction renderers skjules, mens Campaign construction-state bevares som authoritative state.

## Kamera

Campaign camera får:

- cursor-centric scroll zoom mod GIS-terrain,
- terrain-aware minimum clearance,
- `nearClipPlane = 0.02`,
- tættere minimum height,
- middle-mouse drag pan,
- eksisterende WASD/QE/PageUp/PageDown/Home bevares.

Dette er stadig strategy-map camera, ikke first-person/ground camera.

## Infrastruktur og vand

v13i corrected road/ferry topology bevares, men linjerne gøres tyndere. Den fejlagtige Roskilde-Fredericia direkte landlinje over åbent vand må ikke genindføres. Limfjorden og eksisterende hydrology pilot fortsætter som fysisk water context.

## Simulation isolation

v13j ændrer ikke:

- authoritative latitude/longitude,
- geodesic distance,
- ETA,
- campaign time,
- logistics,
- tactical AI/navigation.

MAP-ONLY gate fra v13h1 forbliver aktiv under map QA.

## Input Manager warning

Unity Input Manager deprecation-warning fjernes ikke ved at skifte projektet til New Input System i v13j. Tactical og ældre campaign scripts bruger stadig `UnityEngine.Input`. En project-wide Input System migration skal ske som separat change med regressionstest, så kortarbejdet ikke bryder tactical controls.

## QA gate

1. Ingen compile errors.
2. Efter v13i cache-build skifter kortet til `V013J_SmoothTerrain`.
3. Store rektangulære tile-farvefelter er væk.
4. Terrænet er mindre kantet/facetteret end v13i.
5. Aalborg/Aarhus/Fredericia/byer er markant mindre i forhold til kortet.
6. Gamle mørke/oversized barracks/farm visuals er skjult.
7. Aalborg harbour og Limfjord kan aflæses ved tæt zoom.
8. Scroll kan zoome væsentligt tættere mod terrain under musen.
9. Middle mouse drag kan panorerer kortet.
10. Kun danske semantic labels vises.
11. Battle/contact er stadig deaktiveret i MAP-ONLY.
