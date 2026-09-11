# PROJECT 1864 — Campaign v00.00.10g TRUE 11 BASEMAP LAB

## Formål

v00.00.10g korrigerer arkitekturen fra v00.00.10f. v10f sammenlignede 11 visuelle profiler oven på den samme Natural Earth 1:50m Danmark-geometri. Det opfyldte ikke kravet om 11 forskellige grundkort.

v10g indfører derfor 11 separate basemap-provider roots. Hvert valg i UI'en repræsenterer en selvstændig datakilde eller selvstændig map-builder. Der må ikke ske skjult fallback til Natural Earth, hvis en ekstern provider mangler API-key, netværk eller lokale assets.

## Arkitektur

```text
PROJECT1864_TRUE_11_BASEMAP_LAB_v000010g
├── BASEMAP_01_1_DEM
├── BASEMAP_02_2_Cesium
├── BASEMAP_03_3_ArcGIS
├── BASEMAP_04_4_MapTiler
├── BASEMAP_05_5_1842-99
├── BASEMAP_06_6_QGIS
├── BASEMAP_07_7_Procedural
├── BASEMAP_08_8_OSM
├── BASEMAP_09_9_Imagery
├── BASEMAP_10_10_Diorama
└── BASEMAP_11_11_HOI4
```

Kun den valgte basemap-root er aktiv. Gameplay-data ligger uden for basemap-providerne og fastholder WGS84 longitude/latitude som fælles identitet.

Den gamle `GEO_Denmark_NaturalEarth50m` og `Grand Campaign Sea` beholdes i v10e-baselinen af hensyn til rollback og eksisterende gameplay, men deres renderers skjules af v10g. De må ikke bruges til at få en manglende provider til at se fungerende ud.

## De 11 grundkort

| # | Provider | Faktisk v10g implementation | Afhængighed/status |
|---|---|---|---|
| 1 | DEM / DHM-GeoDanmark target | Eget 3D terrain fra Mapzen/AWS Terrarium DEM + hydrology cut | Kører uden key; officiel DHM er næste danske datakilde |
| 2 | Cesium World Terrain | Cesium 1.25.1, ion terrain asset 1 + Bing aerial asset 2 | Cesium token |
| 3 | ArcGIS | Esri World Topographic XYZ raster tiles | Netværk |
| 4 | MapTiler 3D | Quantized Mesh v2 gennem Cesium FromUrl + MapTiler overlay | MapTiler API key |
| 5 | Historiske kort | Datafordeler Høje målebordsblade WMS, 1:20.000 | Datafordeler API key |
| 6 | QGIS/Blender baked | Lokal baked texture + WGS84 bounds adapter | Lokale StreamingAssets |
| 7 | Procedural | Egen runtime land/relief/landcover mesh | Ingen key |
| 8 | OSM | OpenStreetMap Standard XYZ tiles | Netværk + attribution/cache-policy |
| 9 | Realistic imagery | Esri World Imagery XYZ tiles | Netværk |
| 10 | Diorama | Egen 3D miniature mesh, landskab og bymodeller | Ingen key |
| 11 | HOI4 | Egen province-cell mesh ud fra campaign zone centres | Ingen key |

## Limfjorden som QA-gate

Aalborg/Limfjorden er v10g's obligatoriske geografiske accepttest. Den tidligere 1:50m baseline viste ikke fjorden tilfredsstillende og kunne derfor ikke bruges som kvalitetsmål for et detaljeret campaign map.

Provider 1 har en eksplicit hydrologimaske, som skærer Limfjorden ud af landmeshet. Masken følger et polyline-forløb fra den vestlige Limfjord gennem de centrale bredninger og øst for Aalborg. Samme hydrologifundament bruges i de genererede providers 7, 10 og 11.

Providerne 3, 5, 8 og 9 skal vise deres egen kildes repræsentation af Limfjorden. Providerne 2 og 4 valideres i deres egne globale terræn-/globe-koordinatsystemer.

Dette er en QA-gate: en kandidat kan ikke vælges som endeligt Danmark-grundkort, hvis Aalborg/Limfjorden stadig fremstår som sammenhængende land.

