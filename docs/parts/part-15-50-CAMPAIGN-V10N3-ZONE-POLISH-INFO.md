# Del 15: 50 — Campaign3 v00.00.10n3: skarpere zonegrænser + kompakt zoneinfo

**Designbaseline:** v00.02.16  
**Runtimeversion:** v00.00.10n3  
**Work branch:** `work/channel-campaign3-v10n3-zone-polish-info`  
**Status:** IMPLEMENTERET PÅ WORK BRANCH / AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA

## 50.1 Formål

v10n3 er en visuel/UI-polish oven på v10n2. Den ændrer ikke ZONE-REG-01, CITY-REG-01, movement routing, population eller bygge-regler.

Målene er:

1. gøre zonegrænserne tyndere, skarpere og mindre prototype-agtige,
2. fjerne dobbelttegnede/fuzzy segmenter,
3. undgå at den gule zone-overlaylinje følger hele kysten og dermed fremhæver små fejl i den grove landmaske,
4. vise et lille kontekstuelt zonekort i venstre hjørne ved klik på en zone/by/dansk landområde.

## 50.2 Poleret zone-line layer

`CampaignZoneLinePolishV010N3` læser v10n2's allerede beregnede land-clippede zonepolygoner. Selve v10n2-geometrien forbliver prototype-source for denne visualisering.

Det synlige n3-lag:

- snapper endepunkter til et lille fælles world-grid,
- deduplikerer segmenter med samme endepunkter,
- bruger én tynd, opaque linje pr. unikt segment,
- fjerner cap/corner smoothing der kunne få knudepunkter til at se bløde ud,
- klassificerer segmenter der ligger langs den aktuelle `DNK_Coast_*` envelope og undlader dem fra standard zone-rendering,
- lader basemap/kystlinjen definere en kystzones ydre kant i stedet for at tegne en ekstra gul kant ovenpå.

Det betyder, at standardvisningen primært viser **interne administrative prototype-grænser**, mens kysten fortsat læses direkte fra geografien/World Imagery.

## 50.3 Historisk guardrail

v10n3 gør linjerne pænere; den gør dem **ikke historisk korrekte**.

- ZONE-REG-01 IDs og zoneidentiteter er kanoniske.
- v10n2/v10n3 geometri er fortsat centre-derived prototypegeometri.
- landmasken er fortsat baseret på den nuværende Natural Earth 1:50m-scaffold.
- små fjorde, havne, sunde og hydrografiske detaljer kan stadig afvige fra World Imagery.
- endeligt produktionsmål er fortsat source-backed historiske polygoner, bl.a. DigDag Amt/Region, uden ændring af ZoneId/save/simulation state.

## 50.4 Kompakt zoneinfo

`CampaignZoneInfoPanelV010N3` viser et lille kort i øverste venstre hjørne ved map selection.

Klik kan komme fra:

- zone-marker,
- city-marker,
- et punkt på dansk land; dette opløses efter samme nearest-centre prototype-regel som zonepartitionen.

Panelet viser:

- zone-navn,
- ZoneId,
- Kongeriget Danmark som 1851-scope,
- antal CITY-REG-01-byer,
- A/B/C-fordeling,
- summeret købstadsbefolkning fra 1850,
- byliste,
- landnaboer,
- færgeforbindelser,
- ved direkte city-click: valgt by, tier, folketal og tung-militær-buildregel.

Panelet har X-luk og er bevidst kompakt. Hvis brugeren åbner det gamle detaljerede `INFO/F2`-panel, skjules det kompakte kort, så de to paneler ikke ligger oven i hinanden.

## 50.5 HUD-adfærd

- Map click kan genetablere HUD, hvis F1/minimal HUD var aktiv, fordi et eksplicit zone-click forventes at vise kontekst.
- Ved normal map selection lukkes det store legacy INFO-panel, så compact zone card bliver standard.
- `INFO/F2` kan fortsat bruges til den gamle detaljerede zone/city/formation-visning.
- Debug-paneler og øvrig v10n2 HUD persistence ændres ikke.

## 50.6 Bevaret runtime-kontrakt

Følgende er uændret fra v10n2:

- 20 ZONE-REG-01 zoner,
- 68 CITY-REG-01 købstæder,
- urban population checksum 290.565,
- Esbjerg ude af 1851-startstate,
- A/B/C city/build rules,
- Land/Ferry routing,
- RMB-on-city movement fix,
- World Imagery,
- streamed 3D terrain,
- `Z` toggle af zoneoverlay,
- prototype/historical-geometry guardrail.

## 50.7 QA-gates

1. Unity 6000.6.0f1 compiles without errors.
2. Build badge/topbar viser `v00.00.10n3`.
3. v10n2 polygon overlay bygges før n3 polish-laget.
4. n3 log viser `ZoneLinePolish=True`.
5. Gule kyst-envelope-segmenter er fjernet fra standard zonevisning.
6. Interne zonegrænser er synlige, tyndere og skarpere.
7. Dobbelttegnede identiske segmenter renderes kun én gang.
8. `Z` slukker/tænder også det polished child-layer via overlay-root hierarchy.
9. Zone/city/army selection og RMB movement regressions virker fortsat.
10. Klik på en zone-marker viser compact zone card.
11. Klik på en city-marker viser zonekort + valgt by.
12. Klik på dansk land kan resolve nearest prototype-zone.
13. Klik uden for dansk land åbner ikke zonekort.
14. X lukker compact zone card.
15. INFO/F2 erstatter compact card med det gamle detailpanel uden overlap.
16. Zonekortets city-count/population svarer til CITY-REG-01.
17. 20/68/290565 startup validation er uændret.
18. World Imagery og streamed 3D terrain regress uden breakage.
19. Dokumentation omtaler fortsat nuværende zonegeometri som prototype og ikke 1851-facit.
