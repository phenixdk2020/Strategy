# PROJECT 1864 — Enheds- og AI-officerregler

**Status:** Kanonisk adfærdsreference  
**Prototypebaseline:** v00.00.09f10 TEST  
**Formål:** Dette dokument samler de regler, der bestemmer hvordan enheder, formationer, kampordrer og AI-officerer skal opføre sig. Når runtime-kode og dette dokument er uenige, skal afvigelsen behandles som en bug eller som en eksplicit ny designændring.

> Bemærk: Den nuværende prototype bruger fortsat klassenavnet `Regiment`, men den aktive test-enhed repræsenterer i praksis et **kompagni på ca. 190 mand**.

---

## 1. Prioritet mellem regler

Når flere systemer vil styre samme enhed, gælder denne prioritet fra høj til lav:

1. **Rout/panik og overlevelse** — en routed enhed følger flugtlogik.
2. **Terrænsikkerhed** — åbent vand må ikke krydses uden lovlig bro/overgang.
3. **Eksplicit spillerordre** — fx ANGREB, FORSVAR HER, EROBR HER eller TRÆK BAGLÆNS.
4. **Aktiv missionslogik** — fx bro-routing eller fighting withdrawal.
5. **AI-officerens mission/doctrine/autonomi.**
6. **Automatisk formationspolitik** — Line/Column under march og kontakt.
7. **Fire policy og kampadfærd.**
8. **Idle/hold-adfærd.**

Et lavere system må ikke overskrive et højere systems aktive hensigt.

---

# DEL A — FÆLLES ENHEDSREGLER

## 2. Enhedsskala og styrke

### Implementeret nu

- Aktiv dansk og preussisk test-enhed normaliseres til **190 mand**.
- Ved 1:1-visualisering repræsenterer **én synlig soldat én mand**.
- Ved 1:1 casualty-visualisering repræsenterer **én faldet model ét tab**.
- F9 kan fortsat reducere levende/faldne visualisering til `1:2`, `1:5` eller `1:10` af performancehensyn uden at ændre den simulerede styrke.

### Regel

Grafisk LOD eller display-ratio må aldrig ændre den underliggende styrke, casualties eller combat resolution.

---

## 3. Dansk 1851 Line-formation

### Implementeret nu

- Dansk 1851-standard for linjeinfanteri er **3 geledder**.
- Et fuldt 190-mands kompagni fordeles omtrent **64 + 63 + 63**.
- Horisontal spacing er ca. **0,75 m pr. mand** i prototypen.
- Full-strength frontage er ca. **48 m**.

### Godkendt designregel

- **2 geledder** skal være en senere doktrin-/reglements-researchmulighed.
- Formationer skal på sigt være datadrevne pr. nation, våbenart og årstal.

---

## 4. Line vs. Column

### Line

Line er den primære kampformation.

En enhed skal være i Line når:

- en fjendtlig enhed er inden for enhedens **Long/MaximumRange**;
- enheden skal skyde;
- enheden netop har afsluttet march og skal deployere til kamp;
- enheden udfører kontrolleret fighting withdrawal;
- en ordre kræver forsvar af en position.

### Column

Column er primært en march-/passageformation.

En enhed må automatisk gå i Column når:

- den har en længere bevægelse foran sig;
- **ingen fjendtlig enhed er inden for Long/MaximumRange**;
- eller en højere-prioritets terrænregel kræver Column, fx brokrydsning.

### Ny hård v00.00.09f10-regel

> **Hvis nærmeste gyldige fjende er inden for Long/MaximumRange, må enheden ikke skifte til Column alene fordi den får en ANGREB-ordre eller fordi fire policy ændres fra LONG til MEDIUM/CLOSE.**

Den skal i stedet blive i/deployere til Line og manøvrere i kampformation.

### Eneste nuværende undtagelse

En aktiv **bridge route** må midlertidigt tvinge enheden i Column, selv hvis fjenden er inden for Long, fordi selve passagen kræver en smal formation.

---

