# PROJECT 1864 — Tactical Rebuild v00.01.00c — Gate C

**Status:** TEST / implementation gate  
**Branch:** `work/tactical-rebuild-v00.01.00`  
**Previous accepted gate:** v00.01.00b — stable OOB + independent Company selection  
**Purpose:** prove 1:1 soldier rendering can be driven from clean independent Company state without reintroducing Regiment runtime ownership.

## 1. Scope

Gate C changes presentation only.

The following are unchanged from Gate A+B:

- stable `UnitID`
- OOB hierarchy stored as data
- exactly eight tactical Company entities
- four Danish Companies and four Prussian Companies
- independent world-space Company transforms
- one Company selection owner
- no Company movement owner
- no Company combat owner
- no Officer AI or Brigade AI

Active QA manpower remains:

```text
Denmark, I Battalion: 759 men
Prussia, I Battalion: 812 men
TOTAL: 1,571 men
```

## 2. Renderer ownership

`TacticalCompanyRenderer00C` is strictly presentation-only.

It reads:

```text
TacticalCompanyEntity00B.UnitId
TacticalCompanyEntity00B.Nation
TacticalCompanyEntity00B.PresentStrength
TacticalCompanyEntity00B.transform.position
TacticalCompanyEntity00B.transform.rotation
```

It does not write:

```text
Company position
Company facing
selection state
movement state
combat state
OOB parent data
```

This is a permanent architecture rule: renderers may visualize tactical state but may not become hidden simulation owners.

## 3. 1:1 rule

For every active Company:

```text
1 PresentStrength man = 1 rendered soldier visual
```

Gate C therefore renders approximately **1,571 soldier visuals**.

A soldier is not a Unity simulation object. Ordinary soldiers receive no individual:

- GameObject
- MonoBehaviour
- Collider
- NavMeshAgent
- Animator
- AI Update

Shared primitive meshes are rendered with GPU instancing through `Graphics.DrawMeshInstanced`.

## 4. Company formation geometry

Gate C uses Line only because movement/formation changes belong to Gate D.

Working line geometry:

```text
Ranks        = 3
File spacing = 0.52 m
Rank spacing = 0.76 m
Files        = ceil(PresentStrength / 3)
```

Tiny deterministic positional jitter is applied only to reduce the synthetic visual-grid appearance. It does not change the tactical Company footprint or tactical centre.

## 5. Soldier visual composition

Full-detail soldier:

- left leg
- right leg
- torso/coat
- head
- headgear
- pack
- rifle

Medium detail:

- torso
- head
- headgear
- rifle

Far detail:

- torso
- headgear

The current colours are QA nation-identification palettes only. Final historical uniform artwork remains a later presentation gate and must be data-driven.

## 6. LOD

Company-level LOD is active from the first 1:1 renderer gate:

```text
FULL    <= 260 m
MEDIUM  <= 520 m
FAR     <= 850 m
CULLED  > 850 m
```

All soldiers remain represented 1:1 inside the active LOD range; LOD changes the number of visual body components, not tactical manpower.

## 7. Performance discipline

Each Company currently contains fewer than 1,023 soldiers, so its persistent matrix arrays can be passed directly to `Graphics.DrawMeshInstanced` without allocating a temporary batch array each frame.

The renderer therefore avoids per-frame batch-array garbage.

Gate C telemetry shows:

- visible soldier count
- smoothed FPS
- smoothed frame time in ms
- renderer draw calls
- number of Companies in Full / Medium / Far LOD

This is QA telemetry, not a replacement for Unity Profiler.

## 8. Gate B footprint transition

The solid QA rectangle created in Gate B is hidden by Gate C.

The existing Company collider and selection outline remain because selection is still owned exclusively by the Gate B selection component.

This is intentional: Gate C replaces presentation, not selection ownership.

## 9. Input retained

```text
LMB          select one Danish Company
Shift + LMB  add Company
Ctrl + LMB   toggle Company
LMB drag     box-select Companies
Esc          clear selection
RMB          no movement; logs MovementDeferredToGateD
```

Movement is deliberately not implemented in Gate C.

## 10. Acceptance criteria

Gate C passes only when:

1. Unity 6000.6.0f1 compiles with no red errors.
2. Build marker shows `PROJECT 1864 | v00.01.00c TEST`.
3. Console shows `REBUILD-RENDER-00C|Installed=True`.
4. Exactly eight independent Company tactical entities remain.
5. Solid Gate B rectangles are hidden.
6. Approximately 1,571 visible soldier instances appear at normal tactical zoom.
7. Danish and Prussian Companies are visually distinct.
8. LMB selection still selects one Company only.
9. Shift/Ctrl selection still works.
10. Drag-box selection still works.
11. Selected outline remains visible around the selected Company.
12. RMB does not move units.
13. There are no legacy Regiment formations, HQs, range cones or PlayerCommander movement previews.
14. Camera controls remain functional.
15. Renderer telemetry shows stable behaviour without obvious recurring allocation/GC stalls.

## 11. Gate D boundary

Gate D will add Company movement/navigation with exactly one steering owner.

Gate C must not pre-implement:

- waypoint movement
- Line/Column state changes
- obstacle avoidance
- river/bridge logic
- no-progress recovery

Those belong to the next gate and may only be added after Gate C is accepted.
