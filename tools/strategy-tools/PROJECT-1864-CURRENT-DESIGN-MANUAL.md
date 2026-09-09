# PROJECT 1864 — Current Design Manual

**Consolidated working edition:** 2026-09-09  
**Tactical code state documented through:** v00.00.09l5  
**Purpose:** single current design reference for tactical, campaign, OOB, AI, performance and historical data work

---

# 1. Project vision

PROJECT 1864 is a real-time grand strategy game with a strategic campaign layer and tactical 3D battles.

The design target combines:

- strategic national and regional management
- historically grounded OOB and manpower
- persistent officers and units
- real-time campaign movement
- order delay and delegated command
- tactical 3D battles with thousands of visible soldiers
- company/battalion/regiment structure
- artillery, cavalry and dragoons
- persistent casualties, supply, ammunition and fatigue
- campaign ↔ tactical continuity

The initial historical focus is Denmark / Schleswig / northern Germany around 1864, with a campaign map centred on Denmark, Schleswig-Holstein, northern Germany and southern Scandinavia.

---

# 2. Design pillars

## 2.1 Command, not click-spam

The player should normally command formations and officers by intent rather than manually micro every soldier.

Preferred player interaction:

```text
Defend this area
Attack this position
Capture this village
Hold this bridge
Screen this flank
Keep a reserve
Withdraw to this line
```

The officer AI decides how many subordinate formations to commit, what to retain as reserve and when to reinforce.

Direct company control remains possible, especially with AI OFF, but it is an optional lower-level control mode rather than the default gameplay loop.

## 2.2 Persistent military organisations

A regiment is not a disposable battle token. It persists through the campaign with:

- identity
- history
- officers
- battalions
- companies
- manpower
- casualties
- equipment
- ammunition
- morale
- cohesion
- experience
- traditions
- standards
- battle honours

## 2.3 1:1 visual scale where practical

One real soldier should correspond to one visible soldier in tactical view when LOD/performance permits.

This is a rendering rule, not a simulation-object rule.

Do not create one heavy Unity GameObject/MonoBehaviour/Animator/NavMeshAgent/AI instance per man.

## 2.4 Historically grounded but data-driven

All historical structure must live in data rather than in hardcoded assumptions.

Denmark and Prussia must be allowed to have different regimental structures, HQ hierarchies and manpower values.

Prototype QA values must never silently become historical canon.

---

# 3. Strategic campaign layer

## 3.1 Campaign map

Current strategic direction:

- Denmark / Schleswig / Holstein / northern Germany / southern Sweden region
- approximately 40–50 meaningful map points in the current campaign prototype direction
- cities
- villages
- forts
- bridges
- ports
- rail/road nodes
- farms
- industrial sites
- depots
- military positions

The campaign should be real-time with pause and speed controls.

## 3.2 Strategic movement

Formations physically move between locations.

Movement state should include:

```text
CurrentPosition
Destination
Route
DepartureTime
ETA
RoadUse
Fatigue
SupplyState
FormationType
CommandDelay
```

Roads, bridges, weather, congestion and terrain influence movement.

A unit on the campaign map must never teleport because its organisational parent changed.

## 3.3 Economic and regional systems

Long-term project scope includes:

- money
- food
- wood
- iron
- stone
- farms
- towns
- industry
- weapons production
- research
- trade
- infrastructure
- roads
- railways
- military depots
- horse breeding / horse availability

Recruitment includes volunteers and conscription/draft systems.

---

# 4. OOB hierarchy

The full supported hierarchy is:

```text
Army
└── Corps
    └── Division
        └── Brigade
            ├── Regiment
            │   ├── Battalion
            │   │   ├── Company
            │   │   ├── Company
            │   │   ├── Company
            │   │   └── Company
            │   └── Battalion / additional battalion
            ├── Attached artillery
            ├── Attached cavalry / dragoons
            ├── Engineers
            └── Support / train
```

Not every army uses all levels identically.

### Permanent architecture rule

Organisational parent and Unity transform parent are different concepts.

```text
ParentFormationID != TransformParent
```