## 5. Formation-change skal være fysisk synlig

- Line → Column og Column → Line må ikke teleportere soldaterne.
- Soldater skal fysisk flytte til deres nye slots.
- Range cone skal være skjult mens enheden er i Column.
- Range cone skal forblive skjult mens enheden fysisk reformerer til Line.
- Enheden må ikke skyde før Line-formationen er klar.

---

## 6. Facing og drejning

### Implementeret nu

- Stationære facing-ordrer drejer formationen gradvist.
- Aktuel QA-værdi er **16,8°/sek**.
- Det er 30 % langsommere end den tidligere 24°/sek test.

### Regel

Facing skal være fysisk læsbar; formationen må ikke teleportere 15° ved Z/X-ordre.

March-steering og stationær kampdrejning er separate systemer.

---

## 7. Selection, destination og footprint

- Selection-rammen skal følge enhedens **faktiske formations-footprint**.
- Destination-box skal have samme størrelse og center-offset som den ønskede slutformation.
- Line- og Column-footprint skal beregnes fra formationsslots, ikke gamle hardcodede rektangler.
- Den gule selection-orb følger formationens faktiske center.
- Column-markering må derfor være lang og smal og ligge omkring selve kolonnen, også når formationen ligger asymmetrisk bag pivoten.

---

# DEL B — BEVÆGELSE

## 8. Normal march

### Implementeret QA-værdier

Danmark:

- normal march: ca. **3,20 m/s**

Preussen:

- normal march: ca. **3,35 m/s**

### Regel

- Normal bevægelse bruger gå-animation.
- Enheden må ikke glide hen over terrænet uden synlig gait.
- Lange marcher uden fjende inden for Long kan udløse Column.

---

## 9. Forced March

### Implementeret nu

- Forced March kan toggles med **G / TVANG G**.
- Danmark: ca. **4,25 m/s**.
- Preussen: ca. **4,45 m/s**.
- Forced March giver ekstra cohesion/fatigue-belastning.

### Regel

Forced March er hurtigere end normal march men langsommere og mere kontrolleret end panik/rout.

Det må aldrig være en gratis speed boost uden taktisk omkostning.

---

## 10. Rout/panik

### Implementeret QA-værdier

- Danmark: ca. **5,20 m/s**.
- Preussen: ca. **5,40 m/s**.
- Routed enhed bruger løbe-animation.
- Forced March annulleres ved rout.

### Regel

Rout har højere prioritet end normale spiller- og officerordrer.

---

# DEL C — TERRÆN OG BROER

## 11. Aktuel obstacle-baseline

### Implementeret nu

- Træer: pass-through.
- Hegn: pass-through.
- Bygninger: pass-through.
- Åbent vand/flod: **hard blocker**.
- Fast bro ved den nuværende testflod: eneste lovlige crossing.

Dette er en bevidst isolation-baseline for at undgå tidligere fastkørselsproblemer.

---

## 12. Bridge routing

### Implementeret v00.00.09f9+

Hvis en Move/Attack-rute kræver flodkrydsning:

1. den strategiske slutordre bevares;
2. bro-routeren overtager fysisk steering;
3. enheden tvinges i Column;
4. enheden går til **near staging** på tør grund;
5. derefter **near entry**;
6. derefter over broen mod **far entry**;
7. derefter videre til **far staging**;
8. først efter far staging frigives den normale slutmission igen.

Aktuel prototype bruger omtrent:

- staging: ca. **32 m** fra broens center;
- entry: ca. **13 m** fra broens center.

### Regel

En attack-order må ikke overskrive bro-ruten med en direkte linje mod fjenden.

Afstand til fjenden i fugleflugtslinje må ikke få AI-officeren til at tro, at den kan engagere gennem en flod.

---

# DEL D — KAMP OG ILD

## 13. Aktuel QA-rækkevidde

Prototype-skala: **1 Unity unit = 1 meter**.

Aktuelle testgrænser:

- **Close:** 35 m
- **Medium / Effective:** 70 m
- **Long / Maximum:** 100 m

