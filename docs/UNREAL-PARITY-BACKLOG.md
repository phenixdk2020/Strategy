# PROJECT 1864 — Unreal Engine parity backlog

**Backlog baseline:** UE-P v00.01  
**Unreal branch:** `unreal-port`  
**Unity reference branch:** `channel-test`  
**Unity tactical baseline:** `v00.00.09f30x` plus later compile fixes/source assets  
**Unreal baseline:** UE 5.8.3  
**Purpose:** track every required step until the Unreal tactical prototype reaches or exceeds the documented Unity tactical prototype.

> **Hard rule:** Unreal is not considered the replacement tactical implementation until the relevant item is **PARITY VERIFIED**. A class existing or compiling is not parity.

## Status model

| Status | Meaning |
|---|---|
| BACKLOG | Required parity work has not started. |
| SCAFFOLD | Architecture/class exists, but behaviour is incomplete. |
| IMPLEMENTED | Intended behaviour is coded, but build/runtime QA is incomplete. |
| BUILD VERIFIED | C++/UHT/editor build has passed for the item. |
| QA PENDING | Build works; gameplay behaviour still requires scenario QA. |
| PARITY VERIFIED | Unreal reproduces the documented Unity behaviour and passes the parity test. |
| DEFERRED | Approved feature intentionally waits on a named dependency. |

## Priority model

- **P0** — required for the first playable Unreal battle and/or blocks several later systems.
- **P1** — required before Unreal can replace the Unity tactical prototype.
- **P2** — required parity/presentation work that can follow the first playable battle.
- **P3** — future expansion already approved in design, but not needed for the current Unity-parity gate.

---

# 1. Project, build and test framework

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P001 | P0 | UE project/bootstrap | Engine-port baseline | Module/targets/project | PARITY VERIFIED | UE 5.8.3 editor target builds successfully. |
| UE-P002 | P0 | Unreal source layout | Migration plan | Source modules/directories | BUILD VERIFIED | Stable domain folders/classes compile without Unity revision suffixes. |
| UE-P003 | P0 | Tactical QA level | Unity battle test | Unreal test level | BACKLOG | Reproducible Danish/Prussian battle test can be launched directly. |
| UE-P004 | P0 | Visible build marker | Unity TEST overlay | Unreal HUD/debug overlay | BACKLOG | Running build clearly displays Unreal prototype/version marker. |
| UE-P005 | P1 | Regression checklist | F29/F30 QA | Automated/manual QA suite | BACKLOG | U10 checks can be executed repeatably and results recorded. |
| UE-P006 | P2 | Debug telemetry/logging | Unity prototype logs | UE_LOG + debug subsystem | BACKLOG | Orders, movement, AI, contact and completion expose useful reason/state logs. |

# 2. Camera, mouse input and selection — U01

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P010 | P0 | RTS camera pan | Tactical camera | `AStrategyCameraPawn` | IMPLEMENTED | WASD pans reliably over tactical map. |
| UE-P011 | P0 | Camera rotation | Tactical camera | `AStrategyCameraPawn` | IMPLEMENTED | Q/E rotates camera without changing simulation state. |
| UE-P012 | P0 | Camera zoom | Tactical camera | `AStrategyCameraPawn` | IMPLEMENTED | Mouse wheel zooms within configured limits. |
| UE-P013 | P0 | Single-click selection | Selection baseline | `AStrategyPlayerController` | IMPLEMENTED | Plain click selects one player-controllable unit. |
| UE-P014 | P0 | Box selection | RTS box-select rule | PlayerController + HUD | IMPLEMENTED | Units whose actor/formation centre lies inside marquee are selected. |
| UE-P015 | P0 | Shift additive selection | Selection baseline | PlayerController | IMPLEMENTED | Shift adds without clearing existing selection. |
| UE-P016 | P0 | Ctrl removal selection | Selection baseline | PlayerController | IMPLEMENTED | Ctrl removes targeted units only. |
| UE-P017 | P0 | Selection visual hook | Unity selection marker | Unit BP event | IMPLEMENTED | Selected state can drive ring/highlight without owning gameplay state. |
| UE-P018 | P0 | Selection persistence through order | F29X/F30S | Controller/order input | IMPLEMENTED DESIGN / U03 DEPENDENCY | Issuing point/facing orders never clears selected HQ/formation. |
| UE-P019 | P1 | OOB click selection | F29X | UMG OOB | BACKLOG | Single-click selects; double-click selects + camera navigation. |
| UE-P020 | P1 | Mixed entity box selection | F30H | Selection subsystem | BACKLOG | Infantry, cavalry and higher HQs are selectable by marquee according to ownership rules. |

