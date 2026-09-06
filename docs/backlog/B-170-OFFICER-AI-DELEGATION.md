# PROJECT 1864 — Backlog B-170–B-179: Officer AI og delegeret kommando

**Status: BESLUTTET / PLANLAGT**  
**Designbaseline: v00.02.08**

Dette supplement fastlægger spillerens mulighed for at delegere styringen af egne formationer til deres officerer. Systemet bygger oven på de allerede besluttede commander stats, personality, autonomy, order delay, fog of war og hierarchical AI.

## B-170 — AI Unit ON/OFF

**BESLUTTET.** En valgt formation får en tydelig kommando i den kontekstuelle bundmenu:

`AI UNIT: OFF / ON`

- **OFF** = spilleren vælger selv formationens taktiske handlinger. Officeren påvirker fortsat execution quality, reaction time, cohesion, morale, ordreopfattelse og andre relevante checks, men må ikke frit vælge et nyt lokalt objective.
- **ON** = den lokale officer får lov til at føre formationen inden for den senest modtagne mission, commander intent og gældende constraints.
- Toggle-state skal være synlig både i bundmenuen og på unit card/command status.
- AI ON er delegation, ikke en gameplay-bonus. En dårlig officer kan træffe dårligere beslutninger end en god officer.

## B-171 — Missionen er AI'ens ramme

**BESLUTTET.** AI-officeren må ikke kende spillerens skjulte intention eller frit opfinde et helt nyt strategisk mål. Han arbejder inden for en mission, fx:

- Hold position / hold area.
- Defend line.
- Advance to line/area.
- Attack target/position.
- Support formation.
- Screen flank/front.
- Guard artillery/supply.
- Reserve.
- Delay enemy.
- Withdraw to line.
- Pursue, hvis mission/doctrine tillader det.

En mission kan have constraints som `do not leave area`, `hold until`, `avoid engagement`, `conserve ammunition`, `protect artillery`, `do not pursue` eller lignende.

## B-172 — Officer stats styrer reaktioner

**BESLUTTET.** Officerens beslutninger skal afledes af faktiske stats/personality i stedet for en generisk AI-profil. Relevante dimensioner kan mindst omfatte:

- Tactical skill.
- Initiative.
- Aggressiveness / caution.
- Discipline / obedience.
- Staff/command skill.
- Experience.
- Morale/inspiration/leadership.
- Personality traits og reputation.

Eksempel: To brigadechefer med samme ordre `Hold højderyggen` kan reagere forskelligt. En initiativrig officer kan flytte en bataljon til bedre cover eller sende skirmishers frem. En meget forsigtig officer kan holde en større reserve. En aggressiv officer kan iværksætte et lokalt modangreb, hvis commander intent og autonomy tillader det.

## B-173 — AI bruger kun tilgængelig information

**BESLUTTET.** Officer AI må kun reagere på den knowledge state, officeren/formationen realistisk har adgang til.

Det betyder bl.a.:

- synlige fjender,
- rapporter med timestamp/confidence,
- egne scouts/skirmishers/cavalry,
- meldinger fra overordnede/naboer,
- terræn og kendte objectives,
- observeret artilleriild og andre slagmarkssignaler.

AI må ikke bruge perfekte verdenskoordinater for skjulte fjender eller andre omniscient data.

## B-174 — AI reaction set

**PLANLAGT.** Når AI Unit er ON, kan officeren — hvis mission/autonomy tillader det — selv vælge lokale handlinger som:

- justere formation og facing,
- søge/occupy cover,
- gå prone eller rejse enheden,
- deploy/reform skirmishers,
- vælge fire discipline,
- vælge lokale mål,
- flytte en kort distance for bedre LOS/terræn,
- støtte en truet naboformation,
- sende reserve frem,
- rally/reorganize,
- afbryde eller begrænse pursuit,
- lokal withdrawal ved ekstrem risiko,
- for artilleri: vælge target i Auto Target-mode, ammunitionstype, limber/relocate ved trussel,
- for dragoner/kavaleri: screen, dismount/remount eller pursuit efter mission.

De konkrete tilladelser afhænger af formationsniveau, unit type, mission og autonomy.

## B-175 — Autonomy begrænser AI

