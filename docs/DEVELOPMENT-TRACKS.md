# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09l3`.
- Fokus: battlefield controls, formations, Officer AI, combat, navigation, obstacles, river/bridge, UI, tactical visuals, full-scale OOB, brigade command, company tactical control og tactical QA.
- `v00.00.09f` beholder **Navigation V3** som autoritativ whole-regiment movement/steering-owner. V4 og 09A er runtime-deaktiverede.
- `09B/09h4` fungerer som compatibility/soft-terrain policy: decorative trees og individuelle fence posts er pass-through; Farmhouse/Barn er hard obstacles; river/water er bridge-only.
- `v00.00.09g` leverer scenic battlefield og extended camera zoom.
- `v00.00.09h` leverer Soldier Visual Pass 1, Uniform Designer, RTS box selection, compact combat feedback og soft black-powder smoke.
- `v00.00.09h2` stabiliserer AI attack frontage; `09h3` march/deployment policy; `09h4` manual AI-OFF route recovery.
- `v00.00.09i`–`09i3` leverer Big Graphics Buff, procedural standards, medium-zoom readability, Farmhouse detour continuation og Unity 6.6 compatibility cleanup.
- `v00.00.09j` leverer fire/reload visual state baseret på faktisk udgående volley og weapon-specific reload cadence.
- `v00.00.09k` etablerer **Full-Scale OOB + 1:1 Render + Mounted Regimental HQ + Special Arms Pilot**.
- `v00.00.09l/09l1` etablerer **Brigade HQ + Historical Danish OOB + Reserve AI + Dual Standards** og Unity 6.6 compile compatibility.
- `v00.00.09l2` etablerer **Company Tactical Control + Full-Scale Formation Geometry**.
- `v00.00.09l3` er en midlertidig **Reduced QA Battle** for at stabilisere company-control før fuld brigade-scale genaktiveres.

### v00.00.09l3 reduced QA gate

- Kun to infanteriregimenter er aktive i slaget:
  - Danmark: `1. Infanteri-Regiment`, ca. 1.540 mand, 8 kompagnier.
  - Preussen: `8th Regiment (QA)`, ca. 2.460 mand, 12 kompagnier.
- Aktiv infantry total er dermed ca. **4.000** i stedet for ca. 8.000.
- Aktiv company count er **20** i stedet for 40.
- `11. Infanteri-Regiment` og `18th Regiment` forbliver oprettet i OOB-datamodellen, men fjernes fra aktiv `BattleManager`-roster og skjules i 09l3 QA-scenariet.
- Brigade reserve/commit AI pauses i denne gate, fordi den kræver to aktive regimenter under brigaden og ellers blander endnu en variabel ind i company-control QA.
- Scale-down er kun et testscenarie; det ændrer ikke slutmålet om fuld brigade/division OOB eller campaign-skalering.
- Regimental HQ, dual standards og de eksisterende cavalry/artillery pilot-assets forbliver tilgængelige.
- 09l2 company renderer forbliver authoritative for ordinary infantry; 09k regiment-wide foot renderer deaktiveres efter mounted Regimental HQ er oprettet.
- Primær 09l3 QA: compile uden røde errors; `QA-SCALE-09L3` telemetry; 2 aktive regimenter; 20 aktive companies; company click/box select; RMB movement; F/C/H/Z/X; double-click tilbage til regiment; ren formation geometry og bedre overskuelig performance.
- Autoritativ supplement: `docs/parts/part-09l3-reduced-qa-battle.md`.

### Retained company-control baseline from 09l2

- Company er den mindste direkte kontrollerbare infanteriformation.
- Ordinary 1:1 soldiers forbliver GPU-instanced; der oprettes ikke GameObject/MonoBehaviour/collider/NavMeshAgent/AI pr. soldat.
- Dansk company-control: enkeltklik vælger company; Shift add; Ctrl toggle; LMB drag box-select; RMB move; Alt+RMB append waypoint; RMB drag final facing; F/C Line/Column; H Hold; Z/X facing.
- Double-click på company skifter tilbage til whole-regiment selection, hvor eksisterende PlayerCommander/V3-kæde fortsat bruges.
- Company manual movement rydder parent regimentets aktive PlayerCommander-route, slår aktiv parent OfficerAIController OFF og sætter parent Regiment Hold for at undgå competing movement writes.
- 09l2 bruger company-anchored 1:1 rendering. Company Line bruger 3 ranks; Column bruger 8 files. Companies danner separate blocks under Battalion.
- Casualty changes på Regiment.CurrentStrength synkroniseres til company CurrentStrength; rendering fordeler visuelle gaps gennem company-blokken.
- Company-level ammo, LOS, fire eligibility og casualty targeting er stadig en senere gate.

### Historical/OOB baseline retained

- Historisk dansk working OOB for fuld brigade-test forbliver **7. Brigade: 1. + 11. Infanteri-Regiment**.
- Prussian 8th+18th pair er fortsat QA indtil date-specific brigade OOB er kildevalideret.
- Regimental HQ = 3 ryttere; Brigade HQ = 6 ryttere.
- Dual standards = national/hærfane + regimentsfane/traditionsidentitet.
- Dansk field battery = 8 guns / ca. 190 personnel.
- Cavalry manpower / serviceable horses / mounted effective er separate værdier.
- `docs/parts/part-09m-oob-designer-campaign.md` definerer OOB Designer-gaten: organisatorisk drag/drop i OOB-træet og movement orders ved drag/drop på campaign-kortet.
- Unity Input Manager deprecation er non-blocking. Migration til det nye Input System sker som separat control-platform revision.

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