# 3. Unit identity, hierarchy and OOB data — U02

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P030 | P0 | Base tactical unit state | `Regiment.cs` family | `AStrategyUnit` | SCAFFOLD | Stable ID, side, echelon, name, strength, state and selection are authoritative. |
| UE-P031 | P0 | Company entity | Company baseline | Infantry company actor/data | BACKLOG | Company exists independently with current/initial strength and command parent. |
| UE-P032 | P0 | Battalion/Major entity | F27 | HQ actor + hierarchy component | BACKLOG | Major owns correct company set and can receive player/AI mission. |
| UE-P033 | P0 | Regiment entity/HQ | F27/F28 | HQ actor + hierarchy component | BACKLOG | Regiment owns Major A/B and physical regimental HQ. |
| UE-P034 | P0 | Brigade entity/HQ | F30B | Higher HQ actor | BACKLOG | Brigade exists physically and in hierarchy. |
| UE-P035 | P0 | Division entity/HQ | F30B | Higher HQ actor | BACKLOG | Division exists physically and in hierarchy. |
| UE-P036 | P0 | OrganicParent | F30M | Command relationship state | BACKLOG | Permanent OOB ownership survives temporary tasking. |
| UE-P037 | P0 | CurrentCommandParent | F30M | Command relationship state | BACKLOG | Tactical parent can differ from OrganicParent and be restored. |
| UE-P038 | P1 | Command relationship lines | F27/F29D | Presentation component | BACKLOG | Selected formations show correct parent/subordinate connections. |
| UE-P039 | P1 | Stable entity IDs | Design B-131 | Data model | BACKLOG | OOB entities persist through save/transfer without duplicate identity. |
| UE-P040 | P1 | OOB aggregate status | F29Q/F30D | UMG OOB model | BACKLOG | Parent rows calculate correct subordinate strength/status. |

# 4. Orders, authority and execution state — U03

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P050 | P0 | Order component/core | F18+ | `UStrategyOrderComponent` | SCAFFOLD | One authoritative current order + execution state per command entity. |
| UE-P051 | P0 | MOVE | Unity movement baseline | Order executor | BACKLOG | Selected command entity moves to assigned point. |
| UE-P052 | P0 | ANGRIB HER | F18/F29E/F30S | Attack executor | SCAFFOLD | Finite attack mission reaches assigned attack slots and does not chase forever. |
| UE-P053 | P0 | FORSVAR HER | F29Y/F30U | Defend executor | SCAFFOLD | Defensive mission owns formation/HQ geometry until replaced. |
| UE-P054 | P0 | HOLD/STOP | Command HUD baseline | Hold executor | SCAFFOLD | Movement cancels/stops without corrupting standing state. |
| UE-P055 | P0 | Click-drag facing | F30P | Order target/facing data | BACKLOG | Player-committed facing is mission data before slot generation. |
| UE-P056 | P0 | Direct player authority | F18 | Authority model | SCAFFOLD | Direct order overrides inherited officer AI for its scope. |
| UE-P057 | P0 | Standing intent vs execution | F30S/W | Order state model | BACKLOG | Persistent intent and physical execution are independent states. |
| UE-P058 | P0 | Active order colour lifecycle | F30Q/S/W | UMG command state | BACKLOG | Red=no active execution, blue=pending/executing, green=toggle/state. |
| UE-P059 | P0 | Higher order propagation | F27/F30M | Command planner | BACKLOG | Division/Brigade/Regiment/Major correctly generate subordinate missions. |
| UE-P060 | P0 | Re-issue/supersede | F30V/U | Order lifecycle | BACKLOG | New mission cleanly replaces previous destinations and stale executors. |
| UE-P061 | P1 | RYK FREM | F30Q | Order executor | BACKLOG | Movement active only while executors remain. |
| UE-P062 | P1 | TILBAGETRÆK | F30Q | Order executor | BACKLOG | Coordinated withdrawal maintains command ownership. |
| UE-P063 | P1 | SAML | F30Q | Order executor | BACKLOG | Formation assembles at valid slots and completion is physical. |
| UE-P064 | P1 | Mission target visuals | F30M/O | Presentation subsystem | BACKLOG | Objective circle/label/ghosts show authoritative mission state only. |