A regiment may command eight companies without those companies being children of one moving regiment transform.

This distinction is mandatory in the next rebuild.

---

# 5. Stable unit identity

Every formation receives a stable `UnitID` that survives:

- OOB reorganisation
- campaign movement
- tactical battle
- casualties
- officer changes
- save/load
- attachments
- renaming/display changes

The technical key must not be the display name.

Example:

```text
UnitID: DK-INF-0001
OfficialName1864: 1. Infanteri-Regiment
TraditionalName: Danske Livregiment
```

This avoids the prototype problem where a technical `5. Regiment` slot temporarily represented 11. Regiment.

---

# 6. Denmark and Prussia — 1864 structure

The detailed working historical reference is stored in:

`tools/strategy-tools/ARMY-1864-OOB-REFERENCE.md`

Core baseline:

### Denmark

- ~180–220 men per company
- 4 companies per battalion
- 2 battalions per regiment
- 8 companies per regiment
- typical regiment ~1,500–1,700 men

### Prussia

- ~190–210 men per company
- 4 companies per battalion
- 3 battalions per regiment
- 12 companies per regiment
- typical 1864 campaign regiment ~2,400–2,500 men

The structural difference must be visible and meaningful in tactical gameplay.

---

# 7. Tactical battle entity hierarchy

The intended clean tactical architecture is:

```text
Brigade HQ
│
├── Regiment HQ
│   ├── Battalion tactical state
│   │   ├── Company tactical entity
│   │   ├── Company tactical entity
│   │   ├── Company tactical entity
│   │   └── Company tactical entity
│   └── Battalion tactical state
│       └── Companies
│
└── Regiment HQ
    └── ...
```

## 7.1 Company is the minimum normal infantry tactical entity

Each company owns:

- world position
- facing
- movement order
- formation
- strength
- ammo
- morale/cohesion contribution or company-level state
- combat state
- firing/reload state
- casualty distribution
- selection footprint
- parent IDs

A company should normally represent roughly 180–220 men in this period.

## 7.2 Battalion

Battalion is a command/grouping level above four companies.

Battalion can be assigned:

- frontage sector
- support role
- reserve role
- flank mission
- attack axis
- defensive line

The player may later select a battalion as a group, but it must not require one rigid transform containing all companies.

## 7.3 Regiment

Regiment HQ commands battalions/companies.

It owns:

- regimental officer
- orders
- command quality
- doctrine
- staff
- standards
- parent brigade relation

It must not be the physical firing origin of all companies.

## 7.4 Brigade

Brigade HQ is the principal higher tactical command layer for the intended gameplay.

The player gives mission intent to the brigade commander. The brigade commander decides how to use subordinate regiments and attachments.

---

# 8. HQ visual representation

## 8.1 Regimental HQ

Preferred visual abstraction: **3 mounted figures**

- commander
- adjutant
- orderly/staff rider

They represent existing staff manpower; they are not extra manpower.

## 8.2 Brigade HQ

Preferred visible group: approximately **6–8 personnel**

- brigade commander
- adjutant/chief staff officer
- staff officers
- orderlies/couriers
- optional signal/escort representation

HQs are command entities and courier/order nodes.

---

# 9. Officer system

Officer profile concepts already established:

- Leadership
- Inspiration
- Tactical Skill
- Initiative
- Staff / Command Skill
- Discipline / Obedience
- Aggressiveness / Caution
- Composure / Nerve
- Officer Experience

These affect:

- reaction time
- order interpretation
- command delay
- decision noise
- willingness to commit reserve
- ability to maintain cohesion
- response to unexpected threats
- aggressiveness of pursuit/attack

Difficulty settings must not secretly modify weapon accuracy, reload or physical combat rules.

---

# 10. Objective-based Officer AI

Higher-level orders:

```text
DEFEND AREA
ATTACK AREA
CAPTURE OBJECTIVE
HOLD
WITHDRAW
SCREEN
SUPPORT
RECON
```

The AI evaluates:

- objective
- terrain
- enemy estimate
- own available strength
- morale/cohesion
- fatigue
- ammunition
- flank threats
- nearby friendly units
- attached artillery/cavalry
- commander doctrine and stats

