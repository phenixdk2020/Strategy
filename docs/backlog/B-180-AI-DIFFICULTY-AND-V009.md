# PROJECT 1864 — B-180–B-189: AI difficulty og v00.00.09 prioritering

**Status: BESLUTTET / NÆSTE IMPLEMENTERING**  
**Designbaseline: v00.02.08**  
**Target prototype: P0A v00.00.09 TEST**

## Prioritetsbeslutning

Officer-AI/delegeret kommando rykkes frem til **første gameplay-implementation efter v00.00.08-gaten**.

Der må ikke indsættes ammunition, cover, skirmishers, artillery expansion eller andre større gameplay-systemer mellem v00.00.08 og den første testbare AI-delegation.

Begrundelse: AI/delegation er en arkitektonisk kernefunktion for både spillerens egen command model og fjendens adfærd. Jo tidligere den testes, desto mindre risiko for at senere combat-, formation- og logistics-systemer bliver bygget omkring en for simpel eller asymmetrisk AI-model.

## B-180 — Symmetrisk officer-AI

**BESLUTTET.** Fjenden og spillerens delegerede formationer skal bruge samme grundlæggende officer-AI-model.

- Samme officer stats/personality inputs.
- Samme mission/autonomy model.
- Samme knowledge-state/fog-of-war constraints.
- Samme morale, cohesion, fatigue, ammunition og terrain constraints, når disse systemer findes.
- Ingen skjulte fjende-only tactical capabilities.

En dårlig fjendtlig officer forbliver dårlig på høj sværhedsgrad; difficulty må ikke omskrive officerens historiske stats.

## B-181 — Difficulty er separat fra officer quality

**BESLUTTET.** Spillets sværhedsgrad må primært ændre computerens overordnede beslutningskvalitet, koordinering og fejlmargin — ikke give skjulte combat-stat bonuses.

Difficulty må fx påvirke:

- hvor godt theatre/battle AI prioriterer objectives,
- hvor effektivt den fordeler reserves,
- hvor ofte den foretager unødvendige planændringer,
- hvor godt den koordinerer nabofomationer,
- hvor aggressivt den udnytter kendte muligheder,
- hvor stor ekstra beslutningsstøj/fejlmargin der lægges oven på officerens egne stats,
- hvor konsekvent den bruger de informationer, den faktisk har.

Difficulty må ikke give AI'en omniscience, gratis ammunition, skjult accuracy, ekstra morale eller andre uigennemsigtige buffs som standardmodel.

## B-182 — Første difficulty levels

**PLANLAGT til v00.00.09 MVP.** Første prototype bruger mindst tre niveauer:

- **Easy** — større decision delay/støj, mindre effektiv koordinering og dårligere target/position prioritering.
- **Normal** — referenceadfærd; officer stats/personality udnyttes uden ekstra fordel.
- **Hard** — bedre overordnet koordinering og lavere ekstra AI-fejlmargin, men fortsat bundet til officer stats og samme knowledge state.

En senere udgave kan tilføje `Very Hard`/`Historical`/custom sliders, men det er ikke nødvendigt for første AI-test.

## B-183 — P0A v00.00.09 MVP scope

v00.00.09 skal implementere den mindst mulige komplette vertikale AI-delegationsslice:

1. `AI UNIT: OFF/ON` på spillerstyrede regimenter.
2. Prototype officer profile pr. regiment med mindst Tactical Skill, Initiative og Aggressiveness/Caution.
3. `AI ON` kan udføre en begrænset mission: `Hold`, `Advance/Attack` eller `Defend current area`.
4. Officerens profile skal mærkbart påvirke reaction delay og lokale valg.
5. Fjendens regimenter skal bruge samme decision-kode i stedet for særskilt hardcoded opponent logic, hvor praktisk muligt.
6. Difficulty selector: Easy / Normal / Hard.
7. Difficulty ændrer AI decision quality/coordination/error margin, ikke combat stats.
8. UI viser `AI ON/OFF`, officer summary, current local task og en kort reason code.
9. Manuelt player override skal virke uden at ødelægge selection/movement/attack-regression.
10. Eksisterende v00.00.08 combat/reload/hit-feedback/casualty-visual skal fortsat fungere.

## B-184 — v00.00.09 bevidste begrænsninger

Følgende holdes ude af første AI-test for at bevare en lille testflade:

- fuld courier/order-delay simulation,
- brigade/battalion hierarchy,
- skirmisher deployment,
- prone/cover/fieldworks,
- ammunition logistics,
- advanced artillery fire-control,
- full fog-of-war sensor network,
- strategic campaign AI.

API/state skal dog designes, så disse kan tilføjes uden at erstatte officer-AI-kernen.

## B-185 — Officer prototype-data

**PLANLAGT.** v00.00.09 får midlertidige QA officer-profiler, ikke historiske vurderinger. Profilerne skal bevidst være forskellige, så man kan se forskel på adfærd.

Endelige officer stats skal senere komme fra historisk officer/OOB-data med provenance.

## B-186 — Enemy AI validation

Acceptance-test skal verificere, at fjendens tropper ikke bare bruger en global perfekt AI.

Mindst to fjendtlige regimenter med forskellige prototype-officerprofiler skal ved samme eller sammenlignelige situationer kunne reagere forskelligt på en forklarlig måde.

## B-187 — No-cheat difficulty rule

**BESLUTTET.** Easy/Normal/Hard må i standardopsætningen ikke ændre:

- weapon accuracy,
- reload speed,
- weapon range,
- morale values,
- cohesion values,
- casualty resistance,
- movement speed,
- ammunition quantity,
- hidden map knowledge.

Hvis der senere tilbydes separate handicap-bonuses, skal de være eksplicit synlige custom settings og ikke blandes sammen med AI difficulty.

## B-188 — AI telemetry

**PLANLAGT.** Til QA skal hver AI-beslutning kunne logges kompakt med mindst:

- unit/officer,
- mission,
- AI/difficulty state,
- perceived target/threat,
- chosen action,
- reason code,
- decision score/delay hvis relevant.

Dette er vigtigt for at kunne afgøre, om en dårlig beslutning skyldes bug, dårlig officer, mangelfuld information eller bevidst difficulty noise.

## B-189 — Release order

1. Frys og runtime-test v00.00.08.
2. Klassificer nødvendige Unity metadata uden destruktive git-operationer.
3. Opret v00.00.09 AI work branch fra den validerede baseline.
4. Implementer AI Unit ON/OFF + shared officer decision core.
5. Flyt eksisterende simple enemy behavior over på shared core.
6. Implementer Easy/Normal/Hard difficulty layer.
7. Tilføj UI/reason codes/telemetry.
8. Kør compile + full v00.00.08 regression + særskilt AI acceptance-test.
9. Først derefter fortsættes ammunition/casualty pipeline og øvrige combat systems.
