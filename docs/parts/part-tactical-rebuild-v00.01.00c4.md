# PROJECT 1864 — Tactical Rebuild v00.01.00c4

**Status:** TEST / Gate A+B+C4  
**Branch:** `strategi/kampe_rebuild`  
**Build:** `v00.01.00c4` — Animated Drill + Visual Soldier Scale

## Purpose

c4 keeps the clean Company-first architecture and adds two presentation/test capabilities before Gate D movement:

1. animated in-place drill transitions for formation and facing;
2. configurable visual soldier representation ratio without changing tactical manpower.

## Visual soldier ratio

The tactical state always stores real manpower. `PresentStrength` is authoritative.

The renderer may show fewer representative soldier figures using these selectable ratios:

- 1:1
- 1:2
- 1:3
- 1:4
- 1:5
- 1:10
- 1:20

Example: a 190-man Company at 1:2 renders about 95 visible representatives while all simulation/OOB systems still see 190 men.

This ratio is presentation-only. It must never directly change:

- OOB strength
- Company footprint
- movement speed
- frontage/depth
- ammo
- firepower
- casualties
- morale/cohesion
- fatigue
- AI strength evaluation
- save-game manpower

Reduced visual representatives are sampled across the full-strength canonical formation positions. This preserves the apparent tactical footprint instead of shrinking a 190-man Company to the physical width of 95 tightly packed men.

`F10` opens the visual settings panel and allows live ratio switching.

## Animated drill

`F` toggles selected Danish Companies between Line and Column over approximately 2.2 seconds. Soldiers interpolate from their old formation slots to their target slots instead of teleporting.

`Z` / `X` turn selected Danish Companies 15 degrees left/right over approximately 0.75 seconds. The Company centre remains fixed, so this is still drill-only and not Gate D translation.

Selection footprint/hitbox/outline changes continuously with the formation transition.

## Hover/debug controls

- `H`: hover information ON/OFF with visible feedback.
- `V`: Debug/Clean view with visible feedback.
- Hover shows actual strength, formation, UnitID, current visual ratio, visible representative count and current drill state.

## Soldier presentation pass

c4 remains GPU-instanced and presentation-only. The visual soldier is refined with:

- tapered torso/coat body mesh;
- coat skirt;
- separate legs and boots;
- separate arms and hands;
- cross belt and waist belt;
- pack and cartridge box;
- musket stock;
- metal barrel;
- bayonet;
- nation-specific QA uniform palette.

This remains a low-poly prototype and is not final historical character art.

## Architectural rule

The rendering ratio is never a simulation scale switch. `1:2` means one visible representative per roughly two actual soldiers, not that two historical soldiers become one tactical simulation entity.

Company remains the tactical owner of its actual manpower/state. Renderer settings consume Company state but do not redefine it.

## Gate status

Still deliberately OFF in c4:

- RMB movement
- routes/pathfinding
- obstacle avoidance
- bridge/river navigation
- combat
- reload/ammo/casualties
- Officer AI
- Brigade AI
- artillery
- cavalry

These remain future gates, starting with Gate D movement/navigation.