Subordinate role states:

```text
ENGAGED
SUPPORT
RESERVE
MANOEUVRE
WITHDRAW
```

Example:

```text
Brigade order: CAPTURE VILLAGE

1st Regiment -> ENGAGED / main attack
2nd Regiment -> RESERVE
Battery -> SUPPORT from ridge
Dragoons -> SCREEN left flank
```

If the main attack stalls, the brigade AI may commit the reserve.

---

# 11. Orders, couriers and delay

Orders must not become instantaneous magical control at higher command levels.

Future lifecycle:

```text
Order created
→ transmitted from HQ
→ courier/message route
→ delivery delay
→ subordinate receives
→ interpretation/reaction delay
→ execution
```

Courier routes may be delayed, rerouted or lost depending on:

- distance
- roads
- terrain
- enemy presence
- cavalry/scouts
- darkness
- command quality

Courier graphics must not reveal hidden enemy HQs through fog of war.

---

# 12. Player control modes

The intended hierarchy allows both delegation and direct control.

## Higher-level mode

Select Brigade HQ / Regiment HQ and issue mission intent.

## Lower-level mode

With delegated AI OFF, directly select:

- one company
- multiple companies
- eventually one battalion

Controls established conceptually:

- LMB select
- Shift add
- Ctrl toggle
- drag box selection
- RMB move/order
- RMB drag for facing
- Line / Column
- Hold

### Important rule

Selecting a regiment in company-based architecture should mean:

```text
select subordinate companies as a group
```

not:

```text
move one giant regiment pivot that drags all children
```

---

# 13. Formation system

## Company formations

Initial infantry company forms:

- Line
- Column

Future forms may include:

- skirmish order
- attack column
- road march column
- square where historically appropriate
- dispersed cover posture

## Battalion/regiment templates

Higher formations position independent companies into templates.

Examples:

```text
4 companies abreast
3 forward + 1 reserve
2 + 2
Echelon left/right
Battalion line
Battalion column
Regimental reserve
```

Templates supply desired company centres; each company remains an independent tactical entity.

Terrain fitting may adjust company slots without destroying the hierarchy.

---

# 14. Movement architecture

## 14.1 Historical prototype baseline

Navigation V3 was the accepted whole-regiment movement baseline during the earlier 1:10/Regiment-object phase.

Useful retained concepts:

- persistent detour state
- building avoidance
- river only via bridge
- temporary column through constrained routes
- destination preservation

## 14.2 Company rebuild requirement

V3 must **not** simply be wrapped around every old `Regiment` object while independent company transforms are added as compatibility layers.

The clean next system must expose a reusable navigation service operating on a tactical formation state:

```text
MoveRequest
FormationFootprint
Destination
ObstacleQuery
Path/DetourState
ArrivalState
```

Company entities call this service directly.

No company may depend on a moving parent Regiment transform.

No two movement systems may write the same company position/destination in the same frame.

## 14.3 One steering owner

Permanent invariant:

> One tactical entity has one authoritative steering owner at a time.

AI can choose goals. Player commands can choose goals. Navigation can solve paths. Only one movement layer writes the resulting transform/state.

---

# 15. Tactical recovery decision after v00.00.09l5

The 09l2–09l5 direction proved that the desired company-level gameplay is correct, but the implementation became structurally unstable because company behaviour was layered on top of a regiment-centric runtime architecture.

Observed failure modes included:

- companies visually separate but still parented under `Regiment.transform`
- regiment pivot rotation/movement dragging companies as one rigid body
- compatibility layers overriding previous movement in later execution orders
- selection logic split between physics colliders, GPU instances and screen-space patches
- combat still owned by Regiment while visible troops were companies
- HQ appearing to fire the regiment volley
- standards/guidons tied to the wrong authority layer
- group orders rebuilding multiple companies as one very long line
- successive authority scripts masking rather than removing architectural ownership conflicts

## 15.1 Freeze rule

`v00.00.09l5` is **frozen as failed QA architecture**.

