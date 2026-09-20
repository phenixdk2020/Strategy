# PROJECT 1864 — Designmanual

**Aktuel designbaseline: v00.02.33**  
**Aktuel prototype-workbranch: P0A v00.00.09 TACTICAL COMMAND TEST**  
**Aktuel campaign-workbranch: Campaign3 v00.00.10n7g COASTAL CITY OFFSET PASS 2**

Grand Strategy i realtid + taktiske 3D-slag. Denne GitHub-udgave er opdelt i dele for overskuelig versionsstyring. Den layoutede Word-master opdateres parallelt som projektartefakt, mens GitHub-Markdown er den løbende designmæssige source of truth.

Projektets centrale intake-log for besluttede men endnu ikke implementerede funktioner, planlagte opgaver, research-emner og løse idéer ligger i [PROJECT-BACKLOG.md](PROJECT-BACKLOG.md). Større emner kan have detaljerede backlog-supplementer, som senere konsolideres ind i hovedbackloggen.

Den komplette dokumentation af **hvad der implementeres og skal testes i P0A v00.00.09**, officerstats, AI difficulty, skydning/fire policy, tactical command-menu, kendte begrænsninger og den fulde Unity acceptance-test ligger i [P0A v00.00.09 Release Notes](releases/P0A-v00.00.09-RELEASE-NOTES.md).

## Indhold

- [Del 1: 1–6 — Executive summary, slutvision, strategisk realtid, kort, nationer og OOB](parts/part-01-01-06.md)
- [Del 2: 7–12 — Enheder, officerer, ordrer, march, logistik og fog of war](parts/part-02-07-12.md)
- [Del 3A: 13–19 — Taktiske 3D-slag, kamp, våbenarter, casualties, retreat og flåde](parts/part-03a-13-19.md)
- [Del 3B: 20 — Befolkning, økonomi, byudvikling, industri, handel, forskning, rekruttering, træning, sanitet/fanger, regimentshistorik og perks](parts/part-03b-20-20.md)
- [Del 3C: 20.16 — Strategisk landudvikling: veje, jernbane, gårde, hesteopdræt, våbenindustri og regionale projekter](parts/part-03c-20-16-strategic-development.md)
- [Del 3D: Battle supply, skumring/nat, overnight resupply, kavaleri og dragoner](parts/part-03d-night-supply-cavalry.md)
- [Del 3E: 20.17 — CITY-REG-01: Kongeriget Danmarks 68 købstæder i 1850, population, udviklingsklasser og zone-tilknytning](parts/part-03e-20-17-city-register-1851.md)
- [Del 3F: 20.18 — ZONE-REG-01: Kongeriget Danmarks territoriale 1851-zoner](parts/part-03f-20-18-territorial-zones-1851.md)
- [Del 4: 21–27 — Strategisk/taktisk AI, terræn, performance, UI, save/modding og historisk datamodel](parts/part-04-21-27.md)
- [Del 5A: Feature-arkitektur F00–F15](parts/part-05a-F00-F15.md)
- [Del 5B: Feature-arkitektur F16–F31](parts/part-05b-F16-F31.md)
- [Del 5C: Feature-arkitektur F32–F47](parts/part-05c-F32-F47.md)
- [Del 6: 29–36 — Milepæle, immediate prototype sequence, vertical slice, risici, datarelationer, historisk grounding og designbeslutninger](parts/part-06-29-36.md)
- [Del 7: 37 — Implementeringsstatus P0A Unity 3D Battle Prototype](parts/part-07-37-P0A.md)
- [Del 8: 38 — P0A v00.00.08 reload, experience, salve-feedback og enkel casualty-visual](parts/part-08-38-P0A-v08.md)
- [Del 10: 45 — Campaign3 v00.00.10m: 20 historiske zoner + 68 byer i runtime](parts/part-10-45-CAMPAIGN-V10M-ZONES-CITIES.md)
- [Del 11: 46 — Campaign3 v00.00.10n: 1851 zone-overlay og polygonarkitektur](parts/part-11-46-CAMPAIGN-V10N-ZONE-OVERLAY.md)
- [Del 12: 47 — Fælles sæson-, vegetation- og vejrsystem for Campaign + Battle](parts/part-12-47-SEASONS-WEATHER-CAMPAIGN-BATTLE.md)
- [Del 13: 48 — National AI: økonomi, byggeri, hær, udstyr og mobilisering](parts/part-13-48-NATIONAL-AI-ECONOMY-MILITARY.md)
- [Del 14: 49 — Campaign3 v00.00.10n2: HUD-cleanup og kystklippede zonegrænser](parts/part-14-49-CAMPAIGN-V10N2-UI-COASTLINE.md)
- [Del 15: 50 — Campaign3 v00.00.10n3: skarpere zonegrænser + kompakt zoneinfo](parts/part-15-50-CAMPAIGN-V10N3-ZONE-POLISH-INFO.md)
- [Del 16: 51 — Campaign3 v00.00.10n4: HUD-default, korrekt zonevalg og delte interne grænser](parts/part-16-51-CAMPAIGN-V10N4-HUD-SELECTION-BORDERS.md)
- [Del 27: 62 — Campaign3 v00.00.10n7b: Vej 1 / City Art Polish](parts/part-27-62-CAMPAIGN-V10N7B-CITY-ART-POLISH.md)
- [Del 28: 63 — Campaign3 v00.00.10n7c: Embedded City Asset Reset](parts/part-28-63-CAMPAIGN-V10N7C-EMBEDDED-CITY-ASSET-RESET.md)
- [Del 29: 64 — Campaign3 v00.00.10n7d: City Info + Legacy Startup Hotfix](parts/part-29-64-CAMPAIGN-V10N7D-CITY-INFO-HOTFIX.md)
- [Del 30: 65 — Campaign3 v00.00.10n7e: Coastal City Visual Offsets](parts/part-30-65-CAMPAIGN-V10N7E-COASTAL-CITY-OFFSETS.md)
- [Del 31: 66 — Campaign3 v00.00.10n7f: Compile + HUD Cleanup](parts/part-31-66-CAMPAIGN-V10N7F-COMPILE-HUD-CLEANUP.md)
- [Del 32: 67 — Campaign3 v00.00.10n7g: Coastal City Offset Pass 2](parts/part-32-67-CAMPAIGN-V10N7G-COASTAL-OFFSET-PASS2.md)
- [Release Notes — P0A v00.00.08 TEST](releases/P0A-v00.00.08-RELEASE-NOTES.md)
- [Release Notes — P0A v00.00.09 TACTICAL COMMAND TEST](releases/P0A-v00.00.09-RELEASE-NOTES.md)
- [Projekt-backlog — beslutninger, planlagte funktioner, research og idéer](PROJECT-BACKLOG.md)
- [Backlog B-160–B-169 — strategisk landudvikling](backlog/B-160-STRATEGIC-DEVELOPMENT.md)
- [Backlog B-170–B-179 — Officer AI, delegeret kommando og AI Unit ON/OFF](backlog/B-170-OFFICER-AI-DELEGATION.md)
- [Backlog B-180–B-189 — symmetrisk fjende-AI, sværhedsgrad og v00.00.09-prioritet](backlog/B-180-AI-DIFFICULTY-AND-V009.md)
- [Backlog B-190–B-199 — officerstats, Composure/Nerve og AI decision model](backlog/B-190-OFFICER-STATS-MODEL.md)
- [Backlog B-200–B-209 — Close/Medium/Long range bands, HQ hierarchy, semantic zoom og couriers](backlog/B-200-COMMAND-VISUALS-RANGE-HQ-COURIERS.md)
- [Backlog B-210–B-219 — fire eligibility, skudkegle og højere formation templates](backlog/B-210-FIRE-ELIGIBILITY-AND-HIGHER-FORMATIONS.md)
- [Backlog B-220–B-229 — Pause/x0,5/x1/x2/x5/x20, simulationstid og klokke](backlog/B-220-SIMULATION-TIME-CONTROLS.md)
- [Backlog B-230–B-239 — battle supply, skumring/nat og overnight resupply](backlog/B-230-BATTLE-SUPPLY-NIGHT-OPERATIONS.md)
- [Backlog B-240–B-249 — udvidet kavaleri- og dragonmodel](backlog/B-240-CAVALRY-DRAGOONS-EXPANDED.md)
- [Backlog B-250–B-259 — fog of war, scouts og HQ command effectiveness](backlog/B-250-FOG-SCOUTS-COMMAND-EFFECTIVENESS.md)

