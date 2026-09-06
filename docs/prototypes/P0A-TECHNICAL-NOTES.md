# P0A - Technical Notes

## Formål

P0A beviser det taktiske minimums-loop: select -> order -> move -> acquire target -> fire -> casualties/morale -> rout -> battle result.

## Arkitektur

- `PrototypeBootstrap`: bygger hele prototypeverdenen ved runtime uden eksterne assets.
- `Regiment`: authoritative regimentsimulation og 1:10 visualisering.
- `PlayerCommander`: selection og tactical orders.
- `RTSCameraController`: kamera.
- `BattleManager`: clock, UI, time scale, unit labels og victory state.
- `PrototypeProjectStartup` (Editor): opretter/åbner prototype-scenen og tilføjer den til Build Settings.

## Skalaprincip

`CurrentStrength` er authoritative faktisk mandskab. Det synlige antal modeller er `ceil(CurrentStrength / 10)`. Combat beregnes mellem regimenter; hver 3D-model er ikke en selvstændig combat-agent.

## Bevidste simplifikationer

- Direkte bevægelse uden NavMesh/pathfinding.
- Ingen ordre-delay/couriers endnu.
- Ingen bataljonsunderopdeling endnu.
- Ingen ammunition eller supply.
- Ingen sårede/POW-chain.
- Primitive visuals og immediate-mode HUD.
- Våbenparametre er prototype-tuning, ikke endelig historisk database.

## Næste foreslåede iteration P0B

1. Bataljoner som underformationer i regimentet.
2. Order delay og brigadechef-AI.
3. Rigtig cover/terrain sampling og line-of-sight.
4. Ammunition, reload/fire discipline og wounded vs killed.
5. NavMesh/formation pathfinding.
6. Historiske uniformer, faner og animationssystem.
7. Artilleribatteri og sanitet som første support formations.
