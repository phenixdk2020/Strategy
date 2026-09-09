# PROJECT 1864 — Army / OOB reference 1864

**Status:** Working historical reference for game design  
**Primary use:** OOB Designer, campaign unit database, tactical spawning, manpower, HQ hierarchy, flags, cavalry and artillery  
**Scope:** Denmark and Prussia in the Second Schleswig War / 1864 setting

> This document consolidates the historical working information gathered for PROJECT 1864. It is intended as a design/reference dataset, not as a claim that every number is final source-locked historical canon. Date-specific strengths, exact regimental lineage, mottoes and some flag details must remain editable in data.

---

## 1. Core OOB hierarchy used by PROJECT 1864

The game should support the full hierarchy:

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
            └── Train / support
```

Not every nation must use every echelon identically. The data model must therefore store real parent/child relationships and must not infer structure only from unit numbering.

### Permanent rule

`Company.ParentBattalionID`, `Battalion.ParentRegimentID`, `Regiment.ParentBrigadeID`, etc. must be explicit stable relations. Company number alone must never be used to calculate the parent battalion because historical sources may show reorganisations or non-obvious numbering.

---

# 2. Overall forces in 1864

Working figures used for game scale:

- Danish field army at the beginning of the war: approximately **38,800 men**.
- Prussian force committed against Denmark: approximately **38,400 men**.
- Austrian force in the allied invasion force: approximately **23,000 men**.
- Initial Prussian + Austrian invasion force therefore approximately **61,000 men**.

These figures are not the total military potential of Prussia. Prussia could mobilise substantially more than the forces directly committed to the Danish campaign.

For PROJECT 1864 this is important because most tactical battles will involve only hundreds or a few thousand men. Battles approaching Waterloo-scale concentrations should be exceptional rather than the normal tactical workload.

---

# 3. Denmark — infantry structure

## 3.1 Standard hierarchy

Working 1864 Danish structure:

```text
Regiment
├── I Battalion
│   ├── Company
│   ├── Company
│   ├── Company
│   └── Company
└── II Battalion
    ├── Company
    ├── Company
    ├── Company
    └── Company
```

General design baseline:

- **1 regiment = 2 battalions**
- **1 battalion = 4 companies**
- **1 regiment = 8 companies**
- **1 brigade = normally 2 regiments** in the working 1864 field OOB examples

## 3.2 Typical strengths

| Echelon | Typical working strength |
|---|---:|
| Platoon / deling | ca. 40–50 |
| Company | ca. **180–220** |
| Battalion | ca. **750–900** |
| Regiment | ca. **1,500–1,700** |
| Brigade | ca. **3,000–3,500** |
| Division | ca. **7,000–10,000** |
| Danish field army | ca. **38,800** |

A Danish infantry regiment of approximately **1,600 men** is therefore a good generic full-strength campaign value, while the actual `PresentStrength` must be date/scenario specific.

## 3.3 Example Danish regiment

Illustrative game structure:

```text
3. Infantry Regiment
Regimental staff: 25

├── I Battalion — 812
│   ├── 1 Company — 204
│   ├── 2 Company — 201
│   ├── 3 Company — 198
│   └── 4 Company — 209
│
└── II Battalion — 784
    ├── 5 Company — 196
    ├── 6 Company — 203
    ├── 7 Company — 191
    └── 8 Company — 194

TOTAL: 1,621
```

This is a design example. The important principle is that manpower is stored down to company level, while regiment strength is calculated from subordinate states plus staff/support.

---

# 4. Danish field brigade working OOB

The collected working OOB around early February 1864 includes the following brigade pairings:

| Brigade | Infantry regiments |
|---|---|
| 1st Brigade | 2nd + 22nd Regiment |
| 2nd Brigade | 3rd + 18th Regiment |
| 6th Brigade | 5th + 10th Regiment |
| 7th Brigade | 1st + 11th Regiment |
| 8th Brigade | 9th + 20th Regiment |
| 9th Brigade | 19th + 21st Regiment |

These pairings are particularly useful for the OOB Designer because the game should be able to load a date-specific historical OOB rather than inventing brigade composition at tactical battle start.

### Current historical pilot

A clean Danish brigade pilot for tactical testing is:

```text
7th Brigade
├── Brigade HQ
├── 1st Infantry Regiment — working strength ca. 1,540
└── 11th Infantry Regiment — working strength ca. 1,584
```

Combined infantry is therefore approximately **3,124 men** before brigade staff and attachments.

The previously used QA pair `1st Regiment + 5th Regiment` must not be presented as one historical 1864 brigade.

---

# 5. Prussia — infantry structure

## 5.1 Standard hierarchy

Working Prussian infantry regiment:

```text
Infantry Regiment
├── I. Musketier-Bataillon
│   └── 4 companies
├── II. Musketier-Bataillon
│   └── 4 companies
└── III. / Füsilier-Bataillon
    └── 4 companies
