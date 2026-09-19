# Del 29: 64 — Campaign3 v00.00.10n7d: City Info + Legacy Startup Hotfix

**Designbaseline:** v00.02.30  
**Prototypeversion:** v00.00.10n7d  
**Work branch:** `work/channel-campaign3-v10n7d-city-info-hotfix`

## Formål

n7d er et lille stabiliseringshotfix oven på n7c.

n7c's embedded Proposal-3 city art, scale og anchor bevares uændret. Fokus er:

- fjerne stale n7a/n7b startup-runtime,
- åbne city INFO automatisk ved direkte klik,
- fastholde canonical city coordinates,
- dokumentere begrænsningen i den grove Natural Earth 1:50m landmaske.

## Legacy startup fix

`CampaignMapOnlyCityArtV010N7A` og `CampaignMapOnlyCityArtV010N7B` må kun auto-create i deres egen buildversion.

Dette forhindrer den gamle n7b-komponent i at nå et `Resources.Load<Texture2D>`-kald og logge:

```text
CityArtN7B=False|Reason=Proposal3TextureMissing
```

i en n7c/n7d-session.

## City click / info

`GrandCampaignBootstrap` havde allerede city selection og city-data, men INFO-panelet kunne være skjult.

n7d tilføjer `CampaignHudStateV010N2.ShowSelectionPanel()`.

Ved direkte venstreklik på en `GrandCampaignCityMarker`:

1. `selectedCity` sættes,
2. `selectedZone` sættes fra byens canonical `ZoneId`,
3. INFO/selection-panelet åbnes automatisk,
4. loggen skriver CityId, navn, tier, population 1850 og ZoneId.

INFO-panelet viser fortsat:

- bynavn,
- A/B/C-tier,
- indbyggertal 1850,
- ZoneId,
- regel for tung militær udbygning.

## City coordinates / land audit

CITY-REG-01's WGS84 coordinates forbliver authoritative.

En QA-test mod den indbyggede Natural Earth 1:50m landgeometri klassificerede kun 53/68 canonical city coordinates som inde i landpolygonerne. 15 legitime kyst-/øbyer blev flagget som udenfor, bl.a. København, Korsør, Fredericia, Sæby og Frederikshavn.

Konklusion:

- Natural Earth 1:50m er for grov til automatisk city-anchor korrektion,
- den må ikke flytte canonical city coordinates,
- city-art visual QA skal ske mod den faktiske imagery/coastline-visning eller en senere højere opløst landmask,
- et city icon må gerne overlappe kysten en smule pga. sin visuelle footprint, men markerens anchor må ikke flyttes ud i havet.

## Scope

Amt/county-systemet forbliver OFF efter Vej 1.

## QA

1. Unity 6000.6.0f1 compiler uden blocking errors.
2. topbar/badge viser v00.00.10n7d.
3. n7b `Proposal3TextureMissing` startup-log vises ikke.
4. n7c embedded city art installerer 68/68.
5. klik på en by åbner INFO-panelet automatisk.
6. INFO-panelet viser korrekt navn, tier, population, ZoneId og build rule.
7. city click logger `CitySelected=True`.
8. city-art scale/anchor er uændret fra n7c.
9. Amt/county er fortsat OFF.

## Status

**IMPLEMENTERET PÅ WORK BRANCH — AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA.**
