# PROJECT 1864

Historisk grand-strategy-projekt i Unity med real-time strategisk simulation og taktiske 3D-slag.

## Unity-projekt

Repositoryets **rod er selve Unity-projektet**. Unity Hub kan derfor clone `phenixdk2020/Strategy` og åbne repository-roden direkte.

Unity projektstruktur:

- `Assets/` — scripts, scenes, prefabs og senere assets.
- `Packages/` — Unity package manifest.
- `ProjectSettings/` — Unity project metadata og editor-version.
- `docs/` — designmanual, backlog, release notes og teknisk dokumentation.

Aktuel work-branch prototype: **P0A v00.00.09 TACTICAL COMMAND TEST**.  
Seneste main-baseline: **P0A v00.00.08 TEST**.  
Aktuel designbaseline: **v00.02.08**.  
Unity baseline: **6000.6.0f1 (Unity 6.6)**, changeset `f7f8ed4d1e24`.

## v00.00.09 Tactical Command Test

v00.00.09 samler den første testbare officer-AI med de grundlæggende tactical-command funktioner, som er nødvendige for at kunne teste officeradfærd meningsfuldt.

### Scenario

- **Danmark = forsvarere** omkring den vestlige højderyg/gård.
- **Preussen = angribere** fra øst.
- Battlefield er udvidet fra **180x120 til 360x240**.
- Startafstanden er større, så spilleren kan pause, inspicere og manøvrere før kontakt.
- Kameraets bevægelses- og zoomgrænser er udvidet tilsvarende.

### Skydning og fire discipline

Den gamle 360° range-ring er erstattet af en **retningsbestemt 120° fire fan**, som følger regimentets facing. Fanen åbner ud fra formationens frontage, så soldaterne ikke kun repræsenteres som et smalt center-ray.

Tre visuelle grænser følger samme retning:

- **Close** = 0–50 % af `EffectiveRange`.
- **Medium** = op til `EffectiveRange`.
- **Long** = op til `MaximumRange`.

Et regiment kan i MVP kun skyde på et mål, hvis målet både:

1. er inden for den valgte fire-policy range, og
2. er inden for den fremadrettede fire arc.

Fire policy kan sættes til:

- `HOLD` — ingen automatisk ild,
- `CLOSE` — åbn ild ved Close range,
- `MEDIUM` — åbn ild ved EffectiveRange,
- `LONG` — åbn ild helt ud til MaximumRange.

Accuracy følger en **kontinuerlig distancekurve**: jo tættere målet er, jo nemmere er det at ramme. Close/Medium/Long er UI- og ordregrænser; de skaber ikke kunstige spring i hit chance.

Weapon-profile reload, Experience modifier, 0-hit volleys og `Ramte N` fra v00.00.08 er bevaret.

### Officer til alle regimenter

Alle fire testregimenter får ved runtime:

- `OfficerProfile`
- `OfficerAIController`

Officerprofilen bruger otte 0–100 dimensioner:

1. Leadership
2. Inspiration
3. Tactical Skill
4. Initiative
5. Staff / Command Skill
6. Discipline / Obedience
7. Aggressiveness ↔ Caution
8. Composure / Nerve

Officer Experience gemmes separat og reducerer primært decision noise.

Alle profiler i v00.00.09 er QA-data og **ikke historiske ratings**.

### AI UNIT ON/OFF

- Danske regimenter starter `AI OFF`.
- Preussiske regimenter starter `AI ON`.
- **`I` toggler AI UNIT ON/OFF** for valgte danske regimenter.
- `A` er fortsat kamera-left i WASD og bruges ikke længere som AI-toggle.
- Højreklik på terræn/mål bliver en Officer AI mission, når AI er ON.
- `H` bliver Hold mission for delegerede regimenter.

Spillerens delegerede regimenter og fjendens regimenter bruger samme `OfficerAIController`.

### Officer-command menu

Når et eller flere danske regimenter er valgt, vises en lille command-menu med:

- AI ON/OFF,
- **DEFENSIVE / BALANCED / OFFENSIVE** doctrine,
- 0–100 slider for commander **forsigtig ↔ aggressiv** intent,
- `HOLD / CLOSE / MEDIUM / LONG` fire policy.

Commander intent er en ordre-bias og overskriver ikke officerens personlighed. Før doctrine-bias vægtes effektiv aggression i MVP som:

- 70 % officerens `Aggressiveness`,
- 30 % commander order intent.