Do not add 09l6/09l7 compatibility patches on top of it to rescue company movement.

## 15.2 Rebuild strategy

Next tactical company implementation should start from a known stable tactical baseline and introduce a clean `CompanyTacticalEntity` as first-class architecture.

Recommended approach:

1. Preserve a stable branch containing the earlier proven battle/camera/render foundations.
2. Create a fresh `company-rebuild` work branch.
3. Remove/disable old regiment-authoritative infantry rendering/movement before adding company movement.
4. Add stable `UnitID` + OOB data layer.
5. Spawn only **1 Danish + 1 Prussian regiment** for QA.
6. Create independent company entities in world space from the beginning.
7. Render soldiers from company state.
8. Implement selection directly against company footprints.
9. Implement independent company movement with one steering system.
10. Implement group templates on top of company movement.
11. Add company combat.
12. Add regiment/battalion Officer AI.
13. Restore Brigade AI only after lower layers pass QA.
14. Restore second regiment per side only after stability.

## 15.3 Acceptance gate

A company architecture is not accepted until all of the following work simultaneously:

- one company can be selected reliably
- one company can move independently
- moving HQ does not move company
- two companies can move to different destinations
- multiple selected companies preserve separate centres
- formation change affects only intended companies
- no hidden parent transform changes company position
- company fires/reloads from its own state
- company casualties reduce its own manpower
- regiment totals equal subordinate company totals + staff
- save/load preserves company positions and states

---

# 16. 1:1 soldier rendering

The project direction is 1:1 tactical visual manpower when practical.

Example:

```text
Company strength = 198
Visible soldiers = 198
```

But the renderer uses batching/instancing.

Do not use per-soldier:

- MonoBehaviour
- NavMeshAgent
- full Animator
- collider
- AI Update

Recommended renderer architecture:

```text
CompanyState
→ formation slot buffer
→ instanced soldier draw
→ LOD/culling
```

Performance targets:

- 5,000 soldiers: easy baseline
- 10,000: common large engagement
- 20,000: strong target
- 40,000: upper normal/stress target
- 60,000+: special stress case

Most 1864 engagements in the campaign are expected to be hundreds or a few thousand men, so a 40,000-man target gives substantial headroom.

---

# 17. Soldier visual standard

Desired close/medium visual identity:

- human-like body proportions
- torso/coat
- arms/hands
- separate legs/boots
- head/headgear
- rifle
- bayonet
- pack
- cartridge box
- straps
- officer distinction
- standard bearer distinction
- national uniform differences

Uniforms should be data-driven per unit/regiment.

Uniform Designer should eventually control:

- coat
- trousers
- headgear
- facings
- equipment
- straps
- officer details
- flag/standard identity

LOD must simplify details at distance without changing simulation state.

---

# 18. Flags and standards

At regimental HQ:

- national/army identity
- regimental standard / colour

Danish working design supports Dannebrog-based colours with Roman numeral and royal/regimental identity.

Traditions such as `Sjællandske Livregiment` belong in data separately from the official 1864 name.

Company guidons may exist as tactical readability aids, but must be identified as UI/tactical markers unless historical evidence supports the exact implementation.

Standards should support states:

- unfurled
- rolled
- cased
- damaged
- lost
- captured

---

# 19. Infantry combat

## 19.1 Weapons already modelled conceptually

Denmark:

- rifled muzzle-loader

Prussia:

- Dreyse needle rifle

Weapon profiles include:

- effective range
- maximum range
- reload time
- accuracy baseline

Experience may modestly affect reload while weapon technology remains dominant.

## 19.2 Fire policy

Policies:

```text
HOLD
CLOSE
MEDIUM
LONG
```

Directional fire uses a forward arc rather than 360-degree fire.

## 19.3 Company combat target architecture

Combat must move from Regiment authority to Company authority.

Each company should own:

```text
Ammo
NextFireTime
ReloadState
FirePolicy
Target
EligibleFiringFraction
Morale
Cohesion
CurrentStrength
```

Fire eligibility evaluates:

- range
- facing/fire arc
- line of sight
- friendly obstruction
- terrain
- smoke
- target exposure
- formation state

