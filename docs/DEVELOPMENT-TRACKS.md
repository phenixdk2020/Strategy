# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles nu i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09i2`.
- Fokus: battlefield controls, formations, Officer AI, combat, melee, navigation, obstacles, river/bridge, UI, tactical visuals og tactical QA.
- `v00.00.09f` beholder **Navigation V3** som autoritativ movement/steering-owner. V4 og 09A er runtime-deaktiverede.
- `09B/09h4` fungerer som compatibility/soft-terrain policy: decorative trees og individuelle fence posts er pass-through; Farmhouse/Barn er hard obstacles; river/water er bridge-only.
- `v00.00.09g` leverer scenic battlefield og extended camera zoom.
- `v00.00.09h` leverer Soldier Visual Pass 1, Uniform Designer, RTS box selection, compact combat feedback og soft black-powder smoke.
- `v00.00.09h2` stabiliserer AI attack frontage; dette gælder AI-angreb, ikke manual AI-OFF movement.
- `v00.00.09h3` giver AI-regimenter march Column og deployment til Line nær kontakt.
- `v00.00.09h4` retter manual PlayerCommander movement: Column på march, soft fence posts og bounded no-progress bypass recovery.
- `v00.00.09i` er **Soldier Visual Pass 2 / Big Graphics Buff**: articulated procedural infantry, tydeligere faction headgear, rifle/equipment, officer/standard-bearer og animated procedural regimental standards.
- `v00.00.09i1` retter den QA-bekræftede V3 Farmhouse/Barn detour-repeat. Hvis første Via er nået mens samme hard obstacle stadig spærrer, fortsætter V3-state via et nyt valideret continuation-point i stedet for at gentage samme Via.
- `v00.00.09i2` øger grafisk læsbarhed ved normal tactical zoom: soldier details beholdes til ca. 285 m, render-rigs får bounded distance-based readability scale uden at ændre formation slots/colliders, og 09i standarder skaleres tydeligere ved medium/long zoom.
- 09i2 runtime telemetry skal vise `SOLDIER-09I2`, `STANDARD-09I` og `VIS-09I2`. Manglende 09i standard giver eksplicit warning i stedet for silent fallback.
- Designmanual supplement for detour continuation: `docs/parts/part-09i1-v3-detour-continuation.md`.
- Designmanual supplement for 09i visual baseline: `docs/parts/part-09i-soldier-visual-pass-2.md`.
- **Næste større tactical unit-types efter visual/stability-gaten:** dragoner/kavaleri og feltartilleri som egne unit-types/states, ikke infantry reskins.
- Primær 09i2 QA: compile uden røde errors; Big Graphics Buff skal være tydelig ved normal tactical zoom; procedural standards skal være læsbare; samme AI-OFF manual group move må ikke parkere 1. Regiment ved Farmhouse; identisk Farmhouse/Via må ikke spamme.

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
