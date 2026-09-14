# Del 20: 55 — Campaign3 v00.00.10n6c: City-art asset fix + deterministisk zoneejerskab

**Designbaseline:** v00.02.21  
**Prototype:** v00.00.10n6c  
**Workbranch:** `work/channel-campaign3-v10n6c-city-zone-hotfix`  
**Target:** `channel-campaign3`

## Formål

v10n6c retter to konkrete regressioner fundet ved Play Mode-test af n6b:

1. Proposal-3 byikonerne blev ikke indlæst, så især de små C-byer ikke blev vist som de nye isometriske assets.
2. Klik på et område kunne fortsat give et forkert Amt, fordi områdevalg kunne være påvirket af den grove landmaske og tidligere zonecenter-fallback.

## Proposal 3 city-art

De tre A/B/C-assets ligger som gyldige transparente PNG-filer under `Assets/Resources/Campaign/CityIcons`.

- `City_C_Proposal3.png`: Minor Town, ca. 2–3 bygninger.
- `City_B_Proposal3.png`: Regional Town, ca. 5–6 bygninger + kirke.
- `City_A_Proposal3.png`: Development City, tættere bykerne med kirke/civic centre.

Runtime-loaderen bruger fortsat `Resources.Load<Texture2D>()`, men n6c reducerer retry til maksimalt én gang pr. sekund og logger kun MissingTexture én gang pr. fejlpersistens. Ved succes logges dimensionerne på alle tre textures.

Den gamle runde city-renderer er skjult. `CITY_ICON_ISOMETRIC_10N6` og tidligere `CITY_ICON_PROPOSAL3_10N6B` fjernes, og den aktive visuelle child bliver `CITY_ICON_PROPOSAL3_10N6C`. Klik håndteres fortsat af én usynlig `BoxCollider`, så den transparente kunst ikke bestemmer hitboxen.

## Zone-/Amt-ejerskab ved klik

n6b-loggen viste, at den grove Natural Earth 1:50m-landmaske kun dækkede 53/68 kanoniske bykoordinater og 19/20 zonecentre. Det skyldes primært kyst- og ø-detaljer og må ikke forveksles med den kanoniske CITY-REG-01 til ZONE-REG-01 relation.

n6c introducerer `CampaignZoneOwnershipResolverV010N6C`.

Resolver-kontrakten er:

1. Direkte klik på en by bruger altid byens kanoniske `City.ZoneId`.
2. Områdeklik skal være inden for den eksisterende landklippede zonegeometri; åbent hav må ikke få tildelt nærmeste Amt.
3. Ejerskabet beregnes derefter deterministisk fra de samme kanoniske influence sites som prototypepartitionen: alle zonecentre + alle 68 byer.
4. `GrandCampaignZoneMarker`-collider bruges ikke længere som fallback for områdeklik.

Dette gør klikidentiteten deterministisk og fjerner fejlen, hvor et nærliggende zonecenter kunne få et område ved Vejle/Aalborg/Hjørring til at vise forkert Amt.

## To forskellige QA-mål

Det er vigtigt at skelne mellem:

### Resolver-QA

Resolveren valideres mod de kanoniske sites selv. Målet er:

- `CityCanonical=68/68`
- `ZoneCentreCanonical=20/20`
- `MarkerFallback=False`

Dette betyder, at et kanonisk site resolver til sin egen registrerede zone.

### Geometri-/landmaske-QA

Den synlige overlay-geometri er fortsat `PROTOTYPE_LAND_CLIPPED_MULTI_SITE_VORONOI` og klippes mod Natural Earth 1:50m. Derfor kan rå landmask-coverage fortsat rapportere kystnære afvigelser.

Resolver-QA må **ikke** bruges som påstand om, at de synlige linjer er historisk korrekte 1851-amtsgrænser.

## Historisk guardrail

Den endelige produktionsløsning for Amt-grænser skal fortsat være kildebelagte historiske polygoner, planlagt fra **DigDag — Amt og Region**. Når de importeres, skal `ZONE-REG-01`-ID'er bevares, så simulation, save state, økonomi, rekruttering, owner/controller og city relations ikke ændrer identitet.

## QA-gates

1. Unity 6000.6.0f1 kompilerer uden fejl.
2. Badge/topbar viser `v00.00.10n6c`.
3. `Proposal3TexturesLoaded=True` logges med A/B/C texture-dimensioner.
4. `Proposal3CityArt=True|Cities=68` logges.
5. C/B/A-ikoner er alle synlige og visuelt forskellige.
6. Ingen rund city-cylinder og ingen grøn TownGround er synlig.
7. PNG-baggrunden er transparent omkring settlement-footprintet.
8. Direkte klik på hver by returnerer dens kanoniske ZoneId.
9. Områdeklik logger `Source=CanonicalMultiSiteOwnership`.
10. Zonecentre-colliders bruges ikke som fallback for områdeklik.
11. Resolver-QA giver 68/68 byer og 20/20 zonecentre.
12. Klik i åbent hav vælger ikke et Amt.
13. Regressionstest: Vejle, Aalborg, Hjørring, Thisted, Viborg samt kystbyer som Fredericia, Middelfart og Sæby.
14. Registry forbliver 20 zoner / 68 byer / 290.565 urban population.
15. World Imagery og streamed 3D terrain fungerer uændret.
16. Withdrawal-systemet fra n5/n6a fungerer fortsat.

## Status

**IMPLEMENTERET PÅ WORKBRANCH — afventer Unity 6.6 compile + Play Mode QA.**
