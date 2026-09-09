# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles nu i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09l2`.
- Fokus: battlefield controls, formations, Officer AI, combat, melee, navigation, obstacles, river/bridge, UI, tactical visuals, full-scale OOB, brigade command, company tactical control og tactical QA.
- `v00.00.09f` beholder **Navigation V3** som autoritativ whole-regiment movement/steering-owner. V4 og 09A er runtime-deaktiverede.
- `09B/09h4` fungerer som compatibility/soft-terrain policy: decorative trees og individuelle fence posts er pass-through; Farmhouse/Barn er hard obstacles; river/water er bridge-only.
- `v00.00.09g` leverer scenic battlefield og extended camera zoom.
- `v00.00.09h` leverer Soldier Visual Pass 1, Uniform Designer, RTS box selection, compact combat feedback og soft black-powder smoke.
- `v00.00.09h2` stabiliserer AI attack frontage; `09h3` march/deployment policy; `09h4` manual AI-OFF route recovery.
- `v00.00.09i`–`09i3` leverer Big Graphics Buff, procedural standards, medium-zoom readability, Farmhouse detour continuation og Unity 6.6 compatibility cleanup.
- `v00.00.09j` leverer fire/reload visual state baseret på faktisk udgående volley og weapon-specific reload cadence.
- `v00.00.09k` etablerer **Full-Scale OOB + 1:1 Render + Mounted Regimental HQ + Special Arms Pilot**.
- `v00.00.09l/09l1` etablerer **Brigade HQ + Historical Danish OOB + Reserve AI + Dual Standards** og Unity 6.6 compile compatibility.
- `v00.00.09l2` er **Company Tactical Control + Full-Scale Formation Geometry**.
- 09l2 gør company til den mindste direkte kontrollerbare infanteriformation. Den aktuelle pilot har 40 company tactical centres: 16 danske + 24 preussiske.
- Ordinary 1:1 soldiers forbliver GPU-instanced; der oprettes ikke et GameObject/MonoBehaviour/collider/NavMeshAgent/AI pr. soldat.
- Dansk company-control: enkeltklik vælger company; Shift add; Ctrl toggle; LMB drag box-select; RMB move; Alt+RMB append waypoint; RMB drag final facing; F/C Line/Column; H Hold; Z/X facing.
- Double-click på et company skifter tilbage til whole-regiment selection, hvor den eksisterende PlayerCommander/V3-kæde fortsat bruges.
- Company manual movement rydder parent regimentets aktive PlayerCommander-route, slår aktiv parent OfficerAIController OFF og sætter parent Regiment Hold for at undgå competing movement writes.
- 09l2 erstatter den gamle regiment-wide foot rendering med company-anchored 1:1 rendering. Den gamle 09k foot renderer deaktiveres først efter de mounted Regimental HQ groups er oprettet, så HQ bevares uden duplicate infantry.
- Company Line bruger 3 ranks; Column bruger 8 files. Companies danner separate blocks under deres Battalion, og battalions danner depth rows omkring regimentets command pivot.
- Casualty changes på Regiment.CurrentStrength synkroniseres til company CurrentStrength. Rendering bruger deterministic slot shuffle, så losses vises som distribuerede huller i company blocks i stedet for at skære en formationende af.
- Company combat er endnu ikke fuldt selvstændigt i 09l2: Regiment.CurrentStrength/fire policy/volley resolver er stadig authoritative combat state. Company-level ammo, LOS, fire eligibility og casualty targeting er en senere gate.
- Danish side remains den historiske working pilot **7. Brigade: 1. + 11. Infanteri-Regiment**. Prussian 8th+18th pair er fortsat QA.
- Hvert regiment beholder Regimental HQ på 3 ryttere; hver brigade har Brigade HQ på 6 ryttere.
- Hvert regiment beholder dual standards: national/hærfane + regimentsfane/traditionsidentitet.
- Dansk field battery er fortsat 8 guns / ca. 190 personnel. Cavalry manpower/horses/mounted-effective forbliver separate.
- `docs/parts/part-09l2-company-tactical-control.md` er den autoritative 09l2-supplementsspecifikation.
- `docs/parts/part-09m-oob-designer-campaign.md` definerer næste OOB Designer-gate: organisatorisk drag/drop i træet og movement orders ved drag/drop på campaign-kortet.
- Unity Input Manager deprecation er non-blocking. Migration til det nye Input System sker som separat control-platform revision.
- Primær 09l2 QA: compile uden røde errors; 40 companies installeret; company click/box select; company RMB movement; double-click tilbage til regiment; whole-regiment V3 bevares; rene battalion/company blocks; casualty gaps distribueres; ingen duplicate old/new infantry renderer.

## Track B — Campaign

- Branch: `campaign`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Campaign`
- Aktuel revisionsserie starter ved `v00.00.10a`.
- Fokus: campaign scene/map, strategic formations, movement/time, battle trigger, tactical handoff og state return.
- OOB Designer-arbejdet i 09m skal dele stable Unit IDs og hierarchy-data med campaign-sporet, så tactical og campaign ikke bygger parallelle OOB-modeller.
- Campaign-kode/scener skal så vidt muligt ligge separat fra tactical navigation/combat-filer for at minimere merge-konflikter.

## Stable

- Branch: `main`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy`
- Kun accepterede/promoverede gates.

## Integration rule

Campaign må ikke vente på mindre tactical polish. Kun tactical fixes der er nødvendige for campaign-state continuity eller som er accepteret gennem QA, integreres senere i campaign-sporet. Ingen destructive Git-operationer, force-push, reset --hard eller clean anvendes.