```

General baseline:

- **1 regiment = 3 battalions**
- **1 battalion = 4 companies**
- **1 regiment = 12 companies**

## 5.2 1864 campaign strength

A planned Prussian battalion could be around 1,000 men, but the working 1864 campaign figure gathered for the game is approximately:

- 22 officers
- 56 NCOs
- 17 musicians
- 729 enlisted men
- total approximately **824 men**

This produces the following useful game baseline:

| Echelon | Working 1864 strength |
|---|---:|
| Company | ca. **190–210** |
| Battalion | ca. **820–824** |
| Regiment | ca. **2,400–2,500** |
| Brigade | ca. **4,800–5,000** |
| Division | ca. **11,000–13,000** |
| Army corps | ca. **23,000–27,000** |

The national structural difference should be visible on the battlefield:

- Danish regiment: roughly **1,600 men / 8 companies / 2 battalions**.
- Prussian regiment: roughly **2,450 men / 12 companies / 3 battalions**.

Battalions remain broadly comparable in size; the larger Prussian regiment mainly comes from the extra battalion.

## 5.3 Example Prussian regiment

```text
24th Brandenburg Infantry Regiment

├── I Battalion — 817
│   ├── 1 Company
│   ├── 2 Company
│   ├── 3 Company
│   └── 4 Company
│
├── II Battalion — 828
│   ├── 5 Company
│   ├── 6 Company
│   ├── 7 Company
│   └── 8 Company
│
└── Füsilier Battalion — 805
    ├── 9 Company
    ├── 10 Company
    ├── 11 Company
    └── 12 Company

