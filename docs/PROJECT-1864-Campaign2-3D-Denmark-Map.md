# PROJECT 1864 — campaign2 3D Danmark-diorama

**Status:** DESIGN LÅST / IKKE IMPLEMENTERET  
**Spor:** `campaign2`  
**Version:** v00.00.16-C2  
**Branched from:** `channel-campaign` v00.00.13m

## 1. Beslutning

`channel-campaign` fortsætter som det aktive kampagnekort.  
`campaign2` er et **ekstra spor** til at bygge hovedkortet forfra som et 3D-diorama.

Vi lapper ikke v13n oven på v13j/k/l/m. OSM-drape er forkert både som århundrede og som 3D-sprog.

## 2. Hvad der er galt i v13m

- Danmark er ~400 km nord–syd på ~520 Unity-enheder. Rigtig max-relief (~170 m) bliver under 0,3 enheder. Kortet *er* fladt.
- Huden er OpenStreetMap 2026, ikke 1864.
- Vand er et Y-plan. Fyn, Als, Limfjorden og bælterne bliver tegning, ikke øer.
- v13m-udsnit (~54,48–57,82 N) skærer Holsten næsten af. 1864-teatret kræver Slesvig-Holsten.
- Kamera min-højde 0,12 jager nærhed i et 2D-kort i stedet for at vise relief.

## 3. Målbillede

Ikke Google Earth. Ikke HOI4-satellit.  
**Total War / Grand Tactician-kampagnekort:** malet 3D-land, man kan pitche kameraet ned i, og hvor en kolonne kan følges på en vej over bakker.

Danmark læses som 3D først via:

1. Rigtig kyst — øer, fjorde, bælter som vand vs land.
2. Kraftig visual vertical exaggeration — moræne, Dybbøl, bakkeøer.
3. Malet landcover — ikke satellit.
4. Kamera med pitch 25–70°.

## 4. Unity-arkitektur

Én synlig renderer. Ingen live tile-hud.

| Del | Valg |
|---|---|
| Land | Ét bagt mesh (foretrukket) eller Unity Terrain fra DEM |
| Vand | Separat overflade i havniveau. Kyst som øer/huller |
| Elevation | Copernicus EU-DEM eller SDFE DHM, bagt offline |
| Visual Y | 8× / 12× / 16× exaggeration som testmodes |
| Sand elevation | gemmes separat; march/ETA må ikke bruge visual Y |
| Look | splatmap: ager, hede/mose, skov, klit, by-vask |
| 1864 | vektor/modeller ovenpå: veje, byer, jernbane, forter |
| Forbudt som base | OSM Standard, satellit, Cesium, ArcGIS globe |

### 4.1 Geografisk envelope

Første teater:

- 53,2–58,0 N
- 7,5–13,5 Ø

Det dækker Danmark, Slesvig, Holsten, Kiel og Hamborg-tilgangen.  
`CampaignGeoProjection` forbliver lat/lon-kontrakten. Unity-scale må ændres for læsbarhed uden at ændre DistanceKm.

Bredere Nordeuropa-envelope (47–71,5 N) er senere streaming — ikke første gate.

### 4.2 Lag

Samme L0–L10-kontrakt som v13-arkitekturen, men **kun ét synligt land+vand-stack**:

- L0 lat/lon + rutegeometri (authoritative)
- L1 bagt DEM-landmesh
- L2 vand/kyst
- L3 landcover-splat
- L4 historisk infrastruktur (senere)
- L5 byer/forter som små 3D-fodaftryk (senere)
- L6 formationer (senere, MAP-ONLY indtil da)
- L9 lys/skygge/tåge, så relief kan læses
- L10 labels/debug

Målebordsblade må være et slå-til overlay. De må ikke være selve verden.

### 4.3 Kamera

- Pitch 25–70°
- Pan + zoom fra teater-overblik til nær inspektion af fjorde/forter
- Terrain-aware minimumshøjde, men ikke 0,12-enheders «kort-klistermærke»-kamera
- Home/reset til Danmark-teater

## 5. Første acceptance-gate

Kaldes **C2-G1 Map Reset** og er bestået når:

1. Unity 6000.6.0f1 compiler uden røde fejl på `campaign2`.
2. Ét synligt land-mesh + ét vand-mesh. Ingen OSM-atlas som base.
3. Danmark *ser ud* som øer i et hav også fra skrå vinkel.
4. Pitch viser relief uden labels.
5. Dybbøl, Als, Fredericia, Dannevirke-linjen og Flensborg kan peges ud på formen.
6. Ingen moderne motorveje/forstæder i baselook.
7. Simulation uændret: lat/lon, DistanceKm, ETA.
8. MAP-ONLY: ingen battle-transition.
9. `channel-campaign` og `channel-test` er uændrede.

Først derefter: historiske veje, by-LOD, hære på vejene.

## 6. Hvad der bevidst ikke ændres

- `CampaignGeoProjection` som authoritative geo-API
- Tactical isolation / `PrototypeBattle` explicit-only
- Legacy Input Manager (separat migration)
- v13m på `channel-campaign`

## 7. Implementeringsrækkefølge på dette spor

1. Editor-pipeline: DEM → landmesh + watermesh for C2-envelope.
2. Splat/landcover-materiale og sol/skygge.
3. Kamera-pitch og teater-framing.
4. Semantic landmarks (Dybbøl, Fredericia, København, Flensborg, Kiel) som små 3D-fodaftryk.
5. Først da historisk vej/jernbane-vektor.

## 8. Merge-regel

Ingen merge `campaign2` → `channel-campaign`, før Allan har kørt Play Mode og C2-G1 er accepteret.