Disse er prototype/gameplay-værdier og ikke endelige historiske våbendata.

---

## 14. Fire cone

### Implementeret nu

- Total fire arc: **70°**.
- Ca. **±35°** fra formationens facing.
- Close/Medium/Long deler samme venstre/højre sidegrænser.
- Fanen starter over hele formationens frontage.

### Regel

- Cone vises kun når valgt enhed står i klar Line.
- Cone skjules i Column.
- Cone skjules under Line-reformering.
- Firing eligibility skal følge samme sektor som den synlige cone.

---

## 15. Fire policy

Fire policy er en **maksimal tilladelse til at åbne ild**, ikke en ordre om at holde præcis den afstand.

- HOLD = ingen skydning.
- CLOSE = må åbne ild inden for Close.
- MEDIUM = må åbne ild inden for Medium.
- LONG = må åbne ild inden for Long.

### Vigtig AI-regel

En AI-officer med MEDIUM valgt må gerne vælge at gå tættere på end Medium-grænsen, hvis doctrine, aggressiveness, tactical skill og situation gør det rimeligt.

Men:

> At ændre policy fra LONG til MEDIUM må **ikke i sig selv** få enheden til at gå i Column, hvis fjenden allerede er inden for Long.

---

## 16. Distance påvirker stadig hit chance

Selv når morale midlertidigt er slået fra i QA:

- Close skal i gennemsnit ramme bedre end Medium.
- Medium skal i gennemsnit ramme bedre end Long.
- Accuracy skal ændre sig kontinuerligt med faktisk afstand, ikke kun springe ved band-grænserne.

### Midlertidig QA-regel

- Morale er aktuelt låst til **100** for casualty/hit-test.
- Cohesion er stadig aktiv.
- Denne morale-lock er **ikke** en permanent spildesignregel.

---

## 17. Volley-animation og sortkrudtsrøg

En synlig salvecyklus skal læses som:

`AFVENT -> SIGT -> SKYD -> RELOAD -> AFVENT`

### Visuel regel

- Hele den relevante skydelinje skal bidrage til salven.
- Røg skal læses langs hele frontage, ikke som få tilfældige puff.
- Soldater skal have synlig recoil/fire-pose.
- Reload skal have separat visuel state.
- Gammel røg skal blive i world space og ikke følge regimentet som en fast sky.

---

# DEL E — CASUALTIES

## 18. Faldne

- Ved 1:1 skal ét casualty kunne give én faldet soldat.
- Faldne placeres ved en faktisk formationsslot ved casualty-tidspunktet.
- Tilladt lokal jitter er ca. **±0,30 m**.
- Lig må ikke spawn'e mange meter fra formationen.
- Faldne bliver liggende i world space, når enheden marcherer videre.

---

# DEL F — SPILLERORDRER

## 19. FORSVAR HER

Godkendt/implementeret grundregel:

- gå til valgt punkt;
- deployer i Line;
- hold positionen;
- officer går i defensiv lokal adfærd efter ankomst.

Senere defensive construction/cover-regler kan bygge ovenpå denne ordre.

---

## 20. ANGREB

### Regel

ANGREB betyder:

- mål-enheden er strategisk hensigt;
- lovlig rute og terrænregler har højere prioritet end direkte linje;
- hvis fjenden er på samme side af terrænbarrierer, lukkes der til officerens preferred engagement range;
- hvis fjenden er inden for Long, forbliver/deployer enheden i Line;
- hvis bro skal krydses, bruges midlertidig Column uanset Long-reglen;
- efter broen genoptages den oprindelige attack mission.

---

## 21. EROBR HER

- gå til valgt terrænpunkt;
- brug marchformation efter de normale regler;
- deployer til Line nær slutpunkt;
- hold det valgte område efter ankomst.

---

## 22. HOLD

- stop aktiv bevægelse;
- behold/returner kampformation efter gældende formationsregler;
- firing følger stadig valgt fire policy, hvis målet er lovligt.

---

