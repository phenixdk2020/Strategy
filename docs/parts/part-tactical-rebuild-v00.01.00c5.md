# Tactical Rebuild Supplement — v00.01.00c5

## Scope
v00.01.00c5 remains inside the clean Company-first rebuild before Gate D. It adds QA and presentation capabilities only.

## Visual representative rule
`PresentStrength` remains authoritative tactical manpower. A visual ratio such as 1:2 changes only how many soldier representatives are rendered. Representative figures are sampled across the full canonical formation rather than shrinking the formation itself.

This rule applies to all future scale settings: tactical movement, combat power, ammo, casualties, morale, cohesion and OOB state must never derive from the representative count.

## QA Lab
The F8 QA Lab exposes selected Company data needed to validate the rebuild before movement is introduced:
- UnitID and display name
- actual strength
- visible representative count
- formation
- facing
- selection footprint dimensions
- reform progress
- turning state

## Automated drill stress test
F7 cycles selected Danish Companies through animated Line/Column transitions and facing changes. The test deliberately preserves world position. It exists to stress renderer/selection/formation ownership without creating a second movement owner.

## Ratio benchmark
F6 runs a short performance comparison across 1:1, 1:2, 1:5, 1:10 and 1:20. Average frame time and FPS are logged per ratio. The original visual ratio is restored after the run.

## Facing overlay
Y toggles a selected-Company forward-direction arrow. It is read-only presentation derived from the Company transform and selection state.

## Soldier detail direction
The c5 close-range overlay adds readable silhouette/detail without adding per-soldier GameObjects or AI. Headgear and national visual differences should increasingly be handled by instanced meshes/materials and distance LOD, not individual soldier components.

Current c5 close-detail additions:
- visor/brim
- front badge/plate
- collar/shoulder trim
- Prussian spike silhouette

These remain prototype art and are not yet the final historical uniform asset set.

## Ownership boundary
c5 does not add:
- tactical translation
- pathfinding
- collision avoidance
- river/bridge navigation
- combat
- casualty resolution
- Officer AI
- Brigade AI

Gate D remains the first owner of real Company translation/navigation.
