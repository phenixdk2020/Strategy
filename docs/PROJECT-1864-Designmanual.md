# PROJECT 1864 — Designmanual

**Aktuel designbaseline: v00.02.08**  
**Aktuel prototype-workbranch: P0A v00.00.09 OFFICER AI TEST**

Grand Strategy i realtid + taktiske 3D-slag. Denne GitHub-udgave er opdelt i dele for overskuelig versionsstyring. Den layoutede Word-master opdateres parallelt som projektartefakt, mens GitHub-Markdown er den løbende designmæssige source of truth.

Projektets centrale intake-log for besluttede men endnu ikke implementerede funktioner, planlagte opgaver, research-emner og løse idéer ligger i [PROJECT-BACKLOG.md](PROJECT-BACKLOG.md). Større emner kan have detaljerede backlog-supplementer, som senere konsolideres ind i hovedbackloggen.

Den komplette dokumentation af **hvad der implementeres og skal testes i P0A v00.00.09**, officerstats, AI difficulty, kendte begrænsninger og den fulde Unity acceptance-test ligger i [P0A v00.00.09 Release Notes](releases/P0A-v00.00.09-RELEASE-NOTES.md).

## Indhold

- [Del 1: 1–6 — Executive summary, slutvision, strategisk realtid, kort, nationer og OOB](parts/part-01-01-06.md)
- [Del 2: 7–12 — Enheder, officerer, ordrer, march, logistik og fog of war](parts/part-02-07-12.md)
- [Del 3A: 13–19 — Taktiske 3D-slag, kamp, våbenarter, casualties, retreat og flåde](parts/part-03a-13-19.md)
- [Del 3B: 20 — Befolkning, økonomi, byudvikling, industri, handel, forskning, rekruttering, træning, sanitet/fanger, regimentshistorik og perks](parts/part-03b-20-20.md)
- [Del 3C: 20.16 — Strategisk landudvikling: veje, jernbane, gårde, hesteopdræt, våbenindustri og regionale projekter](parts/part-03c-20-16-strategic-development.md)
- [Del 4: 21–27 — Strategisk/taktisk AI, terræn, performance, UI, save/modding og historisk datamodel](parts/part-04-21-27.md)
- [Del 5A: Feature-arkitektur F00–F15](parts/part-05a-F00-F15.md)
- [Del 5B: Feature-arkitektur F16–F31](parts/part-05b-F16-F31.md)
- [Del 5C: Feature-arkitektur F32–F47](parts/part-05c-F32-F47.md)
- [Del 6: 29–36 — Milepæle, immediate prototype sequence, vertical slice, risici, datarelationer, historisk grounding og designbeslutninger](parts/part-06-29-36.md)
- [Del 7: 37 — Implementeringsstatus P0A Unity 3D Battle Prototype](parts/part-07-37-P0A.md)
- [Del 8: 38 — P0A v00.00.08 reload, experience, salve-feedback og enkel casualty-visual](parts/part-08-38-P0A-v08.md)
- [Release Notes — P0A v00.00.08 TEST](releases/P0A-v00.00.08-RELEASE-NOTES.md)
- [Release Notes — P0A v00.00.09 OFFICER AI TEST](releases/P0A-v00.00.09-RELEASE-NOTES.md)
- [Projekt-backlog — beslutninger, planlagte funktioner, research og idéer](PROJECT-BACKLOG.md)
- [Backlog B-160–B-169 — strategisk landudvikling](backlog/B-160-STRATEGIC-DEVELOPMENT.md)
- [Backlog B-170–B-179 — Officer AI, delegeret kommando og AI Unit ON/OFF](backlog/B-170-OFFICER-AI-DELEGATION.md)
- [Backlog B-180–B-189 — symmetrisk fjende-AI, sværhedsgrad og v00.00.09-prioritet](backlog/B-180-AI-DIFFICULTY-AND-V009.md)
- [Backlog B-190–B-199 — officerstats, Composure/Nerve og AI decision model](backlog/B-190-OFFICER-STATS-MODEL.md)

