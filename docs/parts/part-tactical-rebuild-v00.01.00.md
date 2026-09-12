# PROJECT 1864 — Tactical Rebuild v00.01.00

**Status:** Authoritative clean-rebuild architecture charter  
**Delivery branch:** `strategi/kampe_rebuild`  
**Original work branch:** `work/tactical-rebuild-v00.01.00`  
**Baseline:** v00.00.09j, commit `b9e5f520abe9a8306b76d79df85f79b0a20e6fe8`  
**Current rebuild revision:** `v00.01.00d2` — Gate A+B+C complete, Gate D route/march slice active  
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
13. Regiment-level player selection is an order-group abstraction; it resolves into Company orders and never creates a second Regiment transform writer.
14. Visual soldier ratio (`1:1`, `1:2`, etc.) changes only rendered representatives, never tactical manpower/state.

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

Only I Battalion on each side is flagged `TacticalActive` in the current small QA scenario. Therefore D2 can validate Regiment-selection semantics, but not yet a full 8-/12-Company Regiment battle or true multi-Regiment Danish selection.

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
- No `Regiment` tactical movement object is spawned by the rebuild runtime.
- Selection has one owner: `TacticalCompanySelection00B`.
- LMB selects.
- Shift+LMB adds.
- Ctrl+LMB toggles.
- LMB drag box-selects by Company centre.
- Esc clears.
- Only Danish Companies are player-selectable in the current QA.

### Gate C — Instanced soldier renderer — IMPLEMENTED IN 00.01.00c–c5

Every active infantryman can be represented through instanced visuals tied to Company state.

No ordinary soldier receives its own:

- MonoBehaviour
- NavMeshAgent
- tactical AI
- collider
- heavy Animator

The renderer evolved through the C-series:

- `c`: initial 1:1 instanced renderer.
- `c2`: permanent world labels removed, hover identity panel added, rifle/body readability improved.
- `c3`: sharper proportions and formation-aware rendering.
- `c4`: visual soldier ratio setting (`1:1`, `1:2`, `1:3`, `1:4`, `1:5`, `1:10`, `1:20`) plus animated drill transitions.
- `c5`: QA lab and closer soldier detail presentation.
- `d1`: articulated procedural soldier silhouette and simple procedural march gait became the active renderer.

Visual ratio contract:

```text
ActualStrength = simulation truth
VisibleRepresentatives = render-only sampling
```

Example: a 190-man Company at `1:2` renders roughly 95 representative soldiers spread over the full real-strength formation footprint. Ammo, casualties, morale, movement and combat remain based on 190.

### Gate D1 — Direct ghost destination + facing — IMPLEMENTED IN 00.01.00d1

D1 introduced the first real clean Company movement slice:

- RMB destination ghost.
- RMB drag defines final facing.
- F changes destination Line/Column.
- release confirms.
- Company moves/reforms toward the preview instead of teleporting.
- multi-selection preserves relative Company offsets.
- Company remains the sole writer of its world pose.

D1 deliberately omitted obstacle/bridge/river navigation so the order/pose contract could be isolated first.

### Gate D2 — Regiment route + automatic march deployment — IMPLEMENTED IN 00.01.00d2

D2 adds route and Regiment-order semantics without introducing a Regiment transform owner.

#### Regiment selection

- Double-LMB on a Danish Company selects all currently tactical-active Companies belonging to that Regiment.
- Shift + double-LMB is prepared to add another Regiment when multiple Danish Regiments become tactical-active.
- Ctrl + double-LMB toggles the whole active Regiment subset.
- Selection still resolves to Companies; Regiment is an intent/group abstraction only.

#### Multi-point route planning

- Hold ALT and RMB-click several ground positions to create route waypoints.
- Backspace removes the most recent waypoint before confirmation.
- RMB at the final position creates the final ghost.
- Hold/drag RMB to define final facing.
- F toggles the final Line/Column formation.
- RMB release confirms the route.
- A cyan ghost route line displays the route from current group centre through user waypoints to destination.

Internally, each route is sampled into short ~8 m movement legs. This gives the D2 order coordinator frequent safe decision points while the Company entity remains the actual transform/movement owner.

#### Automatic march formation

A normal march order follows this state model:

```text
Current formation
      ↓
Reform to COLUMN
      ↓
Follow route in COLUMN
      ↓
Near final destination (~35 m)
      ↓
Deploy to requested final formation
      ↓
Finish final approach/facing
```

