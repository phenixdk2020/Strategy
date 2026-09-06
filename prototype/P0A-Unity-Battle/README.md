# PROJECT 1864 - Unity Battle Prototype v00.00.02

Første spilbare taktiske 3D vertical slice til PROJECT 1864.

## Unity-version

Projektet er låst til **Unity 6000.3.17f1 (Unity 6.3 LTS)**. Unity Hub skal derfor kunne identificere Editor-versionen korrekt. Hvis præcis denne patch ikke er installeret, kan Unity Hub installere den, eller projektet kan åbnes i en nyere Unity 6.3 LTS-patch og opgraderes.

## Indhold

- 2 danske regimenter mod 2 preussiske regimenter.
- Ca. 1 grafisk soldat pr. 10 faktiske soldater.
- Proceduralt 3D-kampkort med bølgende terræn, vej, vandløb, gård, hegn og træer.
- RTS-kamera med pan, rotation og zoom.
- Klik/Shift+klik for valg af danske regimenter.
- Højreklik på terræn giver bevægelsesordre; højreklik på fjende giver angrebsordre.
- Line/column formationer og synlig effektiv skudafstand.
- Automatisk volleyild, sortkrudtsrøg, tab, morale, cohesion og rout.
- Simpel preussisk AI med direkte angreb og flankerende waypoint.
- Pause og 1x/2x/3x.

## Start

1. Installer Unity Hub og **Unity 6.3 LTS**. P0A v00.00.02 er markeret som `6000.3.17f1`.
2. Åbn `prototype/P0A-Unity-Battle` som Unity-projekt.
3. Første åbning opretter automatisk `Assets/Scenes/PrototypeBattle.unity`.
4. Tryk **Play**.

Hvis Unity Hub viser **Unknown / Missing Editor version**, er der brugt den gamle P0A v00.00.01. Hent v00.00.02 eller opdater `ProjectSettings/ProjectVersion.txt` fra GitHub.

## Styring

- WASD/piletaster: flyt kamera.
- Q/E: roter kamera.
- Musehjul: zoom.
- Venstreklik: vælg dansk regiment.
- Shift+venstreklik: multi-select.
- Højreklik terræn: flyt.
- Højreklik fjende: angrib.
- F: line formation.
- C: column formation.
- H: hold position.
- T: vis/skjul effektiv skudafstand.
- Space: pause.
- 1/2/3: spilhastighed.
- R: genstart slaget.

Prototypebegrænsninger og P0B-plan findes i `docs/P0A-TECHNICAL-NOTES.md`.
