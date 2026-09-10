# PROJECT 1864 — Tactical Rebuild v00.01.00

**Status:** Authoritative clean-rebuild architecture charter  
**Delivery branch:** `strategi/kampe_rebuild`  
**Original work branch:** `work/tactical-rebuild-v00.01.00`  
**Baseline:** v00.00.09j, commit `b9e5f520abe9a8306b76d79df85f79b0a20e6fe8`  
**Current rebuild revision:** `v00.01.00c2` — Gate A+B+C plus visual/hover QA pass  
**Reason:** v00.00.09l2–09l5 proved the desired Company-level gameplay but also proved that layering Company control on top of Regiment-owned movement/combat creates conflicting authority.

## 1. What is being restarted

The tactical runtime architecture is being rebuilt. The project design, historical research, campaign direction and proven gameplay ideas are retained.

The rebuild does **not** mean starting a new Unity project from zero. It starts from the last pre-full-scale tactical baseline, before the 09k–09l5 full-scale/company compatibility layers were introduced.

## 2. Systems retained as proven concepts

- RTS camera and zoom.
- Pause and simulation speeds as a retained design concept.
- LMB/RMB command philosophy and box selection.
- Directional fire arc and HOLD/CLOSE/MEDIUM/LONG fire discipline.
- Danish rifled muzzle-loader vs Prussian Dreyse weapon profiles.
- Reload timing and presentation-only reload animation.
- Morale, cohesion, fatigue, rout and casualty concepts.
- Officer stats, doctrine, aggression, reaction delay and no-cheat difficulty principle.
- Navigation lessons from V3: one steering owner, persistent detour, hard/soft obstacles, bridge-only river crossing.
- Historical OOB direction: Army -> Corps -> Division -> Brigade -> Regiment -> Battalion -> Company.
- Company as the smallest normal independently controllable infantry tactical entity.
- 1:1 soldier rendering through instancing/LOD rather than heavy per-soldier simulation objects.
- Regimental HQ, Brigade HQ, standards, cavalry/dragoons and artillery as planned first-class tactical systems.
- OOB Designer and campaign-map synchronization as a later shared-data layer.

## 3. Systems explicitly not carried forward from 09k–09l5

The following implementation pattern is rejected:

- Company tactical objects parented under `Regiment.transform`.
- Regiment movement plus later Company movement overrides.
- Regiment combat plus later Company fire suppression patches.
- Multiple execution-order authority scripts writing the same category of state.
- Selection split between several competing owners.
- Technical identity based on display names.
- Higher HQ directly moving subordinate transforms.

The 09l5 branch/state remains preserved for reference and comparison only.

## 4. Non-negotiable architecture rules

1. Every persistent formation receives a stable `UnitID`.
2. Display name is never the technical key.
3. OOB parent relationship is stored in data, not required Transform parenting.
4. Company is a first-class infantry tactical entity.
5. Company owns its world position, facing, route, formation and movement state.
6. Company owns its fire, ammo, reload, casualties and local combat state.
7. One tactical entity has one movement/steering owner at a time.
8. One tactical entity has one combat owner at a time.
9. Battalion, Regiment and Brigade issue intent, sectors and formation templates; they do not directly overwrite Company transforms every frame.
10. Ordinary soldiers are rendering instances, not individual heavy AI/GameObject agents.
11. No compatibility authority patch is accepted when the ownership conflict can be removed instead.
12. Each development gate must compile and pass a small QA scenario before the next gate is introduced.

## 5. Initial QA scale

The first clean Company test is deliberately smaller than a full Regiment battle:

```text
Denmark
1 Battalion
4 Companies
~180–220 men per Company

Prussia
1 Battalion
4 Companies
~190–210 men per Company
```

Current implemented values:

```text
Denmark I Battalion: 759 men
Company strengths: 190 / 190 / 190 / 189

Prussia I Battalion: 812 men
Company strengths: 203 / 203 / 203 / 203

Active tactical manpower: 1,571
Active Company entities: 8
```

Full reference Regiment data remains present in the OOB registry:

- Denmark `DK-INF-001`: 1. Infanteri-Regiment / Danske Livregiment, 1540.
- Prussia `PR-INF-008`: 8th Regiment QA, 2460.

Only I Battalion on each side is flagged `TacticalActive` in the current small QA scenario.

## 6. Development gates

### Gate A — Unit identity and OOB data — IMPLEMENTED IN 00.01.00b

Implemented:

- stable `UnitID`
- Nation
- UnitType
- Echelon
- ParentUnitId
- AuthorizedStrength
- PresentStrength
- Regiment/Battalion/Company records
- official 1864 name and traditional identity as separate fields
- full two-battalion Danish and three-battalion Prussian reference OOB data for the two pilot Regiments

No tactical movement or combat owner is introduced by Gate A.

### Gate B — Independent Company entities and selection — IMPLEMENTED IN 00.01.00b

Exactly eight QA Companies are spawned as independent world-space tactical entities.

Implementation rules:

- Companies are scene children only of neutral `REBUILD_00B_COMPANY_ENTITIES`.
- OOB parenthood is stored in `ParentUnitId`.
- No Company is a Transform child of a Battalion or Regiment.
- No `Regiment` tactical object is spawned by the rebuild runtime.
- Selection has one owner: `TacticalCompanySelection00B`.
- LMB selects.
- Shift+LMB adds.
- Ctrl+LMB toggles.
- LMB drag box-selects by Company centre.
- Esc clears.
- Only Danish Companies are player-selectable in this QA.
- RMB intentionally does not move units yet.

