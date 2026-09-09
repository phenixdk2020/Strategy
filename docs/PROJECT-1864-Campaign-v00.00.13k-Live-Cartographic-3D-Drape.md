# PROJECT 1864 Campaign v00.00.13k — Live Cartographic 3D Drape

Status: DEV / MAP-ONLY / afventer Unity compile + runtime QA.

## Formål

v13k tester den direkte løsning, hvor et rigtigt landkort bruges som synlig overflade på et 3D-terræn. Kortet er derfor ikke længere primært en genereret grøn terrain-shader. Et kartografisk tile-lag lægges direkte oven på det glatte v13j DEM-mesh og følger terrænets højder.

## Renderpipeline

1. v13i sikrer DEM-cache.
2. v13j bygger det sammenhængende glatte Danmark-mesh.
3. v13k finder kameraets aktuelle map-target.
4. Kun tiles omkring det aktuelle viewport hentes/indlæses.
5. Hver tile genereres som et subdivideret mesh.
6. Hvert vertex samples mod `V013J_SmoothTerrain` og følger terrænets Y.
7. Hvor terrain-collideren ikke findes, bruges vandniveauet som fallback.
8. Map tile-rendereren ligger ganske lidt over overfladen for at undgå z-fighting.

Resultatet er et almindeligt 2D-landkort draperet over rigtig 3D-topografi.

## Map provider

Default DEV-provider er OpenStreetMap Standard. Dette er alene en interaktiv development/QA-basemap og er ikke den endelige 1864-kilde.

Client-regler:

- højst et 3x3 tile-vindue omkring det synlige map-target,
- ingen automatisk download af hele Danmark,
- ingen multi-zoom prefetch,
- sekventielle requests,
- 14 dages lokal cache,
- synlig `© OpenStreetMap contributors · ODbL` attribution,
- unik PROJECT 1864 User-Agent.

Provider kan ændres lokalt uden code change via:

`Application.persistentDataPath/PROJECT1864/MapProvider/provider.json`

Eksempel:

```json
{
  "UrlTemplate": "https://example-provider/{z}/{x}/{y}.png",
  "Attribution": "Map © Example Provider",
  "UserAgent": "PROJECT1864-Campaign/0.13k"
}
```

API keys/tokens skal ligge lokalt og må ikke commits til GitHub.

## Historisk raster

Hvis filen

`Application.persistentDataPath/PROJECT1864/HistoricalMap/denmark_1864.png`

findes, fortsætter v13j med at bruge denne som full-map raster, og v13k springer live tile requests over. Dette er den foretrukne slutretning, når et korrekt licenseret/georefereret historisk kort er gjort klar.

## Zoom

Tile zoom følger kameraet:

- langt Danmark-view: z7,
- regionalt view: z8,
- nær-regionalt view: z9,
- lokalt view: z10,
- meget tæt view: z11.

Kameraet får minimumshøjde 0.22, terrain clearance 0.14 og near clip 0.005.

## Prototype cleanup

Når v13k er aktiv, skjules de tidligere `GIS_Road_*` og `GIS_Ferry_*` presentation-lines. Dermed kan en simpel node-to-node linje ikke længere ligge hen over åbent vand som synlig vej. Simulationens route-data slettes eller ændres ikke.

Prototype city blocks og v13j-labels skjules også, så de ikke konkurrerer med kortets egne byer og stednavne.

## Aalborg og Aarhus landmarks

v13k opretter to meget små QA-landmarks:

- `V013K_Aalborg_Barracks_Landmark`
- `V013K_Aarhus_Farm_Landmark`

Aalborg-kasernen anchors på land syd for Limfjorden med radial nearest-land fallback, så landmarket ikke må stå i vandet. Begge landmarks er kun synlige ved tæt zoom.

Disse landmarks er presentation QA og etablerer ikke en historisk præcis bygningsplacering.

## Historisk datadisciplin

OpenStreetMap-data er nutidige. De må bruges til at teste renderkvalitet, coast/water readability og map-navigation, men de må ikke automatisk blive authoritative 1864 roads, railways, buildings eller settlement extents.

Historiske infrastructure-/settlement-data skal senere komme fra godkendte 1864-kilder.

## Acceptance gate

- Unity compiler uden nye errors.
- Et rigtigt kort kan ses direkte på 3D-terrain.
- Kortet følger terrænets relief i stedet for at være et separat fladt plane.
- Scroll-zoom kan komme ned i lokalt map-view.
- Ingen prototype road line må krydse åbent vand.
- Aalborg Kaserne QA landmark står på land og bliver synlig ved tæt zoom.
- Aarhus Farm QA landmark står på land og bliver synlig ved tæt zoom.
- Live provider henter kun aktuelle viewport-tiles.
- Attribution er synlig ved live tiles.
- Campaign combat forbliver deaktiveret i MAP-ONLY QA.
