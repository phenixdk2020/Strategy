# PROJECT 1864 — Tactical Rebuild v00.01.00d1

## Build

- **Version:** `v00.01.00d1`
- **Channel:** `strategi/kampe_rebuild`
- **Gate:** D1 — direct Company destination/facing/formation order
- **Visible marker:** `PROJECT 1864 | v00.01.00d1 TEST`

## Implemented now

### Ghost destination order

The player now defines the destination before the Company moves:

1. Select one or more Danish Companies.
2. RMB-down places the destination centre.
3. Hold RMB and drag to define the final facing.
4. Press F while the ghost is active to switch the target between Line and Column.
5. RMB release confirms the order.

The ghost footprint is a preview only. The real Company does not change position, facing or formation until the order is confirmed.

### Company-owned movement

`TacticalCompanyEntity00B` now owns the first real movement state of the clean rebuild. The Company interpolates from current pose to the confirmed target pose. Position, facing and formation footprint transition together. Higher systems do not directly overwrite the transform every frame.

QA tuning:

- march speed: 1.35 m/s
- turn speed: 55 deg/s
- minimum formation reform time: 2.2 s

This is intentionally direct movement only. It is not yet obstacle navigation.

### Multi-selection

For multiple selected Companies, D1 records their offsets relative to the selected group reference frame. The destination group rotates with the ghost facing while preserving relative Company offsets.

### Soldier Visual V5

The D1 presentation layer supersedes the c4 base renderer and c5 detail overlay for normal display. It adds:

- upper/lower leg articulation
- separate boots
- upper/lower arms
- hands
- shoulders
- neck
- nose/face silhouette
- tapered torso and coat skirt
- distinct national headgear silhouettes
- Prussian spike profile
- pack, cartridge box, belts and straps
- clearer wood/metal musket separation
- procedural marching gait with leg swing, arm swing and body bob

The visual-ratio system remains intact. `1:2` means two real soldiers per visible representative, not half tactical manpower.

## Explicitly not implemented in D1

- obstacle avoidance
- building/tree detours
- river/bridge routing
- no-progress recovery
- V3 persistent detour logic
- combat
- Officer AI
- Brigade AI
- cavalry
- artillery

These are deliberately deferred so the ghost/order/movement ownership contract can be tested independently.

## Controls

- LMB: select Danish Company
- Shift+LMB: add
- Ctrl+LMB: toggle
- LMB drag: box select
- Esc: clear/cancel
- RMB down: destination ghost
- Hold RMB + drag: final facing
- F while ghost active: Line/Column target
- RMB release: confirm and execute
- F10: visual ratio settings
- H: hover info
- V: Debug/Clean
- F8: QA Lab
- F6: visual-ratio benchmark

## Acceptance test

1. No red compile errors.
2. Build marker reads `v00.01.00d1 TEST`.
3. Eight independent Companies remain active.
4. RMB ghost appears before real movement.
5. Dragging RMB changes only ghost facing.
6. F changes only ghost formation while previewing.
7. RMB release starts movement without teleportation.
8. Men visibly walk during translation/reformation/turning.
9. Final position/facing/formation matches the preview.
10. Multi-select relative offsets remain stable.
11. 1:N ratios do not change actual manpower.
12. Hover and selection continue to work.
13. No old Regiment movement owner appears.
14. Obstacles remain intentionally unsupported in D1.
