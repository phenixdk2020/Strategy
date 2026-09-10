# PROJECT 1864 — Tactical Rebuild v00.01.00

**Status:** Authoritative work-branch architecture charter  
**Branch:** `work/tactical-rebuild-v00.01.00`  
**Baseline:** v00.00.09j, commit `b9e5f520abe9a8306b76d79df85f79b0a20e6fe8`  
**Reason:** v00.00.09l2–09l5 proved the desired Company-level gameplay but also proved that layering Company control on top of Regiment-owned movement/combat creates conflicting authority.

## 1. What is being restarted

The tactical runtime architecture is being rebuilt. The project design, historical research, campaign direction and proven gameplay ideas are retained.

The rebuild does **not** mean starting a new Unity project from zero. It starts from the last pre-full-scale tactical baseline, before the 09k–09l5 full-scale/company compatibility layers were introduced.

## 2. Systems retained as proven concepts

- RTS camera and zoom.
- Pause and simulation speeds.
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

Expected visible infantry is roughly 1,500–1,700 total, depending on the selected test strengths.

This is a QA scale only. The final OOB remains historically structured, and later gates scale to full Danish and Prussian Regiments.

## 6. Development gates

### Gate A — Unit identity and OOB data

Implement:

- stable `UnitID`
- Nation
- UnitType
- Echelon
- ParentFormationID
- AuthorizedStrength
- PresentStrength
- Battalion/Company records
- official 1864 name and traditional identity as separate fields

No tactical movement changes yet.

### Gate B — Independent Company entities and selection

Implement exactly eight QA Companies (four per side) as independent world-space tactical entities.

Required acceptance:

- one Company can be clicked reliably
- box selection works
- Shift adds
- Ctrl toggles
- selecting a parent grouping selects subordinate Companies without creating physical parent movement
- moving any HQ does not move a Company

### Gate C — 1:1 renderer

Render every active soldier as an instanced visual tied to Company state.

No per-soldier:

- MonoBehaviour
- NavMeshAgent
- tactical AI
- collider
- heavy Animator

LOD/culling must be designed from the start.

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

## 7. Version strategy

The clean rebuild uses a new tactical version family beginning at:

`v00.01.00a`

This intentionally separates the rebuild from the experimental `v00.00.09*` series.

Every delivered TEST gate receives a visible build suffix. `channel-test` is not moved to the rebuild until a gate has compiled and passed focused QA.

## 8. Source/reference material

The old 09j runtime is the code baseline. 09k–09l5 remain evidence/reference for OOB, 1:1 rendering, HQ, flags, cavalry/artillery and Company-control lessons.

The consolidated design and 1864 OOB reference from the later documentation branch remain authoritative design inputs and must be carried into the rebuild workflow even though the runtime branch starts from 09j.