# DEL G — FIGHTING WITHDRAWAL / TRÆK BAGLÆNS

## 23. Formål

Fighting withdrawal er **ikke rout**.

Det er en kontrolleret ordre, hvor enheden:

- bliver i Line;
- holder front mod nærmeste relevante fjende;
- skyder fra stationær position;
- trækker sig et kort stykke baglæns;
- stopper;
- skyder igen;
- gentager indtil valgt afstand er nået.

---

## 24. Tre withdrawal-knapper

### TRÆK → MED

Mål: ca. **Medium/EffectiveRange**.

Med nuværende QA-range: ca. **70 m**.

### TRÆK → LONG

Mål: omtrent to tredjedele inde i intervallet Medium → Maximum.

Med 70/100 QA-range: ca. **90 m**.

### TRÆK → UD

Mål: uden for fjendens nuværende Long/MaximumRange med sikkerhedsmargin.

Med 100 m Long og nuværende margin: ca. **115 m**.

---

## 25. Fighting-withdrawal cyklus

Aktuel prototypecyklus:

1. enheden går/forbliver i Line;
2. AI UNIT slås midlertidigt fra, så en offensiv officer ikke ophæver spillerordren;
3. enheden får midlertidigt LONG fire permission til covering fire;
4. den står stille gennem en reload/fire-pause;
5. den trækker ca. **10 m baglæns**;
6. under selve backstep er fire policy midlertidigt HOLD;
7. backstep-hastighed er ca. **1,70 m/s**;
8. den stopper, vender fortsat fronten mod fjenden og må skyde igen;
9. cyklussen gentages til mål-afstanden er nået;
10. tidligere fire policy gendannes ved afslutning;
11. Officer AI forbliver OFF efter afslutning, indtil spilleren bevidst aktiverer den igen.

### Terrænregel

Fighting withdrawal må ikke automatisk bakke regimentet ud i åbent vand.

Hvis næste backstep krydser den nuværende flod, stoppes ordren. En senere avanceret regel kan give reverse bridge withdrawal som særskilt officer-/pathfinding-adfærd.

---

# DEL H — AI-OFFICERREGLER

## 26. AI UNIT ON/OFF

- AI OFF = direkte spillerkontrol.
- AI ON = OfficerAIController må fortolke missionen efter doctrine/stats.
- En eksplicit spillerordre har højere prioritet end normal officer-autonomi.

---

## 27. Doctrine

Aktuelle doctrines:

- **DEFENSIVE**
- **BALANCED**
- **OFFENSIVE**

Doctrine ændrer officerens villighed til at avancere, holde område og vælge engagement range.

---

## 28. Officerstats der påvirker adfærd

Aktuel prototype bruger bl.a.:

- Leadership
- Inspiration
- Tactical Skill
- Initiative
- Staff/Command Skill
- Discipline/Obedience
- Aggressiveness/Caution
- Composure/Nerve
- Officer Experience

### Regel

Officerens stats må påvirke **beslutning og timing**, ikke give skjulte direkte våbenbuffs, medmindre en separat regel udtrykkeligt definerer dette.

---

## 29. Commander Aggressiveness

- Spillerens 0–100 aggression-intent er et commander-bias.
- Officerens egen aggressiveness skal stadig have betydelig vægt.
- OFFENSIVE doctrine kan skubbe effektiv aggressiveness op.
- DEFENSIVE doctrine kan skubbe den ned.

---

## 30. Preferred engagement range

AI-officeren vælger preferred engagement range ud fra bl.a.:

- doctrine;
- effective aggressiveness;
- Tactical Skill;
- decision noise/stress;
- valgt fire policy som maksimal firing permission.

### Regel

Fire policy er ikke identisk med preferred engagement range.

En OFFENSIVE officer med MEDIUM må fx forsøge at lukke tættere end 70 m.

---

## 31. Formation under officer-AI

AI-officeren skal følge de samme globale formationsregler som spilleren:

- fjende inden for Long => Line;
- længere march uden fjende i Long => Column tilladt;
- bro-route => Column påkrævet;
- firing => Line påkrævet;
- fighting withdrawal => Line påkrævet.

Officer-AI må ikke have en skjult undtagelse, som får den til at gå i Column i kampzonen.

---

## 32. Explicit AttackTarget

Hvis spilleren udpeger et specifikt fjendtligt mål:

- missionen skal bevares som hensigt;
- officer-doctrine må ikke erstatte målet uden gyldig årsag;
- terræn/pathfinding må indsætte midlertidige steering-punkter;
- bridge routing har prioritet over fugleflugtsafstand;
- efter crossing fortsætter den oprindelige attack mission.

---

## 33. DefendArea

En defensiv officer:

- holder sig omkring sin defensive anchor;
- må reagere på fjender i lokal decision space;
- skal undgå unødigt langt offensivt pursuit;
- kan bruge Line og lokal ild frem for at marchere i Column, når fjenden er i Long-zonen.

---

## 34. MoveToPoint

En MoveToPoint-mission:

- prioriterer det tildelte punkt;
- kan bruge Column på længere afstand;
- men skal stadig deployere til Line hvis en fjende kommer inden for Long;
- undtagelse: en aktiv bridge-route kan fastholde Column gennem crossing.

---

## 35. AI difficulty

Godkendt regel:

Easy/Normal/Hard må påvirke:

- reaction timing;
- decision noise;
- hvor hurtigt computer-AI reassesserer.

Difficulty må **ikke** skjult ændre:

- weapon accuracy;
- reload;
- range;
- movement speed;
- morale;
- cohesion;
- casualties;
- officer stats;
- fog-of-war information.

---

# DEL I — TESTUND-TAGELSER OG FREMTIDIGE REGLER

## 36. Midlertidige QA-regler

Følgende er testværdi og skal ikke automatisk betragtes som endelig balance:

- Close/Medium/Long = 35/70/100 m;
- morale låst til 100;
- forhøjet QA base accuracy;
- 1v1 company-test;
- scenery pass-through.

---

## 37. Godkendte senere designretninger

Følgende er godkendte designretninger, men er ikke nødvendigvis fuldt runtime-implementeret endnu:

- 2-geledders linje som doctrine/regulation research;
- defensive fieldworks/cover via FORSVAR HER;
- bedre terræn-effekter på cohesion, hastighed, cover og frontage frem for gamle hard-blocker chains;
- flere historiske weapon profiles;
- mere avanceret fire eligibility pr. frontage-segment;
- LOS, friendly obstruction, smoke og target exposure;
- reverse bridge withdrawal som senere avanceret withdrawal/pathfinding-regel.

---

# 38. Acceptance-regler for nye ændringer

Når en ny version ændrer enheds- eller AI-adfærd, skal den testes mod følgende minimum:

1. Ændringen må ikke bryde bridge-only crossing.
2. En enhed inden for Long må ikke uventet gå i Column.
3. Line/Column transition skal være visuelt fysisk.
4. Selection/destination footprint skal matche formationen.
5. Fire cone og faktisk firing eligibility skal være enige.
6. Fire policy må ikke forveksles med tvungen standoff-distance.
7. AI-officerer skal følge samme globale formations-/terrænregler som spilleren.
8. Explicit player mission må ikke stille og roligt blive overskrevet af lavere-prioritets AI.
9. Casualties må ikke spawn'e langt fra enheden.
10. Rout må være tydeligt forskellig fra kontrolleret fighting withdrawal.

---

## 39. Versionslog for dette regelsæt

### v00.00.09f10

- Oprettet som samlet source-of-truth for enheds- og AI-officeradfærd.
- Tilføjet hård regel: **enemy inside Long => no automatic Column**.
- Dokumenteret bridge-route som eneste aktuelle undtagelse.
- Tilføjet fighting-withdrawal ordren med `MED / LONG / UD`.
- Samlet eksisterende movement, formation, fire, casualty, terrain og Officer AI-regler fra prototypeforløbet.
