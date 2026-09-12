# Campaign v00.00.10i — World Imagery Streaming Pilot

## Formål

v10i gør kandidat 9, **Imagery**, til den første egentlige globale streaming-prototype i Campaign3. v10h hentede et fast Danmark-udsnit ved z8. v10i beregner i stedet det nødvendige tile-sæt ud fra kameraets aktuelle geografiske centrum, viewport og zoom.

Det ændrer ikke campaign-simulationens datakontrakt. WGS84 longitude/latitude er fortsat autoritativt for gameplay. Imagery er et præsentationslag.

## Arkitektur

Provider 9 bruger fortsat `CampaignRasterBasemapV010G`, men klassen skifter automatisk til world-streaming mode når `providerId == 9`.

Provider 3 (ArcGIS Topographic) og provider 8 (OSM) beholder v10h's faste Danmark-overview, så de kan bruges som stabile sammenligningskort.

### Generation swap

World-streaming bruger to roots:

- aktiv generation — det kort spilleren ser
- pending generation — næste område/LOD som bygges skjult

Når alle tiles i pending-generationen er færdige, aktiveres den samlet, og den gamle generation destrueres. Derfor bør spilleren ikke se et halvt nyt kort oven på et halvt gammelt kort under normale LOD-skift.

Hvis kameraet flyttes igen under loading, invalideres pending-generationen. Efter ca. 0,22 sekunders stabilt kamera bygges en ny generation for den aktuelle view-frustum.

## Dynamic LOD

LOD vælges ud fra orthographic size:

| Orthographic size | XYZ zoom |
| ---: | ---: |
| > 1200 | z2 |
| > 700 | z3 |
| > 360 | z4 |
| > 190 | z5 |
| > 100 | z6 |
| > 55 | z7 |
| > 27 | z8 |
| > 13.5 | z9 |
| > 6.8 | z10 |
| > 3.4 | z11 |
| ellers | z12 |

Der tilføjes en tile-margin omkring viewporten. Hvis det valgte niveau kræver mere end 96 tiles, sænkes zoomniveauet, indtil tile-budgettet er overholdt.

## Start- og preset-views

- **Home** — Danmark, 10.20°E / 56.10°N, ortho 43
- **PageUp** — Europa, ca. 14°E / 52°N, ortho 255
- **End** — world overview, 0° / 0°, ortho 1800

Navigation:

- WASD / piletaster — pan
- midterste museknap + træk — grab/pan
- musehjul — zoom

Pilotgrænser:

- longitude: -179.5..179.5
- latitude: -80..80
- imagery tiles: Web Mercator

## Tile streaming

- op til 8 samtidige requests
- center-nære tiles prioriteres
- persistent disk cache genbruges fra provider 9
- cache key: provider / zoom / x / y
- gamle runtime-materialer, textures og meshes frigives eksplicit ved generation swap
- neutral mørk missing-tile bruges ved enkeltstående source-fejl
- ingen skjult Natural Earth fallback

## Projektion

Dette er en **world-streaming technology pilot**, ikke den endelige globale projektion.

Esri World Imagery leveres som Web-Mercator tiles. v10i placerer tile-hjørnerne via projektets eksisterende WGS84 -> Unity lineære `CampaignGeoProjection`. Det giver et brugbart globalt sammenligningskort, men følgende mangler før production-world-map:

- korrekt global projektion/globe-model
- dateline wrap
- polar treatment
- floating origin
- højpræcis geodesisk camera navigation
- world-scale physics/selection partitionering

## 1851-regel

Moderne imagery er **ikke** historisk 1851-sandhed. Historisk simulation skal fortsat leveres som separate lag:

- politiske grænser
- byer/befolkning
- veje
- jernbane
- havne
- forter/kaserner
- landbrug/industri
- skov/hede/mose/land-use
- militære formationer

Imagery kan bruges som visuelt terrænfundament og senere tones, maskeres eller overmales af historiske land-use lag.

## Licensregel

v10i er en udviklings-/sammenligningsprototype. Den må ikke fortolkes som godkendelse til at bulk-downloade og distribuere en global offline kopi af imagery. Eventuel release-arkitektur kræver særskilt provider/licens-review.

## QA

1. Start Campaign3 og vælg 9 Imagery.
2. Bekræft `v00.00.10i` badge og `WORLD z...` status.
3. Bekræft at Danmark og Limfjorden loader.
4. Zoom ind til mindst z10 og ud igen til z6 eller lavere.
5. Kontroller at gammel generation bliver stående mens ny generation hentes.
6. Pan mod Sverige/Tyskland og kontroller streaming.
7. Tryk PageUp og kontroller Europa-view.
8. Tryk End og kontroller world-view.
9. Tryk Home og kontroller retur til Danmark.
10. Skift til et andet basemap og kontroller at det lokale Danmark-kamera gendannes.
11. Skift tilbage til 9 og kontroller at world-view state er bevaret.
12. Gentag pan/zoom og kontroller Hierarchy for lækkende `WORLD_IMAGERY_PENDING` roots.

## Versionering

- v00.00.10h: raster resume + atomic Denmark overview
- v00.00.10i: camera-driven world imagery + dynamic LOD + world camera presets