## City Register 1851 — kanonisk campaign-baseline

Designbaseline v00.02.17 fastholder CITY-REG-01 som den kanoniske city-node baseline for **Kongeriget Danmark** ved campaign-start 1. januar 1851. Populationen bruger folketællingen 1. februar 1850. Alle 68 købstæder skal findes på strategikortet, mens strategisk relevante ikke-købstæder registreres separat.

Byerne opdeles i **A — Development City**, **B — Regional Town** og **C — Minor Town**. B- og C-byer skal fortsat være synlige, klikbare og økonomisk/logistisk relevante, men de får ikke fri tung militær udbygning med fx kaserner, arsenaler, større militære depoter, våbenfabrikker eller permanente fæstninger. Historisk dokumenterede anlæg kan eksistere som fixed/special buildings uanset klasse.

Hver by har et bindende `ZoneId`. Første zonebaseline følger 1851's historiske amtsstruktur som regionalt simulationslag. Den omfatter **20 zoner** for Kongeriget Danmark, inklusive særskilt København Stad og separat Skanderborg Amt. Zonerne skal senere bære regional population, landbefolkning, rekruttering, skat, produktion, supply, infrastruktur, owner/controller og occupation state. City-tier og zone-state er separate: en lille by bliver ikke militær Development City, blot fordi den ligger i en vigtig zone.

Det fulde byregister, folketal, udviklingsklasser og zone-tilknytning ligger i [Del 3E / CITY-REG-01](parts/part-03e-20-17-city-register-1851.md). Den fulde zone-model ligger i [Del 3F / ZONE-REG-01](parts/part-03f-20-18-territorial-zones-1851.md). Esbjerg er ikke en 1851-startby og indgår derfor ikke i den kanoniske startstate.

## Campaign3 v00.00.10m — runtime-implementering af zoner og byer

Designbaseline v00.02.11 flytter CITY-REG-01 og ZONE-REG-01 fra dokumenteret design til faktisk Campaign3-runtime på `work/channel-campaign3-v10m-zone-city-runtime`. Den tidligere hardcodede liste med 12 grove QA-zoner og 10 QA-byer er erstattet af et separat `CampaignDenmark1851Registry`, der indeholder de 20 kanoniske zoner og samtlige 68 købstæder.

Runtime-registret validerer ved startup **20 zoner, 68 byer og urban population checksum 290.565**. Hver by har `CityId`, `ZoneId`, `Population1850`, A/B/C-tier og WGS84-nodeposition. Byerne er klikbare, city-panelet viser klasse, folketal og zone, og `CanBuildHeavyMilitaryInCity(cityId)` udgør den bindende API-gate for senere construction UI. B/C returnerer ikke normal tilladelse til tung militær udbygning; historiske fixed/special buildings forbliver en separat datakategori.

Zonerne skelner nu mellem `LandNeighbours` og `FerryNeighbours`. Første runtime-model accepterer direkte march gennem begge typer, men færgeforbindelser får en simpel ekstra tidsomkostning. Dette er bevidst kun en prototype; havnekapacitet, fartøjer, vejr, blokade og fjendtlig interdiction skal senere erstatte den simple ferry multiplier. Bornholm får ingen kunstig landforbindelse.

City-markørernes størrelse følger tier plus en begrænset populationseffekt. A-byer får labels på operational zoom, mens alle A/B/C-byer vises med labels ved close zoom. `ZONE_`, `CITY_` og `ARMY_`-præfikserne bevares, så den eksisterende World Imagery/3D-terrain marker-height pipeline fortsat kan identificere gameplay-objekterne. Den danske QA-hær starter nu i `DK-Z11-VEJ` i stedet for den udgåede `DK-SJ`-scaffold.

Den fulde implementerings- og QA-specifikation ligger i [Del 10 / Campaign3 v00.00.10m](parts/part-10-45-CAMPAIGN-V10M-ZONES-CITIES.md).

## Campaign3 v00.00.10n — zone-overlay foundation

Designbaseline v00.02.12 gør de 20 `ZONE-REG-01`-zoner visuelt læselige som territoriale områder. `CampaignZoneOverlayV010N` opretter lukkede boundary-loops for alle 20 zoner og kan toggles samlet med `Z`, uden at overlayet får colliders eller ændrer simulation state.

Zoneidentiteterne er kanoniske, men **v10n-geometrien er ikke historisk facit**. Første overlay bruger centerbaserede, ikke-overlappende sektorer inden for fem geografiske grupper som kontrolleret render-/UI-prototype. Runtime metadata sætter derfor `HistoricallyExactGeometry=false` og `GeometryMode=PROTOTYPE_CENTRE_DERIVED_SECTORS`.