## v00.00.09 Officer AI — implementeringsstatus

P0A v00.00.09 på arbejdsbranchen implementerer den første shared officer-AI-kernel. Danske regimenter kan delegeres med `AI UNIT ON/OFF`, mens preussiske regimenter bruger samme `OfficerAIController` som standard. Officerprofilen består af Leadership, Inspiration, Tactical Skill, Initiative, Staff/Command Skill, Discipline/Obedience, Aggressiveness/Caution og Composure/Nerve samt separat Officer Experience.

Composure/Nerve er et aktivt AI-input: under stigende battle stress påvirker den både reaction delay og decision noise. Dermed holdes officerens egen evne til at bevare overblik adskilt fra hans Leadership, Inspiration og regimentets morale.

Easy/Normal/Hard påvirker i første prototype kun den computerstyrede preussiske sides ekstra reaction/noise layer. Difficulty må ikke ændre weapon accuracy, reload, range, movement, morale, cohesion, casualties eller skjult viden. Player-delegerede danske officerer bruger deres faktiske QA-profil på referenceindstillinger uanset enemy difficulty.

Alle officerprofiler i v00.00.09 er QA-data og er ikke historiske ratings. Historiske officerdata skal senere komme fra kildebelagt OOB/officer-database.

## Versionshistorik

- **v00.02.08 / P0A v00.00.09 work branch** — Officer AI er rykket frem som næste gameplay-gate. Første implementation har shared `OfficerAIController`, `AI UNIT ON/OFF`, missions for defend/hold/move/attack, otte kerne-officerstats + separat Experience, Composure-baseret stressreaktion, Easy/Normal/Hard uden combat cheats, reason codes og `AI-DIAG` telemetry. Den eksisterende v00.00.08 combat/reload/feedback/casualty-visual skal fortsat bestå regressionstesten. v00.00.09 er TEST og må først promoveres efter Unity compile/Play acceptance.
- **v00.02.08 / P0A v00.00.08** — Våbenprofil styrer basis-reload, regimentets experience modificerer reload-tiden bounded, positive salver viser `Ramte N`, salver kan give 0 direkte hits, og første personeltab pr. regiment skaber én repræsentativ liggende casualty-figur. Designbaselinen fastlægger desuden konkret ammunition/casualty split, skirmishers, artilleriklasser, hestetrukket/manhandled artilleri, manuel artillerimåludpegning, supply-vogne, salvage, dragoner, directional cover, prone, hasty fieldworks, strategisk landudvikling samt officer/delegation/difficulty-retningen.
- **v00.02.07** — P0A v00.00.07: statisk Unity 6.6 QA-hardening før runtime-validering. Battle-end er terminalt pauset indtil restart, RTS-kamera timeScale-uafhængigt og defensive guards forbedret.
- **v00.02.06** — Unity compile-gate: `CS0136` rettet, obsolete object lookup erstattet og unused state fjernet.
- **v00.02.05** — Built-in IMGUI, Particle System, Physics og Audio moduler aktiveret.
- **v00.02.04** — Unity baseline flyttet til 6000.6.0f1.
- **v00.02.03** — Repository-roden fastlåst som Unity project root.
- **v00.02.02** — Unity Editor metadata/version rettet.
- **v00.02.01** — Første konkrete P0A implementation koblet til roadmap.
- **v00.02.00** — Expanded systems baseline: økonomi, udvikling, handel, forskning, rekruttering, træning, sanitet/fanger, regimentshistorik, faner og traits.
- **v00.01.00** — Første samlede designbaseline.

## Projektregel

Designmanualen skal opdateres både som layoutet Word-master og her i GitHub, når designbeslutninger eller implementeringsbaselines ændres. Git-historikken bevarer tidligere udgaver af Markdown-delene. Nye beslutninger og idéer registreres desuden i backloggen. Hver testbuild skal have tydelig release-dokumentation, der adskiller **implementeret nu** fra **besluttet senere**.
