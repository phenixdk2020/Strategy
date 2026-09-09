# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Seneste QA-revision: `v00.00.09l5`.
- **Status efter 09l5:** company-level design beholdes, men 09l2–09l5 runtime-layeringen er frosset som fejlet QA-arkitektur. Der må ikke bygges flere compatibility-/authority-lag oven på den retning.
- Fokus for næste tactical workbranch: clean Company-first rebuild med stable Unit IDs, uafhængige company world entities, én steering owner og company-owned combat.

### Retained tactical design concepts

- `v00.00.09f`: Navigation V3 som tidligere accepteret whole-regiment movement baseline.
- `09B/09h4`: decorative trees/fence posts som soft/pass-through; Farmhouse/Barn hard; river/bridge constraint.
- `v00.00.09g`: scenic battlefield + extended camera zoom.
- `v00.00.09h`: Soldier Visual Pass 1, Uniform Designer, RTS box selection, compact combat feedback, black-powder smoke.
- `v00.00.09h2/09h3/09h4`: AI frontage, march/deployment policy og manual AI-OFF route recovery experiments.
- `v00.00.09i–09i3`: Big Graphics Buff, procedural standards, medium-zoom readability og Unity 6.6 compatibility cleanup.
- `v00.00.09j`: fire/reload visual state baseret på faktisk outgoing volley og weapon-specific reload cadence.
- `v00.00.09k`: Full-Scale OOB + 1:1 Render + Mounted Regimental HQ + Special Arms Pilot.
- `v00.00.09l/09l1`: Brigade HQ + Historical Danish OOB + Reserve AI + Dual Standards.
- `v00.00.09l2–09l5`: valuable company-control QA experiments, men ikke accepteret final runtime architecture.

## 09l5 architecture review / freeze

Følgende blev observeret gennem 09l2–09l5:

- company tactical entities blev lagt oven på et runtime-system, hvor `Regiment` stadig ejede transform/movement/combat-antagelser
- companies var initialt transform-children under `Regiment.transform`
- parent rotation/movement kunne derfor trække companies som én rigid formation
- multi-company orders kunne genopbygge selection som én gigantisk linje
- Regiment forblev combat authority selv om companies var de synlige fighting formations
- HQ kunne dermed visuelt fremstå som den enhed, der affyrede hele regimentets salve
- selection blev delt mellem collider-, GPU-instance- og screen-space compatibility-lag
- successive execution-order/authority scripts maskede ownership-konflikter i stedet for at fjerne dem

### Freeze rule

`v00.00.09l5` skal bevares som QA/evidence branch state, men ikke viderepatches til en final company architecture.

Næste tactical implementation starter clean fra en kendt stabil baseline og bygger Company som first-class tactical entity fra begyndelsen.

Autoritativt supplement:

- `docs/parts/part-09l5-company-rebuild-recovery.md`
- `tools/strategy-tools/PROJECT-1864-CURRENT-DESIGN-MANUAL.md`

## Clean company rebuild gates

1. **OOB/data:** stable Unit IDs, 1 dansk + 1 preussisk regiment, battalion/company data, ingen Brigade AI.
2. **Company entities:** independent world position/facing, reliable select/box-select, ingen parent-transform coupling.
3. **Renderer:** 1:1 GPU-instanced soldiers driven exclusively by company state.
4. **Movement:** one steering/path owner per company; obstacles/bridge/formation movement.
5. **Combat:** company ammo, fire policy, LOS, reload, casualties, morale/cohesion.
6. **Battalion/Regiment AI:** assign companies to sectors/roles/reserve/support.
7. **Brigade AI:** restore second regiment, objectives, reserve commitment, cavalry/artillery attachments, courier delay.
8. **Campaign/OOB Designer:** shared stable IDs, organisational drag/drop, campaign movement and tactical handoff.

Until Gate 5 is stable, QA remains **1 Danish + 1 Prussian regiment**.

## Permanent company architecture rules

- Company is the smallest normal independently controllable infantry tactical entity.
- OOB parent relation is data, not Unity transform parenting.
- Regimental HQ commands companies; HQ does not drag them physically.
- Regimental HQ does not impersonate all company fire.
- Company owns company movement state.
- Company owns company combat state.
- Higher formation templates assign target centres to independent companies.
- One tactical entity has one authoritative steering writer at a time.
- Ordinary 1:1 soldiers remain GPU-instanced; no individual heavy GameObject/MonoBehaviour/NavMeshAgent/AI per soldier.

## Historical/OOB baseline retained

- Detailed historical working reference: `tools/strategy-tools/ARMY-1864-OOB-REFERENCE.md`.
- Danish generic infantry: 2 battalions × 4 companies = 8 companies/regiment; typical ~1,500–1,700 men.
- Prussian generic infantry: 3 battalions × 4 companies = 12 companies/regiment; typical 1864 campaign strength ~2,400–2,500 men.
- Historisk dansk working brigade pilot: **7. Brigade = 1. + 11. Infanteri-Regiment**.
- Prussian 8th+18th pair remains QA until date-specific brigade OOB is source-locked.
- Regimental HQ visual abstraction: 3 mounted staff figures.
- Brigade HQ visual abstraction: approximately 6–8 visible staff/orderly figures.
- Full standards remain national/army + regimental identity at Regimental HQ.
- Danish field battery working baseline: **8 guns / approximately 190 personnel**.
- Cavalry uses separate manpower / horses available / mounted effective values.

## OOB Designer direction

`docs/parts/part-09m-oob-designer-campaign.md` remains the core OOB Designer direction:

- drag/drop in OOB tree changes organisational parent
- drag/drop on campaign map creates movement/march orders
- stable Unit IDs link campaign and tactical state
- re-parenting does not teleport formations geographically

## Track B — Campaign

- Branch: `campaign`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Campaign`
- Aktuel revisionsserie starter ved `v00.00.10a`.
- Fokus: campaign scene/map, strategic formations, movement/time, battle trigger, tactical handoff og state return.
- OOB Designer skal dele stable Unit IDs og hierarchy-data med tactical-sporet.
- Campaign-kode/scener skal så vidt muligt ligge separat fra tactical navigation/combat-filer for at minimere merge-konflikter.

## Stable

- Branch: `main`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy`
- Kun accepterede/promoverede gates.

## Strategy Tools documentation hub

Konsolideret arbejdsreference ligger nu i:

- `tools/strategy-tools/README.md`
- `tools/strategy-tools/PROJECT-1864-CURRENT-DESIGN-MANUAL.md`
- `tools/strategy-tools/ARMY-1864-OOB-REFERENCE.md`

Disse dokumenter skal bruges som første reference ved ny tactical/campaign implementation, så tidligere prototype-lag ikke rekonstrueres ved hukommelse alene.

## Integration rule

Campaign må ikke vente på mindre tactical polish. Kun tactical fixes der er nødvendige for campaign-state continuity eller som er accepteret gennem QA, integreres senere i campaign-sporet. Ingen destructive Git-operationer, force-push, reset --hard eller clean anvendes.