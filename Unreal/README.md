# PROJECT 1864 — Unreal port

This folder contains the Unreal Engine port scaffold.

## Current state

Implemented as source scaffold:
- Unreal C++ module `Strategy1864`
- base `AStrategyUnit`
- `UStrategyOrderComponent`
- order types for Move / Angrib her / Forsvar her / Hold / Spejd her / Charge
- initial `ACavalryUnit` with separate horse and rider Skeletal Mesh components
- rider socket architecture

Not yet implemented:
- RTS camera/selection
- formation execution
- navigation/river/bridge logic
- combat/LOS/range
- Officer AI
- OOB/UMG
- Animation Blueprints

## Open locally

Open `Strategy1864.uproject` with Unreal Engine **5.8.3**. UE 5.8 is the project's current Unreal editor baseline.

If Unreal asks to generate project files or compile the C++ module, allow it.

## First gate

The first playable gate is **U01 — Camera and selection**. Do not begin rewriting combat AI until the project builds cleanly and the basic RTS selection loop works.

See:
- `../docs/UNREAL-MIGRATION-PLAN.md`
- `../docs/UNREAL-PORTING-INVENTORY.md`
