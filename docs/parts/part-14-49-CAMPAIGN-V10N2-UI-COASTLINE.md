# Del 14: 49 — Campaign3 v00.00.10n2: HUD-cleanup og kystklippede zonegrænser

**Implementeringsstatus:** IMPLEMENTERET PÅ WORK-BRANCH / AFVENTER UNITY 6.6 QA  
**Campaign-version:** v00.00.10n2  
**Designbaseline:** v00.02.15  
**Work-branch:** `work/channel-campaign3-v10n2-ui-coastline`  
**Rollback:** `backup/channel-campaign3-before-v10n2-ui-coastline-20260914`

## 49.1 Formål

v10n2 rydder Campaign3 Game View op efter v10m/v10n og gør prototype-zoneoverlayet visuelt mere troværdigt. Versionen løser to konkrete problemer:

1. store info/debug-paneler dækkede en væsentlig del af kortet og overlappede hinanden,
2. v10n's centerbaserede zoner blev bygget inden for fem rektangulære geografiske grupper, så zonegrænser kunne fortsætte ud i havet og grupper kunne give indtryk af overlap.

Versionen ændrer ikke de kanoniske `ZONE-REG-01`- eller `CITY-REG-01`-identiteter og gør ikke prototypegeometrien til historisk facit.

## 49.2 Persistent HUD-state

Ny `CampaignHudStateV010N2` bliver fælles visibility-state for Campaign3 HUD-paneler. Tilstanden gemmes gennem `PlayerPrefs`, så brugerens valg overlever nye sessioner.

Standardtilstanden er:

- top-left build badge: synlig
- campaign topbar/tidskontrol: synlig
- selected zone/city/formation panel: synlig
- tekniske World Imagery / terrain / basemap / zone debug-paneler: skjult

Hotkeys:

| Tast | Funktion |
| --- | --- |
| `F1` | Vis/skjul alle ikke-essentielle HUD-paneler |
| `F2` | Vis/skjul selected zone/city/formation info |
| `F3` | Vis/skjul tekniske/debug-paneler |
| `F4` | Reset HUD til selection ON, debug OFF |

Topbaren får desuden `INFO` og `DBG` toggles, når skærmbredden giver plads. Selection-panelet kan lukkes direkte med `×`.

## 49.3 Panel-layout

Tekniske paneler må ikke længere bevidst tegnes oven i gameplay-info:

- World Imagery/3D Terrain debug flyttes under selection-panelet, når selection er synlig.
- Basemap-referenceboksen ligger over nederste campaign-datafelt.
- Zone-overlay statusbadge vises kun i debug-mode.
- Build-badget er fortsat kompakt og altid synligt.

Målet er, at normal gameplay-visning primært består af kortet, tidskontrol og den valgte enheds relevante data.

## 49.4 Én central buildversion

`CampaignBuildInfo.CurrentVersion` er bindende source of truth for den synlige Campaign3-version.

`GrandCampaignBootstrap.CampaignVersion` peger nu på samme konstant, og nation-selection/topbar bruger ligeledes `CampaignBuildInfo`. Dermed må top-left badge og campaign topbar ikke længere divergere som i screenshot-eksemplet med v10n1 øverst og v10m i tidsbaren.

## 49.5 Global ikke-overlappende zonepartition

Den oprindelige v10n-prototype brugte fem separate rektangler:

- Jylland
- Fyn/Langeland
- Sjælland/Møn
- Lolland-Falster
- Bornholm

Denne model fjernes i v10n2 som geometri-generator.

Alle 20 `ZONE-REG-01`-centre indgår i stedet i **én global nearest-centre/Voronoi-partition**. For et vilkårligt punkt er den zone med nærmeste zonecenter den eneste indvendige ejer. Det betyder, at to zonearealer ikke kan overlappe i prototypepartitionen.

Runtime-kontrakt:

`GeometryMode=PROTOTYPE_LAND_CLIPPED_GLOBAL_VORONOI`

Dette er fortsat en visualiseringsprototype og ikke et historisk administrativt kort.

## 49.6 Kystklipning

Efter nearest-centre-partitionen klippes hver zones polygondele mod den landgeometri, som allerede er oprettet af `CampaignDenmarkGeography`.

Landmasken læses direkte fra runtime-objekterne:

`GEO_Denmark_NaturalEarth50m / DNK_LandPart_*`

Det undgår en separat kopieret coastline-datakilde og holder zoneoverlayets yderkant i samme koordinater som den eksisterende Campaign3-landscaffold.

