# Del 25: 60 — Campaign3 v00.00.10n6h: city-art visibility og Amt boundary continuity

**Designbaseline:** v00.02.26  
**Prototypeversion:** v00.00.10n6h  
**Work branch:** `work/channel-campaign3-v10n6h-boundary-city-facing`

## Formål

v10n6h retter to konkrete QA-fejl fra n6g:

1. Proposal-3 byerne fandtes som city-markører og labels, men selve grafikken var fortsat ikke synlig i Game View.
2. De nye kildebaserede Amt-grænser var markant bedre end n6f, men enkelte boundary-kæder havde korte visuelle huller.

## Bygrafik

`CampaignCityArtV010N6H` overtager runtime city-art fra n6g.

Renderer-kontrakten er nu eksplicit:

- dedikeret shader `PROJECT1864/CampaignCityBillboard`,
- `Cull Off`,
- `ZWrite Off`,
- `ZTest Always`,
- alpha blending,
- bottom-anchored billboard mesh,
- Proposal-3 texture som primær kilde,
- genereret synlig fallback hvis Resources-texture ikke kan indlæses.

Bygrafik må herefter ikke kunne forsvinde alene pga. backface-culling, terræn-depth eller manglende texture-import. A/B/C beholder forskellige størrelser og CityId/ZoneId ændres ikke.

## Amt continuity

n6g's faktiske DigDag-afledte sognepolygoner bevares som autoritativ geometri og klik-kilde.

`CampaignAmtBoundaryContinuityV010N6H` laver kun et visuelt efterarbejde på korte boundary-kædehuller. To endepunkter forbindes kun når:

- afstanden er højst 0,040 geografiske grader,
- begge kæders retning peger mod hinanden med dot-product mindst 0,72,
- enderne tilhører forskellige eksisterende LineRenderer-kæder.

Continuity-pass'et ændrer **ikke** Amt-ejerskab, sognepolygoner, ZoneId eller klikresolver.

## Historisk guardrail

Sognegeometrien er fortsat DigDag-afledt og dokumenteret som 1. januar 1820. Den må derfor ikke betegnes som eksakt 1. januar 1851 Amt-geometri. Produktionsmålet er fortsat daterede DigDag Amt/Region-polygoner for campaign-starten.

## QA

1. Unity 6000.6.0f1 kompilerer uden errors.
2. Topbar viser v00.00.10n6h.
3. `CityArtN6H=True|Cities=68` logges.
4. A-, B- og C-byer er synlige ved normal campaign zoom.
5. Ingen rund city-cylinder eller grøn TownGround er synlig.
6. City-art forbliver synlig over terrænet ved pan/tilt/zoom.
7. `AmtBoundaryContinuity=True` logges.
8. Aalborg/Hjørring og Thisted/Viborg kontrolleres for korte afbrudte linjestykker.
9. Source parish polygons forbliver klikresolver.
10. QA-hæren ved Vejle forbliver fjernet.

## Status

**IMPLEMENTERET PÅ WORK BRANCH — AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA.**