# 5. Infantry formations and movement — U04

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P070 | P0 | Formation component | F6+ | `UFormationComponent` | BACKLOG | Formation owns slot geometry separate from command/order state. |
| UE-P071 | P0 | Three-rank Line | Infantry baseline | Formation generator | BACKLOG | Full company forms documented three-rank line geometry. |
| UE-P072 | P0 | March Column | F6/F30K | Formation generator | BACKLOG | Long movement selects march column without parent tug-of-war. |
| UE-P073 | P0 | Physical reform | Formation baseline | Formation movement | BACKLOG | Soldiers/slots move into new geometry rather than teleporting. |
| UE-P074 | P0 | Auto Line before combat | F30X | Formation policy | BACKLOG | Approaching enemy triggers physical Line reform before fire envelope. |
| UE-P075 | P0 | Deploy threshold | F30X | Formation policy | BACKLOG | `max(own range, enemy range)+35m` policy is reproduced/tunable. |
| UE-P076 | P0 | Company nominal spacing | F30X | Parent formation planner | BACKLOG | Full-company centres use ~72 m nominal spacing in current QA baseline. |
| UE-P077 | P0 | Reserved minimum spacing | F30X | Slot deconfliction | BACKLOG | ~68 m minimum reservation prevents company overlap at oblique facings. |
| UE-P078 | P0 | Final-slot authority | F30V | Movement completion service | BACKLOG | One 0.50 m company final-slot completion rule owns mission arrival. |
| UE-P079 | P0 | No premature stop | F30V | Movement completion | BACKLOG | Company cannot be marked arrived while still visibly short of slot. |
| UE-P080 | P1 | Formation facing completion | F30S/P | FormationMotion equivalent | BACKLOG | Final visual turn completes without per-frame facing tug-of-war. |
| UE-P081 | P1 | Parent mission reassert | F27/F30X | Mission executor | BACKLOG | Temporary interruption resumes same mission without forcing wrong formation. |
| UE-P082 | P1 | Full 190-man footprint | Infantry baseline | Formation data | BACKLOG | Formation width/spacing is compatible with ~48 m current full-strength frontage. |
| UE-P083 | P1 | Local sidestep/deconfliction | F29P | Formation movement | BACKLOG | Adjacent formations avoid standing through each other. |

# 6. Terrain, pathfinding, river and bridge routing — U05

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P090 | P0 | Formation-level navigation | Navigation V3 | Navigation service | BACKLOG | Formation path is authoritative; individual soldiers do not independently choose strategic paths. |
| UE-P091 | P0 | Hard water blocking | River baseline | Nav/terrain tags | BACKLOG | Units cannot walk through river except legal crossings. |
| UE-P092 | P0 | True bank-change detection | F30W | River route planner | BACKLOG | Bridge route is required only when destination changes river bank. |
| UE-P093 | P0 | Same-bank bank-follow | F30W | River route planner | BACKLOG | Straight chord crossing a river bend does not cause cross-and-return bridge trip. |
| UE-P094 | P0 | Bridge transaction | F9/F30H | Crossing state machine | BACKLOG | NearBank→FarBank→ExitBank→Direct persists until crossing completes. |
| UE-P095 | P0 | Goal changes during bridge | F30H | Crossing state machine | BACKLOG | New goal updates final destination but does not cancel active bridge transaction. |
| UE-P096 | P1 | Building avoidance | Navigation baseline | Nav obstacles | BACKLOG | Formations route around blocking buildings. |
| UE-P097 | P1 | Fence/obstacle handling | F29P/design | Nav/formation avoidance | BACKLOG | Obstacles affect movement without destroying formation authority. |
| UE-P098 | P1 | Bridge congestion/defile | F30H/X | Formation path policy | BACKLOG | Narrow crossing uses controlled narrow geometry and restores normal formation after exit. |
| UE-P099 | P2 | Expanded battlefield/hills | B-290 | Unreal Landscape | BACKLOG | Tactical QA map includes usable elevation/LOS test terrain. |

