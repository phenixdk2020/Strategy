# PROJECT 1864 — Campaign v00.00.13n Cesium Denmark 3D Foundation

## Formål

v00.00.13n stopper den hjemmelavede raster/DEM-renderkæde som synlig CampaignMap-løsning og flytter den geografiske rendering til Cesium for Unity.

Målet er et professionelt, streamet 3D-kort, hvor Danmark kan ses fra nationalt overblik ned til lokal terræninspektion uden firkantede tile-flader, manuelt tegnede kyster eller cartographic boundary-lines brændt ind i et OSM-raster.

## Teknisk baseline

- Cesium for Unity: `com.cesium.unity` 1.25.1 via den officielle Cesium scoped registry.
- Terrain: Cesium World Terrain, ion asset ID `1`.
- Development imagery: Bing Maps Aerial, ion asset ID `2`, uden labels/road overlay.
- Georeference: Danmark-centret CesiumGeoreference.
- Kamera: CesiumGlobeAnchor + CesiumOriginShift.
- Runtime scene: `Assets/Scenes/CampaignMap.unity`.
- MAP-ONLY: kamp, hostile contact og tactical transition forbliver deaktiveret under map QA.

## Renderer-isolation

Når v13n kører, må gamle v13a-v13m presentation layers ikke være synlige. De gamle systemer er kun historik/reference og force-hides/deaktiveres fra CampaignMap.

Der skal derfor ikke længere være:

- v13m stitched OSM atlas,
- z10-z13 custom raster overlays,
- hjemmelavet 520x340 Denmark mesh,
- gamle GIS road/ferry render-lines,
- gamle city blocks,
- outline polygons,
- legacy settlement labels.

Cesium ejer den synlige geografiske world presentation.

## Kamera

Første v13n navigation:

- `Home`: Danmark overview.
- musehjul: zoom fra cirka 700 km til cirka 80 m altitude.
- midterste museknap + drag: pan.
- WASD/piletaster: pan.
- Denmark-first bounds begrænser QA-kameraet til Danmark/nærmeste kontekst.

Senere kan kameraet udvides med tilt/orbit, smooth fly-to, semantic zoom og click-to-focus uden at ændre geodatafundamentet.

## Ion token og credentials

Ingen Cesium ion-token må commits til GitHub.

Runtime accepterer i denne rækkefølge:

1. environment variable `CESIUM_ION_TOKEN`,
2. lokal fil `Application.persistentDataPath/PROJECT1864/Cesium/ion-token.txt`,
3. Cesiums editor/default ion-login/token.

Editor-menuen `PROJECT 1864 > Campaign > Cesium ion token (lokal)` kan gemme/fjerne en lokal token uden for repoet.

## Historisk 1864-regel

Cesium World Terrain og moderne aerial imagery bruges til fysisk geografi, elevation, kyst og visual QA. De er ikke historisk facit for 1864.

Periodens egne lag skal senere ligge oven på terrænet som separate data/presentation layers:

- historiske veje,
- jernbaner,
- broer,
- færger,
- settlements og byudbredelse,
- skov/vådområder,
- havne,
- kaserner og militære installationer,
- strategiske formationer.

Simulationens lat/lon, geodesiske afstande og route geometry forbliver authoritative og må ikke udledes fra moderne imagery.

## Acceptance gate

1. Unity Package Manager resolver Cesium 1.25.1 uden package errors.
2. CampaignMap compiler uden C# errors.
3. Kun Cesium-world er synlig; ingen v13a-v13m kortlag blinker frem.
4. Danmark kan ses i fuld oversigt og zoomes ned til regional/lokal højde.
5. Bing aerial har ingen tegnede OSM maritime boundary-lines eller tunge kartografiske labels.
6. Limfjorden, Storebælt, Lillebælt, Øresund og danske kyster kommer fra den geospatiale verden i stedet for håndtegnede hydrology-strips.
7. MAP-ONLY forbliver aktiv.
8. Tactical TEST-kode er urørt.
