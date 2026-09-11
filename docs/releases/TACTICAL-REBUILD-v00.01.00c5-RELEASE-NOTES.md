# PROJECT 1864 — Tactical Rebuild v00.01.00c5

## Status
TEST revision on `strategi/kampe_rebuild`.

## Purpose
v00.01.00c5 expands the pre-Gate-D QA surface without adding tactical translation, pathfinding, combat or AI ownership.

## Added
- F8 QA Lab inspector for selected Company state.
- F7 Auto Drill stress loop using animated Line/Column and facing changes.
- F6 visual-ratio benchmark for 1:1, 1:2, 1:5, 1:10 and 1:20.
- Y selected-Company facing arrows.
- Close-range soldier detail overlay with stronger headgear silhouette, visor/brim, front badge, collar/shoulder detail and Prussian spike.
- QA benchmark logs average FPS and ms per visual ratio and restores the original ratio at completion.

## Retained from c4
- 1:1 / 1:N visual soldier scaling remains presentation-only.
- F animated Line/Column transition.
- Z/X animated facing changes.
- F10 visual settings.
- H hover info.
- V Clean/Debug presentation.
- Selection behavior unchanged.

## Architectural boundary
The c5 additions are QA/presentation layers only. They do not own or write:
- tactical translation
- pathfinding/navigation
- combat
- casualties
- ammo
- morale
- AI
- OOB parent relationships

Actual Company manpower remains authoritative regardless of visual representative ratio.

## Test checklist
1. Compile in Unity 6000.6.0f1 with no red errors.
2. Confirm visible marker `PROJECT 1864 | v00.01.00c5 TEST`.
3. Select one or more Danish Companies.
4. Open F8 and confirm live UnitID/strength/formation/facing/footprint data.
5. Toggle F7 and observe repeated animated drill without Company translation.
6. Toggle Y and verify arrows follow actual facing during animated turns.
7. Run F6 and compare FPS/ms across the ratio sequence.
8. Confirm the original visual ratio is restored after benchmark completion.
9. Zoom close and inspect headgear/trim detail on Danish and Prussian figures.
10. Confirm H, V, F10, F, Z/X and selection still work.
11. Confirm RMB still does not move Companies.

## Next gate
After c5 QA is accepted, Gate D introduces the first real Company movement/navigation owner.
