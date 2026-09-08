# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles nu i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09h`.
- Fokus: battlefield controls, formations, Officer AI, combat, melee, navigation, obstacles, river/bridge, UI, tactical visuals og tactical QA.
- `v00.00.09f` beholder **Navigation V3** som eneste autoritative movement/steering-lag. V4 og 09A er runtime-deaktiverede.
- `09B` er aktiv kun som en ikke-steerende kompatibilitetsregel der fjerner individuelle decorative trees fra navigation og nulstiller stale V3 tree-detour state.
- Tactical obstacle-baseline: decorative trees er pass-through; bygninger og fence posts er diskrete forhindringer; river/water er fortsat bridge-only.
- Range fans vises kun på valgte enheder og bruger 09f ghost-alpha-reglen.
- `v00.00.09g` er det separate visual/map pass: scenic battlefield footprint ca. 720x480, markmosaik, sekundære veje, levende hegn, skovklynger, landsbygrupper, høstakke og sten. Visual layer skriver ikke movement/combat/AI-state.
- 09g-kameraet zoomer fra ca. 300 m oversigt til ca. 3,8 m over lokal terrænhøjde. Shift giver hurtig pan, Ctrl giver præcisions-pan.
- 09g-regimentsstandards bevares med tydeligere silhuet, fold, cords og close-up readability; de er fortsat QA-placeholder art indtil historisk flagresearch er valideret.
- `v00.00.09h` er **Soldier Visual Pass 1 + Uniform Designer + RTS Box Selection**.
- 09h forbedrer de repræsentative infantry visuals med separate ben/bukser, hoved, remme, pack/equipment, bayonet og tydeligere dansk/preussisk headgear-silhuet samt officer-/standard-bearer detaljer.
- 09h uniformprofilen er pr. regiment og kan styre coat, trousers, headgear, trim, straps, equipment, flag primary/secondary og ribbon. Farverne er presentation-only.
- F10 åbner Uniform Designer for præcis én valgt enhed; RGB preview er live, og profiler kan gemmes/indlæses lokalt samt resettes til regiment- eller faction-default.
- 09h box selection: LMB drag viser marquee; normal drag erstatter selection, Shift tilføjer, Ctrl toggler. Kun danske regiment-centre kan vælges.
- 09h fjerner persistent `VALGT`/`VALGT xN` direkte i BattleManager. `Ramte N` er kompakt text-only feedback.
- 09h video-QA rettelse: de store hvide firkantede black-powder particle quads erstattes af soft-alpha runtime smoke sprite/fade.
- Optagelse `optagelse 1(2).mp4` viser ikke den tidligere multi-regiment tree-stall; enhederne når engagement. Derfor ændrer 09h ikke V3 movement authority eller combat logic.
- **Næste større tactical unit-types efter visual/stability-gaten:** dragoner/kavaleri og feltartilleri. De implementeres som egne unit-typer/states, ikke som infantry reskins. Dragoner kræver mounted/dismounted + horse-holder/remount state; artilleri kræver battery/piece count, crew, horses/limber, deploy/unlimber, ammunition og egne fire/LOS-regler.
- Den fulde designretning for artilleri ligger i designmanual §16.1, cavalry/dragoon-modellen i §16.2 samt Del 3D §19.5–19.8.
- Primær 09h QA: compile uden fejl; V3 authority uændret; ingen VALGT-boks; soft smoke uden firkantede quads; close-up soldier pass synlig; F10 designer virker; box select virker med Replace/Add/Toggle; range cone forbliver synlig.

## Track B — Campaign

- Branch: `campaign`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Campaign`
- Aktuel revisionsserie starter ved `v00.00.10a`.
- Fokus: campaign scene/map, strategic formations, movement/time, battle trigger, tactical handoff og state return.
- Campaign-kode/scener skal så vidt muligt ligge separat fra tactical navigation/combat-filer for at minimere merge-konflikter.

## Stable

- Branch: `main`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy`
- Kun accepterede/promoverede gates.

## Integration rule

Campaign må ikke vente på mindre tactical polish. Kun tactical fixes der er nødvendige for campaign-state continuity eller som er accepteret gennem QA, integreres senere i campaign-sporet. Ingen destructive Git-operationer, force-push, reset --hard eller clean anvendes.
