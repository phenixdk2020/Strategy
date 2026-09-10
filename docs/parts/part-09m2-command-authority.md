# part-09m2 — Kommando-hierarki (manuel / regiment / brigade)

Kompagnierne er verdensrums-løsrevet i 09l5. Regiment-pivottet er derfor tomt.
OfficerAI og BrigadeCommand tænker stadig i regiment-rum. 09m2 binder kæden.

## Niveauer

1. **MANUEL** — spilleren giver kompagni-waypoints (RMB). Officer AI slået fra.
2. **REG-AI** — regimentets egen officer fører regimentets kompagnier.
3. **BRG** — 7. Brigades officer slår sine underlagte regiment-officerer til og udsteder missioner. Officererne skriver videre til kompagnierne.

## Runtime

`PrototypeKampCommandAuthority09M2` (execution order 800):

- flytter det tomme regiment-pivot hen på kompagni-centroiden, så officer-beslutninger bruger rigtig position
- læser `OfficerAIController.Mission` / `MissionPoint` / `MissionTarget`
- kalder `company.IssueMove` med `DefaultLocalPosition`-offsets, så kompagnierne holder indbyrdes opstilling
- RMB på danske kompagnier sætter player-override = MANUEL

09l5 må ikke længere slukke officer-AI eller `OrderHold()` et regiment, mens officer-AI er aktiv.