Den planlagte produktionskilde er **DigDag — Amt og Region**, som dækker dansk historisk-administrativ geografi fra ca. 1660 og frem. Når de faktiske 1851-polygongrænser importeres, skal de mappes til eksisterende `ZONE-REG-01` IDs. CityId, ZoneId, save-state, økonomi, rekruttering og owner/controller må ikke skulle ændres, blot fordi den midlertidige visualiseringsgeometri udskiftes med kildebelagt geometri.

v10n bevarer hele v10m-runtimekontrakten: 20 zoner, 68 byer, checksum 290.565, Esbjerg ude af startstate, A/B/C-building-regler, separate Land/Ferry-links og QA-hæren i `DK-Z11-VEJ`. World Imagery og streamed 3D terrain ændres ikke af zoneoverlayet.

Den fulde implementerings- og QA-specifikation ligger i [Del 11 / Campaign3 v00.00.10n](parts/part-11-46-CAMPAIGN-V10N-ZONE-OVERLAY.md).

## Fælles sæson-, vegetation- og vejrsystem — Campaign + Battle

Designbaseline v00.02.13 fastlægger, at **campaign map og taktiske 3D-slag skal bruge samme miljøstate**. Campaign-datoen er autoritativ, og et slag skal arve sæson, snegrad, jordfugtighed, vegetation og marktilstand gennem `BattleContext`. Campaign og Battle må ikke have uafhængige sæsonberegninger.

Første model bruger fire basisårstider — `Winter`, `Spring`, `Summer` og `Autumn` — men årstiden er kun én del af state. `SnowCoverage` og `GroundWetness` er separate 0–1-værdier. En dansk vinterdag kan derfor være snefri, våd, let snedækket eller kraftigt snedækket; vinter må **ikke** automatisk betyde fuld hvid snedækning.

Landbrugslandskabet får egne states som `BareField`, `SpringGrowth`, `SummerGreen`, `SummerYellowGrain`, `Harvested`, `WinterDormant` og `SnowCovered`. Især sommeren skal kunne vise en mosaik af grønne områder og **gule/modne kornmarker**, mens efteråret gradvist viser høstede marker, gul/brun vegetation og mere bar jord.

På campaign map påvirker miljøstate primært terrain tint, vegetation, marker og sne-overlay uden at skjule World Imagery, byer, hære eller zonegrænser. På battle map oversættes samme state til højere detalje: terrænmaterialer, sne/frost, træer og løv, markmaterialer, jord/mudder og senere atmosfære og aktivt vejr.

Geografien og permanente bygninger forbliver persistente. Et cached battle map ved eksempelvis Aalborg skal derfor kunne bruges både i juli og januar ved at genindlæse den aktuelle miljøstate frem for at gemme en permanent sommer- eller vinterversion af stedet.

Første runtime-slice skal være visuelt orienteret. Senere gameplay-effekter må komme gennem konkrete mekanismer som movement, traction, visibility, fatigue og supply: våd/mudret jord kan eksempelvis hæmme artilleri og vogne, tæt sommervegetation kan påvirke observation, og vinterløv kan ændre LOS. Der må ikke gives vilkårlige direkte combat-bonuser alene på grund af årstiden.

Den foreslåede første implementation er **Campaign3 v00.00.10o — Seasonal Visual Foundation**, med fælles `CampaignSeasonState`, fire årstider, `SnowCoverage`, `GroundWetness`, marktilstande, campaign-visualisering, miljødata i `BattleContext` og QA-overrides til test af årstider og snegrad.

Den fulde designspecifikation ligger i [Del 12 / Fælles sæson-, vegetation- og vejrsystem](parts/part-12-47-SEASONS-WEATHER-CAMPAIGN-BATTLE.md).

## National AI — økonomi, byggeri, hær og udstyr

Designbaseline v00.02.14 fastlægger, at andre lande skal være fulde strategiske aktører. Et AI-land skal ikke kun flytte hære; det skal gennem en `NationalAIDirector` prioritere budget, byggeri, industri, våbenproduktion/import, rekruttering, træning, mobilisering, udstyr og fordeling af styrker mellem theatres.

AI-landene bruger **samme regler og samme begrænsninger som spilleren**. Nye regimenter kræver manpower, organisation, officerer/NCO'er, våben, uniformer, træning og tid. Artilleri kræver faktiske pjecer og ammunition; kavaleri og transport kræver heste; mobilisering må ikke teleportere en kampklar hær til fronten. Tilsvarende tager kaserner, arsenaler, depoter, fortifikationer, jernbaner, havne, hospitaler, stalde/remount-faciliteter og industri reel tid og ressourcer at opføre.

Hvert land får en datadrevet `NationAIProfile` med historisk rekrutteringssystem, force-structure bias, doktrin, training priority, equipment policy, industrial/infrastructure policy, finansiel risikotolerance og mobiliseringsadfærd. Historiske forhold fungerer som priors, men AI skal kunne reagere på alternativ campaign-udvikling frem for at følge en fast build order.

National AI skal konkret kunne svare på tre spørgsmål: **Hvad bygger vi? Hvilken hær vil vi have? Hvad skal den udrustes med?** Beslutningerne tages ud fra manpower, budget, lagre, produktionskapacitet, træning, transport, geografiske trusler, losses, readiness og forventede krigsskuepladser. Strategisk difficulty må primært ændre planlægningskvalitet og decision noise — ikke give gratis ressourcer, perfekt intelligence eller skjulte combat-bonusser.

Den fulde designspecifikation ligger i [Del 13 / National AI: økonomi, byggeri, hær, udstyr og mobilisering](parts/part-13-48-NATIONAL-AI-ECONOMY-MILITARY.md).

## Campaign3 v00.00.10n2 — HUD-cleanup og kystklippede zonegrænser

Designbaseline v00.02.15 rydder Campaign3 Game View op og erstatter v10n's fem rektangulære zonegrupper med én global prototypepartition.

`CampaignHudStateV010N2` gemmer HUD-valg via `PlayerPrefs`. Topbar og buildbadge forbliver synlige, mens zone/city/formation-info kan toggles med `F2`/`INFO`, tekniske World Imagery/terrain/basemap/zone-paneler med `F3`/`DBG`, hele den ikke-essentielle HUD med `F1`, og `F4` nulstiller layoutet. Tekniske debugpaneler er skjult som standard, og panelerne placeres, så de ikke længere bevidst ligger oven i hinanden.