## 19.4 Reload animation

Visual sequence should follow actual combat timing:

```text
Fire
→ recoil/fire pose
→ reload sequence
→ ready
```

Muzzle-loader and Dreyse must use different reload motion/cadence.

---

# 20. Casualties and medical state

Casualties should persist at company level.

A volley reducing one company must not simply subtract men from a generic regiment pool and later redistribute them arbitrarily.

Track:

- killed
- wounded
- missing
- captured

Campaign systems later include:

- hospitals
- recovery
- prisoners
- prisoner exchange where appropriate
- manpower replacement

Visual casualty state may select individual displayed soldiers to fall while the authoritative casualty count remains in company data.

---

# 21. Morale, cohesion and fatigue

These should exist at appropriate echelon levels.

Possible model:

- company local cohesion
- battalion/regiment aggregate morale modifiers
- officer influence
- fatigue from movement/combat
- shock from casualties/fire

Movement, firing and formation changes may affect cohesion.

Rout/withdrawal should propagate through command structures but need not make every company collapse identically.

---

# 22. Artillery

Danish working 1864 field-battery baseline:

- 8 guns
- 4 divisions/sections × 2 guns
- approximately 190 personnel

Artillery entity must include:

- guns
- crews
- ammunition
- limber state
- horses
- drivers
- caissons/wagons

Future commands:

- deploy/unlimber
- limber/move
- bombard area
- support attack
- defend sector
- counter-battery
- withdraw

Crew and horses are persistent resources.

Prussian battery data remains editable until source-locked.

---

# 23. Cavalry and dragoons

Cavalry manpower and horse availability are separate.

Required state:

```text
PersonnelPresent
HorsesAvailable
MountedEffective
DismountedEffective
HorseFatigue
```

Roles:

- reconnaissance
- screening
- counter-recon
- flank security
- courier support
- pursuit
- raids
- exploitation

Dragoons support mounted movement and dismounted sustained combat.

Dismount creates horse-holder requirements. Remount takes time.

Charge effectiveness depends on:

- target state
- formation
- facing
- terrain
- surprise
- cavalry fatigue/cohesion
- officer quality

Steady infantry frontally engaged should be dangerous to charge.

---

# 24. Supply and logistics

Battle supply is physical and finite.

Track:

- ammunition stock
- wagons
- caissons
- horses
- supply route
- depots/field train

Resupply requires a real route and stock.

Cut-off units do not receive automatic overnight ammunition.

Cavalry raids and enemy presence can disrupt supply and courier routes.

---

# 25. Day/night and multi-day battles

Battle state may continue:

```text
DAYLIGHT
→ DUSK
→ NIGHT
→ DAWN
```

Night reduces organised combat effectiveness but allows:

- reorganisation
- resupply
- casualty collection
- patrols
- courier traffic
- withdrawal
- fieldworks

State persists between days.

---

# 26. Fog of war

Fog of war is knowledge state, not simply rendering visibility.

Enemy contact states may include:

```text
Unknown
Suspected
Contact
Identified
Fresh
Stale
```

Reports decay over time.

Scouts, cavalry, terrain, visibility and officers affect information quality.

---

# 27. Semantic zoom and tactical UI

At close zoom:

- 3D soldiers
- standards
- formations

At medium zoom:

- simplified formations
- clearer unit identity

At far zoom:

- tactical symbols / NATO-APP-6-like abstraction where useful

UI rules:

- overlays must not hide formations
- range/fire lines should be thin/transparent
- selected unit information should be compact
- avoid large battlefield-following info boxes
- box selection must remain available
- command relationships should appear primarily on selection/overlay demand

---

# 28. OOB Designer

The OOB Designer is a core campaign tool.

Two separate drag/drop semantics:

## OOB tree

Drag changes organisational parent.

```text
Regiment: Brigade A → Brigade B
```

No geographic movement.

## Campaign map

Drag creates movement order.

No organisational reassignment unless explicitly requested.

The OOB Designer should show:

- unit name
- commander
- strength
- location
- readiness
- mission
- supply
- parent HQ
- attachments

