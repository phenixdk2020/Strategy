# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles nu i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09g`.
- Fokus: battlefield controls, formations, Officer AI, combat, melee, navigation, obstacles, river/bridge, UI, tactical visuals og tactical QA.
- `v00.00.09f` beholder **Navigation V3** som eneste autoritative movement/steering-lag. V4 og 09A er runtime-deaktiverede.
- `09B` er igen aktiv, men kun som en ikke-steerende kompatibilitetsregel der fjerner individuelle decorative trees fra navigation og nulstiller stale V3 tree-detour state.
- Tactical obstacle-baseline: decorative trees er pass-through; bygninger og fence posts er diskrete forhindringer; river/water er fortsat bridge-only.
- `v00.00.09f` korrigerer 09e-UI: den permanente `VALGT xN` selected-summary-boks skjules helt. Hover-info forbliver kort/transient og bottom command bar bevares.
- Range fans vises fortsat kun på valgte enheder, men 09f undgår dobbelt alpha-multiplikation, så ghost-conen igen er tydeligt synlig uden at blive opak.
- `v00.00.09g` er et **separat visual/map pass** oven på 09f: scenic battlefield footprint udvides til ca. 720x480 med markmosaik, sekundære veje, levende hegn, skovklynger, landsbygrupper, høstakke og sten. Visual layer skriver ikke movement/combat/AI-state.
- 09g-kameraet kan zoome fra ca. 300 m oversigt til ca. 3,8 m over lokal terrænhøjde og panorere over det større scenic område. Shift giver hurtig pan, Ctrl giver præcisions-pan.
- 09g opgraderer de eksisterende regimentsstandards med tydeligere silhuet, cloth fold, cords og større close-up readability. De er fortsat QA-placeholder art indtil historisk flagresearch er valideret.
- **Næste større tactical unit-types efter visual/stability-gaten:** dragoner/kavaleri og feltartilleri. De implementeres som egne unit-typer/states, ikke som infantry reskins. Dragoner kræver mounted/dismounted + horse-holder/remount state; artilleri kræver battery/piece count, crew, horses/limber, deploy/unlimber, ammunition og egne fire/LOS-regler.
- Den fulde designretning for artilleri ligger allerede i designmanual §16.1, og cavalry/dragoon-modellen i §16.2 samt Del 3D §19.5–19.8.
- Primær 09g QA: visual layer skal installere uden compile/runtime-fejl; close zoom må ikke gå under terræn; flags skal være tydeligt læsbare tæt på; 09f movement authority må være uændret; scenic layer må ikke skrive Regiment movement state.

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
