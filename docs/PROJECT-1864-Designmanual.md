# PROJECT 1864 — Designmanual

**Aktuel baseline: v00.02.08**

Grand Strategy i realtid + taktiske 3D-slag. Denne GitHub-udgave er opdelt i dele for overskuelig versionsstyring. Den layoutede Word-master opdateres parallelt som projektartefakt, mens GitHub-Markdown er den løbende designmæssige source of truth.

Projektets centrale intake-log for besluttede men endnu ikke implementerede funktioner, planlagte opgaver, research-emner og løse idéer ligger i [PROJECT-BACKLOG.md](PROJECT-BACKLOG.md). Større emner kan have detaljerede backlog-supplementer, som senere konsolideres ind i hovedbackloggen.

Den komplette dokumentation af **hvad der faktisk er implementeret i P0A v00.00.08 TEST**, hvad der kun er besluttet i designbaseline v00.02.08, kendte begrænsninger og den fulde Unity acceptance-test ligger i [P0A v00.00.08 Release Notes](releases/P0A-v00.00.08-RELEASE-NOTES.md).

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
- [Del 6: 29–36 — Milepæle, vertical slice, risici, datarelationer, historisk grounding og designbeslutninger](parts/part-06-29-36.md)
- [Del 7: 37 — Implementeringsstatus P0A Unity 3D Battle Prototype](parts/part-07-37-P0A.md)
- [Del 8: 38 — P0A v00.00.08 reload, experience, salve-feedback og enkel casualty-visual](parts/part-08-38-P0A-v08.md)
- [Release Notes — P0A v00.00.08 TEST, komplet delta og acceptance-test](releases/P0A-v00.00.08-RELEASE-NOTES.md)
- [Projekt-backlog — beslutninger, planlagte funktioner, research og idéer](PROJECT-BACKLOG.md)
- [Backlog B-160–B-169 — strategisk landudvikling](backlog/B-160-STRATEGIC-DEVELOPMENT.md)

## Versionshistorik

- **v00.02.08** — P0A v00.00.08 TEST: våbenprofilen styrer basis-reload, regimentets experience modificerer reload-tiden inden for et begrænset interval, positive salver viser kort combat feedback som `Ramte N`, og salver kan give 0 direkte hits. Første personeltab pr. regiment skaber desuden én enkel repræsentativ liggende casualty-figur ved tabsstedet; den er kun visualisering og skelner endnu ikke mellem dræbt/såret. Samme designbaseline fastlægger de efterfølgende krav om konkret ammunition og casualty-split i unit UI, skirmishers, forskellige artilleriklasser som feltkanon og haubits, erobringsbare/hestetrukne/manhandled kanoner, manuel artillerimåludpegning som standard med valgfri Auto Target/Hold Fire, supply-vogne, battlefield salvage, dragoner der kan sidde af og føre ildkamp, directional cover, prone/liggende stilling, hasty fieldworks samt en kontekstuel unit-command bar nederst på skærmen. Strategilaget er samtidig konkretiseret med regionale udviklingsprojekter for veje, broer, jernbane, telegraf, gårde, heste/remount-depoter/stutterier, specialiseret våbenindustri, tooling/produktionsomstilling, depoter og synlige ændringer på strategikortet. En central backlog, detaljerede backlog-supplementer og separate release notes er indført som projektregel, så implementerede funktioner, besluttede senere funktioner og åbne idéer ikke blandes sammen. P0A v00.00.07 er observeret fungerende i Play Mode med korrekt runtime-bootstrap/rendering; v00.00.08 afventer Unity compile/Play-validering.
- **v00.02.07** — P0A v00.00.07: statisk Unity 6.6 QA-hardening før runtime-validering. Battle-end er gjort terminalt pauset indtil restart, RTS-kameraet er gjort uafhængigt af `Time.timeScale`, WASD/piletaster læses direkte uden navngivne Input Manager-akser, musehjulszoom er frame-rate-uafhængig, og defensive guards er tilføjet for manglende `MainCamera` samt dublet `BattleManager`.
- **v00.02.06** — P0A v00.00.06: første C#-compile-gate i Unity 6.6 ryddet op. `CS0136` i regimentets formationskode er rettet, obsolete `FindFirstObjectByType` er erstattet af `FindAnyObjectByType`, og den ubrugte `holdPosition`-state er fjernet.
- **v00.02.05** — P0A v00.00.05: Unity 6.6 compile-fix ved at aktivere built-in modulerne IMGUI, Particle System, Physics og Audio i `Packages/manifest.json`. Fejlene CS1069 for `GUIStyle` og `ParticleSystem` er dermed adresseret ved projektkonfiguration frem for kode-workarounds.
- **v00.02.04** — Unity-baseline flyttet til den installerede `6000.6.0f1` (Unity 6.6), changeset `f7f8ed4d1e24`. P0A hævet til v00.00.04, så Unity Hub kan åbne den friske GitHub-clone uden missing-editor warning på testmaskinen.
- **v00.02.03** — Repository-layout fastlåst: `Strategy`-roden er selve Unity-projektet (`Assets`, `Packages`, `ProjectSettings`). Den tidligere placering `prototype/P0A-Unity-Battle/` er fjernet. P0A hævet til v00.00.03.
- **v00.02.02** — P0A v00.00.02: Unity Editor metadata rettet og låst til Unity 6000.3.17f1, så Unity Hub kan identificere projektet korrekt.
- **v00.02.01** — Første konkrete implementeringsbaseline: P0A Unity 3D battle prototype dokumenteret og koblet til roadmap/vertical slice.
- **v00.02.00** — Expanded systems baseline: økonomi, udvikling, handel, forskning, rekruttering, træning, sanitet/fanger, regimentshistorik, faner samt unit traits og formation specialisations.
- **v00.01.00** — Første samlede designbaseline.

## Projektregel

Designmanualen skal opdateres både som layoutet Word-master og her i GitHub, når designbeslutninger eller implementeringsbaselines ændres. Git-historikken bevarer tidligere udgaver af Markdown-delene. Nye beslutninger og idéer skal desuden registreres i backloggen, så de ikke kun eksisterer i chatsamtaler. Hver testbuild skal have tydelig release-dokumentation, der adskiller **implementeret nu** fra **besluttet senere**.
