# PROJECT 1864 — Tactical Rebuild v00.01.00c3

**Build:** `v00.01.00c3 TEST`  
**Branch:** `strategi/kampe_rebuild`  
**Scope:** Visual Polish + Drill Test.  

## Purpose

This revision keeps the clean Company-first rebuild architecture from v00.01.00c2 and adds only presentation/drill functionality that is safe to test before movement/navigation and combat are introduced.

## Implemented in v00.01.00c3

- Exactly eight independent tactical Companies remain active: four Danish and four Prussian.
- Active manpower remains Denmark 759 + Prussia 812 = 1,571 men.
- 1:1 GPU-instanced soldier rendering remains presentation-only.
- Soldier silhouette is sharpened again: narrower torso, separate coat skirt, arms, legs, boots, head, headgear, pack, cross-belt, hands, rifle stock, metal barrel and bayonet.
- Close/medium LOD keeps the rifle readable as a weapon rather than a single stick.
- Permanent world labels remain removed.
- Mouse-over Company information remains available and can now be toggled on/off with `H`.
- `V` toggles Clean/Debug view. Clean view hides rebuild debug/status panels while leaving the visible build marker, selection outline and optional hover information intact.
- `F` toggles selected Danish Companies between `Line` and `Column` in place.
- `Z` rotates selected Danish Companies 15 degrees left in place.
- `X` rotates selected Danish Companies 15 degrees right in place.
- Selection ownership remains unchanged: LMB select, Shift add, Ctrl toggle, drag-box select and Esc clear.
- Drill operations alter only Company formation/facing state. They do not create movement routes, pathfinding, combat or AI ownership.
- No standards/flags are included in this revision.
- RMB movement remains intentionally deferred to Gate D.

## Drill formation geometry

### Line
- 3 ranks.
- 0.52 m file spacing.
- 0.76 m rank spacing.

### Column
- 8 files across.
- 0.60 m file spacing.
- 0.72 m row spacing.

The hidden Company selection footprint is recalculated when formation changes so click/selection outline remains aligned with the rendered Company.

## Controls to test

- `LMB`: select one Danish Company.
- `Shift + LMB`: add Company.
- `Ctrl + LMB`: toggle Company.
- `LMB drag`: box select.
- `Esc`: clear selection.
- `F`: selected Company/Companies Line ↔ Column.
- `Z`: rotate selected Company/Companies 15° left.
- `X`: rotate selected Company/Companies 15° right.
- `V`: Clean/Debug view.
- `H`: hover info on/off.
- `RMB`: still ignored and reports `MovementDeferredToGateD`.

## Acceptance test

1. Unity compiles without red errors.
2. Visible marker is `PROJECT 1864 | v00.01.00c3 TEST`.
3. Console contains `REBUILD-RENDER-00C3|Installed=True`.
4. Console contains `REBUILD-DRILL-00C3|Installed=True`.
5. Exactly eight independent Companies remain in scene.
6. Approximately 1,571 soldier visuals remain visible.
7. Selection works exactly as in c2.
8. `F` changes only selected Danish Companies between Line and Column.
9. Company selection footprint/outline follows the new formation dimensions.
10. `Z/X` rotate only selected Companies in place and do not translate them.
11. `V` toggles debug panels without changing tactical state.
12. `H` toggles hover information without changing selection.
13. No permanent world labels appear.
14. No national/regimental standards appear.
15. RMB still does not move Companies.
16. No old Regiment runtime, Officer AI, Brigade AI, artillery or cavalry is reactivated.
17. FPS remains acceptable and no obvious recurring GC/freeze behaviour is introduced.

## Still deliberately OFF

- Company movement/pathfinding/navigation.
- River/bridge constraints.
- Company combat, fire, reload, ammo, casualties, morale/cohesion.
- Battalion/Regiment HQ and Officer AI.
- Brigade AI.
- Cavalry and artillery.
- Standards/flags.

Next planned gate remains **Gate D: Company movement/navigation with one steering owner** after c3 passes focused QA.