# 7. Fire control, LOS, combat and casualties — U06

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P110 | P0 | LOS query | F30S + combat baseline | Visibility subsystem | BACKLOG | Terrain/objects gate target awareness and firing. |
| UE-P111 | P0 | Fire cone | F8/F30S | Combat geometry | BACKLOG | Infantry firing eligibility respects ±35° forward cone where applicable. |
| UE-P112 | P0 | Close range policy | Fire baseline | Fire-control state | BACKLOG | Close policy selects/uses correct range. |
| UE-P113 | P0 | Medium range policy | Fire baseline | Fire-control state | BACKLOG | Medium policy selects/uses correct range. |
| UE-P114 | P0 | Long range policy | Fire baseline | Fire-control state | BACKLOG | Long policy selects/uses correct range. |
| UE-P115 | P0 | HOLD FIRE | Fire baseline/F29X | Fire-control state | BACKLOG | No volley is produced while hold fire owns firing authority. |
| UE-P116 | P0 | Active-range visual language | F30S/T/W | Debug/presentation | BACKLOG | Active band strong; other physical bands faint references. |
| UE-P117 | P0 | Enemy TEST cones from startup | F30W/X | QA overlay | BACKLOG | Living Prussian infantry cones visible immediately in TEST. |
| UE-P118 | P0 | QA cone != knowledge | F30W/S | Visibility/fire authority | BACKLOG | Debug cone never grants LOS, contact or firing authority. |
| UE-P119 | P0 | Volley/reload cycle | pre-F29 combat | Combat component | BACKLOG | Valid target triggers fire/reload cadence without phantom volleys. |
| UE-P120 | P0 | Hit/casualty resolution | battle baseline | Combat state | BACKLOG | Strength changes only from valid resolved combat. |
| UE-P121 | P1 | 0-hit volley | B-002/current design | Combat state | BACKLOG | Valid volley may resolve zero hits without false strength loss. |
| UE-P122 | P1 | Smoke feedback | F29X/Z | Niagara/combat events | BACKLOG | Smoke corresponds to real firing event; no fake smoke on formation changes. |
| UE-P123 | P1 | Morale/cohesion | battle baseline | Unit combat state | BACKLOG | Morale/cohesion are separate authoritative values and affect behaviour. |
| UE-P124 | P1 | Routed state | battle baseline | Unit combat state | BACKLOG | Routed unit leaves normal command/fire behaviour and presentation updates. |
| UE-P125 | P1 | Under-fire reaction | F26/F30V | Reaction component | BACKLOG | Temporary reaction may pause movement but cannot complete parent mission. |
| UE-P126 | P2 | Representative casualties | v00.00.08 | Visual subsystem | BACKLOG | Visual casualty feedback does not become authoritative casualty state. |

# 8. Infantry Square and anti-cavalry — U06

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P130 | P1 | Square formation geometry | F29 | Formation component | BACKLOG | Infantry physically reforms to square. |
| UE-P131 | P1 | Four square fire sectors | F29V/X/Z | Combat geometry | BACKLOG | Square fires only from eligible face/sector. |
| UE-P132 | P1 | Square visual ownership | F29X | Presentation | BACKLOG | Square outline replaces incompatible Line/Column footprint/fans. |
| UE-P133 | P1 | No formation phantom volley | F29X | Fire state machine | BACKLOG | Entering/readying Square does not produce smoke/0-hit volley. |
| UE-P134 | P1 | LOS-gated cavalry threat | F30S | Reaction AI | BACKLOG | Square/anti-CAV reaction requires actual LOS. |
| UE-P135 | P1 | Range/cone-gated CAV fire | F30S | Combat eligibility | BACKLOG | Cavalry must be inside LOS + selected range + valid sector/cone. |
| UE-P136 | P1 | Directional square smoke | F29Z | Niagara | BACKLOG | Smoke originates from the firing square face only. |