Zoneoverlayet bruger nu `GeometryMode=PROTOTYPE_LAND_CLIPPED_GLOBAL_VORONOI`. Alle 20 zonecentre deltager i én global nearest-centre-partition, så zoneareal ikke kan overlappe. Hver polygondel klippes derefter mod runtime-landdelene fra `CampaignDenmarkGeography`, så de gamle rektangulære sektorer ikke fortsætter ud i det omgivende hav.

Kystklipningen bruger fortsat den forenklede Natural Earth 1:50m-landscaffold og er derfor ikke et løfte om fuld hydrografisk detalje ved alle fjorde, havne og småøer. Limfjorden og tilsvarende detaljer skal indgå i den senere højere-detalje GIS/geografi-pipeline. Historiske amtsgrænser er fortsat målrettet DigDag Amt/Region; `HistoricallyExactGeometry=false` er fortsat bindende.

v10n2 centraliserer også den synlige buildversion gennem `CampaignBuildInfo` og lader RMB på en bymarkør resolve til byens `ZoneId`, så byens collider ikke længere blokerer en ellers gyldig marchordre.

Den fulde implementerings- og QA-specifikation ligger i [Del 14 / Campaign3 v00.00.10n2](parts/part-14-49-CAMPAIGN-V10N2-UI-COASTLINE.md).

## Campaign3 v00.00.10n3 — skarpere zonegrænser og kompakt zoneinfo

Designbaseline v00.02.16 lægger et rent visual/UI-polish-lag oven på v10n2 uden at ændre zoneidentiteter, citydata eller simulationsregler.

`CampaignZoneLinePolishV010N3` genbruger v10n2's land-clippede prototypepolygoner, snapper segmentendepunkter til et fælles world-grid, deduplikerer identiske segmenter og renderer de interne zonegrænser som tyndere opaque linjer uden cap/corner smoothing. Segmenter der følger den nuværende `DNK_Coast_*` envelope skjules i standard zoneoverlayet, så World Imagery/kystgeografien selv definerer kystzonens yderkant frem for en ekstra gul linje.

`CampaignZoneInfoPanelV010N3` indfører et kompakt zonekort i øverste venstre hjørne. Klik på zone-marker, city-marker eller dansk land resolver en ZONE-REG-01-zone. Kortet viser ZoneId, antal byer, A/B/C-fordeling, summeret købstadsbefolkning 1850, byliste samt land- og færgeforbindelser. Ved city-click vises også valgt by, tier, folketal og buildregel. Det store legacy INFO/F2-panel kan fortsat åbnes som detailvisning, men erstatter det kompakte kort i stedet for at overlappe det.

Den nuværende geometri er stadig prototype. n3 gør renderingen skarpere, men `HistoricallyExactGeometry=false` forbliver bindende, og højere-detalje kyst/hydrografi samt source-backed 1851-amtsgrænser er fortsat senere GIS-arbejde.

Den fulde implementerings- og QA-specifikation ligger i [Del 15 / Campaign3 v00.00.10n3](parts/part-15-50-CAMPAIGN-V10N3-ZONE-POLISH-INFO.md).

## Campaign3 v00.00.10n4 — HUD-default, korrekt zonevalg og delte interne grænser

Designbaseline v00.02.17 retter tre Play Mode-fejl fra n3 uden at ændre campaign-data eller simulation.

Legacy zone/city/formation INFO-panelet er nu **skjult som standard**, også for brugere der tidligere havde gemt det som åbent i PlayerPrefs. En one-time n4 migration sætter clean default til HUD synlig, legacy INFO skjult og debug skjult. `INFO/F2` kan fortsat åbne detailpanelet eksplicit, mens compact zone card er normal map-click-visning.

Zoneinfo bruger ikke længere nearest-centre til arealklik. `CampaignZoneInfoPanelV010N3` læser de faktiske source-polygonloops fra `ZONE_OVERLAY_1851_LAND_CLIPPED` og resolver map-click med `PointInPolygon` mod polygonens `CampaignZoneOverlayMetadataV010N.ZoneId`. Dermed skal et klik i Vejle Amt returnere `DK-Z11-VEJ` og ikke en geografisk uvedkommende zone som Aalborg Amt.

Zone-line renderer registrerer samtidig hvilke forskellige ZoneIds der ejer hvert snapped segment. Kun segmenter delt af mindst **to forskellige zoner** vises. Single-owner edges — kystkanter, clipping-fragmenter og de lange one-sided sporer set i n3 — skjules. De oprindelige source-loops bevares som hidden geometry/data.

Historisk guardrail er uændret: zoneidentiteterne er kanoniske, men den nuværende center-derived polygongeometri er stadig prototype og ikke historisk 1851-facit.

Den fulde implementerings- og QA-specifikation ligger i [Del 16 / Campaign3 v00.00.10n4](parts/part-16-51-CAMPAIGN-V10N4-HUD-SELECTION-BORDERS.md).

## Campaign3 v00.00.10n7g — Coastal City Offset Pass 2

Designbaseline v00.02.33 udvider de visuelle coastal city offsets efter ny screenshot-QA. City-art, city scale, clean HUD og city INFO bevares.

Artwork + label får nu visual-only offsets for **Sæby, Helsingør, Nykøbing Sjælland, Bogense, Assens, Stege, Præstø og Nykøbing Mors**. Helsingør og Nykøbing Sjælland flyttes lidt længere ind på land end i n7e. WGS84, CityId, ZoneId, click collider, save identity og simulation geography ændres ikke.

[Del 32 / v00.00.10n7g](parts/part-32-67-CAMPAIGN-V10N7G-COASTAL-OFFSET-PASS2.md)

## Campaign3 v00.00.10n7f — Compile + HUD Cleanup

Designbaseline v00.02.32 rydder `CS0162`-warnings fra de ældre city-art build guards ved at flytte versionstesten bag `CampaignBuildInfo.IsCurrentVersion()`.

n7f indfører samtidig en **clean campaign HUD default**. Ved første n7f-run starter selection/INFO og debug skjult — også hvis ældre PlayerPrefs havde dem slået til. Den separate top-left build-boks, WORLD IMAGERY + 3D TERRAIN og BASEMAP/WGS84-boksene er nu debug-only. Topbaren med tid, pause/hastighed, INFO og DBG er fortsat altid synlig. Direkte klik på en by åbner stadig city INFO.

