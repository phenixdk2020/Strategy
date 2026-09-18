# Del 27: 62 — Campaign3 v00.00.10n7b: Vej 1 / City Art Polish

**Designbaseline:** v00.02.28  
**Prototypeversion:** v00.00.10n7b  
**Work branch:** `work/channel-campaign3-v10n7b-city-art-polish`

## Formål

v10n7b følger den besluttede **Vej 1**: stabiliser city-art først og lad Amt/county-systemet være slået helt fra.

Der må derfor ikke bruges udviklingstid i denne build på nye Amt-grænser, polygoner, click ownership eller selection highlight. Først når city-art er godkendt i Unity Play Mode, må county-laget genindføres som et separat system.

## Runtime ownership

`CampaignMapOnlyCityArtV010N7B` bliver den eneste runtime owner af byernes grafik.

Den deaktiverer:

- `CampaignMapOnlyCityArtV010N7A`,
- n6/n6g/n6h city-art renderers,
- historiske Amt-overlay experiments,
- Amt-info panels,
- continuity/grid/Voronoi boundary visuals.

World Imagery, streamed terrain, CITY-REG-01-data, city labels, CityId/ZoneId og construction-regler fortsætter uændret.

## Visuel city-standard

Kun **Proposal 3 — Isometric Miniature Town** er tilladt.

Der må ikke optræde en alternativ beige/generated fallback-stil. Hvis et Proposal-3 asset mangler, skal det logges som en eksplicit fejl i stedet for at skjule problemet med en anden grafik.

### Ny størrelse

n7b gør byerne væsentligt mindre:

- **A — Development City:** 1.55
- **B — Regional Town:** 1.12
- **C — Minor Town:** 0.78

Dette erstatter n7a's 2.20 / 1.60 / 1.15.

Målet er, at byerne kan læses som kortsymboler uden at dominere geografien eller skjule labels, fjorde, veje og kystlinjer.

## Skarphed

Runtime-textures sættes til:

- Bilinear filtering,
- Clamp wrap mode,
- anisotropic level 0,
- mip bias -0.75.

Den negative mip bias er en kontrolleret skarphedsforbedring ved campaign zoom. Hvis det giver shimmer/aliasing på brugerens hardware, skal bias reduceres i næste polish-pass i stedet for at opskalere selve byerne igen.

## Geografisk anchor

City-art bruger fortsat:

- canonical city marker som eneste X/Z-anchor,
- ingen art-offset i X/Z,
- bottom-center billboard,
- ground lift 0.16.

Den synlige grafik må derfor ikke flytte en bys geografiske position. Især Skagen, Frederikshavn, Sæby, Fredericia, Middelfart, København og andre kystnære byer bruges som QA-cases.

En by kan stadig visuelt overlappe lidt vand, fordi ikonet har en fysisk skærmstørrelse omkring et korrekt land-anchor. n7b reducerer dette problem gennem den væsentligt mindre scale; anchor må ikke kunstigt flyttes væk fra den historiske city position.

## Klik

Klik-target bevares større end den synlige by. Det betyder, at mindre grafik ikke skal gøre city selection unødigt vanskelig.

## Amt/county — eksplicit ude af scope

n7b må ikke vise:

- gule Amt-boundaries,
- historical parish polygon overlay,
- grid/Voronoi boundaries,
- Amt click ownership,
- Amt info panel,
- county highlight.

Når systemet senere genindføres, skal **samme authoritative historiske polygon source** bruges til:

1. visible boundary,
2. click ownership,
3. selected-area highlight.

Der må ikke igen eksistere separate geometrisystemer, som kan være uenige om hvilket Amt et punkt tilhører.

## QA

Versionen accepteres først når:

1. Unity 6000.6.0f1 compiler uden blocking errors.
2. topbar/badge viser v00.00.10n7b.
3. ingen Amt/county lines er synlige.
4. almindeligt land-click åbner ikke county-info.
5. log viser `Way1=True|MapOnly=True|CountyBorders=False`.
6. log viser `CityArtN7B=True|Cities=68`.
7. alle 68 cities har synlig Proposal-3 art.
8. ingen alternativ beige/generated settlement style ses.
9. ingen gamle round city cylinders eller TownGround ses.
10. A/B/C er tydeligt mindre end n7a/n6h.
11. small C towns kan stadig identificeres ved normal campaign zoom.
12. labels er læsbare.
13. kystbyer står på deres canonical anchor og opleves ikke som flyttet ud i åbent vand.
14. World Imagery + streamed terrain fungerer.
15. QA-army ved Vejle er fortsat fjernet.
16. registry forbliver 20 zones / 68 cities / urban checksum 290565.

## Næste fase

Efter accepteret city-art QA starter en separat **County Rebuild**.

Det er en ny gate. n7b er ikke en Amt-build.

## Status

**IMPLEMENTERET PÅ WORK BRANCH — AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA.**
