# PROJECT 1864 — OOB Designer & Campaign Command Map

## Status

Planned campaign/command feature following the 09k full-scale OOB pilot and the planned 09l brigade command/reserve AI gate.

## Purpose

The OOB Designer is the player's organizational editor and command overview for armies on the campaign layer. It must allow reorganization of the command chain without confusing organizational changes with physical movement.

The core invariant is:

**Drag/drop in the OOB tree changes organization. Drag/drop on the campaign map creates physical movement orders.**

A unit must never teleport merely because its parent HQ changes.

## OOB hierarchy

The data model supports:

`Army -> Corps -> Division -> Brigade -> Regiment -> Battalion -> Company`

Not every nation or scenario must use every level. The hierarchy is data-driven and date-valid.

Special arms and support may be either organic or attached assets, including:

- cavalry / dragoons,
- artillery batteries,
- engineers/pioneers,
- supply trains,
- medical assets,
- detached battalions/companies,
- temporary task forces.

Every entity uses a stable Unit ID independent of display name and current parent.

## OOB Designer layout

Recommended first UI:

- **Left:** collapsible OOB tree.
- **Center/right:** campaign map.
- **Inspector:** selected unit/HQ details, commander, strength, readiness, supply, current order and command link.
- **Filter/search:** arm, echelon, nation, location, commander, status.

Tree rows should show at minimum:

- unit name,
- echelon/type icon,
- commander,
- current/effective strength,
- readiness/fatigue,
- supply state,
- campaign location/contact status,
- current mission/order state.

## Organizational drag/drop

Dragging a node inside the OOB tree may:

- re-parent a regiment to another brigade,
- attach/detach artillery or dragoons,
- move a battalion between regiments where historically/data-valid,
- form or dissolve temporary task forces,
- reorder sibling units for UI/readability without changing combat values.

The system must validate:

- legal parent echelon,
- national/command restrictions,
- date/scenario restrictions,
- maximum practical command span,
- whether the target HQ is active/alive/available,
- whether attachment is permanent or temporary.

A reorganization changes `ParentId` / `AttachedToId` and command routing only. It does **not** change the unit's campaign coordinates.

If a unit is transferred while geographically separated from its new HQ, the UI should show reduced command effectiveness / detached status until physical communication or concentration is re-established.

## Campaign-map drag/drop

Dragging a friendly map entity from its current campaign position to another location creates a movement order, not an instant position change.

The generated movement order contains at minimum:

- issuing HQ,
- recipient,
- destination / objective area,
- proposed route,
- march stance,
- priority,
- constraints,
- estimated departure/arrival,
- command/order delay.

The unit then moves over campaign time using roads, bridges, terrain, congestion, weather, fatigue and logistics.

### Dragging an HQ

Dragging a Brigade/Division/Corps HQ should open a small command choice rather than blindly moving everything:

- **Move HQ only**
- **March formation with subordinates**
- **Concentrate formation at destination**
- **Move command post while units retain current missions**

For `March formation with subordinates`, each subordinate receives its own march order/route and retains physical separation. Units do not become one giant map object.

Attached assets can be included/excluded individually before confirming the march.

## Brigade example

`1st Danish Brigade`

- Brigade HQ
- 1. Regiment
- 5. Regiment
- Attached Dragoon Squadron
- Attached Field Battery

If the player drags the Brigade HQ to a town and chooses **Concentrate formation**, Brigade AI may generate:

- HQ -> town command-post location,
- 1. Regiment -> defensive sector west,
- 5. Regiment -> assembly/reserve position south,
- Battery -> suitable firing/support position,
- Dragoons -> screening/recon route.

The player provides the higher-level intent; subordinate Officer AI determines local placement according to commander skill, mission, terrain, known enemy and reserve doctrine.

## Relationship to Officer AI

The OOB Designer changes who commands whom. It does not replace Officer AI.

Higher-level orders should be mission-oriented:

- Defend Area
- Hold
- Attack Area
- Capture Objective
- Delay
- Withdraw
- Screen
- Support
- Reserve
- March / Concentrate

A Brigade commander receiving an objective decides how many subordinate units to commit, support or retain as reserve. If the situation worsens, additional reserve forces may be committed without a new player micro-order, subject to autonomy, officer traits and command information.

## Campaign / tactical continuity

The same stable OOB must persist from campaign to tactical battle.

When a tactical battle starts:

- only formations physically present or able to arrive during the battle are available,
- current parent/attachment structure is preserved,
- commanders/HQ entities are preserved,
- company/battalion manpower and casualties remain persistent,
- artillery/dragoon attachments remain associated with the correct HQ,
- detached or late-arriving units enter according to their campaign position and travel state.

After battle, updated strength, casualties, ammo, fatigue, commander state and physical position return to the campaign layer.

## OOB Designer safeguards

- Undo/redo for organizational changes before confirmation.
- Optional historical lock for scenarios where the player may inspect but not freely reorganize at start.
- Clear warning when an organizational move creates a detached/overstretched command relationship.
- Stable IDs must never change when display name or parent changes.
- Reorganization must be logged in unit history: timestamp, old parent, new parent, issuing authority and reason.
- Physical movement remains an order/event in campaign history, separate from organizational history.

## Planned implementation sequence

### 09l — Brigade Command & Reserve AI

- one Brigade HQ per side in the tactical pilot,
- two infantry regiments subordinate to each brigade,
- attached artillery/dragoon pilot assets,
- Defend/Attack/Capture objective state,
- regiment roles: Reserve / Support / Engaged / Manoeuvre / Withdraw,
- autonomous reserve commitment.

### 09m — OOB Designer Prototype

- collapsible hierarchy tree,
- drag/drop re-parent and attach/detach,
- stable IDs and validation,
- campaign-map entity synchronization,
- map drag creates march order,
- HQ drag offers HQ-only / formation march / concentration choices,
- OOB changes persist into campaign save state.

### Later

- historical OOB templates by date,
- custom task forces,
- command-span penalties,
- multiplayer/network authority rules,
- full campaign <-> tactical handoff using the same OOB graph.
