# PROJECT 1864 Campaign v00.00.13k — LIVE CARTOGRAPHIC 3D DRAPE

Status: DEV / MAP-ONLY / afventer Unity compile + runtime QA.

## Implementeret

- Nyt `CampaignLiveCartographicDrapeV013K` runtime-lag.
- Venter på v13j smooth 3D terrain og bruger det som geometri.
- Rigtige kartografiske raster tiles draperes som subdividerede 3D meshes på terrain/water.
- Default DEV map provider: OpenStreetMap Standard.
- Tile requests er viewport-only: 3x3 omkring aktuelt camera target.
- Zoom-niveau z7-z11 vælges ud fra camera height.
- Tiles caches lokalt i 14 dage.
- Lokal `provider.json` kan ændre provider URL, attribution og User-Agent uden kodeændring.
- Lokal `denmark_1864.png` historical raster prioriteres; live requests springes da over.
- Prototype `GIS_Road_*` / `GIS_Ferry_*` presentation-lines skjules.
- Prototype city dioramas og v13j semantic labels skjules for at undgå dobbelt cartography.
- Ny kompakt Aalborg Kaserne QA landmark med nearest-land terrain anchor.
- Ny kompakt Aarhus Farm QA landmark med terrain anchor.
- Landmarks vises kun ved tæt zoom.
- Kamera: minimum height 0.22, terrain clearance 0.14, near clip 0.005.
- Synlig attribution ved live OpenStreetMap tiles.
- Campaign battle/contact/formation movement forbliver deaktiveret via MAP-ONLY gate.

## Ikke ændret

- Strategic lat/lon.
- Route distance/ETA.
- Campaign time.
- Logistics.
- Tactical AI/navigation.
- Battle scene.
- Historical road/rail authoritative data.

## Kendt begrænsning

OpenStreetMap Standard viser nutidig cartography og stednavne. Den bruges kun som render-/navigationstest. Den må ikke anvendes som faktakilde for 1864 infrastructure eller settlement extents.

## QA

1. Ingen nye compile errors.
2. Overlay viser `v00.00.13k LIVE CARTOGRAPHIC 3D DRAPE DEV`.
3. Rigtigt kort bliver synligt oven på 3D terrain efter v13j er ready.
4. Panning/zoom skifter tiles uden country prefetch.
5. Tiles følger relief og ligger ikke som flad plane højt over terrain.
6. Ingen prototype road/ferry line går direkte over vand.
7. Aalborg city/cartography ligger korrekt omkring Limfjorden; prototype city block må ikke stå ude i vandet.
8. Aalborg Kaserne QA vises på land ved tæt zoom.
9. Aarhus Farm QA vises ved tæt zoom.
10. Attribution er synlig når live provider anvendes.
