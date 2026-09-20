# Del 32: 67 — Campaign3 v00.00.10n7g: Coastal City Offset Pass 2

**Designbaseline:** v00.02.33  
**Prototypeversion:** v00.00.10n7g  
**Work branch:** `work/channel-campaign3-v10n7g-coastal-offset-pass2`

## Formål

n7g er anden visuelle coastal-placement pass for city art.

Brugertest viste, at den nye Proposal-3 grafik nu er korrekt, men flere kystnære city icons stadig visuelt ligger for tæt på vandet. n7g flytter kun artwork og label.

Canonical CITY-REG-01 WGS84, CityId, ZoneId, click collider, save identity og simulation geography ændres ikke.

## Offsets

| CityId | By | Visual world offset |
| --- | --- | --- |
| SAEBY | Sæby | (-0.18, 0.00, -0.02) |
| HELSINGOR | Helsingør | (-0.34, 0.00, -0.12) |
| NYKOBING_SJ | Nykøbing Sjælland | (0.00, 0.00, -0.24) |
| BOGENSE | Bogense | (0.14, 0.00, -0.12) |
| ASSENS | Assens | (0.18, 0.00, 0.00) |
| STEGE | Stege | (0.12, 0.00, 0.02) |
| PRAESTO | Præstø | (-0.10, 0.00, 0.10) |
| NYKOBING_MORS | Nykøbing Mors | (-0.12, 0.00, 0.08) |

Helsingør og Nykøbing Sjælland er stærkere offsets end i pass 1.

## Bindende regel

Visual offsets er kun et render/UI-lag.

De må ikke ændre:

- WGS84,
- CityId,
- ZoneId,
- click target,
- movement,
- save state,
- campaign ownership,
- BattleContext position.

## Runtime

`CampaignMapOnlyCityArtV010N7E` fortsætter som city-art owner og er tilladt i n7g. Der oprettes ikke endnu en parallel city-renderer.

`CampaignCityVisualOffsetsV010N7E` er det fælles offset-register og er udvidet til otte byer.

Labels bruger samme offsets som artwork. Click colliders bliver på canonical markers.

## QA

1. n7g compiles uden blocking errors.
2. Proposal-3 art vises 68/68.
3. alle otte offset-byer ser visuelt landbaserede ud ved normal campaign zoom.
4. labels følger artwork.
5. city click INFO fungerer stadig.
6. canonical city positions er uændrede.
7. clean HUD fra n7f bevares.
8. Amt/county forbliver OFF.

## Status

**IMPLEMENTERET PÅ WORK BRANCH — AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA.**