Contact override:

```text
Marching in COLUMN
      ↓
Enemy contact (QA threshold 160 m)
      OR
NotifyUnderFire(company)
      ↓
Abort remaining march route
      ↓
Deploy to LINE
```

Until Gate E supplies real fire events, `U` is a QA-only key that calls the under-fire path for selected Companies.

#### Route/fire-sector ghost

The final destination preview includes:

- final footprint(s),
- final facing arrow,
- 60-degree sector,
- SHORT arc = 80 m,
- MEDIUM arc = 160 m,
- LONG arc = 260 m.

These D2 ranges are QA visualization/tuning values only. Historical Danish/Prussian weapon profiles remain separate research/balance inputs and will source-lock in the combat gate.

#### National flag + Regiment banner

D2 introduces presentation-only Regiment standards:

- one national flag,
- one Regiment banner,
- one pair per active Regiment.

The standard root follows the average position/facing of the Regiment's currently active Companies. It never writes movement or OOB state.

Current banner artwork is deliberately provisional:

- Denmark: Dannebrog + dark-red/gold Regiment QA banner.
- Prussia: white/black national-style QA flag + dark Regiment QA banner.

Historical vexillology/artwork must be source-locked separately before final asset approval.

### Gate D3 — Obstacles / no-progress recovery — NEXT

Next movement slice should add under the same Company movement ownership:

- hard obstacle detection,
- soft obstacle penalties,
- persistent detour state,
- no-progress recovery,
- reuse of the proven V3 navigation concepts without restoring old Regiment steering.

### Gate D4 — Rivers / bridges / route validation — PLANNED

- river treated as blocked terrain,
- bridge-only crossing,
- route validation and bridge approach logic.

### Gate D5 — Group frontage/deconfliction — PLANNED

- Regiment/Company group templates,
- spacing and frontage,
- route crossing/deconfliction,
- future order-delay hooks.

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

Gate E must connect live incoming fire to `TacticalCompanyOrder00D2.NotifyUnderFire(company)` so a marching Company can deploy automatically on actual fire contact.

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

A `BattleManager` blocker is created before scene load so the legacy `PrototypeBootstrap` does not build the four old Regiment objects. Before the first normal Update, legacy MonoBehaviour runtime layers are disabled except retained safe presentation/camera components.

The clean rebuild creates its own QA field, camera, OOB registry, Company entities, selection owner and the currently approved Gate-D order coordinator.

Current D2 isolation:

```text
Legacy Regiment runtime movement objects: 0
Legacy PlayerCommander: inactive/not used
Company world-pose owner: TacticalCompanyEntity00B
Group/route intent owner: TacticalCompanyOrder00D2
Legacy D1 order owner: suppressed in D2
Old C3/C4 drill owners: suppressed
Company combat owner: none yet
Officer AI: inactive
Brigade AI: inactive
Artillery/cavalry: inactive
```

This isolation remains a hard acceptance rule. Route/regiment grouping must not become a second transform writer.

## 8. Version strategy

The clean rebuild uses a tactical version family beginning at `v00.01.00a`.

- `v00.01.00b` = Gate A+B runtime foundation.
- `v00.01.00c` = Gate C initial 1:1 renderer.
- `v00.01.00c2–c5` = visual/hover/ratio/QA maturation without movement ownership conflicts.
- `v00.01.00d1` = direct ghost destination/facing + first Company movement.
- `v00.01.00d2` = Regiment-selection abstraction, ALT waypoint routes, automatic march Column/deployment, route/range ghost and presentation standards.
- next = D3 obstacle/detour/no-progress recovery under the same Company movement architecture.

Every delivered TEST gate receives a visible build suffix. The old `channel-test` history remains untouched. Tactical rebuild delivery uses the dedicated `strategi/kampe_rebuild` branch and separate local working tree `Strategy-Kampe-Rebuild`.

## 9. Source/reference material

The old 09j runtime is the source-code baseline. 09k–09l5 remain evidence/reference for OOB, 1:1 rendering, HQ, flags, cavalry/artillery and Company-control lessons.

V3 remains the conceptual source for proven obstacle/navigation behaviour, but its Regiment-era movement authority must not be layered under the clean Company owner.

The consolidated design and 1864 OOB reference remain authoritative design inputs. Historical weapon ranges, standards and uniform details shown in QA builds are not treated as source-locked merely because a prototype visualization exists.
