# Del 30: 65 — Campaign3 v00.00.10n7e: Coastal City Visual Offsets

**Designbaseline:** v00.02.31  
**Prototypeversion:** v00.00.10n7e  
**Work branch:** `work/channel-campaign3-v10n7e-coastal-city-offsets`

## Formål

n7e er en lille visuel polish-build oven på n7d.

Brugertest viste, at city-art nu er korrekt og i passende størrelse, men tre kystnære byikoner ligger visuelt for tæt på vandet:

- Sæby,
- Helsingør,
- Nykøbing Sjælland.

Canonical CITY-REG-01 WGS84-koordinater er ikke ændret. n7e flytter kun artwork og label en lille smule visuelt.

## Bindende regel

Visual offset må ikke ændre:

- CityId,
- canonical WGS84 longitude/latitude,
- ZoneId,
- click anchor,
- collider position,
- save identity,
- simulation geography.

Offsettet er udelukkende et campaign-map UI/render lag.

## Offsets

| CityId | By | Visual world offset |
| --- | --- | --- |
| SAEBY | Sæby | (-0.18, 0.00, -0.02) |
| HELSINGOR | Helsingør | (-0.22, 0.00, -0.08) |
| NYKOBING_SJ | Nykøbing Sjælland | (0.00, 0.00, -0.13) |

Artwork og label flyttes sammen. Click-collideren bliver på den canonical city marker.

## Runtime

`CampaignMapOnlyCityArtV010N7E` er aktiv city-art owner.

Den genbruger de indlejrede Proposal-3 A/B/C assets fra n7c, inklusive:

- A/B/C scale 0.95 / 0.72 / 0.52,
- ground lift 0.12,
- bottom-center billboard,
- ingen Resources dependency,
- ingen fallback city style.

n7c får samtidig build-version guard, så den ikke auto-create'r i n7e.

## City INFO

n7d city-click adfærd bevares:

- venstreklik på city marker vælger byen,
- INFO-panelet åbner automatisk,
- navn, tier, population 1850, ZoneId og heavy-military build rule vises.

Da collideren ikke flyttes med visual offsettet, forbliver gameplay-geografien canonical.

## Amt/county

Vej 1 fortsætter. Amt/county boundaries, click ownership, highlight og info forbliver OFF.

## QA

1. Unity 6000.6.0f1 compiles uden blocking errors.
2. topbar/badge viser v00.00.10n7e.
3. `CityArtN7E=True|Cities=68` logges.
4. ingen competing n7c/n7b/n7a renderer.
5. Sæby-art/label ligger tydeligere på land.
6. Helsingør-art/label ligger tydeligere på Sjælland-siden.
7. Nykøbing Sjælland-art/label ligger lidt længere fra kysten.
8. click på de tre byer åbner stadig korrekt INFO.
9. canonical city marker/collider er ikke flyttet.
10. ingen CITY-REG-01 WGS84-data er ændret.
11. Proposal-3 visual language og n7c scale er uændret.
12. Amt/county forbliver OFF.

## Status

**IMPLEMENTERET PÅ WORK BRANCH — AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA.**
