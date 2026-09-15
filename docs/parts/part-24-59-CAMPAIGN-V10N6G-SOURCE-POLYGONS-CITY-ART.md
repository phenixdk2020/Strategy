# Del 24: 59 — Campaign3 v00.00.10n6g: source-backed Amt-polygoner og garanteret bygrafik

**Designbaseline:** v00.02.25  
**Prototypeversion:** v00.00.10n6g  
**Work branch:** `work/channel-campaign3-v10n6g-exact-parish-polygons`

## Formål

v10n6g erstatter to løsninger, som ikke bestod Game View-QA:

1. n6f's sogne-centroid/gridmodel gav stadig forkerte lokale Amt-forløb og gjorde grænserne visuelt trappede/grimme.
2. Proposal-3-bygrafikken kunne stadig være helt usynlig i runtime.

n6g går derfor væk fra syntetisk zonegeometri og fra den tidligere SpriteRenderer-path.

## Source-backed Amt-geometri

`CampaignHistoricalAmtPolygonsV010N6G` downloader og cacher de faktiske `sogne.shp` og `sogne.dbf` fra det offentlige `christianvedels/A_perfect_storm`-datasæt. Datasættets dokumentation angiver, at sognegeografien repræsenterer Danmark pr. **1. januar 1820** og oprindeligt stammer fra DigDag.

Runtime parser SHP/DBF direkte i Unity og bruger:

- SHP-polygonkanterne som geometri,
- DBF-feltet `AMT` som historisk Amt-ejer,
- `SOGN`/`HERRED` som kildeattributter,
- mapping fra historisk Amt-navn til projektets eksisterende `ZONE-REG-01`-IDs.

Der tegnes kun en gul Amt-grænse, når en polygonkant deles af to sogne med **forskellig Amt-ejer**. Derfor tegnes hverken almindelige sognegrænser inde i samme Amt eller kystlinjen som Amt-grænse.

Aktiv geometri-mode:

`DIGDAG_1820_SOURCE_PARISH_POLYGON_EDGES`

n6g bruger ikke:

- zonecentre,
- bycentre som Voronoi-sites,
- Herred-centroid-Voronoi,
- sogne-centroid-grid,
- rasteriserede/trappede grænser.

## Klik og visuel grænse er samme datakilde

`CampaignZoneInfoPanelV010N6G` bruger de samme SHP-sognepolygoner som den synlige grænse.

Regler:

- direkte klik på en by → kanonisk `City.ZoneId`,
- klik på almindeligt land → point-in-polygon mod kildepolygonen,
- ingen nearest-zone-centre fallback,
- ingen grid-resolver,
- ingen separat Voronoi-resolver,
- klik i havet giver intet Amt.

Dermed må UI'et ikke længere rapportere et andet Amt end det polygonbaserede kortlag under markøren.

## Historisk guardrail

n6g er kildebelagt polygon-geometri, men kilden er **1820**, ikke præcis 1. januar 1851.

Derfor gælder fortsat:

`HistoricallyExact1851 = false`

Den langsigtede produktionskilde er fortsat et dateret DigDag Amt/Region-udtræk for den konkrete campaign-dato. n6g er dog væsentligt stærkere end n6f, fordi den bruger de faktiske historiske sognepolygoner i stedet for kunstigt afledt center-/grid-geometri.

## Proposal 3 city art — ny render-path

`CampaignCityArtV010N6G` erstatter den tidligere SpriteRenderer-baserede løsning med eksplicitte, kamera-vendte **unlit Quad-renderers**.

Bygrafikken bruger fortsat Proposal-3 A/B/C-textures fra:

- `Assets/Resources/Campaign/CityIcons/City_A_Proposal3.png`
- `Assets/Resources/Campaign/CityIcons/City_B_Proposal3.png`
- `Assets/Resources/Campaign/CityIcons/City_C_Proposal3.png`

Visuel størrelse i n6g:

- A: base scale `5.00`
- B: base scale `3.80`
- C: base scale `2.80`

Alle tre får en mindre populationsbonus inden for egen tier.

Bygrafikken løftes `0.70` world units over markørens terrænhøjde og renderes uden lys/skyggeafhængighed. Gamle `CITY_ICON_*` / `CITY_ART_*` child-objekter fjernes før installation.

Hvis en PNG mod forventning ikke kan importeres gennem `Resources`, genereres en synlig fallback-settlementtexture. En by må derfor ikke længere forsvinde helt uden grafik.

De gamle runde city-cylindre skal være skjult, og der genereres ingen grøn `TownGround`.

## QA-hær

n6f's oprydning af `DK-ARMY-QA` ved Vejle bevares. Den røde QA-hær må ikke være synlig eller klikbar i normal campaign-visning.

## QA

Følgende skal verificeres i Unity 6000.6.0f1:

1. ingen compile errors,
2. topbar viser `v00.00.10n6g`,
3. `AmtPolygonOverlay=True`,
4. `Geometry=DIGDAG_1820_SOURCE_PARISH_POLYGON_EDGES`,
5. mindst 500 sognepolygoner parses,
6. `AmtBoundarySourceEdges=True` og `RawSharedSegments > 0`,
7. log viser `Grid=False|Voronoi=False`,
8. de trappede n6f-grænser er væk,
9. Aalborg/Hjørring, Thisted/Viborg, Randers/Viborg, Aarhus/Skanderborg og Vejle/Ribe/Ringkøbing kontrolleres visuelt,
10. landklik logger `Source=SourceParishPolygon`,
11. byklik bruger `CanonicalCity`,
12. havklik returnerer intet Amt,
13. `CityArtN6G=True|Cities=68`,
14. A/B/C-byer er tydeligt synlige,
15. små C-byer kan ses ved normal campaign-zoom,
16. fallback flag kan være True ved importerfejl, men byen skal stadig være synlig,
17. ingen runde city-cylindre eller grøn TownGround,
18. QA-hæren ved Vejle er væk,
19. World Imagery + streamed 3D terrain fungerer,
20. 20 zoner / 68 byer / 290.565 bybefolkning er uændret.

## Status

**IMPLEMENTERET PÅ WORK BRANCH — AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA.**
