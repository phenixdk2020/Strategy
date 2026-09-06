# PROJECT 1864 — B-180–B-189: AI difficulty og v00.00.09 prioritering

**Status: BESLUTTET / AKTIV IMPLEMENTERING**  
**Designbaseline: v00.02.08**  
**Target prototype: P0A v00.00.09 TEST**

## Prioritetsbeslutning

Officer-AI/delegeret kommando er første gameplay-implementation efter v00.00.08-baselinen. Der indsættes ikke ammunition, cover, skirmishers, artillery expansion eller andre større gameplay-systemer foran den første testbare AI-delegation.

## B-180 — Symmetrisk officer-AI

**BESLUTTET / AKTIV.** Fjenden og spillerens delegerede formationer bruger samme `OfficerAIController` og samme officerprofil-model.

- Samme officer stats/personality inputs.
- Samme mission model.
- Samme morale/cohesion constraints.
- Samme senere knowledge-state/fog-of-war constraints.
- Ingen skjulte enemy-only tactical capabilities.

En dårlig fjendtlig officer forbliver dårlig på høj sværhedsgrad; difficulty omskriver ikke officerprofilen.

## B-181 — Difficulty er separat fra officer quality

**BESLUTTET.** Difficulty ændrer computerens ekstra decision efficiency/noise, ikke officerens faktiske stats og ikke combat-systemets tal.

I v00.00.09 er laget bevidst smalt:

- enemy reaction multiplier,
- enemy bounded decision noise.

Senere kan difficulty også påvirke theatre-level planning/coordination, men stadig uden omniscience eller skjulte combat buffs.

## B-182 — Første difficulty levels

**AKTIV v00.00.09.** Første prototype bruger:

- **Easy** — længere ekstra reaction delay og højere ekstra decision noise.
- **Normal** — referenceadfærd.
- **Hard** — kortere ekstra reaction delay og lavere ekstra decision noise.

Controls:

- `F6` Easy
- `F7` Normal
- `F8` Hard

Difficulty påvirker kun computerens preussiske side i denne Danmark-player P0A. Player-delegerede danske officerer bruger deres faktiske profil på referenceværdier uanset valgt enemy difficulty.

## B-183 — P0A v00.00.09 MVP scope

v00.00.09 implementerer den mindst mulige komplette AI-delegationsslice:

1. `AI UNIT: OFF/ON` på spillerstyrede danske regimenter.
2. Officerprofil pr. regiment med otte kerne-stats: Leadership, Inspiration, Tactical Skill, Initiative, Staff/Command Skill, Discipline/Obedience, Aggressiveness/Caution og Composure/Nerve.
3. Separat Officer Experience som stability/decision-noise input.
4. Missions: DefendArea, Hold, MoveToPoint, AttackTarget og AttackNearest.
5. Officerprofilen påvirker reaction delay, preferred engagement behaviour, mission adherence, stabilisation og decision noise.
6. Fjendens regimenter bruger samme shared controller.
7. Easy / Normal / Hard.
8. Difficulty ændrer decision layer, ikke combat stats.
9. HUD viser AI state, officer summary, current local task og reason code.
10. `AI-DIAG` telemetry logger state/mission/task/reason.
11. Manuelt player override skal virke uden at ødelægge selection/movement/attack.
12. Eksisterende v00.00.08 combat/reload/hit-feedback/casualty-visual skal regressionsbevares.

## B-184 — Bevidste begrænsninger

Følgende er ikke del af første AI-test:

- fuld courier/order-delay simulation,
- brigade/battalion hierarchy,
- autonomy Strict/Normal/Independent selector i runtime,
- skirmisher deployment,
- prone/cover/fieldworks,
- ammunition logistics,
- advanced artillery fire-control,
- full fog-of-war sensor network,
- strategic campaign AI,
- historiske officer ratings.

## B-185 — Officer prototype-data

**AKTIV QA-data.** De fire regimenter har bevidst forskellige fiktive QA-profiler. De må ikke fortolkes som historiske vurderinger. Endelige officer stats skal komme fra historisk officer/OOB-data med provenance.

Detaljer: `B-190-OFFICER-STATS-MODEL.md` og v00.00.09 release notes.

## B-186 — Enemy AI validation

Acceptance-test skal verificere, at fjendens tropper ikke bare bruger en global perfekt AI.

8th Regiment og 18th Regiment har forskellige QA-profiler og skal over flere sammenlignelige beslutninger kunne vise forskellige reaction/task patterns på en forklarlig måde.

## B-187 — No-cheat difficulty rule

**BESLUTTET.** Easy/Normal/Hard må ikke ændre:

- weapon accuracy,
- reload speed,
- weapon range,
- morale values,
- cohesion values,
- casualty resistance,
- movement speed,
- ammunition quantity,
- regiment strength,
- hidden map knowledge.

Hvis der senere tilbydes separate handicap-bonuses, skal de være eksplicit synlige custom settings og ikke blandes sammen med AI difficulty.

## B-188 — AI telemetry

**AKTIV v00.00.09.** AI-statusændringer logges kompakt som `AI-DIAG` med mindst:

- unit,
- team,
- officer,
- AI state,
- difficulty,
- mission,
- task,
- reason.

Senere udvides dette med perceived threat, score-breakdown og knowledge timestamp/confidence.

## B-189 — Release gate

v00.00.09 må først promoveres efter:

1. Ren Unity compile.
2. Shared officer controller installeret på alle regimenter.
3. AI UNIT ON/OFF fungerer på danske regimenter.
4. Enemy AI bruger shared core og ikke legacy decision loop.
5. Easy/Normal/Hard fungerer uden combat cheats.
6. Composure påvirker stress reaction/noise.
7. Reason codes/telemetry er brugbare.
8. Fuld v00.00.08 regression er ren.

Den fulde trin-for-trin test ligger i `docs/releases/P0A-v00.00.09-RELEASE-NOTES.md`.
