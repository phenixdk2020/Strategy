# PROJECT 1864 — Campaign3 v00.00.10n Zone Overlay

**Designbaseline: v00.02.12**  
**Campaign build: v00.00.10n**  
**Work branch: `work/channel-campaign3-v10n-zone-overlay`**

## 46. Formål

v00.00.10n gør de 20 kanoniske `ZONE-REG-01`-zoner visuelt læselige som territoriale områder på kampagnekortet. v10m etablerede de korrekte zoneidentiteter og alle 68 købstæder i runtime; v10n tilføjer overlay-/polygonarkitekturen uden at ændre city-, population-, construction- eller movement-state.

Det er vigtigt at skelne mellem **historisk zoneidentitet** og **zonegeometri**. Navne, `ZoneId`, bytilknytning og simulationsstruktur følger designmanualens 1851-baseline. Selve grænselinjerne i v10n er derimod en kontrolleret prototype, indtil faktiske historiske polygoner importeres fra en dokumenteret GIS-kilde.

## 46.1 Historisk guardrail

`CampaignZoneOverlayV010N` eksponerer eksplicit:

- `GeometryMode = PROTOTYPE_CENTRE_DERIVED_SECTORS`
- `IntendedHistoricalSource = DigDag - Amt og Region`
- `HistoricallyExactGeometry = false`

Ingen UI-, save-, AI-, movement- eller økonomilogik må bruge v10n-prototypepolygonen som historisk administrativ facit. Zoneidentiteten er autoritativ; den midlertidige polygon er kun visualiserings- og arkitekturdata.

Den planlagte produktionskilde er **DigDag**, Digitalt atlas over Danmarks historisk-administrative geografi. DigDag dækker administrative inddelinger fra ca. 1660 og frem og har særskilt datasæt for **Amt og Region**. Når polygonerne importeres, skal de kobles til de eksisterende `ZONE-REG-01` IDs, så savegames, city relations og simulation state ikke skal migreres alene på grund af ny geometri.

## 46.2 Overlay-generation

v10n grupperer de 20 zoner i fem geografiske beregningsområder:

1. Jylland
2. Fyn/Langeland
3. Sjælland/Møn
4. Lolland-Falster
5. Bornholm

Inden for hvert område beregnes en lukket centerbaseret sektor omkring den kanoniske zone-node. Sektorerne bruges kun som midlertidig visuel opdeling. Denne metode giver stabile, ikke-overlappende zoneområder til UI-/render-test, men må ikke bruges til at rekonstruere administrative 1851-grænser.

## 46.3 Visning

- Overlayet auto-oprettes efter scene load.
- Alle 20 zoneområder får en lukket boundary-loop.
- `Z` toggler hele zoneoverlayet ON/OFF.
- Overlayet har ingen collider og må ikke fange raycasts.
- Eksisterende `ZONE_`, `CITY_` og `ARMY_`-objekter ændres ikke.
- Et diskret runtime-badge viser `v00.00.10n`, overlay-status samt at geometrien er prototype/DigDag-target.

## 46.4 Relation til 3D terrain

Zoneoverlayet er et separat visuelt lag oven på eksisterende World Imagery og streamed 3D terrain. v10n må ikke ændre imagery-streaming, elevation-cache, terrain-meshes eller gameplay-marker-height-kontrakten.

Første v10n-test bruger en fast strategisk overlay-højde. Ved senere præcis polygonimport skal grænselinjer kunne terrain-drapes eller samples mod aktiv terrain-height, så lange grænser ikke skærer gennem mere markant relief.

## 46.5 Bevarede v10m-regler

v10n ændrer ikke følgende:

- 20 `ZONE-REG-01` runtime-zoner.
- 68 `CITY-REG-01` købstæder.
- Urban population checksum 290.565.
- Esbjerg er ikke en 1851-startby.
- A/B/C city tiers.
- B/C-byer kan ikke normalt få ny kaserne, arsenal, større depot, våbenfabrik eller permanent fæstning.
- Historisk dokumenterede `fixed/special buildings` er fortsat en separat undtagelse.
- Land- og Ferry-neighbour links er separate.
- QA-hæren starter i `DK-Z11-VEJ`.

## 46.6 Næste datatrin

Den korrekte efterfølger til v10n er ikke flere håndtegnede polygoner. Næste geometriarbejde skal være en **DigDag-importpipeline**:

1. identificér relevante Amt/Region-features for campaign-dato 1. januar 1851;
2. hent/konverter polygonerne til WGS84;
3. map source-features til `ZONE-REG-01`;
4. håndtér København Stad og historiske særtilfælde eksplicit;
5. validér topologi, kystklip og øer;
6. erstat prototypegeometrien uden at ændre `ZoneId`;
7. gem kilde, gyldighedsdato og confidence sammen med polygondata.

## 46.7 QA gates

1. Unity 6000.6.0f1 compiler uden fejl.
2. v10m-registret validerer fortsat 20 zoner / 68 byer / 290.565.
3. Console logger `CAMPAIGN-10N|Installed=True`.
4. Der oprettes 20 boundary loops.
5. `Z` skjuler og viser overlay uden at påvirke simulationen.
6. Overlayet har ingen collider og blokerer ikke selection/movement-raycasts.
7. 68 city markers er fortsat klikbare.
8. A/B/C-construction-regler er uændrede.
9. Land/Ferry movement regression består.
10. World Imagery og streamed terrain fungerer fortsat.
11. Badge og dokumentation markerer geometrien som prototype.
12. Ingen runtime-funktion beskriver prototypepolygonen som historisk eksakt.
13. Esbjerg forbliver fraværende i 1851.
14. Pause/x1/x5/x20/x100 består regression.
15. Rollback til v10m-backup er dokumenteret.

## 46.8 Rollback

Rollback før v10n:

`backup/channel-campaign3-v10m-before-v10n-zone-overlay-20260913`

## 46.9 Status

**IMPLEMENTED ON WORK BRANCH / AWAITS UNITY 6.6 COMPILE + PLAY MODE QA**