# 9. Officer AI and command hierarchy — U07

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P150 | P0 | Company/Captain AI | Officer AI baseline | AI controller/component | BACKLOG | Company executes delegated mission locally without overriding direct player authority. |
| UE-P151 | P0 | Major/Battalion AI | F27 | AI controller/component | BACKLOG | Major coordinates subordinate company slots/mission. |
| UE-P152 | P0 | Regiment AI | F28+ | AI controller/component | BACKLOG | Regiment delegates coherently to both battalions. |
| UE-P153 | P0 | Brigade AI | F30B/M | Higher AI | BACKLOG | Brigade delegates through Regiment. |
| UE-P154 | P0 | Division AI | F30B/M | Higher AI | BACKLOG | Division delegates through Brigade/Regiment/Majors. |
| UE-P155 | P0 | AI cascade ON | F30M | Command AI state | BACKLOG | Higher AI ON cascades to intended subordinate command chain. |
| UE-P156 | P0 | Direct order precedence | F18/F30M | Authority model | BACKLOG | Direct player mission beats inherited AI task. |
| UE-P157 | P1 | AI ON/OFF uniform state | F30W | OOB/AI state | BACKLOG | Division→Company+CAV all display only ON/OFF consistently. |
| UE-P158 | P1 | Mission completion | F30S/V/W | AI/order integration | BACKLOG | AI no longer chases/rearms after finite mission physically completes. |
| UE-P159 | P1 | HQ follow | F30P | HQ movement | BACKLOG | Major/Regiment/Brigade/Division HQs follow documented rear geometry. |
| UE-P160 | P1 | HQ depth conflict prevention | F30U | HQ goal authority | BACKLOG | Defend mission owner prevents competing HQ-depth goal writers. |
| UE-P161 | P1 | Command zones | F30P | Command visualization/component | BACKLOG | Selected HQ shows correct inner/outer command bands. |
| UE-P162 | P2 | Command-zone gameplay effects | design B-061/F30P | Command simulation | DEFERRED | Order delay/coordination effects implemented after parity presentation. |
| UE-P163 | P2 | Officer stats/personality | B-170/B-190 | AI decision data | BACKLOG | Shared player-delegated/enemy AI decision core uses officer data. |