Unitys Input Manager deprecation-warning er registreret som teknisk gæld og migreres ikke i n7f; Input System migration skal ske separat med fuld campaign/battle regression.

[Del 31 / v00.00.10n7f](parts/part-31-66-CAMPAIGN-V10N7F-COMPILE-HUD-CLEANUP.md)

## Campaign3 v00.00.10n7e — Coastal City Visual Offsets

Designbaseline v00.02.31 bevarer n7c/n7d's Proposal-3 city art, scale og city-info, men tilføjer et visuelt offset-lag for tre kystnære byer: **Sæby, Helsingør og Nykøbing Sjælland**.

Offsets flytter kun artwork og label. CITY-REG-01 WGS84, CityId, ZoneId, click collider, save identity og simulation geography forbliver uændret. Dette er et bevidst render/UI-lag, fordi den coarse Natural Earth 1:50m landmaske ikke er præcis nok til at bruges som automatisk geografi-facit for kystbyer.

[Del 30 / v00.00.10n7e](parts/part-30-65-CAMPAIGN-V10N7E-COASTAL-CITY-OFFSETS.md)

## Campaign3 v00.00.10n7d — City Info + Legacy Startup Hotfix

Designbaseline v00.02.30 bevarer n7c's embedded Proposal-3 city art og Vej 1 scope. n7d stopper n7a/n7b fra at auto-create uden for deres egen buildversion og fjerner dermed den stale n7b `Proposal3TextureMissing` startup-log.

Direkte klik på en by åbner nu INFO-panelet eksplicit via `CampaignHudStateV010N2.ShowSelectionPanel()`. Panelet viser bynavn, tier, indbyggertal 1850, ZoneId og heavy-military build rule.

En audit mod den indbyggede Natural Earth 1:50m landmaske klassificerer kun 53/68 canonical city coordinates som land og giver falske negative for flere legitime kyst-/øbyer. Derfor må Natural Earth 1:50m ikke bruges til automatisk at flytte city anchors. CITY-REG-01's WGS84-positioner forbliver authoritative; visuel kyst-QA skal ske mod imagery eller senere højopløst landmask.

[Del 29 / v00.00.10n7d](parts/part-29-64-CAMPAIGN-V10N7D-CITY-INFO-HOTFIX.md)

## Campaign3 v00.00.10n7c — Embedded City Asset Reset

Designbaseline v00.02.29 fastholder **Vej 1** og retter den konkrete n7b-fejl, hvor Unity rapporterede `Proposal3TextureMissing|A=False|B=False|C=True`.

n7c fjerner runtime-afhængigheden til `Resources.Load<Texture2D>` for city-art. Tre validerede Proposal-3 PNG-assets for A/B/C er i stedet indlejret som runtime-data og decodes med `ImageConversion.LoadImage`. De tidligere Resources-PNG'er fjernes fra den aktive build, og `CampaignMapOnlyCityArtV010N7C` rydder alle ældre `CITY_ART_`, `CITY_ICON_` og `TownGround` children **før** ny art installeres. En texture-fejl kan dermed ikke efterlade den gamle forkerte city-grafik på kortet.

Byerne reduceres yderligere til A=0.95, B=0.72 og C=0.52 med ground lift 0.12. Canonical city marker er fortsat eneste X/Z-anchor, og klikcollideren er større end selve artworket.

Amt/county boundaries, click ownership, highlight og info forbliver OFF. De må først genindføres efter accepteret city-art QA og skal da bruge én authoritative historisk polygonkilde til både line, click og highlight.

Den fulde implementation/QA-specifikation ligger i [Del 28 / Campaign3 v00.00.10n7c](parts/part-28-63-CAMPAIGN-V10N7C-EMBEDDED-CITY-ASSET-RESET.md).

## Campaign3 v00.00.10n7b — Vej 1 / City Art Polish

Designbaseline v00.02.28 følger den besluttede Vej 1: Amt/county-systemet forbliver eksplicit slået fra, mens city-art stabiliseres isoleret. `CampaignMapOnlyCityArtV010N7B` overtager som eneste runtime owner af byernes grafik og deaktiverer n7a samt ældre n6/n6g/n6h city-renderers.

Kun **Proposal 3 — Isometric Miniature Town** er tilladt som visuel stil. Der må ikke vises beige/generated fallback-byer. Byikonerne gøres væsentligt mindre end i n7a: A=1.55, B=1.12 og C=0.78. Bottom-center anchor, ingen X/Z-art-offset og ground lift 0.16 fastholdes, så især kystbyer ikke opleves visuelt flyttet ud i vandet.

Texture sampling bruger Bilinear + Clamp og mip bias -0.75 som første skarpheds-pass. Klikcollideren forbliver større end selve ikonet, så mindre grafik ikke gør city selection vanskeligere.

Amt/county boundaries, county click ownership, county highlight og county info panels må ikke genindføres i n7b. Den senere County Rebuild skal bruge **én authoritative historisk polygonkilde** til visible boundary, click ownership og selected-area highlight.

Den fulde implementation/QA-specifikation ligger i [Del 27 / Campaign3 v00.00.10n7b](parts/part-27-62-CAMPAIGN-V10N7B-CITY-ART-POLISH.md).

## v00.00.09 Tactical Command Test — implementeringsstatus

P0A v00.00.09 på arbejdsbranchen implementerer den første sammenhængende tactical-command slice oven på v00.00.08. Danske testregimenter er **forsvarere**, mens de preussiske testregimenter er **angribere**. Battlefield er udvidet fra 180x120 til **360x240**, startafstanden er forøget, og kameraets bounds/zoom er udvidet, så spilleren har reel tid og plads til at pause, inspicere, udstede ordrer og manøvrere før kontakt.

Alle fire regimenter får `OfficerProfile` + `OfficerAIController`. Danske regimenter starter AI OFF, mens preussiske regimenter starter AI ON gennem samme shared decision core. Officerprofilen består af Leadership, Inspiration, Tactical Skill, Initiative, Staff/Command Skill, Discipline/Obedience, Aggressiveness/Caution og Composure/Nerve samt separat Officer Experience. Composure påvirker stress-relateret reaction delay og decision noise; alle profiler i v00.00.09 er QA-data og ikke historiske ratings.

Valgte danske regimenter kan toggles med **`I` = AI UNIT ON/OFF**; `A` er fortsat kamera-left i WASD. En contextual command-menu giver `DEFENSIVE / BALANCED / OFFENSIVE` doctrine, en 0–100 commander `forsigtig ↔ aggressiv` intent og fire policy `HOLD / CLOSE / MEDIUM / LONG`. Commander intent biaser execution, men officerens egen Aggressiveness er fortsat dominerende i første model. Højreklik går direkte til Regiment, når AI er OFF, og bliver Officer AI Move/Attack mission, når AI er ON.

