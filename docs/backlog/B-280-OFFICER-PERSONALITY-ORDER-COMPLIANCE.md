# B-280 — Officerpersonlighed, ordrelydighed og selvstændig fortolkning

**Status:** Senere fase / planlagt  
**Type:** Officer AI / command & control  
**Bygger videre på:** `docs/UNIT-AND-OFFICER-BEHAVIOUR-RULES.md` §§26–31

## Formål

Officerer skal ikke opføre sig som robotter. Spillerens ordre angiver chefens hensigt, men den underordnede officer fortolker ordren gennem egne stats, personlighed, erfaring og den aktuelle situation.

En ordre som **FORSIGTIG**, **BALANCERET** eller **AGGRESSIV** er derfor et commander-bias og ikke en absolut garanti for adfærd.

## Centrale officerstats

- **Leadership** — evne til at holde underordnede enheder samlet og få dem til at fortsætte en plan.
- **Inspiration** — påvirker morale/rally og hvordan underordnede reagerer på pres.
- **Tactical Skill** — kvaliteten af officerens situationsvurdering og valg af position/manøvre.
- **Initiative** — villighed til selv at handle eller fortolke en ordre, når situationen ændrer sig.
- **Staff / Command Skill** — planlægning, koordinering, ordre-delay og styring af flere underordnede enheder.
- **Discipline / Obedience** — hvor tæt officeren følger den givne ordre og doctrine.
- **Aggressiveness / Caution** — naturlig tendens til at presse frem, holde kontakt, lukke afstand eller søge sikkerhed/reserve.
- **Composure / Nerve** — evne til at bevare beslutningskvalitet under ild, tab, flanketrussel og kaos.
- **Officer Experience** — reducerer dårlig decision noise og forbedrer timing og stabilitet.

## Grundregel: ordre = hensigt, ikke robotkommando

Spillerens ordre skal altid definere den overordnede mission, fx:

- **ANGRIB HER** — tag/press området.
- **FORSVAR HER** — hold området.
- **TILBAGETRÆK HERTIL** — gennemfør organiseret retræte til angivet position.
- **RYK FREM HERTIL** — flyt formationen frem uden nødvendigvis at søge nærkamp.

Officerens stats bestemmer derefter, hvordan missionen fortolkes inden for rimelige taktiske grænser.

## Eksempel: spiller vælger FORSIGTIG

En officer med høj Aggressiveness og høj Initiative kan stadig:

- lukke længere frem end spilleren forventer;
- iværksætte et lokalt modangreb;
- bruge mindre reserve;
- fortsætte presset længere;
- søge tættere engagement range;
- vælge melee, hvis officeren vurderer muligheden som fordelagtig.

En officer med høj Discipline/Obedience vil i højere grad holde sig til den forsigtige posture.

## Eksempel: spiller vælger AGGRESSIV

En meget forsigtig officer kan stadig:

- stoppe tidligere;
- holde større reserve;
- vælge ildkamp i stedet for melee;
- undgå udsatte flanker;
- afbryde fremrykning ved store tab;
- fortolke ANGREB som pres frem til en god firing position snarere end blind storm.

## Kombinationer af stats

### Høj Initiative + høj Tactical Skill

Officer må ofte afvige fra detaljer i ordren, men gør det normalt af en taktisk forståelig årsag.

### Høj Initiative + lav Tactical Skill

Officer handler ofte på egen hånd, men beslutningerne kan være dårlige eller risikable.

### Lav Initiative + høj Obedience

Officer følger ordren meget bogstaveligt og reagerer langsommere på nye muligheder eller trusler.

### Høj Aggressiveness + lav Obedience

Stor risiko for at presse længere end commander-intent, overcommitte reserve eller søge melee.

### Lav Nerve / Composure

Under hård ild kan officeren tøve, stoppe, trække sig tidligere eller miste kvalitet i beslutningerne.

## Ikke ren tilfældighed

Afvigelser fra spillerens ordre må ikke opleves som tilfældig sabotage.

En officers beslutning skal kunne forklares af mindst én af disse faktorer:

1. personlighed/stats;
2. doctrine/posture;
3. fjendens afstand og styrke;
4. egne tab/morale/cohesion;
5. flanketrussel eller terræn;
6. reserve-status;
7. HQ-afstand/kommunikation;
8. stress/decision noise.

## Synlighed for spilleren

Spilleren skal kunne forstå, hvorfor en officer afviger.

Senere UI bør vise fx:

- **Order:** FORSVAR HER
- **Posture:** FORSIGTIG
- **Officer tendency:** Aggressiv
- **Obedience:** 42/100
- **Initiative:** 81/100
- **Current interpretation:** Lokalt modangreb
- **Reason:** Enemy flank exposed / tactical opportunity

Det gør systemet læsbart og reducerer frustration.

## Ordrestrenghed — mulig senere mekanik

En senere mulighed kan være at lade spilleren vælge hvor stramt ordren skal følges:

- **STRICT** — lav autonomi; høj prioritet på præcis ordrelydighed.
- **NORMAL** — balanceret fortolkning.
- **DISCRETION** — stor frihed til officerens Initiative/Tactical Skill.

STRICT bør have en pris, fx længere ordre-delay, dårligere lokal reaktion eller større risiko for at ordren bliver forældet, så det ikke altid er den bedste løsning.

## Vigtig designregel

> En dygtig officer skal ikke nødvendigvis være den mest lydige officer. En højtkvalificeret, initiativrig officer kan tilsidesætte detaljer i en ordre og stadig skabe et bedre resultat; en aggressiv eller dårlig officer kan gøre det samme og skabe en katastrofe.

## Implementeringsrækkefølge

Denne feature kommer **senere**. Før den aktiveres skal følgende være stabile:

1. pathfinding / destination routing;
2. ranged lethality;
3. melee baseline;
4. fysisk HQ/Major-enhed;
5. Major styrer 1 kompagni;
6. Major styrer 2 kompagnier;
7. først derefter personlighed, ordrelydighed og selvstændig fortolkning.
