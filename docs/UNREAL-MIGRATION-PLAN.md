# PROJECT 1864 — Unreal Engine migration plan

**Migration branch:** `unreal-port`  
**Reference branch:** `channel-test`  
**Unity reference baseline:** `v00.00.09f30x` plus subsequent compile fixes and imported infantry rig  
**Unreal Engine baseline:** `5.8.3`  
**Migration principle:** preserve gameplay/design behaviour first; replace engine implementation second.  
**U00 status:** PASS — UE 5.8.3 C++ editor build verified on Windows.

## Goal

Port PROJECT 1864 from the existing Unity tactical prototype to Unreal Engine without discarding the design, historical data, gameplay rules, AI behaviour, UI semantics, formation doctrine, OOB structure, or reusable art assets.

The Unity implementation remains the behavioural reference until the Unreal implementation passes equivalent QA. The Unreal port must not silently simplify or reinterpret established rules.

The detailed feature-for-feature tracker is [UNREAL-PARITY-BACKLOG.md](UNREAL-PARITY-BACKLOG.md). The migration plan defines the phase order; the parity backlog defines the individual required features and QA exits.

## Repository strategy

The existing repository remains authoritative during the port.

- `main`: accepted/stable work.
- `channel-test`: Unity tactical reference.
- `campaign`: Unity campaign work.
- `unreal-port`: Unreal Engine port.

The Unreal project lives under `/Unreal` so the Unity reference remains available in the same branch for direct code/design comparison during migration.

## Migration order

### U00 — Project/bootstrap — PASS
- Create Unreal C++ project scaffold.
- Establish module, source layout and ignore rules.
- Establish source-asset handoff rules.
- Preserve the Unity files as reference.

### U01 — Camera and selection — IMPLEMENTED / BUILD+QA PENDING
- RTS camera Pawn with WASD pan, Q/E yaw and mouse-wheel zoom.
- Left-click unit selection through visibility trace.
- Drag box selection uses each unit actor's projected centre, matching the established Unity selection rule.
- Shift adds to the current selection.
- Ctrl removes units from the current selection.
- Empty plain click clears selection; additive/removal modifiers do not clear unrelated units.
- Selection state lives on `AStrategyUnit`; Blueprint receives `OnSelectionChanged` for rings/highlights.
- Order placement is intentionally separate from selection input, preserving selected units through future order placement.
- Default GameMode now wires the RTS camera Pawn, player controller and selection HUD.
- U01 is not marked PASS until the UE 5.8.3 editor target builds and the interaction QA below is completed.

#### U01 QA gate
1. UE 5.8.3 editor target builds without C++/UHT errors.
2. WASD pans over the tactical level; Q/E rotates; mouse wheel zooms within configured limits.
3. Plain click selects one player-controllable `AStrategyUnit`.
4. Drag selection selects every player-controllable unit whose actor centre lies inside the rectangle.
5. Shift-click/drag adds units without dropping the existing selection.
6. Ctrl-click/drag removes only targeted units.
7. Plain click on empty ground clears selection.
8. Future right-click/order placement must not clear selection.

### U02 — Unit/OOB data model
- Division → Brigade → Regiment → Battalion/Major → Company.
- Physical HQ actors.
- OrganicParent vs CurrentCommandParent.
- Strength, status and selection state.

### U03 — Orders and authority
- MOVE.
- ANGRIB HER.
- FORSVAR HER.
- HOLD.
- Facing placement.
- Direct player authority vs inherited Officer AI authority.
- Physical execution state separated from standing intent.

### U04 — Infantry movement/formations
- Line.
- March Column.
- Three-rank company geometry.
- Early deployment before enemy fire range.
- Company final-slot spacing/deconfliction.
- Final-slot arrival authority.

### U05 — Terrain/navigation
- Hard-blocking river/water.
- Bridge-only crossing where a true bank change is required.
- Same-bank river routing without bridge round-trip.
- Building/fence obstacle handling.
- Formation-aware bridge/defile movement.

### U06 — Combat/visibility
- Close / Medium / Long fire policies.
- LOS.
- ±35 degree fire arc.
- Test/QA threat cones.
- Volley/reload/casualties.
- Under-fire reactions.
- Square vs cavalry.

### U07 — Officer AI
- Captain/company AI.
- Major/battalion AI.
- Regiment AI.
- Brigade/division delegation.
- Command zones.
- HQ follow/rear depth.
- Mission completion.

### U08 — Cavalry
- Mounted cavalry movement.
- 4-rank Line/Charge.
- 4-abreast normal Column.
- 2-abreast bridge/defile approach only near the bridge.
- Charge/contact rules.
- Dynamic task attachment to Major A/B.
- Return to previous command parent.
- SPEJD HER reconnaissance order.
- Horse + rider as separate skeletal meshes/animation graphs.

### U09 — OOB/HUD/semantic zoom
- OOB panel.
- AI ON/OFF status.
- Drag/drop cavalry attachment.
- Command buttons and execution colours.
- NATO/semantic zoom ownership.
- HQ command circles.
- Range/cone visual language.

### U10 — Tactical parity gate
The Unreal battle test is not considered a replacement for the Unity prototype until the current tactical QA baseline is reproduced.

Primary parity checks:
1. Enemy QA cones visible from battle start.
2. Infantry deploys before entering enemy maximum fire range.
3. Company lines do not overlap at final slots.
4. Cavalry uses two-abreast only near bridge/defile.
5. Same-bank river movement avoids unnecessary bridge round-trips.
6. Attack/defend buttons show active execution only while execution remains.
7. Parent order completion waits for actual final-slot arrival.
8. OOB AI status is consistent.
9. Higher-level selection persists through order placement.
10. Cavalry / Square / fire reactions respect LOS, range and cone.

## Asset policy

### Directly reusable
- FBX meshes.
- Rigged Mixamo infantry.
- Textures.
- Audio.
- Historical/OOB data.
- Heightmaps and reference maps where format permits.

### Rebuilt in Unreal
- C# MonoBehaviour systems → Unreal C++ / Blueprint.
- Unity Prefabs → Unreal Blueprints.
- Unity Animator state machines → Animation Blueprints / Montages.
- Unity ParticleSystem → Niagara.
- Unity UI/IMGUI → UMG/Common UI as appropriate.
- Unity scene-specific wiring → Unreal Levels/Actors/Data Assets.

## Animation policy

### Infantry/rider
Use humanoid skeletal meshes and Unreal retargeting/IK tooling. The current Mixamo-rigged infantry FBX is retained as a source asset.

### Horse
Horse is a separate quadruped skeletal mesh and is not treated as Humanoid. Horse animation uses its own Skeleton/Animation Blueprint. Rider and horse remain separate rigs, connected through a saddle/rider socket and synchronized animation state.

## Architecture direction

Initial Unreal module layout:

```text
Unreal/
├── Config/
├── Content/
├── Source/
│   ├── Strategy1864.Target.cs
│   ├── Strategy1864Editor.Target.cs
│   └── Strategy1864/
│       ├── AI/
│       ├── Combat/
│       ├── Command/
│       ├── Formations/
│       ├── Orders/
│       ├── Units/
│       └── UI/
└── Strategy1864.uproject
```

The port should prefer stable domain names rather than carrying Unity prototype revision suffixes such as `09F30...` into permanent Unreal class names.
