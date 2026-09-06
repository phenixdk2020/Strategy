# PROJECT 1864

Historisk grand-strategy-projekt i Unity med real-time strategisk simulation og taktiske 3D-slag.

## Unity-projekt

Repositoryets **rod er selve Unity-projektet**. Unity Hub kan derfor clone `phenixdk2020/Strategy` og åbne repository-roden direkte.

Unity projektstruktur:

- `Assets/` — scripts, scenes, prefabs og senere assets.
- `Packages/` — Unity package manifest.
- `ProjectSettings/` — Unity project metadata og editor-version.
- `docs/` — designmanual, backlog, release notes og teknisk dokumentation.

Aktuel work-branch prototype: **P0A v00.00.09 OFFICER AI TEST**.  
Seneste main-baseline: **P0A v00.00.08 TEST**.  
Aktuel designbaseline: **v00.02.08**.  
Unity baseline: **6000.6.0f1 (Unity 6.6)**, changeset `f7f8ed4d1e24`.

## v00.00.09 Officer AI

Den komplette implementation-, scope- og acceptance-dokumentation ligger i:

- **`docs/releases/P0A-v00.00.09-RELEASE-NOTES.md`** — fuld beskrivelse af Officer AI, stats, difficulty, controls, telemetry, begrænsninger og Unity-test.
- `docs/backlog/B-170-OFFICER-AI-DELEGATION.md` — delegation, AI UNIT ON/OFF, missions og transparency.
- `docs/backlog/B-180-AI-DIFFICULTY-AND-V009.md` — shared enemy/player AI, Easy/Normal/Hard og no-cheat-regler.
- `docs/backlog/B-190-OFFICER-STATS-MODEL.md` — otte officerstats, Composure/Nerve og første decision model.

### Nye runtime-systemer

- `OfficerProfile.cs` — QA-officerprofil.
- `OfficerAIController.cs` — fælles officerbaseret decision core.
- `OfficerAIPrototypeManager.cs` — runtime-installation, AI UNIT toggle, difficulty, input og QA HUD.

### Officerprofil

Første v00.00.09-profil bruger otte 0–100 dimensioner:

1. Leadership
2. Inspiration
3. Tactical Skill
4. Initiative
5. Staff / Command Skill
6. Discipline / Obedience
7. Aggressiveness ↔ Caution
8. Composure / Nerve

Officer Experience gemmes separat og bruges primært som stabilitets-/decision-noise-input.

**Composure/Nerve er aktiv i første build.** Lav Composure giver større stress-relateret reaction delay og mere decision noise, når morale/cohesion falder. Den er bevidst adskilt fra Leadership, Inspiration og regimentets egen morale.

### AI UNIT ON/OFF

- Danske regimenter starter `AI OFF`.
- Preussiske regimenter starter `AI ON`.
- `A` toggler AI UNIT for valgte danske regimenter.
- Højreklik med AI ON bliver en officer-mission til move/attack.
- `H` bliver Hold mission for delegerede regimenter.

Spillerens delegerede regimenter og fjendens regimenter bruger samme `OfficerAIController`.

### AI difficulty

- `F6` = Easy
- `F7` = Normal
- `F8` = Hard

Difficulty påvirker i P0A kun computerens preussiske decision layer: reaction multiplier og bounded decision noise. Den ændrer **ikke** accuracy, reload, range, movement speed, morale, cohesion, casualties, strength eller skjult map knowledge.

Player-delegerede danske officerer bliver derfor ikke kunstigt dårligere på Easy eller bedre på Hard.

### AI transparency

Test-HUD viser bl.a.:

- AI ON/OFF,
- QA officer,
- Tactical Skill,
- Initiative,
- Composure,
- current task,
- reason code.

Console logger kompakte `AI-DIAG` entries med unit, team, officer, difficulty, mission, task og reason.

Alle officerprofiler i v00.00.09 er bevidste QA-data og **ikke historiske ratings**.

## v00.00.08 baseline der skal regressionsbevares

v00.00.09 bygger oven på v00.00.08 og må ikke regressere:

- weapon profile-baseret reload,
- regiment Experience modifier,
- 0-hit volleys,
- `Ramte N`,
- enkel repræsentativ casualty-visual,
- selection/movement/attack,
- line/column/hold/range,
- pause + 1x/2x/3x,
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

Designmanualen er projektets designmæssige source of truth og opdateres samtidig med den layoutede Word-master ved design-, arkitektur- og implementeringsændringer.

## P0A battle prototype

P0A indeholder 2 danske regimenter mod 2 preussiske regimenter, ca. 1:10 visuel styrkerepræsentation, RTS-kamera, movement/attack orders, line/column formation, range, volleyild, sortkrudtsrøg, weapon/experience-afledt reload, combat feedback, enkel casualty-visual, morale/cohesion, rout samt v00.00.09's shared officer-AI prototype.

Prototypekoden er bevidst asset-light: battlefield og simple soldater genereres ved runtime, så systemarkitekturen kan testes før historiske 3D-assets og animationer produceres.

### Repository-metadata

Repositoryet har fortsat en minimal Unity-metadata-baseline. Lokale Unity-genererede `.meta`, scene-, `packages-lock.json`- og `ProjectSettings`-filer bevares sikkert af updater-workflowet og skal klassificeres særskilt til den reproducerbare repository-baseline. De må ikke slettes blindt.
