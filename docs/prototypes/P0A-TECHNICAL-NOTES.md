# P0A - Technical Notes

**Aktuel prototypebaseline: v00.00.08**  
**Designmanual: v00.02.08**  
**Unity baseline: 6000.6.0f1 (Unity 6.6), changeset `f7f8ed4d1e24`**

## Formål

P0A beviser det taktiske minimums-loop: select -> order -> move -> acquire target -> fire -> casualties/morale -> rout -> battle result.

## Arkitektur

- `PrototypeBootstrap`: bygger hele prototypeverdenen ved runtime uden eksterne assets.
- `Regiment`: authoritative regimentsimulation, weapon/reload/experience og 1:10 visualisering.
- `PlayerCommander`: selection og tactical orders.
- `RTSCameraController`: kamera.
- `BattleManager`: clock, UI, time scale, unit labels, combat feedback og victory state.
- `PrototypeProjectStartup` (Editor): opretter/åbner prototype-scenen og tilføjer den til Build Settings.

## Skalaprincip

`CurrentStrength` er authoritative faktisk mandskab. Det synlige antal modeller er `ceil(CurrentStrength / 10)`. Combat beregnes mellem regimenter; hver 3D-model er ikke en selvstændig combat-agent.

## P0A v00.00.07 - valideret observation

- Runtime-bootstrap opretter slagmark og kamera korrekt i Play Mode.
- Battle-resultatet er en terminal pause-state; `R` kan stadig restarte.
- RTS-kameraets pan/rotation bruger `Time.unscaledDeltaTime`.
- WASD/piletaster læses direkte.
- Musehjulszoom bruger fast step.
- Defensive guards findes for manglende `MainCamera`, dublet `BattleManager` og dublet-registering.

Den fulde smoke-test for alle controls blev ikke brugt som begrundelse for at markere v00.00.07 som endelig release; observationen dokumenterer specifikt, at bootstrap/rendering virker i den installerede Unity-version.

## P0A v00.00.08 - reload, experience og salve-feedback

### Weapon profiles

P0A bruger to midlertidige infantry weapon profiles:

- **Rifled muzzle-loader**: basis-reload 5,0 s, effective range 43, maximum range 55.
- **Dreyse needle rifle**: basis-reload 3,6 s, effective range 37, maximum range 49.

Tallene bevarer den tidligere P0A-cadence og er gameplay-tuning, ikke endelig historisk weapon-database.

### Experience og reload

`Experience` er en 0-100 regiment-state. Experience 50 er neutral. Reload-multiplikatoren interpolerer lineært fra 1,20 ved Experience 0 til 0,80 ved Experience 100.

Til QA bruges bevidst forskellige prototypeværdier:

- 1. Regiment: 55
- 5. Regiment: 42
- 8th Regiment: 65
- 18th Regiment: 50

Værdierne er kun QA-data og skal senere komme fra OOB/unit-data.

### Volley resolution og feedback

- Salver kan resolve til 0-16 direkte hits.
- 0 hits reducerer ikke `CurrentStrength`.
- Shock påvirker fortsat morale/cohesion, også når salven giver 0 direkte hits.
- Positive hits viser midlertidigt `Ramte N` over målet i ca. 1,45 sekunder.
- Feedback-timeren bruger `Time.unscaledTime`, så teksten er læsbar ved 2x/3x.
- Unit labels viser midlertidigt weapon short name, experience og beregnet reload for QA.

## Repository-metadata

GitHub-baselinen er stadig bevidst minimal. Under `ProjectSettings/` er kun `ProjectVersion.txt` versionsstyret, mens `Assets/Scenes/PrototypeBattle.unity`, `.meta`-filer, `Packages/packages-lock.json` samt øvrige Unity-genererede metadata kan være oprettet lokalt.

Disse filer må ikke slettes eller overskrives blindt. Efter v00.00.08-testen skal lokal `git status` inspiceres, og derefter træffes en særskilt beslutning om hvilke Unity-genererede metadata der skal indgå i den reproducerbare repository-baseline.

## Bevidste simplifikationer

- Direkte bevægelse uden NavMesh/pathfinding.
- Ingen ordre-delay/couriers endnu.
- Ingen bataljonsunderopdeling endnu.
- Ingen ammunition eller supply.
- Resolved hits reducerer fortsat strength direkte; ingen killed/wounded/missing-pipeline endnu.
- Ingen rigtig LOS/cover/smoke-simulation endnu.
- Primitive visuals og immediate-mode HUD.
- Våbenparametre og experience-værdier er prototype-tuning/QA-data.

## v00.00.08 release-gate

1. Ingen compiler errors eller blokkerende Console-fejl.
2. Reload varierer korrekt med weapon profile og experience.
3. Positive salver viser `Ramte N`.
4. 0-hit salver kan forekomme uden strength-tab.
5. Selection/orders/formation/range virker.
6. Pause samt 1x/2x/3x virker.
7. Kameraet forbliver timeScale-uafhængigt.
8. Rout og battle outcome virker.
9. Victory/defeat kan ikke genoptages med Space/1/2/3.
10. `R` restarter korrekt.

P0B påbegyndes først efter denne gate og efter repository-metadata er klassificeret.
