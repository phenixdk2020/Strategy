# Del 26: 61 — Campaign3 v00.00.10n7a: Map Only + City Art Cleanup

**Designbaseline:** v00.02.27  
**Prototypeversion:** v00.00.10n7a  
**Work branch:** `work/channel-campaign3-v10n7a-map-only-city-cleanup`

## Formål

v10n7a er en bevidst scope-reset efter flere versioner, hvor Amt-grænser og city-art blev ændret samtidigt. Formålet er at få ét stabilt visuelt delsystem ad gangen.

Denne version viser derfor campaign-kortet og byerne, men ingen Amt-grænser eller Amt-selection. Amt-systemet genindføres først, når city-art er accepteret i Unity Play Mode.

## Map-only regel

Følgende er midlertidigt slået fra i normal campaign-visning:

- Amt/county boundary overlay,
- historisk polygon-overlay,
- n6f grid-overlay,
- n6h continuity bridges,
- Amt/county klik-selection,
- Amt/county info-panel.

World Imagery, streamed 3D terrain, city labels og CITY-REG-01-data bevares.

## Én city-renderer

`CampaignMapOnlyCityArtV010N7A` er den eneste aktive ejer af byernes visuelle repræsentation.

Ældre city-systemer fra n6, n6g og n6h deaktiveres kontinuerligt, så flere renderer-paths ikke kan konkurrere om de samme 68 city markers.

### Visuel standard

Kun **Proposal 3 — Isometric Miniature Town** må bruges.

- A — Development City: base scale 2.20
- B — Regional Town: base scale 1.60
- C — Minor Town: base scale 1.15

Bygrafikken bruger bottom-center anchor og ingen X/Z-offset fra den kanoniske city marker. Det betyder, at rendereren ikke må flytte fx Skagen, Frederikshavn eller Sæby ud i vandet i forhold til deres registrerede position.

## Ingen skjult fallback-stil

n7a må ikke erstatte et manglende Proposal-3 texture med en beige/debug settlement-grafik. Hvis et asset ikke kan indlæses, logges en eksplicit fejl. Dette gør fejl synlige i stedet for at blande to city-art-stilarter på campaign-kortet.

## Asset-kvalitet

A- og B-assets opgraderes i denne pass til skarpere 128×128 RGBA Proposal-3 source art. C forbliver Proposal-3 og vises ved den mindste scale i denne slice; et senere samlet art-pass kan opgradere alle tre tiers til samme højere source resolution, når renderer/scale/placering er godkendt.

## Klik og data

City-markerens parent beholder en stabil BoxCollider. `CityId`, `ZoneId`, `Population1850`, A/B/C-tier og WGS84-positioner ændres ikke af n7a.

## QA

Versionen accepteres først når:

1. Unity 6000.6.0f1 kompilerer uden fejl.
2. v00.00.10n7a vises i topbar/badge.
3. Ingen gule Amt-grænser vises.
4. Klik på almindeligt land åbner ikke Amt-panelet.
5. `CityArtN7A=True|Cities=68` logges.
6. Alle 68 byer har city-art.
7. Kun Proposal-3-stilen ses.
8. Ingen gamle runde city-markører eller grøn TownGround ses.
9. Ingen beige fallback-bytype ses.
10. A/B/C er tydelige men væsentligt mindre end n6h.
11. Labels kan stadig læses.
12. Kystbyer testes specifikt for visuel placering.
13. World Imagery og streamed terrain fungerer fortsat.
14. QA-hæren ved Vejle er fortsat væk.
15. Registry-checksum forbliver 20 zoner / 68 byer / 290565 urban population.

## Næste fase

Når city-art er accepteret, starter en separat Amt/county-rebuild. Den må kun have én autoritativ geometri, der bruges identisk til boundary line, click ownership og selection/highlight.

## Status

**IMPLEMENTERET PÅ WORK BRANCH — AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA.**
