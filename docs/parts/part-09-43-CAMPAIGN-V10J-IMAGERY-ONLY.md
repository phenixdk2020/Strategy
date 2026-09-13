# PROJECT 1864 — Campaign v00.00.10j — Imagery Only

## Beslutning

Campaign3 bruger fra v00.00.10j kun World Imagery som aktivt campaign-basemap. Det tidligere 11-basemap-laboratorium er afsluttet som sammenligningsfase og er ikke længere en del af runtime-oplevelsen.

Den valgte retning bygger videre på det tidligere kort 9 / World Imagery, fordi det gav den klart bedste umiddelbare geografiske læsbarhed af Danmark, Limfjorden, øer, Skåne og Nordtyskland i Unity QA.

## Aktiv runtime

Der oprettes kun én basemap-root:

`BASEMAP_09_9_Imagery`

Rooten bruger den eksisterende camera-driven XYZ/Web-Mercator streaming engine fra v10i.

Der oprettes ikke længere runtime-roots til de tidligere kandidater:

- DEM / DHM pilot
- Cesium
- ArcGIS Topographic
- MapTiler
- historisk WMS
- QGIS baked
- procedural
- OSM
- diorama
- HOI4/province map

De gamle implementationer beholdes i repository/rollback-historikken, men Campaign3 v10j instansierer dem ikke.

## UI

Følgende fjernes fra runtime:

- TRUE 11 BASEMAPS-panelet
- 1-11 map-knapperne
- M / [ / ] provider-skift
- Shift+F1...F11 provider-valg
- provider-status for andre maps

World Imagery starter automatisk efter GrandCampaignBootstrap har oprettet kamera og WGS84 campaign-scaffold.

Det eksisterende v10e Natural Earth map-info-panel dækkes af et v10j-panel med korrekt information om World Imagery, WGS84 og historiske lag.

## World streaming

v10j bevarer v10i-streamingarkitekturen:

- dynamisk LOD z2-z12
- maks. 96 tiles i et view
- op til 8 samtidige requests
- center-prioriteret tile-kø
- atomic generation swap
- persistent tile-cache
- cleanup af runtime textures/materials/meshes

Kameraet kan fortsat bevæges uden scenesift mellem Danmark, Europa og world overview.

## Kontroller

- `Home` — Danmark
- `PageUp` — Europa
- `End` — verden
- `WASD` / pile — pan
- midterste mus + træk — grab/pan
- musehjul — zoom

## Historisk kontrakt

World Imagery er moderne billedmateriale og må ikke bruges som historisk 1851-sandhed.

Historiske gameplay-data ligger fortsat som separate WGS84-lag:

- politiske grænser
- byer og befolkning
- veje
- jernbaner
- havne
- skove/landbrug/land-use
- bygninger og militære anlæg
- hære og OOB

Det betyder, at imagery fungerer som visuelt terrain/reference-lag, mens PROJECT 1864 selv ejer historisk simulation og data.

## Projektion

Den nuværende world-pilot placerer Web-Mercator source tiles i den eksisterende lineære WGS84 Unity-projektion. Det er tilstrækkeligt til nuværende QA, men ikke endelig global arkitektur.

Fremtidigt globalt arbejde skal afklare:

- dateline wrap
- globe curvature vs. strategisk flad projection
- polarområder
- floating origin
- højdemodel/3D terrain under imagery
- historisk land-use/presentation overlay

## Licensing guardrail

World Imagery anvendes i denne fase som streamed development/prototype-basemap. En release må ikke pakke eller redistribuere en bulk/offline kopi uden særskilt provider- og licensafklaring. Attribution skal bevares.

## QA for v10j

1. Unity 6000.6.0f1 compiler uden errors.
2. `v00.00.10j` vises i Game view.
3. World Imagery starter automatisk.
4. Ingen 11-basemap-toolbar eller provider-knapper vises.
5. Ingen andre basemap-roots oprettes.
6. `Home` viser Danmark med tydelig Limfjord.
7. `PageUp` viser Europa.
8. `End` viser world overview.
9. Pan/zoom udløser korrekt LOD-streaming uden blankt kort under generation swap.
10. Campaign time/speed, selected zone/army og gameplay markers fungerer fortsat.
11. Natural Earth geometry og den gamle blue sea-board forbliver skjult.
12. Bundens map-info viser World Imagery/WGS84 og ikke Natural Earth 1:50m.

## Rollback

v10i før imagery-only beslutningen er gemt på:

`backup/channel-campaign3-v10i-before-imagery-only-20260913`