### Gate C — 1:1 renderer — IMPLEMENTED IN 00.01.00c

Every active infantryman is rendered 1:1 as an instanced visual tied to Company state.

No ordinary soldier receives its own:

- MonoBehaviour
- NavMeshAgent
- tactical AI
- collider
- heavy Animator

LOD/culling is present from the start and the renderer is read-only presentation.

### Gate C2 — Soldier readability + hover UI — IMPLEMENTED IN 00.01.00c2

This is deliberately a visual/UI QA pass, not a new simulation authority layer.

Implemented:

- Permanent world-space Company labels removed.
- Mouse-over info panel shows Regiment, Battalion, Company, nation, strength, formation, UnitID and selected state.
- Selection behaviour is unchanged from Gate B.
- Soldier body is less block-like: narrower torso, separate coat skirt, capsule limbs, two visible arms, cross-belt and pack.
- Rifle silhouette is split into wooden stock, long metal barrel and bayonet.
- Danish and Prussian QA palettes remain distinct.
- Full detail <=250 m; Medium <=500 m; Far <=850 m.
- No national or regimental standards in c2; flag work is deferred after QA feedback.
- No movement, navigation, combat or AI is added in c2.

Acceptance focus:

1. Build marker is `PROJECT 1864 | v00.01.00c2 TEST`.
2. Eight Company entities remain intact.
3. 1571 1:1 soldier visuals remain visible at suitable LOD.
4. White world labels are gone.
5. Mouse-over info is readable and points to the correct Company/OOB lineage.
6. LMB/Shift/Ctrl/drag-box/Esc selection remains unchanged.
7. Soldier and rifle silhouette are visibly improved without unacceptable FPS loss.
8. RMB still logs `MovementDeferredToGateD` and performs no movement.

### Gate D — Company movement and navigation

One Company movement owner handles:

- direct movement
- waypoints
- final facing
- Line/Column
- group templates
- obstacles
- bridge/river restrictions
- no-progress recovery

V3 algorithms may be reused conceptually, but not by layering old Regiment steering under new Company steering.

### Gate E — Company combat

Company owns:

- target
- fire policy
- firing fraction
- LOS/fire arc
- ammo
- next fire time
- reload state
- casualties
- morale/cohesion effects

The existing combat tuning values are reference defaults, not immutable final balance.

### Gate F — Full Regiment scale

Scale to:

- one Danish Regiment: 2 battalions / 8 Companies / roughly 1,500–1,700 men
- one Prussian Regiment: 3 battalions / 12 Companies / roughly 2,400–2,500 men

Only after selection, movement, rendering and combat have passed their earlier gates.

### Gate G — Battalion/Regiment HQ and Officer AI

Add:

- Regimental HQ
- Battalion grouping
- officer profiles
- objective/sector assignment
- Company ENGAGED/SUPPORT/RESERVE/MANOEUVRE roles
- order/reaction delay

Higher AI supplies goals to Company movement rather than directly moving transforms.

### Gate H — Cavalry/dragoons and artillery

Introduce each as its own first-class tactical unit type.

Cavalry separates personnel and horses. Artillery separates guns, crew, horses, ammunition and limber state.

### Gate I — Brigade HQ and reserve AI

Restore two Regiments per side only after lower levels are stable.

Brigade AI receives Defend/Attack/Capture intent and decides how much strength to commit or retain in reserve.

### Gate J — OOB Designer and campaign synchronization

Shared stable Unit IDs connect campaign and tactical layers.

- OOB-tree drag/drop changes organisational parent.
- Campaign-map drag/drop creates movement orders.
- Reorganisation never teleports units.
- Tactical casualties/ammo/fatigue return to the campaign state.

## 7. Runtime isolation

The clean rebuild deliberately does not allow the old 09j runtime to become a hidden parent authority.

A `BattleManager` blocker is created before scene load so the legacy `PrototypeBootstrap` does not build the four old Regiment objects. Before the first normal Update, legacy MonoBehaviour runtime layers are disabled except:

- `RTSCameraController`
- `PrototypeBuildVersionOverlay`

The clean rebuild then creates its own minimal QA field, camera, OOB registry, eight Company entities and selection owner.

Current isolation remains:

```text
Legacy Regiment runtime objects: 0
Legacy PlayerCommander: inactive/not used
Company movement owner: none
Company combat owner: none
Officer AI: inactive
Brigade AI: inactive
Artillery/cavalry: inactive
```

This isolation is intentional and remains a hard acceptance rule until each later gate explicitly introduces a single owner.

## 8. Version strategy

The clean rebuild uses a tactical version family beginning at `v00.01.00a`.

- `v00.01.00b` = Gate A+B runtime foundation.
- `v00.01.00c` = Gate C 1:1 renderer.
- `v00.01.00c2` = soldier visual/readability upgrade + hover info, no simulation changes.
- next simulation gate = Gate D Company movement/navigation.

Every delivered TEST gate receives a visible build suffix. The old `channel-test` history remains untouched. Tactical rebuild delivery now uses the dedicated `strategi/kampe_rebuild` branch and separate local working tree `Strategy-Kampe-Rebuild`.

## 9. Source/reference material

The old 09j runtime is the source-code baseline. 09k–09l5 remain evidence/reference for OOB, 1:1 rendering, HQ, flags, cavalry/artillery and Company-control lessons.

The consolidated design and 1864 OOB reference from the later documentation branch remain authoritative design inputs and must be carried into the rebuild workflow even though the runtime branch starts from 09j.
