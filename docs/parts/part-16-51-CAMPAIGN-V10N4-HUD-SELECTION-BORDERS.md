# Del 16: 51 — Campaign3 v00.00.10n4: HUD-default, korrekt zonevalg og delte interne grænser

**Designbaseline:** v00.02.17  
**Runtimeversion:** v00.00.10n4  
**Work branch:** `work/channel-campaign3-v10n4-hud-zone-lines`  
**Status:** IMPLEMENTERET PÅ WORK BRANCH / AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA

## 51.1 Formål

v10n4 er et hotfix oven på v10n3 efter Play Mode-observationer. Det retter tre konkrete problemer uden at ændre ZONE-REG-01, CITY-REG-01 eller campaign-simulationen:

1. de to store legacy INFO-bokse skal ikke stå åbne ved start,
2. klik på et amt skal returnere det amt, som den synlige polygon faktisk tilhører,
3. lange enkeltstående gule zone-segmenter og vand-/kystrester fra n3 skal ikke renderes.

## 51.2 Legacy INFO lukket som standard

`CampaignHudStateV010N2` bevares som den eksisterende HUD-controller, men n4 ændrer standarden:

- legacy zone/city/formation INFO starter lukket,
- tekniske debugpaneler starter lukket,
- compact zone card er normal kontekstvisning efter map-click,
- `INFO/F2` åbner fortsat legacy detaljepanelet eksplicit,
- `F4` resetter til HUD synlig + legacy INFO skjult + debug skjult.

For eksisterende installationer anvendes en one-time `PlayerPrefs` migration, så gamle n2/n3-indstillinger ikke tvinger de store bokse åbne første gang n4 kører.

## 51.3 Zonevalg skal følge faktisk polygon

n3 brugte en nearest-centre fallback ved klik på et område uden collider. Det kunne give en forkert zoneidentitet, fx returnere Aalborg Amt ved et klik i Vejle-området.

n4 fjerner denne genvej. `CampaignZoneInfoPanelV010N3` læser i stedet de faktiske source-polygoner fra:

`ZONE_OVERLAY_1851_LAND_CLIPPED`

Hver source-loop har `CampaignZoneOverlayMetadataV010N.ZoneId`. Map-click konverteres til campaign X/Z og testes med `PointInPolygon` mod disse faktiske polygondele.

Prioritet ved klik er:

1. city marker → byens registrerede `ZoneId`,
2. zone marker → markerens `ZoneId`,
3. zonepolygon → polygonens metadata `ZoneId`.

Der bruges ikke længere nearest-centre til zoneinfo-selection.

## 51.4 Delte interne grænser alene

n3's line-polish renderede alle unikke segmenter efter deduplikering. Et segment kunne derfor stadig være et polygon-ydersegment eller clipping-artifact og kun eksistere på én zones polygon. Det skabte enkelte lange lodrette/diagonale gule sporer og vandrester.

n4 ændrer border-reglen:

- hvert snapped segment registrerer de **forskellige ZoneIds**, der ejer segmentet,
- kun segmenter med mindst **to forskellige ZoneIds** renderes,
- single-owner segments skjules,
- originale v10n2 source-loops bevares skjult som geometry/data,
- det gule standardlag bliver dermed et rent **internal shared-border layer**.

Kysten fungerer fortsat som naturlig ydre afgrænsning via basemap/geografien frem for en ekstra gul zonekant.

## 51.5 Historisk guardrail

Dette hotfix forbedrer konsistens og UI, men ændrer ikke polygonernes historiske status:

- `ZONE-REG-01` identity er kanonisk,
- polygongeometrien er fortsat prototype centre-derived,
- Natural Earth 1:50m er fortsat en forenklet coastline scaffold,
- endelige administrative 1851-grænser skal fortsat komme fra source-backed historisk GIS, fx DigDag Amt/Region,
- senere udskiftning af polygonerne må ikke ændre ZoneId, city relationer, save state eller simulation state.

## 51.6 QA

Minimum acceptance:

1. Unity 6000.6.0f1 compile uden errors.
2. Badge/topbar viser `v00.00.10n4`.
3. Legacy INFO-bokse er lukkede første gang n4 starter.
4. Debugpaneler er lukkede.
5. `INFO/F2` kan åbne/lukke detaljepanelet.
6. `F4` går tilbage til clean default.
7. Klik i Vejle Amt viser `DK-Z11-VEJ`.
8. Klik i Aalborg Amt viser `DK-Z16-AAL`.
9. Klik i andre zoner følger polygonens metadata, ikke nearest centre.
10. Klik på by bruger byens authoritative `ZoneId`.
11. Lange one-sided n3-sporer er væk.
12. Gult borderlag viser kun shared internal borders.
13. `Z` toggler fortsat overlayet.
14. 20 zoner / 68 byer / 290.565 checksum består.
15. World Imagery, 3D terrain og movement fungerer som før.