# 10. Cavalry and dragons — U08

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P180 | P0 | Cavalry base actor | F30 | `ACavalryUnit` | SCAFFOLD | Mounted tactical entity has strength/order/command state. |
| UE-P181 | P1 | Mounted cavalry movement | F30 | Cavalry movement | BACKLOG | Mounted unit moves as formation with appropriate speed/state. |
| UE-P182 | P1 | 4-rank Line | F30H | Formation generator | BACKLOG | Normal mounted Line physically uses four ranks. |
| UE-P183 | P1 | 4-rank Charge | F30H | Formation generator | BACKLOG | Charge line uses four ranks. |
| UE-P184 | P1 | 4-abreast Column | F30H | Formation generator | BACKLOG | Normal mounted march column is four abreast. |
| UE-P185 | P1 | 2-abreast bridge/defile | F30H/X | Formation policy | BACKLOG | Narrow two-abreast geometry exists only near/on crossing. |
| UE-P186 | P1 | 36 m narrow-mode approach | F30X | Bridge formation policy | BACKLOG | Opposite-bank mission does not trigger two-abreast hundreds of metres early. |
| UE-P187 | P1 | Restore pre-bridge formation | F30H/X | Crossing state | BACKLOG | Formation used before narrow mode is restored after exit clearance. |
| UE-P188 | P1 | Physical cavalry reform | F30H | Formation movement | BACKLOG | Riders physically reform; charge speed respects incomplete reform. |
| UE-P189 | P1 | Cavalry charge/contact | F30/F30N | Cavalry combat | BACKLOG | Charge executes to valid contact without passing through enemy formation. |
| UE-P190 | P1 | Defensive cavalry reserve | F30S | Higher mission planner | BACKLOG | Attached CAV stays behind/outside supported battalion (~150m rear/~45m lateral QA baseline). |
| UE-P191 | P1 | Temporary attack attachment | F30M | Command-parent state | BACKLOG | Available CAV task-attaches to attacking Major A/B without changing OrganicParent. |
| UE-P192 | P1 | Travel-cost pairing | F30M | Task allocator | BACKLOG | Two-CAV/two-battalion allocation minimises crossing/travel cost. |
| UE-P193 | P1 | Return to prior command parent | F30M | Command-parent state | BACKLOG | CAV returns to previous parent/reserve after attack mission ends. |
| UE-P194 | P2 | Cavalry screen/opportunity AI | F30N | Cavalry AI | BACKLOG | CAV screens/repositions without unjustified autonomous charge. |
| UE-P195 | P2 | SPEJD HER | F30Q | Recon order | DEFERRED | Enabled only after true FOG/contact subsystem exists. |
| UE-P196 | P2 | Dragon dismount | F30J/S | Cavalry specialization | BACKLOG | Mounted Dragon splits into combat group + horse park/holders. |
| UE-P197 | P2 | Horse-holder anchor | F30J/S | Dismounted state | BACKLOG | Horse holders/horses stay at dismount anchor while combat group moves. |
| UE-P198 | P2 | STIG OP return/remount | F30S | Remount task | BACKLOG | Away-from-horses remount becomes return task then auto-remount. |
| UE-P199 | P2 | Dismounted Dragon fire control | F30R | Fire component | BACKLOG | Dragon firing respects policy, range, LOS and anchor state. |
| UE-P200 | P1 | Horse + rider separate rigs | Engine-port decision | Skeletal meshes + AnimBP | BACKLOG | Horse/rider are independent rigs synchronized at RiderSocket/saddle. |

# 11. OOB, HUD, semantic zoom and tactical presentation — U09

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P220 | P1 | Unified command HUD | F29C/G | UMG | BACKLOG | Relevant selected echelon receives one authoritative HUD. |
| UE-P221 | P1 | Company HUD | HUD baseline | UMG | BACKLOG | Unit info, state, orders and fire controls are readable. |
| UE-P222 | P1 | Major HUD | F29C | UMG | BACKLOG | Major orders/status/AI/subordinates shown. |
| UE-P223 | P1 | Regiment HUD | F29C/G | UMG | BACKLOG | Regiment mission/status/subordinates shown. |
| UE-P224 | P1 | Brigade HUD | F30M/N/O | UMG | BACKLOG | Brigade HUD has ENHEDSINFO/AI, ORDRER/MISSION, UNDERLAGTE/STATUS/ATTACHMENT. |
| UE-P225 | P1 | Division HUD | F30M/N/O | UMG | BACKLOG | Division HUD reaches same higher-command parity. |
| UE-P226 | P1 | Cavalry HUD | F30H/I/N | UMG | BACKLOG | CAV uses shared visual language with cavalry-specific controls/state. |
| UE-P227 | P1 | OOB panel | F29Q/F30D | UMG tree/list | BACKLOG | Full Division→CAV hierarchy is scrollable/selectable. |
| UE-P228 | P1 | OOB AI status | F30W | UMG | BACKLOG | All echelons show consistent ON/OFF. |
| UE-P229 | P1 | OOB attachment state | F30D/M | UMG | BACKLOG | Organic/current/task attachment is readable. |
| UE-P230 | P1 | OOB cavalry drag/drop | F30D | UMG drag/drop | BACKLOG | Valid tactical attachment can be changed via OOB without corrupting OrganicParent. |
| UE-P231 | P1 | Semantic zoom states | F29D | Presentation subsystem | BACKLOG | Close/Medium/Operational/Strategic presentation changes without simulation changes. |
| UE-P232 | P1 | HQ semantic counters | F29D/Q | UMG/world overlay | BACKLOG | Major/Regiment/Brigade/Division counters appear at correct zoom importance. |
| UE-P233 | P1 | Company semantic counters | F29D | UMG/world overlay | BACKLOG | Operational/strategic view presents company state. |
| UE-P234 | P1 | Cavalry semantic counters | F30H | UMG/world overlay | BACKLOG | Cavalry gets appropriate counter/name/echelon. |
| UE-P235 | P1 | NATO echelon symbols | F29D/F30H | Presentation data | BACKLOG | Company I, Battalion II, Regiment III, Brigade X, Division XX. |
| UE-P236 | P1 | Strategic mesh suppression | F29D | Presentation LOD | BACKLOG | Far zoom hides tactical meshes while simulation/colliders/command state continue. |
| UE-P237 | P1 | Selected HQ command circles | F30P | World visualization | BACKLOG | Only selected HQ level exposes detailed command range rings. |
| UE-P238 | P1 | Route/destination visuals | F30M/O | World visualization | BACKLOG | Higher selection exposes subordinate authoritative routes/slots without duplicate writers. |
| UE-P239 | P1 | Hover info infantry | battle UI baseline | Hover subsystem | BACKLOG | Unit type/strength/loss/morale/order data appears. |
| UE-P240 | P1 | Hover info cavalry | F30S | Hover subsystem | BACKLOG | CAV hover includes mounted state, formation, AI/order, parent and Dragon fire data. |

