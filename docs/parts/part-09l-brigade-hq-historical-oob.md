# PROJECT 1864 — 09l Brigade HQ, historical OOB and dual standards

## Purpose

09l moves the tactical pilot from isolated regiments toward a real command hierarchy. The primary Danish reference is the documented 7. Brigade structure around 1 February 1864, while the Prussian side remains an explicitly labelled QA formation until a matching date-specific brigade source is locked.

## Danish 7. Brigade pilot

- 7. Brigade HQ
- 1. Infanteri-Regiment — approx. 1,540 men
- 11. Infanteri-Regiment — approx. 1,584 men
- attached cavalry pilot
- attached field battery pilot

The 09k renderer still uses a stable technical key for its second Danish slot; 09l separates technical key from historical display identity. 09m replaces this prototype bridge with stable Unit IDs that are independent of display name and current parent.

## Brigade HQ

A Brigade HQ is a physical command entity distinct from regimental HQ. The first visual model uses six mounted figures:

1. brigade commander,
2. adjutant,
3. staff officer,
4. staff officer,
5. courier,
6. courier.

The HQ becomes the source/destination for later courier, command-delay, report and command-effectiveness systems. Losing or displacing the HQ must eventually create friction rather than merely removing a cosmetic marker.

## Command hierarchy

The tactical hierarchy in this gate is:

`Brigade HQ -> Regiment HQ -> Battalion -> Company`

The player should normally give the Brigade commander a mission/objective rather than micro-ordering every company. Regiment/Battalion/Company AI allocates local frontage, reserve, support and engagement according to command intent, officer skill, terrain and known enemy state.

## Brigade missions

First explicit mission states:

- Hold
- Defend Area
- Attack / Capture Area

Future additions:

- Delay
- Withdraw
- Screen
- Support
- Reserve
- March / Concentrate

## Regiment commitment roles

- Reserve
- Support
- Engaged
- Manoeuvre
- Withdraw

The commander should not automatically commit every subordinate at battle start. The first 09l model commits one regiment and holds the second as reserve. Reserve commitment may occur when the engaged regiment routes, suffers major strength/morale/cohesion deterioration, or an attack fails to make useful progress.

This is the first prototype of the intended rule:

**Player gives the objective; commander decides how many subordinate formations to commit.**

## Officer AI relationship

Brigade AI must not write `Regiment.destination` directly. It assigns missions through the existing `OfficerAIController` layer. The subordinate regiment officer remains responsible for local execution and navigation.

Danish Brigade AI starts OFF for manual-player QA. F11 toggles it. Prussian QA Brigade AI starts ON.

## Unit identity model

Every historical unit should eventually carry at minimum:

- StableUnitId
- OfficialName1864
- TraditionalName
- HistoricalLineage
- ParentFormationId
- AttachedToId
- AuthorizedStrength
- PresentStrength
- CombatEffective
- Battalion/Company graph
- RegimentalColour / flag identity
- BattleHonours / traditions as date-valid data

Official 1864 identity and later tradition are separate fields. Example:

- `OfficialName1864 = 5. Infanteri-Regiment`
- `TraditionalName = Sjællandske Livregiment`

A later tradition must not silently replace the 1864 official unit name in historical OOB data.

## Dual standards

Each infantry regiment gets two visible identity standards in 09l:

### National / army flag

Provides immediate side/national recognition.

### Regimental flag

Carries the regiment-specific identity. For Danish units the visual direction remains Dannebrog-based and can show the Roman numeral associated with the regiment/colour. Traditional identity is shown separately as data/label/streamer rather than falsely presenting a later tradition banner as the official 1864 field colour.

Examples supported by the data model:

- 1. Infanteri-Regiment — Roman I — tradition: Danske Livregiment
- 5. Infanteri-Regiment — Roman V — tradition: Sjællandske Livregiment
- 11. Infanteri-Regiment — Roman XI — Falstersk/Jysk lineage label in the current working data

Older one-flag prototype objects are hidden, not destroyed, during 09l.

## Cavalry state

Cavalry manpower and horses are separate values:

- Personnel
- HorsesAvailable
- MountedEffective

`MountedEffective <= min(Personnel, HorsesAvailable)`.

A unit may therefore have additional personnel but fewer men capable of mounted action. Dismounted/unhorsed personnel remain part of the unit state rather than disappearing.

## Artillery state

The Danish field-battery pilot uses the current historical working baseline:

- 8 guns
- about 190 personnel
- conceptually 4 sections/platoons of 2 guns

Future state must also include horses, limbers, ammunition wagons, operational guns, crew availability and ammunition type/quantity. Not all battery personnel belong physically at the gun line.

## 1:1 rendering rule

09l retains 09k's 1:1 visual target but does not simulate every soldier as an independent actor. Ordinary infantry soldiers are GPU/instanced visual entities under company/regiment state; no individual MonoBehaviour, NavMeshAgent, collider or autonomous combat AI is required.

Company/Battalion/Regiment remain the authoritative simulation and command layers.

## Relationship to 09m OOB Designer

The OOB Designer uses the same hierarchy/data graph. Organizational drag/drop changes parent/attachment, while map drag creates campaign movement orders. Stable Unit IDs are required before final OOB editing so historical display names can change without breaking renderer or save references.

09l therefore intentionally exposes the remaining technical/display-name coupling in the tactical prototype and defines it as a temporary migration item rather than expanding that coupling further.
