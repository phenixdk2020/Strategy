# PROJECT 1864 — Tactical Rebuild v00.01.00d2

## Gate D2 — Regiment Route + Auto March Deployment + Range Ghost + Standards

### Purpose
D2 extends the clean Company-first Gate-D architecture without reintroducing a Regiment transform owner. Regiment selection/order grouping resolves into Company intent; each Company still owns its own world pose.

### Selection
- LMB: Company selection.
- Double-LMB on a Danish Company: select all currently tactical-active Companies in the same Regiment.
- Shift + double-LMB: add another Regiment when more player Regiments become tactical-active.
- Ctrl + double-LMB: toggle the active Regiment subset.
- Current QA contains only one Danish tactical-active Regiment subset, so true multi-Regiment player QA remains for later scale-up.

### Route planning
- Hold ALT and RMB-click multiple ground points to add waypoints.
- Backspace removes the latest waypoint.
- Release ALT and RMB the final destination.
- Hold/drag RMB to set final facing.
- F toggles the final Line/Column formation while planning.
- RMB release confirms the route.
- A cyan ghost line shows the route and remains visible while the order is active.

### Automatic march behaviour
- A march order first reforms into Column.
- User route legs are sampled into short approximately 8 m internal legs; this keeps Company as the movement owner and gives D2 frequent safe points for contact/deployment decisions.
- The unit remains in Column during ordinary route travel.
- At approximately 35 m from the final destination it deploys to the requested final formation.
- Enemy proximity at the current QA contact threshold (160 m) aborts the remaining march route and deploys to Line.
- `TacticalCompanyOrder00D2.NotifyUnderFire(company)` is the future Gate-E combat hook.
- U is a QA-only under-fire simulation for selected Companies.

### Destination ghost / fire-sector preview
The final ghost includes:
- final Company footprint(s),
- final facing arrow,
- 60-degree sector,
- SHORT arc: 80 m,
- MEDIUM arc: 160 m,
- LONG arc: 260 m.

These distances are QA visualization values, not historical source-locked weapon ranges.

### Regiment standards
A presentation-only standard pair is generated for each active Regiment:
- one national flag,
- one Regiment banner.

Danish QA art uses Dannebrog plus a dark-red/gold Regiment banner. Prussian QA art uses a provisional white/black flag plus a dark Regiment banner. Banner art is intentionally marked provisional; D2 validates ownership, placement and identity, not final vexillology.

### Preserved systems
- stable UnitID/OOB,
- 8 independent Company tactical entities,
- 1571 actual men in current QA,
- 1:1 / 1:N visual-soldier scaling,
- D1 articulated soldier renderer and procedural march gait,
- hover info,
- box selection,
- one Company movement owner.

### Explicitly not in D2
- obstacle avoidance,
- V3 detour/no-progress recovery,
- bridge-only river routing,
- live combat/fire events,
- Officer AI / Brigade AI,
- full Regiment tactical scale.

### QA focus
1. Double-click selects the active Regiment subset.
2. ALT+RMB builds a multi-point route.
3. Ghost line follows every waypoint.
4. Final ghost rotates correctly and F changes final formation only.
5. Range cone rotates with final facing.
6. Confirmed route reforms to Column before marching.
7. Unit follows route points, not the direct straight-line destination.
8. Near destination it deploys before final arrival.
9. Enemy contact and U-under-fire paths cause Line deployment.
10. Standards follow their Regiment presentation centre without affecting movement/selection.
11. Visual ratios remain presentation-only.

### Backup
Pre-D2 safety branch:
`backup/strategi-kampe-rebuild-v00.01.00d1-pre-d2`
