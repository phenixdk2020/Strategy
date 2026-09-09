# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles nu i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09j`.
- Fokus: battlefield controls, formations, Officer AI, combat, melee, navigation, obstacles, river/bridge, UI, tactical visuals og tactical QA.
- `v00.00.09f` beholder **Navigation V3** som autoritativ movement/steering-owner. V4 og 09A er runtime-deaktiverede.
- `09B/09h4` fungerer som compatibility/soft-terrain policy: decorative trees og individuelle fence posts er pass-through; Farmhouse/Barn er hard obstacles; river/water er bridge-only.
- `v00.00.09g` leverer scenic battlefield og extended camera zoom.
- `v00.00.09h` leverer Soldier Visual Pass 1, Uniform Designer, RTS box selection, compact combat feedback og soft black-powder smoke.
- `v00.00.09h2` stabiliserer AI attack frontage; dette gælder AI-angreb, ikke manual AI-OFF movement.
- `v00.00.09h3` giver AI-regimenter march Column og deployment til Line nær kontakt.
- `v00.00.09h4` retter manual PlayerCommander movement: Column på march, soft fence posts og bounded no-progress bypass recovery.
- `v00.00.09i` er **Soldier Visual Pass 2 / Big Graphics Buff**: articulated procedural infantry, tydeligere faction headgear, rifle/equipment, officer/standard-bearer og animated procedural regimental standards.
- `v00.00.09i1` retter den QA-bekræftede V3 Farmhouse/Barn detour-repeat med et valideret continuation-point efter første detour-leg.
- `v00.00.09i2` øger grafisk læsbarhed ved normal tactical zoom: soldier details beholdes til ca. 285 m, render-rigs får bounded distance-based readability scale, og 09i standards bliver tydeligere ved medium/long zoom.
- `v00.00.09i3` retter 09i compile compatibility og migrerer de fire viste navigation/reference scripts væk fra deprecated Unity 6.6 `FindObjectsSortMode` overloads.
- `v00.00.09j` er **Fire & Reload Animation Pass**. En faktisk udgående volley registreres via den eksisterende `Regiment.nextFireTime` cadence i stedet for det fejlagtige `HasHitFeedback` visual-signal.
- 09j visual state: actual volley -> fire pose -> weapon-specific reload -> ready. Muzzle-loader får længere load/ram sekvens; Dreyse får kortere bolt/cartridge sekvens. Animationen følger den eksisterende faktiske reload-window og skriver ikke combat state.
- 09j overtager kun riflemen arms/rifle posing efter 09i; 09i beholder legs/body/LOD/uniform/equipment. Standard-bearer uden aktiv rifle reload-animeres ikke.
- Designmanual supplement for 09i visual baseline: `docs/parts/part-09i-soldier-visual-pass-2.md`.
- Designmanual supplement for 09i1 detour continuation: `docs/parts/part-09i1-v3-detour-continuation.md`.
- Designmanual supplement for 09j fire/reload: `docs/parts/part-09j-fire-reload-animation.md`.
- QA-video `Optagelse 3(1).mp4`: movement er markant bedre og den gamle totale Farmhouse-stall reproduceres ikke. Der ses stadig lejlighedsvis skarp turn/reforming omkring bridge-routing og tæt multi-regiment contact, især med AI ON; dette forbliver separat movement/AI QA og blandes ikke ind i reload-laget.
- Unity Input Manager deprecation er non-blocking. Migration til det nye Input System sker som separat control-platform revision, fordi LMB/RMB, box-select, keyboard hotkeys og UI pointer guards skal migreres samlet.
- **Næste større tactical unit-types efter visual/stability-gaten:** dragoner/kavaleri og feltartilleri som egne unit-types/states, ikke infantry reskins.
- Primær 09j QA: compile uden røde errors; `RELOAD-09J` telemetry; shooter og ikke target skal trigge fire pose; dansk muzzle-loader og preussisk Dreyse skal have tydeligt forskellige reload-sekvenser; samme movement test skal fortsat være stabil.

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
