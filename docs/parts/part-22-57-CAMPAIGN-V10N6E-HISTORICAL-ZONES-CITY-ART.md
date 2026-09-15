# Del 22: 57 — Campaign3 v00.00.10n6e: historiske Amt-seeds + synlig Proposal 3 bygrafik

**Designbaseline:** v00.02.23  
**Prototype:** v00.00.10n6e  
**Workbranch:** `work/channel-campaign3-v10n6e-historical-zones-city-art`  
**Target:** `channel-campaign3`

## Formål

n6e retter to Play Mode-problemer, som fortsat var synlige efter n6d:

1. Amtsgrænserne kunne danne lange kunstige udløbere ind i naboområder, fordi geometrien stadig var en sparsom Voronoi-model baseret primært på zonecentre og købstæder.
2. Proposal 3-bygrafikken var teknisk tilkoblet, men for lille/lav i world-space til at være tydelig på campaign-kortet.

## Historisk Amt-seedfelt

`CampaignZoneOverlayV010N` bruger i n6e et nyt historisk seedlag. Første prioritet er `Data/Geo.csv` fra forsknings-/replikationsrepoet `christianvedels/A_perfect_storm`. Repoets dokumentation angiver, at den underliggende sognegeografi stammer fra DigDag og repræsenterer danske sogne pr. **1. januar 1820**.

CSV-rækkerne grupperes efter `County + Hundred`. For hver historisk Herred-gruppe beregnes et gennemsnitligt WGS84-seed. Dette giver et langt tættere geografisk seedfelt end den tidligere model med 20 zonecentre og 68 købstæder alene.

De relevante `County`-værdier mappes til eksisterende `ZONE-REG-01` IDs. Hertugdømmer og andre områder uden for den aktuelle 20-zone Kongeriget Danmark-baseline ignoreres i denne build.

Efter de historiske Herred-seeds tilføjes CITY-REG-01 og ZONE-REG-01 som guard sites. De historiske seeds skal dermed styre den overordnede form, mens canonical guard sites beskytter kendte by- og zoneidentiteter.

### Runtime dataflow

1. Forsøg at læse en tidligere cache fra `Application.persistentDataPath`.
2. Hvis cache ikke findes/kan valideres, hentes den offentlige `Geo.csv`-kilde.
3. Mindst 40 historiske Herred-seeds kræves for at acceptere datasættet.
4. Ved succes caches rå CSV lokalt til senere starter.
5. Hvis download/cache fejler, falder runtime eksplicit tilbage til den gamle city+zonecentre-model og logger dette.

## Historisk guardrail

n6e er **ikke** slutmålet for 1851-grænser.

- Kilden repræsenterer 1820.
- n6e bruger Herred-centroider, ikke de originale Amt-polygoner.
- `HistoricallyExactGeometry=false` forbliver bindende.
- Aktiv mode ved historisk seedload er `DIGDAG_1820_HERRED_SEEDS_LAND_CLIPPED_APPROX`.
- Produktionsmålet er fortsat de daterede DigDag **Amt/Region** polygoner gældende 1. januar 1851.

Fordelen ved n6e er, at zoneformen nu bygger på et tæt historisk administrativt datasæt i stedet for en ren spilteknisk nærmeste-by-model.

## Klik skal følge synlig polygon

n6e fjerner den dobbelte sandhed mellem synlig grænse og klik-resolver.

- Direkte klik på en by bruger altid byens kanoniske `City.ZoneId`.
- Områdeklik bruger `CampaignZoneOverlayMetadataV010N.ZoneId` fra den faktiske polygon, som indeholder klikpunktet.
- Der køres ikke bagefter en separat nearest-site resolver, som kan ændre Amt-navnet.
- `GrandCampaignZoneMarker`-collider bruges ikke som fallback.
- Klik uden for en zonepolygon giver intet Amt i stedet for at gætte.

Dermed skal infopanelet altid beskrive det samme område, som spilleren ser under markøren.

## Proposal 3 city-art visibility

`CampaignCityIconV010N6` beholder de transparente Proposal 3 A/B/C assets, men n6e gør dem langt mere synlige.

- direkte `Resources.Load<Texture2D>()` bevares,
- sekundær `Resources.LoadAll<Texture2D>()` folder-scan tilføjes,
- gamle n6/n6b/n6c visual children fjernes,
- `GroundLift` øges fra 0,09 til **0,32**,
- base scale ændres til:
  - **C = 1,18**
  - **B = 1,60**
  - **A = 2,15**
- population giver fortsat kun en begrænset ekstra variation,
- sprite sorting order øges til 120,
- gammel rund city-renderer skjules,
- der genereres ingen grøn `TownGround`,
- én usynlig `BoxCollider` er fortsat stabil klikflade.

## QA

1. Unity 6000.6.0f1 kompilerer uden fejl.
2. Build-ID viser `v00.00.10n6e`.
3. `HistoricalCountySeeds=True` logges ved vellykket historisk load.
4. Historisk seed count er mindst 40.
5. Aktiv geometri er `DIGDAG_1820_HERRED_SEEDS_LAND_CLIPPED_APPROX` ved historisk load.
6. Aalborg/Hjørring/Thisted/Viborg/Randers/Vejle kontrolleres visuelt for lange kunstige udløbere.
7. Zoneinfo logger `Mode=VISIBLE_POLYGON_METADATA`.
8. Et klik inde i en synlig polygon giver polygonens eget ZoneId.
9. Byklik giver fortsat kanonisk City.ZoneId.
10. Åbent hav vælger ikke et Amt.
11. `Proposal3TexturesLoaded=True`.
12. `Proposal3CityArt=True|Cities=68`.
13. A/B/C-bygrafikken er tydeligt synlig ved normal campaign zoom.
14. C-byer er synlige og mindre end B/A.
15. Ingen runde city-cylindre eller grøn TownGround er synlige.
16. World Imagery, 3D terrain, movement og withdrawal regressions fungerer.

## Status

**IMPLEMENTERET PÅ WORKBRANCH — kræver Unity 6.6 compile + Play Mode QA.**