# 12. Visual assets and animation parity

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P250 | P0 | Infantry source FBX | imported Unity asset | UE Skeletal Mesh | SOURCE PRESENT | Source asset remains preserved for Unreal import/retarget. |
| UE-P251 | P1 | Infantry skeletal import | current infantry rig | UE Skeleton/SkeletalMesh | BACKLOG | Soldier imports with valid materials/skeleton/scale. |
| UE-P252 | P1 | Infantry locomotion | Unity walk/march | AnimBP | BACKLOG | Idle/march/reform movement visually tracks formation movement. |
| UE-P253 | P1 | Infantry firing | combat visuals | AnimBP/Montage | BACKLOG | Fire animation aligns with authoritative volley event. |
| UE-P254 | P2 | Infantry visual fidelity | B-300 | Mesh/material pass | BACKLOG | Soldier silhouette is recognisably armed infantry at tactical zoom. |
| UE-P255 | P1 | Horse skeletal import | horse project asset | Horse Skeleton | BACKLOG | Quadruped skeleton and mesh function independently of rider. |
| UE-P256 | P1 | Horse gait states | F30L | Horse AnimBP | BACKLOG | Walk/trot/charge states correspond to cavalry movement state. |
| UE-P257 | P1 | Rider skeletal import | cavalry asset plan | Rider Skeleton | BACKLOG | Rider remains separate human rig. |
| UE-P258 | P1 | Rider/horse synchronization | Engine-port decision | RiderSocket + state sync | BACKLOG | Rider stays aligned with saddle and gait. |
| UE-P259 | P2 | Dismount/remount animation | F30J/S | Montage/state machine | BACKLOG | Visual transition matches authoritative mounted state. |
| UE-P260 | P2 | Regimental standards/flag system | B-104/B-310 | Flag pole + dynamic cloth/material | BACKLOG | Standard pole is reusable and flag identity/data can vary per unit. |
| UE-P261 | P2 | 1864 cavalry equipment | approved asset direction | Skeletal/static attachments | BACKLOG | Saddle/tack/rifle+bayo/sabre/standard can be equipped independently. |

# 13. Tactical time, scenario flow and baseline utilities

