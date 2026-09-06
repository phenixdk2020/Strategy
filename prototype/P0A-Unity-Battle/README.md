# PROJECT 1864 - Unity Battle Prototype v00.00.01

Første spilbare taktiske 3D vertical slice til PROJECT 1864.

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

1. Installer Unity Hub og en Unity 6 editor.
2. Åbn `prototype/P0A-Unity-Battle` som Unity-projekt.
3. Første åbning opretter automatisk `Assets/Scenes/PrototypeBattle.unity`.
4. Tryk **Play**.

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