Features:

- collapse/expand hierarchy
- search/filter
- drag/drop re-parent
- attach/detach artillery/cavalry
- select in tree → centre camera on map
- select on map → reveal in tree
- stable Unit IDs
- save/load

---

# 29. Campaign ↔ tactical handoff

The tactical battle uses the exact campaign units.

Persist across handoff:

- UnitID
- OOB parent relationships
- officers
- company manpower
- casualties
- ammunition
- fatigue
- morale/cohesion
- horses
- guns
- supply
- map position
- current movement/order state

A formation that has not reached the battle area does not spawn magically at battle start.

---

# 30. Data confidence and historical canon

Every historical datum should support a confidence tag:

```text
SOURCE_LOCKED
WORKING
QA_PLACEHOLDER
INFERRED
UNKNOWN
```

Examples:

- verified date-specific regiment strength → SOURCE_LOCKED
- generic 1,600-man Danish regiment → WORKING
- arbitrary Prussian test brigade pair → QA_PLACEHOLDER

This prevents test values from becoming permanent history by accident.

---

# 31. Recommended implementation sequence after recovery freeze

## Gate A — clean data foundation

- Stable UnitID
- OOB data model
- 1 Danish + 1 Prussian regiment
- battalions + companies
- no brigade AI yet

## Gate B — company world entities

- independent positions
- independent facing
- selection
- box selection
- no parent-transform coupling

## Gate C — rendering

- 1:1 instancing driven from company state
- LOD
- flags/readability

## Gate D — company movement

- one steering owner
- obstacle/path service
- single company move
- multi-company template move
- road/bridge constraints

## Gate E — company combat

- ammo
- fire policy
- LOS
- reload
- casualties
- morale/cohesion

## Gate F — battalion/regiment Officer AI

- company role assignment
- reserve/support
- attack/defend objective

## Gate G — Brigade HQ / reserve AI

- 2 regiments per brigade
- attachments
- reserve commitment
- courier delay

## Gate H — OOB Designer / campaign sync

- drag/drop organisation
- map movement
- persistent handoff

Only after each gate passes QA should the next layer be added.

---

# 32. Current tactical status summary

Useful earlier prototype achievements remain valid as concepts:

- RTS box selection
- extended camera zoom
- scenic battlefield
- directional fire arcs
- muzzle-loader vs Dreyse profiles
- reload visual concept
- procedural soldier rendering
- regimental standards
- mounted HQ concept
- 1:1 instancing direction
- historical OOB data structure
- Brigade objective/reserve concept
- artillery and dragoon pilots

The specific 09l2–09l5 company implementation is not the accepted foundation.

The accepted design is **independent Company tactical entities**; the rejected implementation is **compatibility layering on top of Regiment-authoritative transforms/combat/movement**.

---

# 33. Non-negotiable architecture rules going forward

1. **Stable UnitID, never display-name identity.**
2. **OOB parent is data, not transform parenting.**
3. **One steering owner per tactical entity.**
4. **Company owns company movement.**
5. **Company owns company combat.**
6. **Regiment HQ commands; it does not impersonate all firing soldiers.**
7. **Brigade AI assigns roles; it does not directly overwrite every company transform.**
8. **1:1 soldiers are renderer instances, not heavy simulation objects.**
9. **Historical values carry confidence/source status.**
10. **No new compatibility authority layer without first removing the ownership conflict it is masking.**
11. **Every prototype gate gets a small QA scenario before scale-up.**
12. **Second regiment/Brigade AI stays off until single-regiment company control is stable.**

---

# 34. Reference documents

- Historical/OOB working reference: `tools/strategy-tools/ARMY-1864-OOB-REFERENCE.md`
- Modular detailed design manual: `docs/PROJECT-1864-Designmanual.md`
- Tactical/campaign tracks: `docs/DEVELOPMENT-TRACKS.md`
- OOB Designer supplement: `docs/parts/part-09m-oob-designer-campaign.md`
- Existing historical tactical supplements remain useful as implementation history but do not override the recovery architecture in this document.
