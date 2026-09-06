# P0A - Technical Notes

**Aktuel prototypebaseline: v00.00.07**  
**Unity baseline: 6000.6.0f1 (Unity 6.6), changeset `f7f8ed4d1e24`**

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

## P0A v00.00.07 – statisk QA-hardening

- Battle-resultatet er en terminal pause-state. Space og 1/2/3 ignoreres efter victory/defeat; `R` kan stadig restarte.
- RTS-kameraets pan/rotation bruger `Time.unscaledDeltaTime`, så controls er uafhængige af pause samt 1x/2x/3x battle speed.
- WASD og piletaster læses direkte i kamera-controlleren i stedet for via navngivne legacy Input Manager-akser.
- Musehjulszoom anvender et fast zoom-step og er dermed ikke afhængig af frame delta.
- `PlayerCommander` returnerer sikkert fra input-frame, hvis `Camera.main` ikke kan findes.
- `BattleManager` beskytter sin singleton mod dubletter og rydder `Instance` ved destruction.
- `BattleManager.OnGUI()` genbruger ét `Camera.main`-opslag pr. GUI-pass.

Den statiske gennemgang af alle aktuelle runtime- og editor-scripts fandt ingen yderligere klare C#-compile-blockere eller obsolete API-kald. Dette er ikke en erstatning for compile/Play-test i den fastlåste Unity-editor; Unity 6000.6.0f1 runtime-validering er fortsat release-gaten.

## Repository-metadata

GitHub-baselinen er stadig bevidst minimal. Under `ProjectSettings/` er kun `ProjectVersion.txt` versionsstyret, og `Assets/Scenes/PrototypeBattle.unity`, `.meta`-filer, `Packages/packages-lock.json` samt øvrige Unity-genererede metadata kan blive oprettet lokalt af Unity.

Disse lokale filer må ikke slettes eller overskrives blindt. Efter næste Unity-test skal `git status` inspiceres, og derefter træffes en særskilt beslutning om hvilke Unity-genererede metadata der skal indgå i den reproducerbare repository-baseline.

## Bevidste simplifikationer

- Direkte bevægelse uden NavMesh/pathfinding.
- Ingen ordre-delay/couriers endnu.
- Ingen bataljonsunderopdeling endnu.
- Ingen ammunition eller supply.
- Ingen sårede/POW-chain.
- Primitive visuals og immediate-mode HUD.
- Våbenparametre er prototype-tuning, ikke endelig historisk database.

## Næste foreslåede iteration P0B

P0B må først påbegyndes, når P0A v00.00.07 er compile-clean og runtime-valideret i Unity 6000.6.0f1.

1. Bataljoner som underformationer i regimentet.
2. Order delay og brigadechef-AI.
3. Rigtig cover/terrain sampling og line-of-sight.
4. Ammunition, reload/fire discipline og wounded vs killed.
5. NavMesh/formation pathfinding.
6. Historiske uniformer, faner og animationssystem.
7. Artilleribatteri og sanitet som første support formations.