Konsekvensen er, at de gamle zone-rektangler ikke længere fortsætter ud i det omgivende hav. Øer bliver kun tildelt den nærmeste zone gennem samme globale partition.

## 49.7 Begrænsning: coastline er stadig en scaffold

Kystklipningen løser v10n's tydelige hav-overløb, men `CampaignDenmarkGeography` bruger fortsat den forenklede Natural Earth 1:50m Danmark-geometri.

Det betyder:

- den er bedre egnet som land/sea scaffold end de gamle rektangler,
- den er ikke en detaljeret hydrografisk facit ved hvert fjordløb, havnebassin eller mindre ø,
- World Imagery kan derfor vise lokale vanddetaljer, som 1:50m-masken ikke gengiver fuldt ud,
- Limfjorden og andre komplekse kyst-/fjordområder skal fortsat indgå i den senere højere-detalje geografi/produktionspolygon-pipeline.

v10n2 må derfor ikke beskrives som endelig historisk eller hydrografisk zonegeometri.

## 49.8 Historiske zonegrænser

Produktionsmålet er fortsat kildebaserede 1851-grænser fra **DigDag — Amt og Region** eller anden verificeret historisk GIS-kilde.

Når de data importeres, skal de erstatte geometrien uden at ændre:

- `ZoneId`
- `CityId` / `ZoneId` relationer
- owner/controller
- økonomi
- population
- recruitment
- supply
- save-state

Prototypegeometrien er kun render-/interaktionsarkitektur.

## 49.9 Shared borders

Nabozoner kan fortsat have hver sin LineRenderer på præcis den samme delte grænse. For at undgå at dette visuelt ligner overlap ændres grænsematerialet til fuld opacity, og linjen gøres lidt smallere.

Der er derfor forskel på:

- **area overlap:** ikke tilladt og elimineret af global partition,
- **identisk shared-border line:** tilladt, fordi begge zone-loops mødes på samme koordinater.

## 49.10 Marchordre på bymarkører

v10n2 retter samtidig en interaktionskant fra v10m: en bys collider kunne opsnappe højreklik, så en valgt hær ikke modtog marchordre til zonen bag byen.

RMB på en `GrandCampaignCityMarker` resolves nu til byens `ZoneId`. De eksisterende Land/Ferry adjacency-regler afgør fortsat, om ordren accepteres.

## 49.11 Uændrede kontrakter

v10n2 bevarer:

- 20 `ZONE-REG-01` zoner
- 68 `CITY-REG-01` købstæder
- urban population checksum 290.565
- Esbjerg ude af 1851-startstate
- A/B/C-building-regler
- separate Land/Ferry neighbour-links
- QA-hæren i `DK-Z11-VEJ`
- World Imagery som basemap
- streamed 3D terrain
- `Z` som zone-overlay toggle
- overlay uden colliders

## 49.12 QA-gates

Før promotion til `channel-campaign3` skal følgende testes i Unity 6000.6.0f1:

1. Projektet compiler uden errors.
2. Build badge og campaign topbar viser begge `v00.00.10n2`.
3. Fresh/default HUD viser ikke de store tekniske debugbokse.
4. F1/F2/F3/F4 fungerer og PlayerPrefs-state overlever genstart.
5. INFO/DBG-knapper fungerer på normal Game View-bredde.
6. Selection-panelets `×` skjuler panelet uden at påvirke simulationen.
7. Debug-paneler overlapper ikke selection-/campaign-datafeltet som tidligere.
8. Alle 20 zoneidentiteter får mindst én synlig landklippet polygon-del.
9. Zoneoverlay fortsætter ikke til de tidligere geografiske rektangelkanter ude i havet.
10. Der ses ikke areal-overlap mellem nabozoner.
11. Shared borders fremstår som én ensartet grænse visuelt.
12. `Z` slår hele overlayet til/fra.
13. Overlay har fortsat ingen colliders.
14. RMB direkte på en by kan bruges som marchdestination for byens zone, når den er adjacent.
15. 20/68/290565 registry-validation består.
16. City interaction og A/B/C-building gate er uændret.
17. World Imagery og 3D terrain loader som før.
18. Pause/x1/x5/x20/x100 fungerer som før.

## 49.13 Status

**IMPLEMENTERET PÅ `work/channel-campaign3-v10n2-ui-coastline`, MEN IKKE UNITY-COMPILE/PLAY-TESTET I VÆRKTØJSMILJØET.**

Promotion til `channel-campaign3` må først ske efter brugerens Unity QA eller efter konkrete compile/runtime-fejl er rettet.