Fjendens Officer AI må ikke blot løbe frem. Preussiske angribere starter Offensive med OrderAgg 65 og Medium fire policy. AI beregner preferred engagement range ud fra officer/order/doctrine, kan bruge column på længere approach, deployer til line ved engagement, kan stabilisere under morale pressure og lukker nu faktisk til sin beregnede preferred range i stedet for at blive maskeret af den gamle `OrderAttack` stopafstand. 18th Regiment har desuden et flank/approach waypoint før det reassesserer angrebet.

Easy/Normal/Hard påvirker kun den computerstyrede preussiske sides ekstra reaction/noise layer. Difficulty må ikke ændre weapon accuracy, reload, range, movement, morale, cohesion, casualties, officer stats eller skjult viden. Player-delegerede danske officerer bruger deres faktiske QA-profil på referenceindstillinger uanset enemy difficulty.

v00.00.09 har en fælles simulation time-control bar med **PAUSE / x0,5 / PLAY x1 / x2 / x5 / x20** og synligt dato/klokkeslæt. x0,5 er slow tactical mode til ordreafgivelse og observation under pres. Pause stopper AI, movement, reload/fire progression og battle clock samlet, mens kamera/UI fortsat er brugbart.

## Directional fire, fire discipline og accuracy

Den gamle 360° infantry range-ring er erstattet af en **120° fremadrettet fire fan (±60°)**, som følger regimentets facing og åbner ud fra formationens frontage. Det repræsenterer, at soldater i en linje kan traverse våbnet til siderne, men at regimentet ikke kan skyde lige effektivt bagud uden at vende/reformere.

Ved valgt regiment vises tre nested grænser:

- **Close:** op til 50 % af `EffectiveRange`.
- **Medium:** op til `EffectiveRange`.
- **Long:** op til `MaximumRange`.

Fire policy bestemmer **hvornår** enheden må åbne ild: `HOLD`, `CLOSE`, `MEDIUM` eller `LONG`. Et mål skal både være inden for fire-policy-afstanden og inden for den forward fire arc. Close/Medium/Long er ordre-/UI-grænser; den underliggende accuracy er en **kontinuerlig distancekurve**, så tættere mål gradvist bliver lettere at ramme uden kunstige hit-chance spring ved band-grænserne. Weapon-profile reload, Experience-reload, 0-hit og positiv `Ramte N` feedback fra v00.00.08 bevares.

Næste combat-fase udbygger samme retning med formation-segmenteret `eligibleFiringFraction`, LOS, friendly obstruction, terrain/smoke og target exposure, så kun den del af regimentets frontage der reelt kan skyde bidrager til salven.

## Command-visualisation efter v00.00.09

Efter v00.00.09 runtime-gaten fortsætter command-UI-retningen med fysiske HQ-entities, command-links, courier/order lifecycle, fog-of-war reports og semantic zoom. Valg af et HQ viser relationer til direkte underenheder. Ved udzoomning skifter enheder via semantic zoom fra 3D-formationer til forenklet formation display og derefter NATO/APP-6-lignende taktiske symboler uden at ændre simulation state.

Ordrer transporteres senere gennem et egentligt courier/order-lifecycle-system. En aktiv ordre kan vises som en stiplet route fra afsender-HQ til modtager med en bevægelig courier-markør, hvis position svarer til faktisk simulation progress. Command relationship lines og konkrete order routes er to separate overlays, og de vises primært ved valgt HQ/enhed eller aktiv Command Overlay for at undgå visuelt rod.

Courier-interception håndteres primært som **område-/risikomodel**, ikke som manuel jagt på en enkelt lille rytter. Enemy presence, cavalry/scouts, screening, roads, terrain, mørke og command quality kan føre til reroute, delay, searching eller i sjældnere tilfælde lost/intercepted. Fjendens couriers er selv underlagt fog of war, så courier-markører ikke bliver en skjult radar til enemy HQ.

## Fire eligibility og højere formationer efter v00.00.09

Combat resolution skal senere beregne **hvor stor en del af formationens frontage der faktisk kan skyde på målet**. En fjende inden for range betyder derfor ikke automatisk, at hele regimentet deltager. Formationens frontage opdeles i et begrænset antal fire groups/segmenter, som testes mod fire arc, LOS, range, friendly obstruction, terrain/smoke og target exposure. Resultatet bliver en `eligibleFiringFraction` mellem 0 og 1, der indgår i salveberegningen sammen med den kontinuerlige accuracy-by-range-kurve.

Højere HQ'er skal samtidig kunne opstille deres underenheder efter data-drevne formation templates, fx **4 abreast**, **3 + 1 reserve**, **2 + 2**, echelon, march column og kombinationer med artilleri i centre/wing/rear-high-ground slots. Reserve er en faktisk rolle/state, og Officer AI skal senere kunne vælge og tilpasse template ud fra mission, terræn, frontage, artilleri, flanker, reservebehov og officerens stats. Templates giver målpositioner; terrain fitting må justere dem til brugbart terræn uden at bryde enhedsidentitet eller command relation.

## Battle supply, nat og flerdagsslag

Forsyning under et taktisk slag skal være fysisk og begrænset. Enheder forbruger konkret ammunition og kan kun genforsynes fra kompatible wagons/caissons/field trains/depots med reel beholdning, transportkapacitet og en brugbar rute. Resupply under aktiv kamp er muligt, men kan være langsomt, delvist eller blokeret af enemy fire, terrain, manglende wagons/horses eller afskårne forbindelser.

Skumring skal være en **phase transition**, ikke et universelt hard battle stop. Battle state kan fortsætte som `DAYLIGHT -> DUSK -> NIGHT -> DAWN`. Normal organiseret formation combat reduceres kraftigt i mørke, mens hold, withdrawal, reorganisation, casualty collection, fieldworks, patrols, courier traffic og resupply fortsat kan ske. Begrænset night fighting er muligt, men skal være risikabelt og afhænge af mission/officer/doctrine.

Natten bliver et naturligt resupply-vindue. En formation får dog kun overnight resupply, hvis den stadig har eller kan genetablere en fysisk supply route tilbage til egne lines/field train, og der faktisk findes stock og transportkapacitet. **Afskårne enheder får ingen automatisk ammunition om natten.** Flerdagsslag fortsætter med persistent casualties, ammo, fatigue, positions, fieldworks, officer/equipment state og supply connectivity.

