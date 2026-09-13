# PROJECT 1864 — Campaign v00.00.10k — Imagery + 3D Terrain

## Beslutning

Campaign3 fortsætter med World Imagery som eneste aktive basemap. v10k bygger næste lag oven på v10j: streamed elevation/3D terrain under den samme imagery-overflade.

Målet er ikke at introducere et nyt kort. Imagery forbliver den synlige overflade; elevation ændrer geometrien på de samme tiles.

## Terrænarkitektur

Ny runtime-komponent:

`CampaignImageryTerrainV010K`

Komponenten scanner de World Imagery tiles, der oprettes af den eksisterende streamer, og matcher deres z/x/y-identitet mod Terrarium elevation tiles.

Elevation pilot source:

`Mapzen / AWS Terrain Tiles — Terrarium encoding`

Terrarium er valgt som global udviklingspilot, fordi den kan bruges med den eksisterende XYZ tile-identitet. Den er ikke endelig dansk terrænkilde.

Danmark skal senere kunne få en high-detail override fra officiel DHM/GeoDanmark uden at ændre World Imagery streaming-kontrakten.

## Graceful fallback

Elevation må aldrig være en single point of failure for campaign-kortet.

Hvis en elevation tile fejler:

- World Imagery forbliver synlig.
- Imagery-tile forbliver flad.
- Fejlen tælles/logges i terrain-status.
- Ingen anden basemap-provider bruges som fallback.

## Mesh

De oprindelige imagery tiles er simple quads. Når elevation er tilgængelig, erstattes tile-meshet med et subdivideret grid med samme UV-koordinater og samme imagery-materiale.

Grid-density følger imagery LOD:

- z5: 6×6 cells
- z6-z7: 10×10
- z8-z9: 16×16
- z10: 20×20
- z11-z12: 24×24

World overview under z5 bruger ikke elevation-streaming.

Det reducerer requests og mesh-omkostning, hvor relief alligevel ikke er visuelt relevant.

## Højdeskala

Gameplay-kortets render-skala er ikke 1 Unity unit = 1 meter. Ægte fysisk højdeskala ville derfor være næsten usynlig på strategisk zoom.

v10k bruger strategisk vertical exaggeration:

`x18`

Det er en præsentationsparameter, ikke historisk/fysisk dataændring. Den originale elevation i meter beholdes som samplinggrundlag, og kun Unity Y-visualiseringen exaggereres.

## Kamera

v10k lægger et 3D-view oven på world-streamerens eksisterende kamera-center.

Streamerens WGS84-center og LOD er stadig autoritative. Terrain-laget ændrer efterfølgende kun kameraets position/rotation i frame og ændrer ikke streaming-centeret.

Kontroller:

- `T` — toggle 2D/3D
- `Q` / `E` — rotate strategic 3D camera
- `Home` — Denmark
- `PageUp` — Europe
- `End` — world
- `WASD` / arrows — pan
- mouse wheel — zoom
- middle mouse drag — pan

Ved store world zooms auto-flatter kameraet for at undgå et kunstigt horisont-view på den nuværende flade globale projection.

## Gameplay markers

Terrænmesh får ingen MeshCollider. Det er bevidst, så terrain ikke fanger existing campaign raycasts.

Eksisterende gameplay-colliders på zones/armies bevares.

For at markers ikke synker ned i terrain, samples aktiv terrænhøjde for:

- `ZONE_*`
- `CITY_*`
- `ARMY_*`

Deres eksisterende visuelle offsets lægges oven på sampled terrain Y.

Hvis der ikke findes en aktiv terrain sample, bruges den eksisterende flat-map baseline.

## Atomic imagery generation

v10i/v10j imagery streaming bygger et nyt tile-root skjult og viser det først efter hele imagery-generationen er klar.

v10k kan processere både aktive og skjulte pending imagery tiles. Når pending generation aktiveres, kan den derfor allerede have elevation.

Hvis en pending imagery generation annulleres/destroyes under pan/zoom, ignorerer terrain-coroutinen den destroyed tile sikkert.

## Cache

Imagery cache er uændret.

Elevation cache placeres separat:

`<persistentDataPath>/PROJECT1864/TerrainCache/terrarium/<z>/<x>/<y>.png`

Terræncache er source-data cache og er uafhængig af World Imagery cache.

## Historisk kontrakt

World Imagery og elevation beskriver moderne fysisk reference, ikke 1851 samfunds-/infrastrukturtilstand.

Separate historiske WGS84-lag ejer fortsat:

- landegrænser
- regioner
- byer/befolkning
- veje
- jernbaner
- havne
- land-use
- bygninger/anlæg
- militære formationer/OOB

## Projection scope

v10k løser ikke den endelige globale projection.

Stadig åbent:

- dateline wrap
- poles
- globe curvature vs strategic flat projection
- floating origin

Disse ændringer bør isoleres i en senere version, efter 3D terrain er runtime-valideret.

## Næste planlagte punkter

1. QA af Danmark, Limfjorden og sydlige Norge med 3D terrain.
2. Officiel DHM/GeoDanmark high-detail terrain override for Danmark.
3. Global/floating-origin projection architecture.
4. Historisk 1851 land-use/presentation overlay.
5. Source-backed 1851 regions, cities and infrastructure data.

## QA

1. Unity 6000.6.0f1 compiler uden errors.
2. v00.00.10k badge vises.
3. World Imagery loader som i v10j.
4. Terrain-status viser elevation requests på z5+.
5. Danmark er stadig geografisk korrekt i imagery, inkl. Limfjorden.
6. Terrain relief ses ved Danmark/regional zoom.
7. Norge/Sverige viser tydeligere relief end Danmark.
8. T skifter mellem 2D og 3D.
9. Q/E roterer 3D camera view.
10. Pan/zoom/LOD atomic swap virker fortsat.
11. Gameplay markers ligger over terrain.
12. Army/zone selection og RMB marchordre virker fortsat.
13. World overview auto-flatter kameraet.
14. Ingen voksende duplicate imagery/terrain roots efter gentagne LOD-skift.

## Rollback

v10j før 3D terrain er gemt på:

`backup/channel-campaign3-v10j-before-3d-terrain-20260913`
