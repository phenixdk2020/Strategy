# Del 23: 58 — Campaign3 v00.00.10n6f: parish Amt boundaries, Proposal 3 city art og QA-oprydning

**Designbaseline:** v00.02.24  
**Prototypeversion:** v00.00.10n6f  
**Work branch:** `work/channel-campaign3-v10n6f-city-art-amt-click`

## Formål

v10n6f retter tre konkrete problemer fundet i Game View/video-QA:

1. Amt-grænserne i n6e kunne stadig skabe forkerte lokale forløb og kliksvar, især omkring Aalborg/Hjørring og andre naboområder.
2. Proposal 3-bygrafikken var fortsat ikke synlig; de promoverede PNG-payloads var ikke de tilsigtede assets.
3. Den midlertidige røde `DK-ARMY-QA` ved Vejle fyldte campaign-kortet og skulle fjernes fra normal visning.

## Amt-geometri: tæt sognebaseret ejerskabsfelt

n6f introducerer `CampaignHistoricalAmtOverlayV010N6F`.

Datagrundlaget er den offentlige `A_perfect_storm`-replikationsdata, hvor sognegeografien er dokumenteret som dansk sogneinddeling pr. 1. januar 1820 og oprindeligt fra DigDag. `Geo.csv` indeholder blandt andet `County`, `Hundred`, `Parish`, longitude og latitude.

I modsætning til n6e bruges nu de individuelle historiske sognecentroider som et tæt Amt-ejerskabsfelt. Det reducerer de lange kunstige Voronoi-arme, som opstod når ét gennemsnitspunkt pr. Herred skulle repræsentere et stort område.

Runtime:

- parser alle understøttede danske Amt/sognepunkter,
- mapper historiske County-navne til `ZONE-REG-01`,
- afviser åbent hav via campaign-landgeometrien,
- sampler ejerskab på et geografisk grid,
- tegner kun overgange mellem to forskellige Amt-ejere,
- skjuler den tidligere n6e/n5 synlige zone-linje efter den nye boundary mesh er klar.

Aktiv prototype-geometri:

`DIGDAG_1820_PARISH_DENSE_FIELD_GRID_CONTOUR`

## Samme system til linje og klik

`CampaignZoneInfoPanelV010N6F` erstatter den tidligere klikresolver i normal brug.

Regler:

- klik direkte på en by: brug altid byens kanoniske `City.ZoneId`,
- klik på almindeligt land: brug `CampaignHistoricalAmtOverlayV010N6F`,
- ingen `GrandCampaignZoneMarker`-fallback,
- ingen separat nearest-zone-centre-beregning,
- åbent hav giver intet Amt.

Designmålet er, at det Amt spilleren ser ved den gule grænse og det Amt infopanelet rapporterer kommer fra samme ejerskabsmodel.

## Historisk guardrail

n6f er et stort skridt mod historisk korrekt geografi, men må ikke beskrives som eksakte 1851-grænser.

- kildedatoen for sognegeografien er 1820,
- n6f bruger sognecentroider og et tæt klassifikationsfelt,
- n6f bruger ikke de originale polygonkanter for hvert sogn,
- n6f bruger ikke endnu et dateret DigDag Amt/Region polygonudtræk fra 1. januar 1851.

`HistoricallyExact1851 = false` er derfor fortsat bindende.

Produktionsmålet er stadig source-backed DigDag Amt/Region-polygoner for den relevante campaign-dato.

## Proposal 3 city art

De tre city-art-filer er erstattet med reelle transparente RGBA miniatureby-assets:

- `City_A_Proposal3.png`
- `City_B_Proposal3.png`
- `City_C_Proposal3.png`

De eksisterende n6e runtime-regler bevares:

- A = største miniatureby,
- B = regional miniatureby,
- C = lille settlement,
- billboard vender mod campaign-kameraet,
- transparent baggrund,
- ingen grøn TownGround,
- ingen synlig rund cylinder,
- usynlig BoxCollider bruges fortsat til stabilt klik.

## QA-hær

Den midlertidige campaign-testhær `DK-ARMY-QA` ved Vejle fjernes visuelt og kan ikke vælges i normal campaign-visning.

Dette udføres af `CampaignQaArmyRemovalV010N6F`. Bootstrap-recorden bevares i denne slice alene for rollback-kompatibilitet og bør fjernes permanent fra bootstrap, når campaign-army-systemet erstattes af den rigtige 1851 OOB.

## QA

Følgende skal verificeres i Unity 6000.6.0f1:

1. ingen compile errors,
2. topbar viser v00.00.10n6f,
3. `HistoricalAmtOverlay=True`,
4. mindst 500 historiske sogne-sites,
5. Amt-boundary mesh har segmenter,
6. gamle zone-linjer forsvinder når n6f mesh er klar,
7. Aalborg/Hjørring og Thisted/Viborg testes målrettet,
8. klik på land logger `Source=ParishOwnershipField`,
9. byklik bruger `CanonicalCity`,
10. Proposal 3 A/B/C-ikoner er synlige,
11. små C-byer er synlige,
12. ingen gamle city-cirkler eller grøn bund,
13. den røde QA-hær ved Vejle er væk,
14. World Imagery og streamed 3D terrain fungerer fortsat,
15. 20 zoner / 68 byer / 290565 bybefolkning er uændret.

## Status

**IMPLEMENTERET PÅ WORK BRANCH — AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA.**
