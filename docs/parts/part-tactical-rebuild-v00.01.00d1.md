# PROJECT 1864 — Tactical Rebuild v00.01.00d1 Gate D1

## Status

`v00.01.00d1` is the first clean-rebuild movement slice. It promotes the Company from a static tactical entity to the single owner of its own destination execution while keeping higher command layers out of transform ownership.

## Player-order contract

The destination is defined before the Company moves.

1. Select one or more Companies.
2. RMB-down places a ghost destination centre.
3. RMB drag defines the final facing.
4. F while the ghost is active switches target formation between Line and Column.
5. RMB release confirms the order.
6. Companies then move, rotate and reform toward the previewed target.

The ghost is therefore the authoritative player preview of the requested end state. The Company does not teleport when the preview changes.

## Ownership rule

D1 preserves the rebuild rule:

- `TacticalCompanySelection00B` owns selection only.
- `TacticalCompanyOrder00D1` converts player input into target intent.
- `TacticalCompanyEntity00B` owns execution of world position, facing and formation state.
- `TacticalCompanyRenderer00D1` is presentation-only and reads Company state.
- Battalion/Regiment/Brigade do not write Company transforms.

This is the basis for later Officer AI: higher HQs may produce the same kind of target intent, but the Company movement owner remains singular.

## Direct movement in D1

D1 deliberately validates the simplest direct order path first:

- direct destination movement
- final facing
- Line/Column target formation
- multi-Company relative offsets
- no teleporting
- walking/reforming presentation

It deliberately does **not** yet include:

- hard/soft obstacle avoidance
- V3 persistent detours
- no-progress recovery
- bridge-only river crossing
- terrain-fit group templates

Those belong to D2–D4 after the input/ownership contract passes QA.

## Multi-Company ghost semantics

The selected group has a reference centre and reference facing. Each selected Company stores its offset in that local group frame. Rotating the destination ghost rotates the group frame; individual Company offsets and relative orientations are preserved at the destination.

This avoids the old 09l2/09l5 failure mode where group commands collapsed Companies into one giant line or coupled them through Transform parenting.

## Formation-change semantics

Line/Column is chosen on the destination ghost. The preview rectangle immediately shows the target footprint, but the real Company keeps its current state until order confirmation.

During execution:

- Company centre moves toward destination.
- Company facing interpolates toward requested facing.
- representative soldier slots interpolate from current formation slots to target formation slots.
- selection footprint/collider/outline transitions toward the target footprint.

## Soldier visual ratio

The c4 visual-ratio rule remains authoritative:

- 1:1 = one visible representative per real soldier
- 1:2 = one visible representative per two real soldiers
- 1:5 = one per five, etc.

This changes presentation only. `PresentStrength`, future ammo, casualties, morale, combat power and OOB state always use real strength.

Reduced representatives are sampled across the full real-strength formation so a 1:2 setting does not halve physical frontage or depth.

## Soldier Visual V5

D1 replaces the previous base/detail layering with one articulated presentation renderer for normal play. It adds:

- tapered torso and coat skirt
- neck and face/nose silhouette
- distinct headgear geometry
- Prussian spike silhouette
- shoulders
- upper/lower arms
- hands
- upper/lower legs
- boots
- pack, cartridge box, belts and straps
- clearer wood/metal musket construction
- procedural marching gait while translating, turning or reforming

The gait is presentation only and does not become an Animator or per-soldier simulation owner.

## D1 acceptance gate

D1 passes only when:

1. the ghost appears before movement;
2. drag-facing matches final facing;
3. F changes only the preview formation before confirmation;
4. RMB release starts execution without teleporting;
5. men visibly walk/reform into the previewed end state;
6. final centre/facing/formation matches the ghost;
7. multi-selection preserves relative offsets;
8. visual ratios do not alter actual manpower;
9. selection and hover continue to work;
10. no old Regiment movement authority is active.

After D1 passes, D2 can introduce hard/soft obstacles and no-progress recovery at Company level.
