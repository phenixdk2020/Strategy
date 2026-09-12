# CAMPAIGN4 v00.00.10l — Strategic Visual Pass

Branch: `channel-campaign4`

## Formål

Første større visuelle opgradering af Campaign4 Danmark-overblikket efter den tekniske 3D/DEM-pipeline blev gjort spilbar.

Referencebilledet bruges som art direction, ikke som geografisk/historisk facit. Især kopieres vejnet, byliste, markskel og kystdetaljer ikke 1:1.

## Ændringer

- Ny `Campaign4StrategicVisualPassV010L.cs`.
- Global WGS84-baseret UV mapping på streamede DEM tiles.
- Procedural 512x512 landcover texture med græs, marker, hede, skovtoner og mikrovariation.
- Mørkere hav og separat hydrology-materiale.
- Varmere Campaign Sun, bløde skygger og mørkere ambient light.
- Rust-røde/orange strategiske city markers.
- Procedurale 3D forest masses på faktiske DEM-højdesamples.
- Nyt separat `CAMPAIGN4_v10l_StrategicRoads_NONCANON` lag.
- Road scaffold forbinder udvalgte byankre med diskrete terrain-following LineRenderers.
- Road scaffold er ikke traced fra referencebilledet og er ikke historisk facit.
- Overview camera tunet til FOV 38, distance 94, pitch 68°, yaw -3°.
- `F` Aalborg/Limfjord close focus bevares.
- Compact Campaign4 HUD opdateret til v10l.

## Historiske guardrails

- 40-city 1850-laget forbliver autoritativt for strategiske city anchors.
- Roads i v10l er kun presentation scaffold.
- Procedural landcover er ikke præcis 1851 arealanvendelse.
- Storebælts-/moderne bridge-logik må ikke opstå som en bivirkning af vejnettet.
- Source-backed transport- og landcoverdata skal senere kunne erstatte scaffold-lagene.

## QA-status

`IMPLEMENTERET I GITHUB / UNITY 6.6 COMPILE + PLAY MODE QA PENDING`

Primær QA er et nyt screenshot af Danmark-overblikket og derefter `F`-visningen ved Aalborg/Limfjorden.