Officerens stats bestemmer derfor fortsat den konkrete udførelse.

### Fjendens Officer AI

Preussiske testregimenter er angribere, men skal ikke blot løbe lige frem:

- de bruger samme officerstats som player-delegated AI,
- de vurderer preferred engagement range,
- skifter fra column til line nær engagement,
- kan stoppe/stabilisere ved morale pressure,
- 18th Regiment får et flank/approach waypoint før det reassesserer angrebet,
- de åbner som default ild ved Medium range,
- difficulty giver ingen skjulte combat bonuses.

### AI difficulty

- `F6` = Easy
- `F7` = Normal
- `F8` = Hard

Difficulty påvirker kun computerens preussiske decision layer: reaction multiplier og bounded decision noise. Den ændrer **ikke** accuracy, reload, range, movement speed, morale, cohesion, casualties, strength eller skjult map knowledge.

### Simulationstid

Topbaren har clickable:

`PLAY | PAUSE | x0.5 | x2 | x5 | x20 | dato/klokkeslæt`

Taster:

- `Space` = pause/resume
- `0` = x0.5
- `1` = x1 / Play
- `2` = x2
- `3` = x5
- `4` = x20

Pause stopper AI, movement, reload/fire progression og battle clock samlet. Kamera/UI er fortsat brugbart. Tidsrater ændrer kun simulationens rate og giver ingen combat-bonus.

### AI transparency

Test-HUD og `AI-DIAG` viser bl.a.:

- AI ON/OFF,
- officer,
- doctrine,
- commander aggression intent,
- effective aggression,
- fire policy,
- mission,
- task,
- reason code.

## Controls — v00.00.09

- Click = select
- Shift+click = multi-select
- Right-click = move / attack mission
- F = line
- C = column
- H = hold
- T = show/hide directional range fan
- I = AI UNIT ON/OFF
- WASD/arrows = camera
- Q/E = rotate camera
- mouse wheel = zoom
- Space = pause/resume
- 0/1/2/3/4 = x0.5/x1/x2/x5/x20
- F6/F7/F8 = Easy/Normal/Hard enemy AI
- R = restart

## v00.00.08 baseline der skal regressionsbevares

v00.00.09 bygger oven på v00.00.08 og må ikke regressere:

- weapon profile-baseret reload,
- regiment Experience modifier,
- 0-hit volleys,
- `Ramte N`,
- enkel repræsentativ casualty-visual,
- selection/movement/attack,
- line/column/hold,
- kamera,
- morale/cohesion/rout,
- battle result og R restart.

## Aktiverede built-in Unity-moduler

- `com.unity.modules.imgui` — prototype-HUD og `GUIStyle`.
- `com.unity.modules.particlesystem` — sortkrudtsrøg.
- `com.unity.modules.physics` — raycasts og colliders til selection/orders.
- `com.unity.modules.audio` — `AudioListener` på kameraet.

## Designmanual

- `docs/PROJECT-1864-Designmanual.md` — aktuel GitHub-læsbar designmanual.
- `docs/PROJECT-BACKLOG.md` — beslutninger, planlagte funktioner, research og idéer.
- `docs/backlog/` — detaljerede backlog-supplementer.
- `docs/parts/` — versionsvenlige designmanual-dele.
- `docs/prototypes/` — tekniske noter.
- `docs/releases/` — detaljerede release notes og acceptance gates.

Designmanualen er projektets designmæssige source of truth og opdateres sammen med den layoutede Word-master ved væsentlige design-, arkitektur- og implementeringsændringer.

## P0A battle prototype

P0A indeholder 2 danske regimenter mod 2 preussiske regimenter, ca. 1:10 visuel styrkerepræsentation, RTS-kamera, movement/attack orders, line/column formation, retningsbestemt fire fan, volleyild, sortkrudtsrøg, weapon/experience-afledt reload, combat feedback, enkel casualty-visual, morale/cohesion, rout samt v00.00.09's shared Officer AI og tactical-command menu.

Prototypekoden er bevidst asset-light: battlefield og simple soldater genereres ved runtime, så systemarkitekturen kan testes før historiske 3D-assets og animationer produceres.

### Repository-metadata

Repositoryet har fortsat en minimal Unity-metadata-baseline. Lokale Unity-genererede `.meta`, scene-, `packages-lock.json`- og `ProjectSettings`-filer bevares sikkert af updater-workflowet og skal klassificeres særskilt til den reproducerbare repository-baseline. De må ikke slettes blindt.
