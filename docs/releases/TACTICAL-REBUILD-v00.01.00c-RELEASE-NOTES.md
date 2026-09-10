# PROJECT 1864 — Tactical Rebuild v00.01.00c TEST

## Gate

**Gate C — Company-driven 1:1 renderer**

## Implemented now

- Keeps v00.01.00b Gate A+B architecture unchanged.
- Eight independent tactical Companies remain active: four Danish and four Prussian.
- Active manpower remains 1,571.
- Adds `TacticalCompanyRenderer00C` as a presentation-only renderer.
- Hides solid Gate B QA rectangles while keeping selection collider and selection outline.
- Renders one visible soldier per active manpower record using GPU instancing.
- No individual GameObject, MonoBehaviour, Collider, NavMeshAgent, Animator or AI per ordinary soldier.
- Full soldier QA silhouette includes legs, torso, head, headgear, pack and rifle.
- Danish and Prussian QA palettes are visually distinct.
- Three-rank Line presentation uses 0.52 m file spacing and 0.76 m rank spacing.
- Adds deterministic micro-variation to soldier positions without changing Company tactical footprint.
- Adds Company-level visual LOD: Full <=260 m, Medium <=520 m, Far <=850 m, culled beyond 850 m.
- Uses persistent per-Company matrix arrays with no temporary per-frame instance-batch arrays.
- Adds renderer QA telemetry: visible soldiers, FPS, frame ms, draw calls and LOD distribution.
- Build marker updated to `PROJECT 1864 | v00.01.00c TEST`.

## Deliberately not implemented

- Company movement
- pathfinding/navigation
- Line/Column commands
- Company combat
- fire/reload/ammo/casualties
- Officer AI
- Brigade AI
- artillery
- cavalry/dragoons

RMB therefore remains intentionally non-operative and logs `MovementDeferredToGateD`.

## QA focus

1. Compile without red Unity errors.
2. Verify `REBUILD-RENDER-00C|Installed=True`.
3. Confirm eight Company entities remain independent.
4. Confirm approximately 1,571 soldier visuals appear.
5. Confirm Danish and Prussian colours are visibly distinct.
6. Confirm LMB, Shift, Ctrl and drag-box selection still work.
7. Confirm selected Company outline remains visible.
8. Confirm RMB does not move units.
9. Check FPS/ms and visual LOD at close, normal and far zoom.
10. Confirm no legacy Regiment/HQ/range/order-preview runtime appears.

## Next gate

**v00.01.00d — Gate D: Company movement/navigation** after 00.01.00c passes QA.
