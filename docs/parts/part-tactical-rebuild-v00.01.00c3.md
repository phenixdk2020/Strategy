# PROJECT 1864 — Tactical Rebuild v00.01.00c3

## Status

`v00.01.00c3 TEST` is the current visual/drill revision on `strategi/kampe_rebuild`.

It builds on the accepted clean Company-first architecture from v00.01.00b/c/c2. The purpose is to improve battlefield readability and test Company formation/facing state before the project introduces Gate D movement/navigation.

## Architecture boundary

The following ownership rules remain mandatory:

- Company remains the first-class tactical infantry entity.
- OOB parent relationships remain data, not required Transform parenting.
- Selection remains owned only by `TacticalCompanySelection00B`.
- c3 drill controls may change only selected Company `Formation` and facing.
- c3 renderer reads Company state and writes presentation only.
- Hover UI reads Company/OOB state and writes no tactical state.
- No route, translation, navigation, combat, casualty or AI owner is introduced in c3.
- RMB remains deferred to Gate D.

## Visual pass c3

The renderer continues the 1:1 strategy: one visible soldier per active manpower record, rendered through instancing rather than one heavy GameObject per soldier.

Full-detail silhouette now contains:

- separate left/right legs;
- separate boots;
- narrower torso;
- coat skirt;
- head and headgear;
- separate arms and hands;
- cross-belt;
- pack;
- rifle wooden stock;
- long metal barrel;
- bayonet.

The goal is sharper human/rifle readability at normal game-camera distances while retaining the performance architecture.

No standards or flags are part of c3.

## Drill controls

- `LMB`: select one Danish Company.
- `Shift + LMB`: add Company.
- `Ctrl + LMB`: toggle Company.
- `LMB drag`: box select.
- `Esc`: clear selection.
- `F`: toggle selected Danish Companies between Line and Column in place.
- `Z`: rotate selected Danish Companies 15 degrees left in place.
- `X`: rotate selected Danish Companies 15 degrees right in place.
- `V`: toggle Clean/Debug presentation.
- `H`: toggle mouse-over information.
- `RMB`: still ignored; movement remains Gate D.

## Formation geometry

### Line

- 3 ranks.
- 0.52 m file spacing.
- 0.76 m rank spacing.

### Column

- 8 files across.
- 0.60 m file spacing.
- 0.72 m row spacing.

The Company recalculates its hidden selection footprint and selection outline when changing formation, so click/select geometry remains consistent with the visual formation.

## Current QA force

- Denmark: I Battalion, 4 Companies, 759 men.
- Prussia: I Battalion, 4 Companies, 812 men.
- Total: 8 independent Companies, 1,571 active infantry.

## Acceptance gate

c3 is accepted only when:

1. Unity compiles without red errors.
2. Visible marker is `PROJECT 1864 | v00.01.00c3 TEST`.
3. Exactly eight Companies and approximately 1,571 soldiers appear.
4. Existing selection behaviour is unchanged.
5. `F` changes only selected Companies between Line and Column.
6. The selection outline/footprint follows formation dimensions.
7. `Z/X` changes facing without translating Company position.
8. `V` changes presentation only.
9. `H` changes hover visibility only.
10. No world labels or flags appear.
11. RMB does not move Companies.
12. No legacy Regiment movement/combat/AI authority reappears.
13. Performance remains acceptable without obvious recurring GC/freezes.

## Next gate

After c3 passes focused QA, the next build is Gate D: Company movement/navigation with exactly one steering owner per Company. It may reuse V3 navigation lessons conceptually, but must not reintroduce Regiment movement underneath Company movement.