Sunrise/sunset/dusk skal senere beregnes ud fra dato, geografisk position og relevant weather/light state. UI kan auto-pause ved skumring/daggry og give command-valg som `Hold positions`, `Withdraw`, `Continue night operations`, `Reorganize/resupply` eller `Prepare dawn attack`.

## Kavaleri og dragoner

Kavaleri skal være langt mere end en charge-knap. Det får roller inden for **reconnaissance, screening/counter-recon, flank security, courier support/escort, pursuit, raids mod supply/courier/telegraph routes og exploitation**.

Dragoner kan bevæge sig mounted og sidde af til sustained fire/combat. Ved dismount efterlades horses og horse holders som en faktisk tactical state; remount tager tid og kan reduceres eller mislykkes, hvis heste/holdere er tabt eller spredt. Mounted mobility påvirkes af horse fatigue, casualties og terrain.

Charge-resultater skal afhænge af target formation/state, facing, terrain, surprise, cavalry cohesion/fatigue/momentum og officer quality. Et frontalt charge mod steady formed infantry med god fire discipline skal være meget risikabelt, mens cavalry kan være særdeles effektivt mod routed/disordered infantry, exposed skirmishers, retreating artillery/transport og isolerede rear-area targets.

Cavalry raids kobles direkte til supply- og command-systemet: en cavalry formation kan true eller skære en supply/courier route uden at erobre hele regionen. Det kan dermed forhindre overnight resupply og øge command delay.

## Fog of war, scouts og command effectiveness

Fog of war er en **knowledge-state model**, ikke blot skjult grafik. En fjendtlig formation kan være `Unknown`, `Suspected`, `Contact`, `Identified`, `Fresh observation` eller `Stale`. Når kontakt mistes, kan last known position blive stående med faldende confidence i stedet for perfekt live-tracking.

Reconnaissance kommer fra faktiske kilder som cavalry patrols, dragoner, skirmishers/scout detachments, line units, HQ, observation points og senere civilians/telegraph/naval reports. Spotting og identification påvirkes bl.a. af afstand, terrain, vegetation, elevation, daylight/night, weather, smoke, target size/movement, firing signature, scout quality og enemy screening.

Information skal **rapporteres gennem command-nettet**. En scout kan derfor se en fjende før Division HQ ved det. Reports kan forsinkes eller gå tabt efter samme grundprincipper som couriers/orders. En lokal Officer AI kan reagere på frisk lokal information, mens overordnet HQ stadig arbejder med ældre knowledge state.

HQ får et visuelt **command effectiveness envelope**, men ikke en hård magisk radius. Første niveauer er `Command Core -> Supported -> Extended -> Detached -> Isolated`. Command effectiveness falder gradvist med afstand, terrain og communication quality og følger den hierarkiske chain `Division HQ -> Brigade HQ -> Regiment/Battalion`.

Dårlig command connectivity påvirker primært order delay, acknowledgement, reporting, coordination, reserve/support reaction og hvor meget formationen må stole på lokal Initiative/Tactical Skill/Composure. Den giver **ikke** en vilkårlig direkte accuracy- eller damage-penalty. Roads og gode courier routes kan senere udvide den effektive command reach, mens woods, rivers uden crossings, svært terræn og enemy interdiction kan skabe svage sektorer i envelope-visningen.

Screens/counter-recon bliver en rigtig mission. Cavalry og skirmishers kan beskytte HQ/courier/supply approaches, opdage enemy scouts tidligere og reducere modstanderens observation confidence uden nødvendigvis at skulle destruere hver enkelt scout fysisk.

## Versionshistorik