**BESLUTTET.** `AI Unit ON/OFF` og `Autonomy` er to forskellige ting.

- **AI OFF**: direkte spillerkontrol; officer vælger ikke nye lokale tasks.
- **AI ON + Strict**: officer udfører ordren konservativt og afviger kun ved umiddelbar nødvendighed.
- **AI ON + Normal**: officer må foretage lokale justeringer for at opfylde intent.
- **AI ON + Independent**: officer får betydelig frihed til at vælge metode, lokale objectives og timing inden for overordnet mission.

Autonomy kan være bundet til doctrine, officerens tillid/reputation, formationsniveau eller spillerindstilling.

## B-176 — Player override og order delay

**BESLUTTET.** Spilleren kan altid vælge at slå AI Unit OFF og udstede nye ordrer, men toggle må ikke teleportere information eller ophæve command friction.

Ved et senere realistisk order-delay-system gælder derfor:

- `AI OFF` stopper officerens **nye autonome beslutninger**, når kontrolændringen/ordren er modtaget efter den relevante command model.
- En allerede igangsat bevægelse eller kamp fortsætter, indtil en ny ordre faktisk når formationen, medmindre UI-assistance/arcade-indstilling eksplicit reducerer delay.
- På formationsniveau, hvor spilleren repræsenterer den lokale chef direkte, kan kontrolskiftet være hurtigere; dette skal afhænge af valgt realism mode.

## B-177 — Hierarkisk delegation

**PLANLAGT.** Delegation skal kunne bruges på flere niveauer.

Eksempel:

- Spilleren giver en brigade missionen `Forsvar denne sektor` og sætter brigaden til **AI ON**.
- Brigadechefen fordeler opgaver til sine bataljoner ud fra terræn, fjende, egne stats og doctrine.
- En enkelt bataljon kan eventuelt sættes tilbage til direkte spillerkontrol uden at resten af brigaden mister sin AI-mission.

UI skal tydeligt vise, hvilke enheder der er direkte styret, og hvilke der er delegeret.

## B-178 — AI transparency / reason codes

**BESLUTTET.** Spilleren skal kunne forstå, hvorfor en officer gjorde noget. AI-beslutninger får korte reason codes/tooltip-forklaringer, fx:

- `Occupying stronger cover`
- `Threat to left flank`
- `Low ammunition — conserving fire`
- `Supporting 5. Regiment`
- `Officer cautious — preserving reserve`
- `Enemy artillery observed`
- `Withdrawal: morale critical`

Dette er både et gameplay- og QA-værktøj og skal forhindre oplevelsen af vilkårlig eller “snydende” AI.

## B-179 — Battle UI

**PLANLAGT.** Den kontekstuelle bundmenu udvides med en command/delegation-gruppe, eksempelvis:

`AI UNIT [OFF/ON] | AUTONOMY [STRICT/NORMAL/INDEPENDENT] | MISSION | ORDRESTATUS`

Når AI er ON, vises også officerens aktuelle lokale task og kort begrundelse. Eksempel:

`AI ON | Maj. Petersen | Defend ridge | Occupying stone wall | Reason: stronger cover`

Ved multi-select kan spilleren slå AI ON/OFF for flere kompatible formationer, men individuelle officerer fortsætter med at træffe egne beslutninger ud fra deres egne stats.

## Designværn

- AI ON må ikke give officeren information, som formationen ikke burde have.
- AI ON må ikke ignorere ammunition, fatigue, casualties, terrain, LOS eller command constraints.
- Officerens kvalitet skal påvirke beslutningskvaliteten, men ikke på en måde der gør udfald fuldstændigt deterministisk.
- Dårlige officerer må kunne misforstå, reagere sent eller vælge en suboptimal løsning; dette skal dog være forklarligt gennem stats, information og reason codes.
- En spiller skal kunne føre hele hæren selv, delegere dele af den eller i princippet delegere næsten hele slaget og primært fungere som øverstkommanderende.

## Ikke del af P0A v00.00.08

Dette er en bindende senere funktion. P0A v00.00.08 skal fortsat holdes som en afgrænset testbuild for reload/experience, 0-hit volleys, `Ramte N`, casualty-visual og regression af den eksisterende prototype.
