# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles nu i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09i`.
- Fokus: battlefield controls, formations, Officer AI, combat, melee, navigation, obstacles, river/bridge, UI, tactical visuals og tactical QA.
- `v00.00.09f` beholder **Navigation V3** som eneste autoritative movement/steering-lag. V4 og 09A er runtime-deaktiverede.
- `09B/09h4` fungerer som ikke-steerende compatibility/soft-terrain policy: decorative trees og individuelle fence posts er pass-through; bygninger/barns er hard obstacles; river/water er fortsat bridge-only.
- `v00.00.09g` er visual/map pass: scenic battlefield footprint ca. 720x480, markmosaik, sekundære veje, levende hegn, skovklynger, landsbygrupper, høstakke og sten. Kameraet går fra ca. 300 m overview til ca. 3,8 m over lokal terrænhøjde.
- `v00.00.09h` leverer Soldier Visual Pass 1, per-regiment Uniform Designer, RTS box selection, compact combat feedback og soft black-powder smoke.
- `v00.00.09h2` stabiliserer AI attack frontage med sticky slots og bounded no-progress replan. Dette gælder AI-angreb, ikke manual AI-OFF movement.
- `v00.00.09h3` giver AI-regimenter march Column og deployment til Line nær kontakt.
- `v00.00.09h4` retter den manuelle PlayerCommander movement-kæde: manual routes marcherer i Column, fence posts er soft/pass-through, og en bounded route-recovery kan indsætte lokalt bypass waypoint ved dokumenteret no-progress.
- `v00.00.09i` er **Soldier Visual Pass 2 / Big Graphics Buff** og ændrer ikke movement/combat/AI semantics.
- 09i skjuler legacy soldier-renderers og bygger nye lightweight procedural infantry rigs med tapered torso, coat skirts, separate arme/hænder/ben/støvler, hoved/hals, forbedret rifle/bayonet, backpack, straps, cartridge box og equipment-varianter.
- Danske og preussiske soldater får stærkere faction-specific headgear silhouettes; officer og standard-bearer får særskilte visual details.
- 09i tilføjer simpel cosmetic march/ready/fire posing uden Animator dependency. Posing må ikke trigge eller ændre simulation events.
- 09i genbruger 09h Uniform Designer som data-source-of-truth og live-synkroniserer soldier materials.
- 09i erstatter den synlige 09g QA-fane med et større procedural two-sided cloth standard med wind animation, pole/finial, cords, ribbons og fringe; flag/ribbon colours følger Uniform Designer live.
- 09i har første close-detail LOD: små equipment details kan skjules på lang kameraafstand, mens formationens grundsilhuet bevares.
- Designmanual supplement for 09i: `docs/parts/part-09i-soldier-visual-pass-2.md`.
- **Næste større tactical unit-types efter visual/stability-gaten:** dragoner/kavaleri og feltartilleri. De implementeres som egne unit-typer/states, ikke som infantry reskins.
- Primær 09i QA: compile uden røde errors; close zoom skal vise markant mere menneskelige infantry silhouettes; officer/standard-bearer skal være aflæselige; flag skal bølge og følge Uniform Designer; samme AI-OFF manual movement test fra 09h4 skal fortsat virke uændret.

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