| ID | Pri | Feature | Unity/reference | Unreal target | Status | Exit / parity criterion |
|---|---:|---|---|---|---|---|
| UE-P270 | P1 | Pause | Unity tactical baseline | Game speed subsystem | BACKLOG | Player can pause tactical singleplayer without corrupting AI/orders. |
| UE-P271 | P1 | 1x/2x/3x | Unity tactical baseline | Game speed subsystem | BACKLOG | Time controls behave consistently for movement, combat and AI timers. |
| UE-P272 | P1 | Victory/defeat state | Unity tactical baseline | Scenario subsystem | BACKLOG | Battle can resolve and expose outcome state. |
| UE-P273 | P1 | Restart/test reset | Unity tactical baseline | Scenario subsystem | BACKLOG | QA battle can restart to deterministic baseline. |
| UE-P274 | P2 | Deterministic QA seed | B-133 | Scenario/debug subsystem | BACKLOG | Repeat tests can reproduce critical movement/combat situations. |

# 14. U10 — tactical replacement parity gate

These are **release gates**, not separate optional polish items.

| ID | Pri | Gate | Required result |
|---|---:|---|---|
| UE-G001 | P0 | U01 controls | Camera + click + marquee + modifiers build and pass runtime QA. |
| UE-G002 | P0 | Command hierarchy | Division→Brigade→Regiment→Major→Company + CAV parentage works. |
| UE-G003 | P0 | Order authority | Player/higher-AI authority and pending/executing/standing states do not conflict. |
| UE-G004 | P0 | Infantry movement | March Column, Line, early deploy, spacing and final-slot completion pass. |
| UE-G005 | P0 | River/bridge | Same-bank routes avoid bridge round-trip; true bank changes use bridge. |
| UE-G006 | P0 | Enemy cones | QA cones visible at battle start but provide no knowledge/fire authority. |
| UE-G007 | P0 | Fire eligibility | LOS + range + cone determine valid fire. |
| UE-G008 | P0 | Square/CAV | Square reaction and fire do not bypass LOS/range/sector rules. |
| UE-G009 | P0 | Cavalry crossing | CAV stays normal formation far away and switches to 2-abreast only near bridge. |
| UE-G010 | P0 | Order completion | Parent button stays active until actual final-slot/HQ/CAV execution ends. |
| UE-G011 | P0 | Selection persistence | Higher HQ remains selected through target/facing order commit. |
| UE-G012 | P1 | OOB/HUD | AI status, hierarchy, current command parent and active order state are consistent. |
| UE-G013 | P1 | Higher HQ geometry | Brigade/Division follow, facing and command zones reproduce documented behaviour. |
| UE-G014 | P1 | Semantic zoom | Presentation changes do not alter simulation state. |
| UE-G015 | P1 | Full battle regression | Combined battle scenario runs without critical regression relative to Unity baseline. |

## Current migration order

1. **Finish U01 build/runtime verification** — UE-P010 through UE-P018.
2. **U02 hierarchy/data** — UE-P030 through UE-P040.
3. **U03 order authority** — UE-P050 through UE-P064.
4. **U04 infantry formations/movement** — UE-P070 through UE-P083.
5. **U05 navigation/river/bridge** — UE-P090 through UE-P099.
6. **U06 combat/LOS/Square** — UE-P110 through UE-P136.
7. **U07 officer AI/HQ** — UE-P150 through UE-P163.
8. **U08 cavalry/dragons** — UE-P180 through UE-P200.
9. **U09 OOB/HUD/semantic zoom** — UE-P220 through UE-P240.
10. **Assets/animation parity** in parallel where it does not block simulation.
11. **U10 parity gate** — UE-G001 through UE-G015.

## Definition of Unreal tactical parity

The Unreal port reaches tactical parity only when:

- all **P0** items required by UE-G001..UE-G011 are **PARITY VERIFIED**;
- all **P1** items required by UE-G012..UE-G015 are either **PARITY VERIFIED** or explicitly documented as non-blocking presentation differences;
- the full U10 battle regression has been recorded against the current Unity reference;
- no Unreal system silently simplifies established command authority, formations, navigation, LOS/fire eligibility, cavalry behaviour, OOB semantics or execution completion.

After that gate, Unreal may become the active tactical implementation and Unity can move from behavioural reference to archived/reference status.
