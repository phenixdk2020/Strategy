# Del 21: 56 — Campaign3 v00.00.10n6d: Embedded zone-resolver compile hotfix

**Prototype:** v00.00.10n6d  
**Unity baseline:** 6000.6.0f1  
**Workbranch:** `work/channel-campaign3-v10n6d-resolver-compile-hotfix`  
**Target:** `channel-campaign3`

## Fejl

Efter promotion af n6c rapporterede Unity tre `CS0103`-fejl i `CampaignZoneInfoPanelV010N3.cs`, fordi `CampaignZoneOwnershipResolverV010N6C` ikke fandtes i den lokale compile context.

## Løsning

n6d fjerner den separate compile-afhængighed. Zoneejerskabsresolveren er nu indlejret direkte i `CampaignZoneInfoPanelV010N3.cs`.

Den separate fil `CampaignZoneOwnershipResolverV010N6C.cs` og dens `.meta` fjernes fra builden, så der kun findes én autoritativ implementation.

Den funktionelle kontrakt fra n6c ændres ikke:

- direkte byklik bruger altid canonical `City.ZoneId`;
- områdeklik skal først være inden for den landklippede zonegeometri;
- områdeejerskab beregnes ud fra de 20 zonecentre + de 68 canonical city-sites;
- normal area selection bruger ikke `GrandCampaignZoneMarker` som fallback;
- klik i åbent hav skal ikke vælge et Amt.

## Runtime QA

Ved startup skal loggen indeholde:

```text
ZoneOwnershipResolver=True
Mode=CANONICAL_CITY_PLUS_ZONE_CENTRE_NEAREST_SITE_EMBEDDED
CompileDependency=Embedded
CityCanonical=68/68
ZoneCentreCanonical=20/20
MarkerFallback=False
```

Ved områdeklik forventes:

```text
Source=CanonicalMultiSiteOwnershipEmbedded
```

## Bevarede n6c-funktioner

Proposal-3 A/B/C city-art, transparente city-icons, skjulte gamle city-cirkler, withdrawal-systemet, World Imagery, streamed 3D terrain og CITY-/ZONE-REG-01 data ændres ikke af hotfixet.

## Historisk guardrail

Den synlige Amt-geometri er fortsat prototype (`PROTOTYPE_LAND_CLIPPED_MULTI_SITE_VORONOI`). n6d retter compile- og klikresolver-konsistens, men gør ikke de viste grænser historisk præcise. Source-backed DigDag Amt/Region polygoner er fortsat produktionsmålet.

## Status

**IMPLEMENTERET — afventer Unity 6.6 compile + Play Mode QA.**
