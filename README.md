# PROJECT 1864

Historisk grand-strategy-projekt i Unity med real-time strategisk simulation og taktiske 3D-slag.

## Unity-projekt

Repositoryets **rod er selve Unity-projektet**. Unity Hub kan derfor clone `phenixdk2020/Strategy` og åbne repository-roden direkte.

Unity projektstruktur:

- `Assets/` — scripts, scenes, prefabs og senere assets.
- `Packages/` — Unity package manifest.
- `ProjectSettings/` — Unity project metadata og editor-version.
- `docs/` — designmanual og teknisk dokumentation.

Aktuel prototype: **P0A v00.00.07**.
Unity baseline: **6000.6.0f1 (Unity 6.6)**, changeset `f7f8ed4d1e24`.

Baseline er valgt, fordi projektets aktuelle testmaskine allerede har 6000.6.0f1 installeret. Det reducerer unødvendige Unity Hub-versionadvarsler og gør clone -> open-workflowet direkte.

### Aktiverede built-in Unity-moduler

P0A bruger Unity-funktionalitet, som i Unity 6.6 skal være eksplicit aktiveret i `Packages/manifest.json`:

- `com.unity.modules.imgui` — prototype-HUD og `GUIStyle`.
- `com.unity.modules.particlesystem` — sortkrudtsrøg.
- `com.unity.modules.physics` — raycasts og colliders til selection/orders.
- `com.unity.modules.audio` — `AudioListener` på kameraet.

### Aktuel compile- og QA-status

P0A v00.00.06 ryddede den første C#-compile-gate efter package-konfigurationen:

- `CS0136` i `Regiment.GetFormationPosition()` er rettet ved at undgå navnekollisionen på `column`.
- Obsolete `FindFirstObjectByType` er erstattet med `FindAnyObjectByType` for Unity 6.6.
- Den ubrugte `holdPosition`-state er fjernet, så `CS0414`-warningen ikke længere genereres.

P0A v00.00.07 er en statisk QA-hardening før næste Unity Play-test:

- Et afsluttet slag forbliver pauset; Space og 1/2/3 kan ikke genstarte simulationen efter victory/defeat. `R` genstarter fortsat slaget.
- RTS-kameraet bruger `Time.unscaledDeltaTime`, så pan og rotation ikke bliver 2x/3x hurtigere sammen med simulationen.
- WASD/piletaster læses direkte i kamera-controlleren i stedet for via navngivne legacy Input Manager-akser.
- Musehjulszoom er frame-rate-uafhængig.
- `PlayerCommander` springer inputbehandling over, hvis en `MainCamera` midlertidigt ikke findes, i stedet for at risikere en null reference.
- `BattleManager` beskytter sin singleton mod en dublet og rydder `Instance`, når den aktive manager destrueres.
- HUD'et cacher `Camera.main` pr. GUI-pass frem for at slå kameraet op gentagne gange pr. regiment.

Den statiske gennemgang fandt ingen yderligere klare C#-compile-blockere i den nuværende P0A-kode. **Unity 6000.6.0f1 compile- og Play-test er fortsat release-gaten**, før P0A markeres runtime-valideret.

## Designmanual

Aktuel designbaseline: **v00.02.07**

- `docs/PROJECT-1864-Designmanual.md` — aktuel GitHub-læsbar designmanual med links til alle dele.
- `docs/parts/` — manualen opdelt i versionsvenlige Markdown-dele.
- `docs/prototypes/` — tekniske noter for implementerede prototyper.

Designmanualen er projektets designmæssige source of truth og skal opdateres på GitHub, når designbeslutninger eller implementeringsbaselines ændres. Git-historikken bevarer tidligere versioner. Den layoutede Word-master opdateres samtidig som projektartefakt og QA-kontrolleres før levering.

## P0A battle prototype

P0A indeholder 2 danske regimenter mod 2 preussiske regimenter, ca. 1:10 visuel styrkerepræsentation, RTS-kamera, movement/attack orders, line/column formation, range, volleyild, sortkrudtsrøg, casualties, morale/cohesion, rout og simpel preussisk AI.

Prototypekoden er bevidst asset-light: battlefield og simple soldater genereres ved runtime, så systemarkitekturen kan testes før historiske 3D-assets og animationer produceres.

### Repository-metadata

Repositoryet har fortsat en bevidst minimal Unity-metadata-baseline: den aktuelle GitHub-version indeholder kun `ProjectSettings/ProjectVersion.txt` under `ProjectSettings/`, mens scene, `.meta`-filer, `packages-lock.json` og øvrige Unity-genererede metadata kan blive oprettet eller ændret lokalt ved første åbning. De må ikke slettes blindt. Efter næste runtime-test skal lokal `git status` gennemgås, før projektets permanente metadata/versioneringspolitik udvides.
