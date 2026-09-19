# Del 28: 63 — Campaign3 v00.00.10n7c: Embedded City Asset Reset

**Designbaseline:** v00.02.29  
**Prototypeversion:** v00.00.10n7c  
**Work branch:** `work/channel-campaign3-v10n7c-embedded-city-art`

## Formål

v10n7c retter den konkrete n7b-fejl rapporteret fra Unity:

```text
CAMPAIGN-10N7B|CityArtN7B=False|Reason=Proposal3TextureMissing|A=False|B=False|C=True|FallbackStyle=False
```

Det betyder, at n7b's city renderer ikke kunne importere A- og B-assets som `Texture2D`, mens C blev fundet. Samtidig kunne ældre city-art blive stående synligt, fordi n7b aldrig nåede installationsfasen, når texture-gaten fejlede.

n7c erstatter derfor selve asset-pipelinen frem for at justere endnu en Resources-loader.

## Vej 1 fortsætter

Amt/county-systemet forbliver helt slået fra.

n7c ændrer ikke:

- county boundaries,
- county click ownership,
- county highlight,
- county info panel,
- county historical geometry.

City-art skal være stabil og accepteret før County Rebuild starter.

## Embedded Proposal-3 assets

A-, B- og C-art leveres som validerede PNG-data indlejret i runtime-koden:

- `CampaignCityArtEmbeddedDataA_V010N7C`
- `CampaignCityArtEmbeddedDataB_V010N7C`
- `CampaignCityArtEmbeddedDataC_V010N7C`

`CampaignMapOnlyCityArtV010N7C` decoder dem med `ImageConversion.LoadImage`.

Konsekvensen er:

- ingen `Resources.Load<Texture2D>` dependency,
- ingen afhængighed af TextureImporter for de tre city-art assets,
- ingen A=False/B=False import-state,
- samme validerede art bytes på alle tre tiers.

De gamle PNG-filer i `Assets/Resources/Campaign/CityIcons` fjernes fra n7c-branchen, så en gammel renderer ikke kan hente den tidligere forkerte grafik ved et uheld.

## Legacy purge

n7c rydder city-marker children, før ny art installeres.

Følgende fjernes:

- alle children med navn der starter `CITY_ART_`,
- alle children med navn der starter `CITY_ICON_`,
- `TownGround`.

Parent-markerens gamle renderer slås også fra.

Dette sker før n7c texture/material-gaten. En asset-fejl må derfor ikke efterlade en ældre forkert bytype på kortet.

## Visuel standard

Kun **Proposal 3 — Isometric Miniature Town** er tilladt.

Der findes ingen beige/generated fallback-stil i normal runtime.

### Scale

n7c reducerer city footprint yderligere:

| Tier | n7b | n7c |
| --- | ---: | ---: |
| A — Development City | 1.55 | **0.95** |
| B — Regional Town | 1.12 | **0.72** |
| C — Minor Town | 0.78 | **0.52** |

Ground lift reduceres til **0.12**.

Målet er, at campaign-geografien forbliver dominant, mens byen stadig kan aflæses som et tydeligt settlement-symbol.

## Anchor

Alle city-art billboards bruger:

- canonical city marker som X/Z-anchor,
- ingen X/Z art offset,
- bottom-center anchor,
- ground lift 0.12.

Klikcollideren forbliver større end det synlige ikon, så mindre art ikke reducerer usability.

## Runtime-log

En korrekt installation skal indeholde:

```text
CAMPAIGN-10N7C|Way1=True|MapOnly=True|CountyBorders=False|CountySelection=False
```

og:

```text
CAMPAIGN-10N7C|CityArtN7C=True|Cities=68|AssetSource=EMBEDDED_VALIDATED_PROPOSAL3
```

samt:

```text
ResourcesDependency=False
TextureA=128x128
TextureB=128x128
TextureC=128x128
LegacyCityVisualPurge=True
```

Der må ikke længere forekomme `Proposal3TextureMissing` for city-art.

## QA

Versionen accepteres først når:

1. Unity 6000.6.0f1 compiler uden blocking errors.
2. v00.00.10n7c vises i topbar/badge.
3. ingen Amt/county boundaries vises.
4. ordinary land click åbner ikke Amt-info.
5. `CityArtN7C=True|Cities=68` logges.
6. `AssetSource=EMBEDDED_VALIDATED_PROPOSAL3` logges.
7. A/B/C rapporteres som 128x128.
8. `ResourcesDependency=False` logges.
9. ingen `Proposal3TextureMissing` city-art fejl.
10. `LegacyCityVisualPurge=True` logges.
11. alle 68 byer viser samme Proposal-3 visual language.
12. ingen beige/generated eller anden ældre city style ses.
13. ingen round cylinders eller TownGround ses.
14. A/B/C-skala er 0.95 / 0.72 / 0.52.
15. city labels er stadig læsbare.
16. kystbyer opleves ikke som flyttet ud i åbent vand.
17. World Imagery + streamed terrain fungerer.
18. QA-hæren ved Vejle er fortsat væk.
19. registry er stadig 20 zones / 68 cities / 290565.

## County Rebuild gate

County/Amt må først genindføres efter accepteret city-art QA.

Den senere implementation skal bruge **én authoritative historical polygon source** til:

1. visible boundary,
2. click ownership,
3. selected-area highlight.

## Status

**IMPLEMENTERET PÅ WORK BRANCH — AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA.**