- **v00.02.17 / Campaign3 v00.00.10n4** — hotfix efter Play Mode-feedback. Legacy INFO-panelet og debugpanelerne starter lukkede gennem en one-time n4 PlayerPrefs-migration. Zoneinfo-selection bruger nu de faktiske source-polygoner og `CampaignZoneOverlayMetadataV010N.ZoneId` med PointInPolygon i stedet for nearest-centre, så klik følger det synlige amt. Zone-line polish renderer kun segmenter som ejes af mindst to forskellige ZoneIds; single-owner kystkanter, clipping-fragmenter og lange one-sided sporer skjules.
- **v00.02.16 / Campaign3 v00.00.10n3** — zone-line polish og compact zone info. `CampaignZoneLinePolishV010N3` snapper/deduplikerer det synlige segmentlag, gør linjerne tyndere/opaque og undlader kyst-envelope-segmenter fra standard gul overlay, så kysten læses direkte fra geografi/World Imagery. `CampaignZoneInfoPanelV010N3` viser et lille kontekstuelt zonekort ved klik på zone, by eller dansk landområde med ZoneId, city count, A/B/C, 1850-købstadsbefolkning, byliste, land/ferry-links og valgt by. Geometrien er fortsat prototype og ikke historisk 1851-facit.
- **v00.02.15 / Campaign3 v00.00.10n2** — HUD-cleanup og zonegeometri-cleanup. Persistent `CampaignHudStateV010N2` indfører F1/F2/F3/F4 og INFO/DBG toggles; tekniske debugpaneler er skjult som standard og overlapper ikke længere gameplay-panelerne. Campaign-topbar og buildbadge bruger samme `CampaignBuildInfo.CurrentVersion`. Zoneoverlayet går fra fem rektangulære gruppe-sektorer til `PROTOTYPE_LAND_CLIPPED_GLOBAL_VORONOI`: én global 20-zone nearest-centre partition klippes mod CampaignDenmarkGeography-landmasken, så hav-overløb reduceres og area-overlap elimineres ved konstruktion. Natural Earth 1:50m er fortsat kun en forenklet coastline scaffold; detaljer som Limfjorden kræver senere højere-detalje geografi/GIS. RMB på bymarkør resolves desuden til byens ZoneId for marchordrer.
- **v00.02.14 / National AI Design** — strategisk AI udvidet med et eksplicit `NationalAIDirector`-lag for alle AI-styrede lande. Landene bruger samme økonomi, manpower, construction, production, recruitment, training, equipment og logistics som spilleren. Datadrevne `NationAIProfile`-profiler styrer nationale forskelle i rekrutteringssystem, force mix, doktrin, træning, modernisering, industri, infrastruktur, finansiel risikotolerance og mobilisering. AI skal selv beslutte hvad landet bygger, hvilken hær det træner, og hvilket udstyr der produceres/indkøbes og fordeles. Historiske forhold er priors, ikke faste scripts; normal difficulty må ikke give gratis ressourcer, perfekt intelligence eller skjulte combat-bonusser.
- **v00.02.13 / Seasonal Environment Design** — fælles `SeasonState` besluttet for Campaign + Battle. Campaign-datoen er autoritativ; BattleContext arver sæson, SeasonProgress, SnowCoverage, GroundWetness, vegetation og FieldState. Fire basisårstider fastlægges med særskilt snegrad, så vinter ikke automatisk betyder fuld sne. Sommer skal kunne vise både grønne og gule/modne kornmarker; efterår høstede marker og løvfald; forår tidlig grøn vækst og vådere jord. Første foreslåede runtime-slice er Campaign3 v00.00.10o Seasonal Visual Foundation. Gameplay-effekter kommer senere gennem movement, visibility, fatigue, traction og supply frem for direkte årstidsbonusser.
- **v00.02.12 / Campaign3 v00.00.10n** — 20 ZONE-REG-01-zoner får zone-overlay/polygonarkitektur og `Z` toggle. Første geometri er eksplicit en centerbaseret prototype med `HistoricallyExactGeometry=false`, ikke historisk facit. Produktionsmålet er DigDag Amt og Region, hvor faktiske 1851-polygongrænser senere skal mappes til eksisterende ZoneIds uden at ændre city/save/simulation state. Hele v10m-kontrakten med 20 zoner, 68 byer, 290.565 checksum, A/B/C-regler, Land/Ferry-links og Esbjerg ude af startstate bevares.
- **v00.02.11 / Campaign3 v00.00.10m** — CITY-REG-01 og ZONE-REG-01 implementeret i runtime. De 12 grove QA-zoner og 10 QA-byer er erstattet af 20 historiske 1851-zoner og 68 købstæder. Startup validerer 20/68/290.565, Esbjerg er fjernet fra startstate, alle byer får CityId/ZoneId/Population1850/tier/WGS84, B/C-byer blokeres gennem runtime-gaten for normal tung militær nybygning, city selection viser bydata, og zone-routing skelner mellem land- og færgelinks. QA-hæren bruger nu DK-Z11-VEJ. World Imagery/3D-terrain marker-kontrakten bevares.
- **v00.02.10 / ZONE-REG-01** — territorialt 1851-zone-lag fastlagt for Kongeriget Danmark. Alle 68 CITY-REG-01-købstæder har nu bindende `ZoneId` og historisk zone-navn. Første baseline består af 20 zoner, primært samtidens amter, med København som særskilt hovedstadszone og Skanderborg Amt bevaret som separat 1851-zone. Zoner bliver regionalt simulationslag for landbefolkning, rekruttering, skat, produktion, supply, infrastruktur, owner/controller og occupation state. City-tier A/B/C forbliver separat og B/C-byer får fortsat ikke fri tung militær udbygning.
- **v00.02.09 / CITY-REG-01** — Kongeriget Danmarks 68 købstæder ved 1851-starten fastlagt med folketal fra 1. februar 1850. A/B/C-udviklingsklasser indført. Alle mindre købstæder findes på kortet, men B/C-byer får ikke fri tung militær udbygning; historisk dokumenterede anlæg kan eksistere som fixed/special buildings. Esbjerg er ikke en 1851-startby. Slesvig/Holsten/Lauenborg og strategiske ikke-købstads-settlements får separate registre.
- **v00.02.08 / P0A v00.00.09 TACTICAL COMMAND TEST work branch** — Shared `OfficerAIController`/`OfficerProfile` på alle fire regimenter; `I` toggler player delegation; Defensive/Balanced/Offensive doctrine og 0–100 commander aggression intent; preussiske Officer AI-angribere med preferred-range/manoeuvre/stabilise adfærd; directional 120° infantry fire fan; HOLD/CLOSE/MEDIUM/LONG fire discipline; continuous closer-is-easier accuracy; battlefield 360x240; Pause/x0,5/x1/x2/x5/x20 og battle clock. v00.00.08 reload/0-hit/`Ramte N`/casualty-visual skal fortsat regressionsbestå. De senere besluttede systemer omfatter segmenteret fire eligibility, fysiske HQ-entities, semantic zoom, courier/order progress/interception, højere formation templates, fysisk battle resupply, night/overnight logistics, udvidet cavalry/dragoon model og fog of war/scouts/gradvis HQ command effectiveness. v00.00.09 er TEST og må først promoveres efter Unity compile/Play acceptance.
- **v00.02.08 / P0A v00.00.08** — Våbenprofil styrer basis-reload, regimentets experience modificerer reload-tiden bounded, positive salver viser `Ramte N`, salver kan give 0 direkte hits, og første personeltab pr. regiment skaber én repræsentativ liggende casualty-figur. Designbaselinen fastlægger desuden konkret ammunition/casualty split, skirmishers, artilleriklasser, hestetrukket/manhandled artilleri, manuel artillerimåludpegning, supply-vogne, salvage, dragoner, directional cover, prone, hasty fieldworks, strategisk landudvikling samt officer/delegation/difficulty-retningen.
- **v00.02.07** — P0A v00.00.07: statisk Unity 6.6 QA-hardening før runtime-validering. Battle-end er terminalt pauset indtil restart, RTS-kamera timeScale-uafhængigt og defensive guards forbedret.
- **v00.02.06** — Unity compile-gate: `CS0136` rettet, obsolete object lookup erstattet og unused state fjernet.
- **v00.02.05** — Built-in IMGUI, Particle System, Physics og Audio moduler aktiveret.
- **v00.02.04** — Unity baseline flyttet til 6000.6.0f1.
- **v00.02.03** — Repository-roden fastlåst som Unity project root.
- **v00.02.02** — Unity Editor metadata/version rettet.
- **v00.02.01** — Første konkrete P0A implementation koblet til roadmap.
- **v00.02.00** — Expanded systems baseline: økonomi, udvikling, handel, forskning, rekruttering, træning, sanitet/fanger, regimentshistorik, faner og traits.
- **v00.01.00** — Første samlede designbaseline.

## Projektregel

Designmanualen skal opdateres både som layoutet Word-master og her i GitHub, når designbeslutninger eller implementeringsbaselines ændres. Git-historikken bevarer tidligere udgaver af Markdown-delene. Nye beslutninger og idéer registreres desuden i backloggen. Hver testbuild skal have tydelig release-dokumentation, der adskiller **implementeret nu** fra **besluttet senere**.