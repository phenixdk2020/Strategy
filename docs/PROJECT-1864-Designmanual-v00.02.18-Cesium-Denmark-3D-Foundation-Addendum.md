# PROJECT 1864 — Designmanual Addendum v00.02.18

## Cesium Denmark 3D Foundation

Denne addendum erstatter v13i-v13m's hjemmelavede GIS/raster-renderkæde som anbefalet synlig CampaignMap-arkitektur.

### Ny visual baseline

- Cesium for Unity ejer den georefererede 3D-world rendering.
- Cesium World Terrain er development terrain baseline.
- Bing Maps Aerial uden labels er development imagery baseline.
- Historiske 1864-data lægges som separate period-correct overlays og må ikke udledes direkte af moderne imagery.
- Campaign simulation, OOB, logistics, movement, ETA og route geometry forbliver adskilt fra presentation terrain.

### Arkitekturregel

Der må kun være én synlig geographic world owner på CampaignMap. Pre-v13n V013/GIS/Outline/StrategicLink/legacy settlement presentation må ikke reaktiveres som parallel kortstack.

### Kamera og skala

CesiumGeoreference + CesiumGlobeAnchor + CesiumOriginShift er baseline for Danmark og senere international udvidelse. Kamera skal kunne skifte kontinuerligt mellem nationalt overblik og lokal terræninspektion uden at renderfundamentet skiftes.

### Credentials

Cesium ion credentials er lokal konfiguration og må aldrig commits. Repoet må kun indeholde package/configuration code og dokumentation.

### Historical layer principle

Fysisk geografi og elevation kan komme fra moderne geodata. Alt der repræsenterer menneskeskabt 1864-indhold — veje, rail, broer, havne, byudbredelse, militære sites og strategiske links — skal have en period-correct kilde/model.
