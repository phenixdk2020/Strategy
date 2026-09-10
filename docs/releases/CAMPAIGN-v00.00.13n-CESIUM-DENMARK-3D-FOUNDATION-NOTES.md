# PROJECT 1864 Campaign v00.00.13n — CESIUM DENMARK 3D FOUNDATION

Status: DEV / afventer Package Manager resolve + Unity compile + Cesium ion runtime QA.

## Hovedændring

CampaignMap skifter fra custom DEM + OSM raster tiles til Cesium for Unity som geospatial renderer.

## Implementeret

- Officiel Cesium scoped registry tilføjet til `Packages/manifest.json`.
- `com.cesium.unity` pinned til `1.25.1`.
- `CampaignCesiumDenmarkV013N` opretter `CesiumGeoreference` centreret på Danmark.
- Cesium World Terrain ion asset `1` bruges som terrain.
- Bing Maps Aerial ion asset `2` bruges som imagery uden road/place-name labels.
- Campaign camera får `CesiumGlobeAnchor` og `CesiumOriginShift`.
- Home overview, wheel zoom, middle-drag/WASD pan.
- Pre-v13n Campaign presentation components deaktiveres.
- Legacy V013*/GIS_/Outline_/StrategicLink_/CampaignNode_/Settlement3D_ renderers force-hides.
- Lokal token-helper under `PROJECT 1864 > Campaign > Cesium ion token (lokal)`.
- Ingen token eller credential commits til GitHub.
- v13h1 MAP-ONLY combat gate bevares.

## Ikke ændret

- Tactical TEST.
- Campaign movement/ETA/logistics/state data.
- Historical OOB.
- Battle transition design ud over den eksisterende midlertidige MAP-ONLY gate.

## Første lokale test

1. Kør updater `3 = CAMPAIGN`.
2. Unity skal først resolve/installere Cesium package 1.25.1; første åbning kan tage længere tid.
3. Hvis Cesium ikke har en fungerende ion-token, brug editor-menuen `PROJECT 1864 > Campaign > Cesium ion token (lokal)` og gem en personlig token lokalt. Del ikke token i chat/GitHub.
4. Start `CampaignMap` Play Mode.
5. Overlay skal vise `v00.00.13n CESIUM DENMARK 3D FOUNDATION DEV`.
6. Der må ikke vises noget custom OSM-raster, håndbygget Denmark mesh eller gamle grønne terrain-lag.
7. Danmark skal vises via streamet 3D terrain + aerial imagery.
8. Musehjul zoomer; midterste mus/WASD panorerer; Home resetter til Danmark.

## Kendte DEV-forhold

- Bing Aerial er moderne visual reference, ikke 1864-facit.
- Historical road/rail/settlement overlays kommer i senere version.
- Legacy Input Manager-warning kan fortsat eksistere indtil en separat Input System migration.
- Runtime QA på brugerens Unity 6.6-maskine er påkrævet før 13n kan betragtes som stabil.