## Data- og historikregler

- Moderne OSM, ArcGIS, imagery, Cesium og MapTiler bruges til teknisk og visuel basemap-sammenligning. De er ikke 1851-kilder.
- Provider 1 bruger i v10g Mapzen Terrarium som runnable elevation pilot. Det må ikke omtales som Danmarks Højdemodel.
- Produktionsmålet for dansk detailterrain er DHM kombineret med danske vektor/hydrologidata.
- Høje målebordsblade dækker historiske kort fra perioden 1842–1899. Det er tæt på campaign-perioden, men et enkelt ark kan have en opmålings-/udgivelsesdato, som ikke er præcis 1851. Senere temporal QA skal derfor registrere sheet-date.
- 1851-veje, jernbaner, havne, byudbredelse, skove, administrative grænser og militære anlæg skal ligge i historiske gameplay/overlay-lag og må ikke udledes ukritisk fra moderne basemaps.
- HOI4-provinserne i v10g er simulation scaffolding, ikke historiske administrative grænser.

## Credentials

Følgende miljøvariable understøttes:

```text
CESIUM_ION_TOKEN
MAPTILER_API_KEY
DATAFORDELER_API_KEY
```

Lokale alternativer under Unity `Application.persistentDataPath`:

```text
PROJECT1864/Cesium/ion-token.txt
PROJECT1864/Keys/maptiler.txt
PROJECT1864/Keys/datafordeler.txt
```

Secrets må ikke committes til GitHub.

## QGIS baked contract

Provider 6 forventer:

```text
StreamingAssets/PROJECT1864/Basemaps/QGIS/qgis-denmark.png
StreamingAssets/PROJECT1864/Basemaps/QGIS/qgis-denmark.bounds
```

`qgis-denmark.bounds` indeholder én linje:

```text
minLon,minLat,maxLon,maxLat
```

Hvis filerne ikke findes, skal UI vise `QGIS BAKED ASSET MISSING`. Der må ikke vises et andet basemap som erstatning.

## Tile/cache-regler

Raster-providerne henter kun det aktive Danmark-overblik. v10g har ikke en bulk/offline-downloadfunktion.

OSM-tiles gemmes i den fælles provider-cache med mindst syv dages friskhedsinterval for at undgå unødvendige genkald. Synlig attribution beholdes i basemap-informationspanelet.

Cacheplacering:

```text
<persistentDataPath>/PROJECT1864/BasemapCache/provider-XX/z/x/y.img
```

## Kontroller

- `M` eller `]`: næste grundkort.
- `[`: forrige grundkort.
- `Shift+F1` ... `Shift+F11`: direkte valg.
- Cesium/MapTiler: WASD/piletaster, musehjul, midterste museknap, `Home` reset.

## QA før promotion

1. Unity Package Manager skal installere Cesium 1.25.1 uden package-fejl.
2. Projektet skal compile uden C# errors.
3. Alle 11 knapper skal eksistere, og source/status-panelet skal skifte korrekt.
4. Provider 1 skal bygge faktisk terrain fra DEM og vise Limfjorden ved Aalborg.
5. Provider 3, 8 og 9 skal vise tre forskellige eksterne rastergrundkort.
6. Provider 2 og 4 skal testes med gyldige respektive keys.
7. Provider 5 skal testes med Datafordeler-key og må ikke fallbacke ved manglende key.
8. Provider 6 skal rapportere manglende asset indtil et ægte QGIS-bake er leveret.
9. Provider 7, 10 og 11 skal generere tre separate Unity-geometrier.
10. Campaign selection, orders, clock og WGS84 anchors skal regressions-testes på lokale map modes.
11. Aalborg/Limfjorden screenshots fra mindst provider 1, 3, 5, 8 og 9 skal indgå i næste mapvalg.

## Rollback

v10f er bevaret i:

`backup/channel-campaign3-v10f-before-true11-20260911`

Den tidligere Cesium-baserede v13n3 Campaign3 er bevaret i:

`backup/channel-campaign3-v13n3-before-maplab11-20260911`
