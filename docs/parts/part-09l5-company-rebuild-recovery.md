# PROJECT 1864 — 09l5 Company architecture review and recovery

**Status:** Authoritative design supplement  
**Date:** 2026-09-09  
**Decision:** Freeze 09l5 company runtime layering; preserve company-level design; rebuild implementation cleanly.

## 1. Design decision

PROJECT 1864 keeps **Company as the minimum normal infantry tactical entity**.

The desired hierarchy remains:

```text
Brigade HQ
→ Regiment HQ
→ Battalion
→ Company
→ 1:1 visual soldier instances
```

The problem found in 09l2–09l5 was not the company-level design. The problem was adding company behaviour as compatibility layers on top of a runtime where `Regiment` still owned transform movement, navigation, combat, selection assumptions and several presentation systems.

## 2. Observed failures

The 09l2–09l5 QA sequence exposed the following ownership conflicts:

- company objects were initially children of `Regiment.transform`
- rotating or moving a Regiment therefore moved all companies as one rigid body
- multi-company group orders rebuilt companies as a single very long line
- parent Regiment remained the combat authority while companies were the visible fighting formations
- mounted Regimental HQ could visually appear to fire the regiment volley
- selection became split across colliders, GPU-instanced soldiers and screen-space compatibility logic
- flags/guidons were added at multiple layers to compensate for unclear unit authority/readability
- later execution-order scripts increasingly overrode earlier scripts rather than removing the underlying ownership conflict

## 3. Freeze

`v00.00.09l5` is frozen as a **failed QA architecture branch/state** for company runtime behaviour.

Do not continue by adding additional `09l6`, `09l7`, etc. authority/compatibility scripts merely to make the same layered architecture appear stable.

The branch remains valuable as:

- evidence of desired controls
- proof of 1:1 rendering feasibility direction
- OOB/HQ/flag experimentation
- source of lessons learned

but not as the base architecture for the final company system.

## 4. Permanent architecture rules

1. `UnitID` is stable identity. Display name is not identity.
2. OOB parent relationship is stored in data and is independent of Unity transform parenting.
3. Company tactical entities exist in world space independently of Regiment transforms.
4. Company owns company movement state.
5. Company owns company combat state.
6. One tactical entity has one steering owner per frame.
7. Regimental HQ issues commands; it does not physically drag subordinate companies.
8. Regimental HQ does not impersonate all subordinate fire.
9. Battalion/Regiment/Brigade formation templates assign company target centres, not transform parenting.
10. Higher AI chooses missions/roles; lower navigation executes movement.
11. 1:1 soldiers are render instances, not individual heavy simulation objects.
12. No compatibility authority layer is accepted if the ownership conflict can instead be removed.

## 5. Clean rebuild order

### Gate A — OOB/data

- stable Unit IDs
- one Danish regiment
- one Prussian regiment
- battalion/company structure
- staff/manpower totals
- no Brigade AI

### Gate B — company entities

- independent company world position
- independent facing
- company footprint
- reliable click + box selection
- movement not yet complicated by combat AI

### Gate C — renderer

- 1:1 GPU-instanced soldiers from company state
- correct formation slots
- LOD/culling
- HQ and standard readability

### Gate D — movement

- one company steering/path service
- single-company move
- multi-company template move
- obstacles
- river/bridge
- formation deploy/march state

### Gate E — combat

- company ammo
- fire policy
- LOS
- reload
- casualties
- morale/cohesion

### Gate F — Regiment/Battalion AI

- objective/sector assignment
- company support/reserve/manoeuvre roles
- no direct transform overwrites from higher AI

### Gate G — Brigade AI

- restore second regiment per side
- defend/attack/capture objectives
- reserve commitment
- attached cavalry/artillery
- couriers/order delay

### Gate H — Campaign/OOB Designer

- organisational drag/drop
- campaign-map movement orders
- campaign ↔ tactical persistent state

## 6. QA scenario size

Until Gate E is stable, use only:

- 1 Danish regiment
- 1 Prussian regiment

The regiments may retain historically realistic manpower, but higher formation complexity remains disabled.

Scale back to two-regiment brigades only after company selection, movement, formation and combat are stable.

## 7. Related consolidated reference

The full current architecture is consolidated in:

`tools/strategy-tools/PROJECT-1864-CURRENT-DESIGN-MANUAL.md`

The 1864 OOB/historical working data is consolidated in:

`tools/strategy-tools/ARMY-1864-OOB-REFERENCE.md`
