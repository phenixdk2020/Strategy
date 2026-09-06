# PROJECT 1864 — Backlog B-170–B-179: Officer AI og delegeret kommando

**Status: BESLUTTET / AKTIV IMPLEMENTERING**  
**Designbaseline: v00.02.08**  
**Target prototype: P0A v00.00.09 TEST**

Dette supplement fastlægger spillerens mulighed for at delegere styringen af egne formationer til deres officerer. Systemet bygger oven på commander stats, personality, autonomy, order delay, fog of war og hierarchical AI.

Fjendens formationer bruger samme officerbaserede decision core. Difficulty ligger som et separat lag ovenpå og må ikke omskrive officerens stats eller give skjulte combat-bonusser/omniscience. Den detaljerede difficulty-plan ligger i `B-180-AI-DIFFICULTY-AND-V009.md`, mens officerstats ligger i `B-190-OFFICER-STATS-MODEL.md`.

## B-170 — AI Unit ON/OFF

**BESLUTTET / AKTIV v00.00.09.** En valgt formation får en tydelig kommando:

`AI UNIT: OFF / ON`

- **OFF** = spilleren vælger selv formationens taktiske handlinger. Officeren påvirker senere execution quality, reaction time, cohesion, morale og ordreopfattelse, men vælger ikke nye lokale tasks.
- **ON** = den lokale officer fører formationen inden for mission, commander intent og constraints.
- Toggle-state skal være synlig i battle UI.
- AI ON er delegation, ikke en gameplay-bonus.

I v00.00.09 toggles valgte danske regimenter med `A`.

## B-171 — Missionen er AI'ens ramme

**BESLUTTET / DELVIST IMPLEMENTERET v00.00.09.** AI-officeren må ikke kende spillerens skjulte intention eller frit opfinde et helt nyt strategisk mål.

Første runtime missions:

- DefendArea.
- Hold.
- MoveToPoint.
- AttackTarget.
- AttackNearest.

Senere udvides dette med Defend line, Support formation, Screen, Guard artillery/supply, Reserve, Delay, Withdraw og Pursue samt mission constraints som `do not leave area`, `conserve ammunition` eller `do not pursue`.

## B-172 — Officer stats styrer reaktioner

**BESLUTTET / AKTIV v00.00.09.** Officerens beslutninger afledes af en konkret 0–100 profil.

Første otte kerne-dimensioner:

1. Leadership.
2. Inspiration.
3. Tactical Skill.
4. Initiative.
5. Staff / Command Skill.
6. Discipline / Obedience.
7. Aggressiveness ↔ Caution.
8. Composure / Nerve.

Officer Experience er separat baggrundsværdi.

**Composure/Nerve** beskriver officerens egen evne til at bevare overblik under pres og må ikke blandes sammen med Leadership, Inspiration eller regimentets morale. I v00.00.09 påvirker den stress-relateret reaction delay og decision noise.

To officerer med samme mission kan derfor reagere forskelligt. En initiativrig/aggressiv officer kan søge tættere engagement eller afbryde en movement mission for en lokal trussel, mens en mere disciplineret/forsigtig officer kan holde missionen og området længere.

## B-173 — AI bruger kun tilgængelig information

**BESLUTTET.** Officer AI må kun reagere på den knowledge state, officeren/formationen realistisk har adgang til.

Senere kilder omfatter:

- synlige fjender,
- rapporter med timestamp/confidence,
- egne scouts/skirmishers/cavalry,
- meldinger fra overordnede/naboer,
- terræn og kendte objectives,
- observeret artilleriild og andre slagmarkssignaler.

v00.00.09 bruger endnu prototype-slagmarkens simple target visibility og er derfor ikke fuld fog-of-war implementation.

## B-174 — AI reaction set

**PLANLAGT / MVP DELVIST IMPLEMENTERET.** Når AI Unit er ON kan officerens første controller:

- move/advance,
- hold/stabilise,
- vælge/holde attack target,
- vælge approach/engagement distance,
- skifte line/column i simple situationer,
- overholde eller lokalt afvige fra movement mission ud fra Discipline/Aggressiveness.

Senere reaction set omfatter cover, prone, skirmishers, fire discipline, reserve support, rally, withdrawal, artillery fire-control og cavalry/dragoon actions.

## B-175 — Autonomy begrænser AI

**BESLUTTET.** `AI Unit ON/OFF` og `Autonomy` er separate systemer.

- AI OFF: direkte spillerkontrol.
- AI ON + Strict: konservativ mission execution.
- AI ON + Normal: lokale justeringer tilladt.
- AI ON + Independent: betydelig metode-/timingfrihed inden for intent.

Autonomy er endnu ikke eksponeret som runtime selector i v00.00.09 MVP; første build beviser shared officer decision core før dette lag kobles på.

## B-176 — Player override og order delay

**BESLUTTET.** Spilleren kan slå AI Unit OFF og udstede nye ordrer. Toggle må på sigt ikke teleportere information eller ophæve command friction.

v00.00.09 bruger immediate prototype-control. En allerede igangsat order kan fortsætte, indtil spilleren giver en ny direkte ordre. Senere order-delay/courier-system håndterer realistisk kontrolskift.

## B-177 — Hierarkisk delegation

**PLANLAGT.** Delegation skal senere kunne bruges på regiment/bataljon/brigade/division-niveau. En brigadechef med AI ON skal kunne fordele opgaver til underformationer, mens spilleren stadig kan tage enkelte formationer tilbage under direkte kontrol.

v00.00.09 arbejder kun på regimentsniveau.

## B-178 — AI transparency / reason codes

**BESLUTTET / AKTIV v00.00.09.** AI-beslutninger skal kunne forklares.

Første reason/task eksempler:

- `Moving to assigned mission point`
- `Discipline keeps unit on assigned movement`
- `Aggressive local reaction during movement`
- `Holding assigned local area`
- `Morale pressure - officer pauses advance`
- `Preferred engagement range reached`

Console telemetry bruger `AI-DIAG|...` med unit, team, officer, AI state, difficulty, mission, task og reason.

## B-179 — Battle UI

**AKTIV v00.00.09 QA.** Test-HUD viser:

- AI ON/OFF,
- QA officer,
- Tactical Skill,
- Initiative,
- Composure,
- current task,
- reason code.

Et separat selected-unit AI-panel viser den kompakte fulde officerprofil. Den endelige kontekstuelle bundmenu kommer senere og skal mindst have:

`AI UNIT [OFF/ON] | AUTONOMY | MISSION | ORDRESTATUS`

## Designværn

- AI ON må ikke give information, formationen ikke burde have.
- Officerstats er AI-/command-inputs, ikke skjulte generiske combat buffs.
- Difficulty må ikke omskrive officerens historiske profil.
- Dårlige/stressede officerer må kunne reagere suboptimalt på en forklarlig måde.
- Composure beskriver officerens egen stressstabilitet; regimentets morale/cohesion er separate states.
- En spiller skal senere kunne føre alt selv, delegere dele eller fungere primært som øverstkommanderende.

## Implementeringsprioritet

P0A v00.00.09 implementerer den første vertikale slice med AI UNIT ON/OFF, otte QA-officerstats + separat Experience, shared decision core for Danmark/Preussen, Easy/Normal/Hard, UI-state/reason codes og telemetry. Fuld brigadehierarki, courier-system og avancerede combat-actions kommer efter første AI-gate.
