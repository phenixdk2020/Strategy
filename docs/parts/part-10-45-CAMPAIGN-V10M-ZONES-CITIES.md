# PROJECT 1864 — Campaign3 v00.00.10m

**Designbaseline: v00.02.11**  
**Build: v00.00.10m — HISTORICAL 1851 ZONES + 68 CITY RUNTIME**  
**Work branch: `work/channel-campaign3-v10m-zone-city-runtime`**

## Formål

v10m flytter CITY-REG-01 og ZONE-REG-01 fra designmanual til faktisk Campaign3-runtime. Den gamle prototype havde 12 grove QA-zoner og kun 10 QA-byer. Denne build erstatter dem med den kanoniske 1851-startstate for Kongeriget Danmark.

## Implementeret

- **20 ZONE-REG-01-zoner** indlæses fra `CampaignDenmark1851Registry`.
- **68 CITY-REG-01-købstæder** indlæses ved campaign-start.
- Esbjerg er fjernet fra 1851-startstate.
- Hver by har stabilt `CityId`, `ZoneId`, `Population1850`, `CityTier` og WGS84-position.
- Registeret valideres ved startup: 20 zoner, 68 byer og urban population checksum **290.565**.
- Byklasse A/B/C påvirker visuel størrelse og runtime-build-regel.
- B- og C-byer er fortsat synlige og klikbare, men `AllowsHeavyMilitaryConstruction=false`.
- A-byer kan returnere `true` via `CanBuildHeavyMilitaryInCity(cityId)`, men økonomi, teknologi, nationale regler og historiske begrænsninger skal stadig kontrolleres af det senere construction-system.
- Historiske kaserner, fæstninger, depoter m.m. i B/C-byer forbliver tænkt som `fixed/special buildings`; v10m opretter endnu ikke dette special-building-register.
- City selection viser navn, klasse, population 1850, ZoneId og bygge-regel.
- A-byer får labels på operational zoom; alle A/B/C-byer får labels på close zoom.
- QA-hæren bruger nu `DK-Z11-VEJ` i stedet for den udgåede `DK-SJ`-zone.

## Zone- og rute-model

ZONE-REG-01 er nu authoritative runtime-zone-listen. Zonerne har separate arrays for:

- `LandNeighbours`
- `FerryNeighbours`

Direkte marchordre accepteres kun, når destinationen findes i et af disse links. `RouteType=Ferry` får i denne build en simpel tidsmultiplikator på 1,35. Det er en prototype-abstraktion; senere skal færger/søtransport have havn, kapacitet, timetable/availability, vejr, blokade, fjendtlig interdiction og konkret transportkapacitet.

Bornholm får ikke en kunstig landforbindelse. En rigtig strategisk sea-transport layer skal senere forbinde øen.

## City tiers

| Tier | Runtime | Tung militær nybygning |
| --- | --- | --- |
| **A — Development City** | synlig, klikbar, stor node | kan kvalificere efter øvrige krav |
| **B — Regional Town** | synlig, klikbar, mellem node | blokeret som normal byggeoption |
| **C — Minor Town** | synlig, klikbar, lille node | blokeret som normal byggeoption |

Tier og zone er fortsat separate. En C-by bliver ikke A-by, blot fordi dens zone er vigtig, rig eller militært besat.

## Nye runtime-filer

- `Assets/Scripts/Campaign/CampaignDenmark1851Registry.cs`
- `Assets/Scripts/Campaign/GrandCampaignCityMarker.cs`

## Ændrede runtime-filer

- `Assets/Scripts/Campaign/GrandCampaignBootstrap.cs`
- `VERSION.txt`

## Kompatibilitet med World Imagery / 3D terrain

Objektnavne beholder præfikserne:

- `ZONE_`
- `CITY_`
- `ARMY_`

Dermed kan den eksisterende marker-height pipeline fortsat løfte gameplay-markører over streamed terrain. v10m ændrer ikke imagery-provider, terrain tiles, EntityId-hotfix, camera tilt eller terrain LOD.

## Begrænsninger i v10m

- Zonepolygoner er endnu ikke historiske polygons; runtime bruger fortsat zonecentre til movement/selection.
- Zonefolketal indeholder endnu ikke landbefolkning.
- Roads, railways, bridges og ferry capacity påvirker endnu ikke den detaljerede route cost.
- City coordinates er WGS84-nodepositioner og skal senere kilde-QA'es systematisk sammen med historiske city extents.
- Der er endnu ikke et city construction UI; v10m leverer build-gaten/API-kontrakten.
- Historiske special buildings er ikke oprettet endnu.
- Slesvig, Holsten og Lauenborg kommer i separate zone/city-registre.

## Acceptance / QA

1. Unity 6000.6.0f1 kompilerer uden fejl.
2. Campaign starter som `v00.00.10m`.
3. Startup-log viser `Zones=20`, `Cities=68`, `UrbanPopulation1850=290565`.
4. Esbjerg findes ikke som city-node.
5. Alle 68 city markers vises på kortet.
6. City markers følger 3D-terrain height.
7. Klik på en by viser korrekt navn, tier, folketal og ZoneId.
8. København viser 129.695 / A / DK-Z01-KBH.
9. Aalborg viser 7.745 / A / DK-Z16-AAL.
10. Nibe viser 1.161 / C / DK-Z16-AAL og tung militær udbygning blokeret.
11. Fredericia viser 4.326 / A / DK-Z11-VEJ.
12. QA-hæren starter i DK-Z11-VEJ.
13. Land-adjacency accepterer marchordre.
14. Ferry-adjacency accepterer marchordre og logger `RouteType=Ferry`.
15. Ikke-nabozone afviser direkte marchordre.
16. Close zoom viser også små B/C-byer.
17. Operational zoom viser A-city labels uden at alle små labels fylder skærmen.
18. World Imagery, streamed terrain, T/Q/E samt pan/zoom regresser ikke.

## Næste naturlige build

Efter v10m QA bør næste datalag være historiske **zonepolygoner + rural population 1850**, så zonerne bliver egentlige territorier frem for centre. Derefter kan regional manpower, landbrug, skat og occupation-state begynde at bruge ZONE-REG-01 som simulationens territoriale base.
