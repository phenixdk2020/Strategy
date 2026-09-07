# B-290–B-299 — Grand Campaign: byer, byggeri, rekruttering, befæstning og handel

## Status

**BESLUTTET / PLANLAGT**

Grand Campaign skal ikke kun handle om at flytte hære mellem zoner. Zonerne skal have økonomisk, logistisk og militær infrastruktur, så spilleren kan udvikle landet, træne styrker, bygge forsvar og føre handel.

## B-290 — Byer og settlements som objekter inde i zoner

Zoner er det territoriale simuleringslag. Byer er særskilte settlement-objekter inde i zonerne.

Settlement-tier:

1. Capital / hovedstad
2. Major city / større by
3. Regional town / regional købstad
4. Small town / mindre købstad
5. Village / landsby

Ikke alle landsbyer behøver en individuel campaign-token. Små bebyggelser kan indgå som zone-population/production data, mens militært, administrativt eller økonomisk vigtige byer vises eksplicit.

Et settlement kan have:

- population
- urban capacity
- local tax base
- market size
- recruitment pool
- garrison capacity
- barracks/training capacity
- depot/storage
- workshop/industry
- fortification level
- port/harbour
- rail station senere
- telegraph access senere
- hospital/medical capacity
- supply production/consumption
- trade throughput

## B-291 — Byggeri

Byggeri sker i en zone eller et settlement og kræver tid, arbejdskraft, penge og relevante materialer.

Første bygningskategorier:

### Militær

- Barracks / kaserne
- Training ground
- Arsenal
- Ammunition depot
- Supply depot
- Horse/remount depot
- Engineer depot
- Hospital
- Permanent fort
- Coastal battery
- Magazine

### Infrastruktur

- Road improvement
- Bridge
- Rail line/station
- Telegraph
- Port expansion
- Warehouse

### Økonomi

- Farm/agricultural improvement
- Mill
- Workshop
- Foundry / metal works hvor historisk relevant
- Textile production
- Shipyard hvor relevant
- Commercial warehouse

Byggeri må være data-driven og historisk plausibelt for landet og perioden.

## B-292 — Feltbefæstning og skanser

Der skelnes mellem permanent og feltmæssigt forsvar.

### Fieldworks

En hær med ingeniører kan i en zone eller tactical position bygge:

- skanse/redoubt
- trench/earthwork
- breastwork
- gun emplacement
- abatis/obstacle
- field bridge
- pontoon bridge ved egnet vandløb

Fieldworks kræver:

- engineer capability
- manpower
- værktøj/materialer
- byggetid
- passende terræn
- ingen eller begrænset enemy pressure

Jo længere en formation holder samme position, desto stærkere kan feltbefæstningen blive op til et designet maksimum.

### Permanent fortification

Permanent fort/fæstning bygges over måneder/år, er dyrt og bliver en zone-/settlement-ejendom med garrison, artillery capacity og supply requirements.

## B-293 — Rekruttering og træning

Nye hære må ikke skabes øjeblikkeligt ved betaling.

Pipeline:

`population/recruitment pool -> mobilisation/recruitment -> barracks/training -> equipment -> officer assignment -> field-ready formation`

Krav kan omfatte:

- manpower fra zone/nation
- våben og uniformer
- ammunition reserve
- officers/NCOs
- barracks/training capacity
- training time
- remounts for cavalry/artillery

Training påvirker senere regiment Experience/Training og dermed stabilitet, reload, formation handling og combat performance.

Mobilisering af eksisterende reserve-/værnepligtsstrukturer skal senere være forskellig fra helt nyoprettede formationer.

## B-294 — Garnisoner

Byer, forter, broer, depoter og havne kan have garrison.

Garrison er ikke gratis manpower. Tropper bundet til garnison er ikke samtidigt tilgængelige i felthæren.

Garnison påvirker:

- kontrol af zone/settlement
- modstand mod raid/capture
- depot/arsenal security
- prisoner handling
- local unrest/security senere

## B-295 — Handel og marked

Grand Campaign får et handelslag.

Handel skal forbinde økonomi, supply, diplomati og naval warfare i stedet for at være en separat mini-game.

Grundmodel:

- settlements/zones producerer og forbruger varer
- overskud kan eksporteres
- underskud kan importeres
- handel flyttes via land-, rail- og sø-ruter
- handel skaber told/skat/indtægt
- krig, blokade, besættelse og ødelagt infrastruktur kan afbryde ruter

Første relevante varegrupper:

- grain/food
- livestock
- fodder
- timber
- coal
- iron/metal
- textiles
- weapons
- ammunition/munition inputs
- horses/remounts
- industrial goods

Systemet bør anvende aggregerede varegrupper frem for hundredvis af individuelle produkter.

## B-296 — Markedspriser og supply/demand

Hver varegruppe har national/regional availability og prisniveau.

Pris påvirkes af:

- lokal produktion
- lokal efterspørgsel
- importmuligheder
- transportkapacitet
- blockade/interdiction
- krig og mobilisation
- seasonal harvest effects senere

Spilleren skal kunne mærke at fx manglende kul, jern, korn eller heste begrænser militær og økonomisk aktivitet uden at økonomien bliver et regnearks-spil.

## B-297 — Handelsruter

Trade route typer:

- road
- river/canal hvor relevant
- rail
- coastal shipping
- international sea trade

En rute har capacity, travel time og risk.

Naval blockade/raiding kan reducere eller lukke havnehandel. Enemy occupation eller raids kan afbryde landruter, broer, rail og telegraph.

## B-298 — Byernes strategiske betydning

Byer er ikke blot map-decoration. En større by kan være vigtig pga. én eller flere roller:

- population/recruitment
- administration
- market/trade
- port
- rail junction
- fortress
- arsenal/depot
- industry
- bridge/crossing
- naval base

Byens gameplay-værdi skal derfor komme fra dens historiske funktioner, ikke kun fra befolkningstal.

## B-299 — Første implementeringsrækkefølge

1. Settlement data model og settlement-tiers.
2. Vis større byer på det geografiske Grand Campaign-kort.
3. Population + market + recruitment hooks.
4. Barracks/depot/fortification construction queue.
5. Recruitment/training queue.
6. Basic goods production/consumption.
7. Trade route graph og import/export.
8. Blockade/interdiction/supply integration.
9. Historical tuning pr. land/zone/by.

Første version skal holde economy abstraheret nok til at campaign stadig primært handler om politisk-strategiske og militære beslutninger.
