# Del 19: 54 — Campaign3 v00.00.10n6b: Proposal 3 City Art

**Designstatus:** IMPLEMENTERET PÅ BUILD / AFVENTER UNITY 6.6 PLAY MODE QA  
**Prototypeversion:** v00.00.10n6b  
**Designbaseline:** v00.02.20  
**Work branch:** `work/channel-campaign3-v10n6b-proposal3-city-art`

## 54.1 Formål

v00.00.10n6b erstatter den procedurale n6-byprototype med den godkendte visuelle retning **Proposal 3 — Isometric Miniature Town**. Målet er, at byerne læses som små historiske miniaturebyer direkte oven på World Imagery-kortet i stedet for som runde markører eller simple Unity-primitiver.

## 54.2 A/B/C-artstandard

- **C — Minor Town:** lille transparent settlement-cutout med cirka 2–3 bygninger.
- **B — Regional Town:** mellemstor transparent by med cirka 5–6 bygninger og kirke.
- **A — Development City:** større og tættere transparent by med cirka 10–15 bygninger, kirke og tydeligere civilt/bycentrum.

Alle tre assets bruger samme painterly/isometriske billedsprog med lyse pudsede mure, røde/grå tage, vegetation, stier og en kompakt historisk europæisk/dansk karakter.

## 54.3 Transparens og bund

City-art leveres som PNG med alpha. Der oprettes **ingen grøn `TownGround`-plade** i n6b. Kun selve den tegnede settlement-footprint er synlig; alt uden for footprintet er transparent, så satellitkortet fortsat kan ses.

## 54.4 Runtime-arkitektur

`CampaignCityIconV010N6` bevarer class/file-navnet af kompatibilitetshensyn, men implementerer nu n6b-renderingen:

1. A/B/C PNG'er indlæses fra `Assets/Resources/Campaign/CityIcons`.
2. Det eksisterende `CITY_*` GameObject og `GrandCampaignCityMarker` forbliver authoritative.
3. Gammel city-renderer slås fra.
4. Eventuelt legacy-child `CITY_ICON_ISOMETRIC_10N6` fjernes.
5. En enkelt usynlig `BoxCollider` bruges fortsat som stabil click-target.
6. Synlig grafik ligger i `CITY_ICON_PROPOSAL3_10N6B` som `SpriteRenderer`.
7. City-art billboards mod `Camera.main`, så det godkendte isometriske artwork bevarer sin læsbarhed gennem campaign-kameraets vinkler.
8. A/B/C styrer primær størrelse; population giver kun en begrænset variation inden for samme tier.

## 54.5 Runde cirkler

De gamle runde city-renderere er slået fra. De synlige runde `GrandCampaignZoneMarker`-centre, som blandt andet kunne ligne store by-/amt-cirkler, skjules også i n6b. Deres metadata/collider bevares som sidste-resort fallback for eksisterende zonefunktionalitet.

## 54.6 Datakontrakt

n6b ændrer ikke:

- `CityId`
- `ZoneId`
- `Population1850`
- A/B/C-klassifikation
- WGS84-position
- labels
- city selection identity
- RMB city-to-zone movement
- 20 zone / 68 city / 290.565 checksum-kontrakten

## 54.7 Relation til zonegeometri

Denne build ændrer **ikke** den historiske præcision af amtsgrænserne. Den aktuelle zoneflade er fortsat `PROTOTYPE_LAND_CLIPPED_MULTI_SITE_VORONOI`. Zonepanelet resolver mod den aktuelle polygon før zone-centre fallback, men de endelige historiske 1851 Amt-polygongrænser skal stadig komme fra en source-backed GIS/DigDag-pipeline.

## 54.8 Bevarede n6a/n5-systemer

- CS1612 RallyPoint compile-hotfix bevares.
- `TILBAGETRÆK HERTIL` bevares.
- kæmpende tilbagetrækning bevares.
- `BRYD KONTAKT` bevares.
- morale-baseret rout/flugt er fortsat separat fra kontrolleret withdrawal.

## 54.9 QA

Før builden kan betragtes som Unity-valideret skal følgende kontrolleres i Unity 6000.6.0f1:

1. projektet compiler uden errors;
2. badge/topbar viser `v00.00.10n6b`;
3. alle 68 byer eksisterer fortsat;
4. alle byer får `CITY_ICON_PROPOSAL3_10N6B`;
5. A/B/C bruger korrekt artwork;
6. ingen gamle city-cylindre er synlige;
7. ingen grøn procedural city-plade er synlig;
8. transparent område omkring byerne lader World Imagery være synligt;
9. zone-centrenes runde renderere er skjult;
10. city click og RMB city-to-zone movement fungerer;
11. n6a/n5 withdrawal regressionstest består;
12. World Imagery og streamed 3D terrain fungerer uændret.

**QA-state:** implementeret og publicerbar build; faktisk Unity compile/Play Mode QA skal udføres i Editor.
