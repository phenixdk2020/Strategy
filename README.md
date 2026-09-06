# PROJECT 1864

Historisk grand-strategy-projekt i Unity med real-time strategisk simulation og taktiske 3D-slag.

## Designmanual

Aktuel designbaseline: **v00.02.02**

- `docs/PROJECT-1864-Designmanual.md` — aktuel GitHub-læsbar designmanual med links til alle dele.
- `docs/parts/` — den aktuelle manual opdelt i versionsvenlige Markdown-dele.

Designmanualen er projektets designmæssige source of truth og skal opdateres på GitHub, når designbeslutninger eller implementeringsbaselines ændres. Git-historikken bevarer tidligere versioner. Den layoutede Word-master opdateres samtidig som projektartefakt og QA-kontrolleres før levering.

## Spilbar prototype

- `prototype/P0A-Unity-Battle/` — **P0A v00.00.02**, første spilbare Unity 3D battle vertical slice: 2 danske regimenter mod 2 preussiske regimenter, 1:10 visualisering, RTS-kamera, movement/attack orders, line/column, range, volleyild, sortkrudtsrøg, casualties, morale/cohesion, rout og simpel preussisk AI.
- Unity baseline: **6000.3.17f1 (Unity 6.3 LTS)**. P0A v00.00.02 retter den ugyldige Editor-version i v00.00.01, som kunne få Unity Hub til at vise `Unknown / Missing Editor version`.

Prototypekoden er bevidst asset-light: battlefield og simple soldater genereres ved runtime, så systemarkitekturen kan testes før historiske 3D-assets og animationer produceres.
