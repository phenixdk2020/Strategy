# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles nu i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09k`.
- Fokus: battlefield controls, formations, Officer AI, combat, melee, navigation, obstacles, river/bridge, UI, tactical visuals, full-scale OOB og tactical QA.
- `v00.00.09f` beholder **Navigation V3** som autoritativ movement/steering-owner. V4 og 09A er runtime-deaktiverede.
- `09B/09h4` fungerer som compatibility/soft-terrain policy: decorative trees og individuelle fence posts er pass-through; Farmhouse/Barn er hard obstacles; river/water er bridge-only.
- `v00.00.09g` leverer scenic battlefield og extended camera zoom.
- `v00.00.09h` leverer Soldier Visual Pass 1, Uniform Designer, RTS box selection, compact combat feedback og soft black-powder smoke.
- `v00.00.09h2` stabiliserer AI attack frontage; `09h3` march/deployment policy; `09h4` manual AI-OFF route recovery.
- `v00.00.09i`–`09i3` leverer Big Graphics Buff, procedural standards, medium-zoom readability, Farmhouse detour continuation og Unity 6.6 compatibility cleanup.
- `v00.00.09j` leverer fire/reload visual state baseret på faktisk udgående volley og weapon-specific reload cadence.
- `v00.00.09k` er **Full-Scale OOB + 1:1 Render + Mounted HQ + Special Arms Pilot**.
- 09k går tilbage til de oprindelige fire infantry-regimenter og sætter QA full-scale manpower til ca. 1.6k pr. dansk regiment og ca. 2.45k pr. preussisk regiment, i alt 8.107 infantry.
- 09k tilføjer intern Battalion/Company OOB: DK 2 bataljoner/8 kompagnier, PR 3 bataljoner/12 kompagnier.
- 09k renderer én synlig infantryman pr. current manpower via instancing, ikke én GameObject/MonoBehaviour/NavMeshAgent/collider pr. mand.
- Existing 09h/09i representative soldier layers deaktiveres for at undgå dobbelte formationer; 1:1 rifle-pose afspejler stadig faktisk reload state.
- Hvert regiment får mounted HQ på 3 ryttere: commander, adjutant, orderly. De tre erstatter tre stabsslots visuelt.
- 09k etablerer `DefendArea` og `AttackCaptureArea` som eksplicit officer mission-data plus requested reserve fraction. Fuldt battalion/company allocation-system kommer i næste command-gate, hvor underenheder fordeles mellem ENGAGED/SUPPORT/RESERVE.
- 09k tilføjer én QA dragoon squadron pr. side (160 mounted) samt ét QA field battery pr. side (6 guns + ca. 120 crew). De er egne special-arm pilots og ikke infantry reskins.
- Designmanual supplement: `docs/parts/part-09k-fullscale-oob-command-arms.md`.
- Unity Input Manager deprecation er non-blocking. Migration til det nye Input System sker som separat control-platform revision, fordi LMB/RMB, box-select, keyboard hotkeys og UI pointer guards skal migreres samlet.
- Primær 09k QA: compile uden røde errors; kun fire infantry-regimenter; OOB-09K korrekt; SCALE-09K VisualRatio=1:1; mounted HQ synlige; dragoner og seks-kanoners batterier synlige; frame rate observeres ved close/medium/full map; eksisterende infantry movement/combat/box select må ikke regressere.

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
