# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09l5`.
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
- `v00.00.09l3` reducerer QA-slaget til 1 dansk + 1 preussisk regiment for at stabilisere company-control.
- `v00.00.09l4` etablerer **Company Selection + Combat Authority + Company Guidons**.
- `v00.00.09l5` etablerer **Independent Company Movement + Group Order Geometry Fix**.

### v00.00.09l5 independent company movement gate

- Root cause fra 09l4-videoen: company tactical centres var stadig transform-children under `Regiment.transform`, så enhver parent-rotation eller movement mekanisk trak alle companies med som én rigid enhed.
- Active company tactical centres flyttes nu til en selvstændig world-space tactical root. `ParentRegiment` bevares som OOB-/command-relation i data, men er ikke længere fysisk transform-parent.
- Regimental HQ er dermed command-parent, ikke movement-body for underenhederne.
- Parent Officer AI movement slås midlertidigt fra i denne company-QA-gate, og Regiment-pivot holdes, så der ikke er konkurrerende movement-writers.
- Whole-regiment selection konverteres til selection af alle underlagte danske companies; en regimentordre er dermed en gruppe company-orders frem for movement af én gigantisk pivot.
- Multi-company RMB bevarer de valgte companies' relative offsets omkring selection-centroid. De bliver ikke længere sorteret og lagt ud som én ny sammenhængende lang linje.
- RMB-drag kan rotere den relative company-layout omkring gruppens centroid og sætte fælles final facing, men hvert company beholder eget tactical centre og egen destination.
- Single-company RMB movement ændres ikke.
- Autoritativ supplement: `docs/parts/part-09l5-independent-company-movement.md`.

### v00.00.09l4 company authority gate

- GPU-instanced ordinary soldiers kræver fortsat ingen individuelle colliders.
- Company short-click bruger et screen-space hit-test af den synlige company footprint, så man kan klikke direkte på formationens mænd.
- Mounted Regimental HQ er command entity og må ikke affyre regiment-wide volley fra HQ/pivot, mens companies er synlige tactical formations.
- Parent regiment fire policy tvinges derfor midlertidigt til HoldFire i company-QA-mode, og parent range cone skjules.
- Store historiske national-/regimentsfaner forbliver ved Regimental HQ.
- Hvert aktivt company har to mindre tactical guidons: national identitet + regiment/company-identitet.

### v00.00.09l3 reduced QA gate

- Kun to infanteriregimenter er aktive i slaget:
  - Danmark: `1. Infanteri-Regiment`, ca. 1.540 mand, 8 kompagnier.
  - Preussen: `8th Regiment (QA)`, ca. 2.460 mand, 12 kompagnier.
- Aktiv infantry total er ca. **4.000**.
- Aktiv company count er **20**.
- `11. Infanteri-Regiment` og `18th Regiment` forbliver i OOB-datamodellen, men er skjult i dette QA-scenarie.
- Brigade reserve/commit AI pauses i denne gate.
- Scale-down ændrer ikke slutmålet om fuld brigade/division OOB eller campaign-skalering.

### Retained company-control baseline from 09l2

- Company er den mindste direkte kontrollerbare infanteriformation.
- Ordinary 1:1 soldiers forbliver GPU-instanced; der oprettes ikke GameObject/MonoBehaviour/collider/NavMeshAgent/AI pr. soldat.
- Dansk company-control: enkeltklik vælger company; Shift add; Ctrl toggle; LMB drag box-select; RMB move; Alt+RMB append waypoint; RMB drag final facing; F/C Line/Column; H Hold; Z/X facing.
- 09l2 bruger company-anchored 1:1 rendering. Company Line bruger 3 ranks; Column bruger 8 files. Companies danner separate blocks under Battalion.
- Casualty changes på Regiment.CurrentStrength synkroniseres til company CurrentStrength; rendering fordeler visuelle gaps gennem company-blokken.
- Company-level ammo, LOS, fire eligibility og casualty targeting er fortsat en senere combat architecture gate.

### Historical/OOB baseline retained

- Historisk dansk working OOB for fuld brigade-test forbliver **7. Brigade: 1. + 11. Infanteri-Regiment**.
- Prussian 8th+18th pair er fortsat QA indtil date-specific brigade OOB er kildevalideret.
- Regimental HQ = 3 ryttere; Brigade HQ = 6 ryttere.
- Full dual standards = national/hærfane + regimentsfane/traditionsidentitet ved Regimental HQ.
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
