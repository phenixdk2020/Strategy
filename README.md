# PROJECT 1864

Historisk grand-strategy-projekt i Unity med real-time strategisk simulation og taktiske 3D-slag.

## Unity-projekt

Repositoryets **rod er selve Unity-projektet**. Unity Hub kan derfor clone `phenixdk2020/Strategy` og åbne repository-roden direkte.

Unity projektstruktur:

- `Assets/` — scripts, scenes, prefabs og senere assets.
- `Packages/` — Unity package manifest.
- `ProjectSettings/` — Unity project metadata og editor-version.
- `docs/` — designmanual og teknisk dokumentation.

Aktuel prototype: **P0A v00.00.08**.  
Aktuel designbaseline: **v00.02.08**.  
Unity baseline: **6000.6.0f1 (Unity 6.6)**, changeset `f7f8ed4d1e24`.

### Aktiverede built-in Unity-moduler

- `com.unity.modules.imgui` — prototype-HUD og `GUIStyle`.
- `com.unity.modules.particlesystem` — sortkrudtsrøg.
- `com.unity.modules.physics` — raycasts og colliders til selection/orders.
- `com.unity.modules.audio` — `AudioListener` på kameraet.

## Aktuel QA-status

P0A v00.00.07 blev åbnet i Unity 6000.6.0f1, og runtime-bootstrap/renderingen blev observeret fungerende i Play Mode. Kameraet og den procedurale battle-scene oprettes korrekt.

P0A v00.00.08 bygger videre med en afgrænset combat-QA-udvidelse:

- infantry weapon profile styrer basis-reload,
- regimentets Experience 0-100 modificerer reload-tiden bounded fra +20 % til -20 %,
- Experience 50 er neutral,
- salver kan resolve til 0 direkte hits,
- positive hits viser kort `Ramte N` over målet,
- unit labels viser weapon, Experience og beregnet reload til direkte QA,
- første strength loss for et regiment opretter én enkel repræsentativ liggende casualty-figur ved tabsstedet; den er kun visualisering og skelner endnu ikke mellem dræbt/såret.

De fire prototype-regimenters experience-værdier er bevidst forskellige testdata og er **ikke historiske vurderinger**. Senere skal experience og weapon assignment komme fra OOB/unit/weapon-databaser.

**v00.00.08 er endnu ikke runtime-valideret.** Næste release-gate er compile uden nye fejl og fuld Play regressionstest af combat, casualty-visual, kamera, controls, pause/speed, rout og battle outcome.

## Designmanual

- `docs/PROJECT-1864-Designmanual.md` — aktuel GitHub-læsbar designmanual.
- `docs/PROJECT-BACKLOG.md` — beslutninger, planlagte funktioner, research og idéer som ikke nødvendigvis er aktive endnu.
- `docs/parts/` — versionsvenlige designmanual-dele.
- `docs/prototypes/` — tekniske noter for implementerede prototyper.

Designmanualen er projektets designmæssige source of truth og opdateres samtidig med den layoutede Word-master ved design-, arkitektur- og implementeringsændringer.

## P0A battle prototype

P0A indeholder 2 danske regimenter mod 2 preussiske regimenter, ca. 1:10 visuel styrkerepræsentation, RTS-kamera, movement/attack orders, line/column formation, range, volleyild, sortkrudtsrøg, weapon/experience-afledt reload, combat feedback, en enkel casualty-visual, casualties, morale/cohesion, rout og simpel preussisk AI.

Prototypekoden er bevidst asset-light: battlefield og simple soldater genereres ved runtime, så systemarkitekturen kan testes før historiske 3D-assets og animationer produceres.

### Repository-metadata

Repositoryet har fortsat en minimal Unity-metadata-baseline. Den lokale Unity-test har genereret `.meta`, scene-, `packages-lock.json`- og `ProjectSettings`-filer, som bevares og gennemgås efter v00.00.08-testen. De må ikke slettes blindt. Målet er derefter en reproducerbar fresh-clone baseline før P0B.