Regimental staff: ~25
TOTAL: ~2,475
```

The appearance of 9th–12th companies in Prussian regiments during the Dybbøl operations supports using a 12-company structure in the game.

---

# 6. Date-dependent strength

A unit must have persistent identity independent of its current strength.

Minimum manpower fields:

```text
AuthorizedStrength
PresentStrength
CombatEffective
Date
```

Recommended extension:

```text
Killed
Wounded
Missing
Captured
Sick
Detached
Hospitalized
AvailableForDuty
```

The same regiment may therefore exist through an entire campaign while its current manpower, ammunition, officers, horses and equipment change after every march and battle.

Company strength must persist between tactical battle and campaign map.

---

# 7. Regimental identity, names and traditions

The database must separate official contemporary identity from later or inherited traditions.

Required fields:

```text
OfficialName1864
TraditionalName
HistoricalLineage
FoundingYear
UnitType
ParentFormationID
```

Example design principle:

```text
OfficialName1864: 5. Infanteri-Regiment
TraditionalName: Sjællandske Livregiment
```

The game UI may display both, but it must not silently substitute a later traditional name for the official 1864 field designation.

Suggested UI:

```text
5. Infanteri-Regiment
Tradition: Sjællandske Livregiment
```

The same model can support names such as Danske Livregiment and other inherited traditions once each lineage is source-locked.

---

# 8. Flags / standards

## 8.1 Danish full regimental colours

Working description from the project material:

- square Dannebrog-based field colour
- royal cipher / royal identity
- Roman numeral representing the original battalion/regimental identity
- older battalion colours could continue into reorganised regiments
- a full colour may be associated primarily with the first battalion
- in combat the colour was not necessarily always carried fully unfurled; rolled state must be supported

Required data fields:

```text
FlagType
NationalIdentity
RoyalCipher
RomanNumeral
CarriedByBattalionID
BattleFlagState
TraditionStreamer
```

`BattleFlagState` should support at least:

```text
Unfurled
Rolled
Cased
Lost
Captured
Damaged
```

## 8.2 National identity vs regiment identity

The tactical visual design may use:

1. a national / army identification standard at HQ,
2. a regiment-specific standard at HQ,
3. smaller company readability markers only when needed by gameplay.

Company markers/guidons used in QA must not automatically be treated as historically accurate full company colours.

---

# 9. Regimental HQ and Brigade HQ

## 9.1 Regimental HQ

Preferred tactical visual representation:

- **3 mounted figures**
  - regimental commander
  - adjutant
  - orderly / staff rider

These are not three additional men on top of manpower. They visually represent members already included in regimental staff.

Regimental HQ is a **command entity**, not the firing point of the regiment.

## 9.2 Brigade HQ

Preferred visible brigade HQ:

- brigade commander mounted
- adjutant / chief staff officer
- additional staff officers
- couriers/orderlies
- optional bugler/signaller/escort
- approximately **6–8 visible figures** as a readability abstraction

Brigade HQ is the main tactical objective-command layer for the intended game style.

---

# 10. Officer command philosophy

The player should normally give higher-level intent rather than direct movement coordinates to every company.

Examples:

```text
DEFEND AREA
ATTACK AREA
CAPTURE OBJECTIVE
HOLD
WITHDRAW
SCREEN
SUPPORT
```

The officer AI evaluates:

- enemy estimate
- terrain
- frontage
- own strength
- morale/cohesion
- fatigue
- ammunition
- nearby support
- flanks
- objective importance
- reserve requirement
- commander stats/doctrine

Subordinates receive roles such as:

```text
ENGAGED
SUPPORT
RESERVE
MANOEUVRE
WITHDRAW
```

A brigade commander may initially commit one regiment and retain another in reserve. If the first attack stalls or suffers unacceptable losses, the reserve may be committed.

This principle later continues to battalion/company assignment under regimental officers.

---

# 11. Cavalry / dragoons

Cavalry strength must not be represented by manpower alone.

Required fields:

```text
PersonnelPresent
HorsesAuthorized
HorsesAvailable
MountedEffective
DismountedEffective
HorseFatigue
```

Working Danish cavalry figures gathered for the project:

- squadron roughly **120 enlisted / approximately 130–140 total personnel** in a reduced wartime state
- three-squadron half-regiment roughly **400**
- full six-squadron regiment roughly **800**

Horse shortages must directly reduce mounted effectiveness.

### Dragoon roles

- mounted movement
- reconnaissance
- screening/counter-recon
- flank security
- courier escort/support
- pursuit
- raids against supply/courier routes
- dismounted fire/combat

Dismount creates a real tactical horse-holder state. Remount takes time and may be reduced by lost/scattered horses or holders.

---

# 12. Artillery

## 12.1 Danish field battery working baseline

The collected Danish 1864 working figure is:

- **8 guns**
- **4 gun divisions/sections of 2 guns each**
- approximately **190 personnel** total

Not all 190 should be shown as men directly standing on the gun line. The unit includes:

- gun crews / artillerymen
- officers/NCOs
- ammunition service
- drivers
- horse handlers
- train personnel
- ammunition wagons / caissons
- horses

Recommended structure:

```text
Field Battery
├── Battery HQ
├── 1st Gun Division — 2 guns
├── 2nd Gun Division — 2 guns
├── 3rd Gun Division — 2 guns
├── 4th Gun Division — 2 guns
├── Ammunition service
├── Train
├── Horses
└── Wagons / caissons
```

Required data fields:

```text
GunsAuthorized
GunsOperational
GunType
AmmunitionByType
CrewPresent
HorsesAvailable
WagonsAvailable
LimberedState
```

The former 6-gun/120-man Danish prototype battery is only a QA placeholder and must not be considered the Danish historical baseline.

## 12.2 Prussian artillery

Prussian battery structure must remain editable/QA until a date-specific historical baseline is source-locked for the intended scenario.

---

# 13. Tactical 1:1 visual scale

PROJECT 1864 intends:

```text
1 real soldier in manpower
=
1 visible soldier when tactical LOD permits
```

This does **not** mean one full Unity GameObject/MonoBehaviour/Animator/NavMeshAgent per soldier.

Simulation remains echelon-based:

```text
Brigade
→ Regiment
→ Battalion
→ Company
```

Visible soldiers are rendering instances driven by formation state.

Performance targets:

| Visible soldiers | Engineering target |
|---:|---|
| 5,000 | easy baseline |
| 10,000 | normal large battle |
| 20,000 | target at good frame rate |
| 40,000 | upper normal/stress design target |
| 60,000+ | stress/specialised rendering |

Most historical battles in the campaign are expected to be much smaller than 20,000–40,000, making 1:1 rendering practical if implemented with instancing, LOD and culling.

---

# 14. OOB Designer requirements

The OOB Designer must show organisational hierarchy independently from geographic position.

## 14.1 OOB-tree drag/drop

Dragging a unit inside the OOB tree changes its command relationship.

Example:

```text
5. Regiment
1st Brigade → 2nd Brigade
```

Effects:

- parent HQ changes
- command chain changes
- courier/report relationship changes
- reserve/support relationship changes

It does **not** teleport the regiment on the campaign map.

## 14.2 Campaign-map drag/drop

Dragging a formation on the campaign map creates a movement/march order.

It must calculate or store:

```text
Origin
Destination
Route
Departure
ETA
Fatigue
Road usage
Bridge constraints
Supply impact
Command delay
```

Dragging a whole HQ can offer:

- Move HQ only
- March formation with subordinates
- Concentrate formation at destination
- Move command post while retaining current missions

Subordinate units remain separate formations and may have different routes and arrival times.

---

# 15. Campaign ↔ tactical continuity

The tactical battle must use the same units that exist on the campaign map.

No artificial fresh OOB should be generated when a battle begins.

Persist:

- Unit IDs
- command chain
- officers
- manpower down to company
- casualties
- ammunition
- morale/cohesion
- fatigue
- horses
- operational guns
- position
- marching status
- attachments
- supply state

A regiment that is still kilometres away when battle starts must arrive later or not participate.

---

# 16. Recommended unit database fields

Minimum persistent unit record:

```text
UnitID
OfficialName1864
TraditionalName
HistoricalLineage
FoundingYear
Nation
UnitType
ParentFormationID
Echelon

