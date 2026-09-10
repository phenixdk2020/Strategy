# PROJECT 1864 — v00.01.00b TEST

## Clean OOB + Independent Company Selection

### Purpose

This is the first runtime gate of the clean tactical rebuild. It validates Unit identity, OOB data and Company selection without introducing movement or combat authority.

### Implemented

- Stable UnitID-based OOB records.
- One complete Danish reference Regiment: 1. Infanteri-Regiment / Danske Livregiment, 1540.
- One complete Prussian QA Regiment: 8th Regiment, 2460.
- Full Battalion/Company records stored for both reference Regiments.
- Only I Battalion per side is tactical-active.
- Four Danish + four Prussian independent Company tactical entities.
- Active manpower: Denmark 759, Prussia 812, total 1571.
- Companies are not Transform children of OOB Regiment/Battalion objects.
- One Company selection owner.
- LMB select, Shift add, Ctrl toggle, LMB drag box, Esc clear.
- Clean QA footprint visuals.
- Legacy four-Regiment bootstrap blocked.
- Legacy tactical runtime MonoBehaviours disabled before normal gameplay Update, except camera control and build marker.

### Intentionally not implemented in this gate

- Company movement.
- Company pathfinding.
- Company combat.
- 1:1 soldier rendering.
- Officer AI.
- Brigade AI.
- Cavalry/dragoons.
- Artillery.

RMB therefore does not move anything and logs `MovementDeferredToGateD`.

### QA acceptance

1. Compile with no red errors.
2. Marker: `PROJECT 1864 | v00.01.00b TEST`.
3. Eight footprints visible.
4. Each Danish Company can be selected individually.
5. Shift adds, Ctrl toggles, drag-box selects multiple, Esc clears.
6. No old Regiment formations/HQs/range cones/order previews appear.
7. RMB does not move units.
8. Console shows `REBUILD-OOB-00B|Installed=True` and `REBUILD-00B|Installed=True|GateA=True|GateB=True`.

### Next accepted gate

After QA acceptance, Gate C adds the 1:1 Company-driven GPU renderer while preserving the exact same Company tactical entities and selection owner.
