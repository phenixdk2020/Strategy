# PROJECT 1864

Historisk grand-strategy-projekt i Unity med real-time strategisk simulation og taktiske 3D-slag.

## Unity-projekt

Repositoryets **rod er nu selve Unity-projektet**. Det betyder, at Unity Hub kan clone `phenixdk2020/Strategy` og åbne repository-roden direkte.

Unity projektstruktur:

- `Assets/` — scripts, scenes, prefabs og senere assets.
- `Packages/` — Unity package manifest.
- `ProjectSettings/` — Unity project metadata og editor-version.
- `docs/` — designmanual og teknisk dokumentation.

Aktuel prototype: **P0A v00.00.03**.
Unity baseline: **6000.3.17f1 (Unity 6.3 LTS)**.

Hvis Unity Hub ikke har præcis 6000.3.17f1 installeret, kan projektet åbnes med en anden installeret Unity 6.3 LTS patch; Unity kan i så fald bede om at opgradere projektmetadata.

## Designmanual

Aktuel designbaseline: **v00.02.03**

- `docs/PROJECT-1864-Designmanual.md` — aktuel GitHub-læsbar designmanual med links til alle dele.
- `docs/parts/` — manualen opdelt i versionsvenlige Markdown-dele.
- `docs/prototypes/` — tekniske noter for implementerede prototyper.

Designmanualen er projektets designmæssige source of truth og skal opdateres på GitHub, når designbeslutninger eller implementeringsbaselines ændres. Git-historikken bevarer tidligere versioner. Den layoutede Word-master opdateres samtidig som projektartefakt og QA-kontrolleres før levering.

## P0A battle prototype

P0A indeholder 2 danske regimenter mod 2 preussiske regimenter, ca. 1:10 visuel styrkerepræsentation, RTS-kamera, movement/attack orders, line/column formation, range, volleyild, sortkrudtsrøg, casualties, morale/cohesion, rout og simpel preussisk AI.

Prototypekoden er bevidst asset-light: battlefield og simple soldater genereres ved runtime, så systemarkitekturen kan testes før historiske 3D-assets og animationer produceres.