AuthorizedStrength
PresentStrength
CombatEffective

BattalionCount
CompanyCount

HorsesAuthorized
HorsesAvailable
MountedEffective

GunsAuthorized
GunsOperational

Morale
Experience
Cohesion
Discipline
Fatigue
Supply
Ammo

CommanderID
OfficerState
Mission
Role

RegimentalColour
Motto
BattleHonours
Traditions

CampaignPosition
CurrentRoute
ETA
```

Motto, battle honours and traditions must be optional and sourceable; absence of verified data must not block unit creation.

---

# 17. Game-design consequences of Danish vs Prussian structure

The different regiment sizes should matter without becoming a simplistic strength multiplier.

A larger Prussian regiment may have:

- wider potential frontage
- more companies/battalions
- larger reserve options
- more manpower resilience

But combat effectiveness must also depend on:

- weapons
- officer quality
- training
- experience
- fire discipline
- morale
- cohesion
- fatigue
- terrain
- formation
- ammunition
- command delay

A 2,450-man regiment must not automatically be treated as 1.5× as effective as a 1,600-man regiment in every situation.

---

# 18. Historical data status conventions

Every historical database record should carry a data-confidence state:

```text
SOURCE_LOCKED
WORKING
QA_PLACEHOLDER
INFERRED
UNKNOWN
```

Use cases:

- exact date-specific regiment strength from a primary/strong secondary source → `SOURCE_LOCKED`
- generic ~1,600 Danish regiment used before exact figure is found → `WORKING`
- 8th + 18th Prussian brigade pairing created only for a test → `QA_PLACEHOLDER`
- inferred staff number → `INFERRED`

This prevents prototype values from silently becoming historical canon.
