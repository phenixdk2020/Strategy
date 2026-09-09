# PROJECT 1864 — Campaign v00.00.13i GIS Terrain Foundation

Status: **DEV / MAP-ONLY / afventer Unity compile + runtime QA**

## Formål

v00.00.13i erstatter den håndbyggede/polygon-dominerede Danmark-præsentation med en ren GIS-orienteret runtime foundation. Målet er at få rigtig terrænrelief, tydelig hydrologi og en struktur der senere kan modtage højopløselige GeoDanmark/DHM/historiske data uden at ændre campaign-simulationens lat/lon-kontrakt.

## Datakilder og provenance

### Elevation

Pilot-DEM hentes fra Mapzen/Terrain Tiles Terrarium-format via AWS public dataset:

`https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png`

Terrarium decode-kontrakt:

`height_m = R * 256 + G + B / 256 - 32768`

v13i bruger zoom 8 og cacher downloadede PNG tiles lokalt. Hvis netværk/cache ikke er tilgængelig, bruges en deterministisk low-relief fallback. Fallback er kun en QA-funktion og må ikke beskrives som rigtig DHM-elevation.

### Coastline

Natural Earth 1:50m DNK-data, allerede indlejret i `CampaignDenmarkGeography`, bruges fortsat som midlertidig landmask/coastline scaffold.

### Hydrology

v13i indeholder et Danmark-pilotlag med eksplicitte lat/lon-korridorer for:

- Limfjorden
- Mariager Fjord
- Randers Fjord
- Horsens Fjord
- Vejle Fjord
- Kolding Fjord
- Odense Fjord
- Roskilde Fjord
- Isefjord
- Ringkøbing Fjord
- Nissum Fjord

Disse er **pilot vectors**, ikke endelig GeoDanmark-hydrologi. De skal senere erstattes af højopløselige authoritative water polygons/lines.

## Geospatial kontrakt

Strategisk state bruger fortsat campaign lat/lon og geodesiske afstande. DEM/elevation ændrer kun Unity Y-coordinate/presentation. Terrain tessellation, vertical exaggeration og render-LOD må aldrig ændre ETA, route distance eller simulation outcome.

## Runtime-pipeline

1. CampaignMap starter i v13h1 MAP-ONLY gate.
2. Tidligere v13 presentation components deaktiveres.
3. Legacy map renderers skjules.
4. `V013I_DENMARK_GIS_FOUNDATION` oprettes.
5. Sea base + hydrology water surfaces bygges.
6. Terrarium tiles hentes fra cache/network sekventielt.
7. Terrain grid tiles bygges i broad campaign projection.
8. Denmark coast/hydrology mask afgør hvilke terrain quads der renderes.
9. Coastline, roads/ferries, settlements, Aalborg harbour/barracks og landscape dressing bygges efter terrain er klar.
10. Semantic Denmark-only labels tegnes af v13i.

## Aalborg pilot

Aalborg skal i denne version visuelt læses som en **Limfjord harbour city**:

- Limfjorden skærer terrain ved Aalborg.
- Aalborg ligger på sydsiden af fjorden.
- dekorativ Nørresundby-kontekst vises på nordsiden.
- kajer/pakhuse/små både/færge giver havneidentitet.
- ingen moderne Limfjordsbro må opfindes som 1864-element.
- Aalborg barracks construction compound skal være tydeligt synligt ved close zoom.

## Infrastruktur-pilot

Generic `StrategicLink_*`/v13e links skjules. v13i viser kun eksplicitte Denmark pilot routes.

Særlige regler:

- Roskilde → Fredericia må **ikke** tegnes som direkte landlinje over åbent vand.
- Nyborg ↔ Korsør tegnes som færge.
- Fredericia ↔ Middelfart tegnes som færge; videre mod Odense som landrute.
- Rail visualisering fra generiske `HasRail` flags undertrykkes, indtil 1864-historiske ruter er valideret.

## MAP-ONLY fortsætter

v13h1 gate forbliver aktiv:

- ingen QA enemy march orders,
- ingen strategic formation movement,
- ingen hostile contact popup,
- ingen tactical transition,
- formation presentation skjult.

Campaign clock fortsætter, så staged construction kan testes.

## Acceptance gate

- Unity compiles uden errors.
- Terrain vises med DEM-data fra cache/network eller tydeligt fallback-mode.
- Danmark har synligt 3D-relief uden prototype-slabs.
- Limfjorden er tydelig og Aalborg læses som havneby.
- kun danske settlements/labels vises.
- ingen duplicate legacy labels.
- ingen Roskilde-Fredericia straight water-crossing road.
- Nyborg-Korsør og Fredericia-Middelfart vises som water crossings/ferry.
- Aalborg barracks compound er synligt ved close zoom.
- kamera pan/rotate/zoom/Home fungerer.
- MAP-ONLY battle gate forbliver aktiv.

## Næste datatrin

Når v13i pipeline er visuelt valideret, skal pilot-vectors gradvist erstattes af højopløselig GeoDanmark/DHM/historisk data. Det er en dataopgradering på samme geospatial/render architecture — ikke endnu en ny map stack.
