# B-360–B-369 — Campaign Living World & State-Driven Ambient Animation

## Status

**Design:** GODKENDT  
**Implementation:** PLANLAGT  
**Scope:** Campaign map only  
**Tactical AI:** Må ikke ændres af dette work package.

## Designprincip

Campaign-kortet skal føles levende, mens campaign-tiden går, men de vigtigste animationer skal så vidt muligt være **state-drevne** frem for ren pynt. Det betyder, at visuelle hændelser skal afspejle faktisk campaign-state, når der findes relevant simulation bag dem.

Eksempler:
- et tog bør repræsentere aktiv rail-trafik eller et gyldigt rail-flow,
- en supply-vogn kan repræsentere en reel depot-/supply-relation,
- en militærlejr kan opstå, når en formation har stået stille i længere tid,
- vej- og jernbanearbejde følger faktisk construction progress,
- aktivitet ved fabrik/depot kan afspejle produktion eller stockflow,
- skader, røg og aftermath kan være knyttet til faktisk battle/event history.

Rent dekorative ambient-loops er tilladt, men de må aldrig vise gameplay-information, som ikke er sand i campaign-state.

## B-360 — Living-world coordinator

**Status:** PLANLAGT

Fælles campaign-layer til at styre ambient aktivitet ud fra campaign time, zoomniveau, node-state, formation-state, infrastructure-state og events.

Acceptance:
- ingen separat authoritative simulation state,
- systemet kan slås helt fra uden at ændre gameplay,
- ambient events følger campaign pause/time scaling,
- deterministic QA-mode kan bruges.

## B-361 — Systemisk construction visualisation

**Status:** PLANLAGT

Den valgte retning er den systemiske model: construction visuals skal være bundet til faktisk build progress.

Første construction-typer:
- barracks,
- farm,
- road,
- rail,
- depot/warehouse,
- fortification/fieldworks.

Visual stages kan fx være:
`SITE -> FOUNDATION -> FRAME/SCAFFOLD -> PARTIAL -> NEAR COMPLETE -> COMPLETE`.

Acceptance:
- stage bestemmes af build progress,
- pause stopper progression,
- completion giver én tydelig overgang uden duplication,
- visual state kan rekonstrueres efter load.

## B-362 — Road life and road works

**Status:** PLANLAGT

Små arbejdere, vogne og jordarbejde kan vises på aktive vejprojekter. Eksisterende vejnet kan have begrænset civil/militær trafik ved tættere zoom.

Acceptance:
- arbejde vises kun på aktivt construction/repair-state,
- trafik kan skaleres efter route importance/activity,
- fjendekontrol/blokering kan reducere eller stoppe aktivitet.

## B-363 — Rail construction and train movement

**Status:** PLANLAGT

Jernbane skal kunne udvikles visuelt med jordarbejde, sveller, skinner og små arbejdshold. Færdige aktive linjer kan vise små tog.

Acceptance:
- rail construction følger faktisk progress,
- tog vises kun på gyldige rail edges,
- lukket/ødelagt rail-link stopper gennemgående togtrafik,
- langt zoom reducerer eller fjerner tog-animationer.

## B-364 — Civil, military and supply traffic

**Status:** PLANLAGT

Campaign-kortet kan vise hestevogne, diligencer, forsyningsvogne, militære kolonner og enkelte couriers.

Acceptance:
- traffic density afhænger af route/activity hooks,
- supply traffic kan kobles til depot/supply flow,
- military traffic skal ikke skabe falske formationsdata,
- fjendtlige movement visuals følger fog-of-war-regler.

## B-365 — Ports, ferries and ships

**Status:** PLANLAGT

Havne og sea/ferry links kan have små damp-/sejlskibe, færger, kajaktivitet, vogne og lastning.

Acceptance:
- aktivitet kan kobles til port-state og sea-link availability,
- lukket/blockaded route kan reducere eller stoppe aktivitet,
- skibe er visuel repræsentation og må ikke omgå naval/transport simulation.

## B-366 — Agriculture, seasons and civilian ambience

**Status:** PLANLAGT

Landbrugsområder skal kunne ændre udtryk over tid med pløjning, såning, voksende marker, høst, høstakke, mindre gårdaktivitet og evt. dyr ved tæt zoom.

Acceptance:
- sæson kan udledes af campaign date,
- animationer er lette og LOD-styrede,
- civilian ambience reduceres i aktive kamp-/besættelsesområder, hvis state understøtter det.

## B-367 — Military camps, couriers, scouts and supply columns

**Status:** PLANLAGT

Stationære formationer kan efter relevant tid få små lejre med telte, lejrbål, heste og aktivitet. Couriers, scouts og supply columns kan bruges som visualisering af eksisterende command/recon/logistics state.

Acceptance:
- camp opstår ikke straks ved kort stop,
- camp forsvinder gradvist, når formation flytter,
- courier/scout/supply visuals må ikke lække hidden enemy state,
- visuel aktivitet skal kunne afledes af authoritative campaign data.

## B-368 — War effects, weather, day/night and control change

**Status:** PLANLAGT

Kortets liv skal også afspejle krigens konsekvenser og tidens gang.

Elementer:
- dag/nat-lys,
- by-/lejrbål om natten,
- skyer, regn, tåge og senere sne,
- vindretning i flag/røg,
- battle aftermath, røg, ødelagte vogne og midlertidige skader,
- hospitals-/ambulanceaktivitet efter større slag,
- kontrolskifte med flag/checkpoints/ændret aktivitet,
- civil trafik/flygtninge som senere state-driven hook.

Acceptance:
- vejreffekter og lys må ikke skabe betydelig map-navigation stutter,
- aftermath har timestamps/lifetime,
- control-change visuals følger faktisk controller-state.

## B-369 — Ambient animation LOD, performance and QA

**Status:** PLANLAGT

Living-world systemet skal bruge semantic LOD, så kortet kan være levende uden at blive dyrt.

Foreslået LOD:
- **Far zoom:** ingen worker loops; kun større state-indikatorer, røg/lys og evt. tog/skib som meget simple marks.
- **Medium zoom:** construction stages, simple traffic, camps og større ambient activity.
- **Near zoom:** workers, små køretøjer, dyr, værktøj, kajaktivitet, lejrbål og andre korte loops.

Acceptance:
- pooled/reused actors hvor relevant,
- ingen unbounded spawn accumulation,
- off-screen og langt væk aktivitet culles,
- pause/time scale håndteres korrekt,
- synthetic stress-test kan køres med mange samtidige ambient sites,
- living-world layer kan deaktiveres under performance QA.

## Prioriteret første implementation

Første praktiske slice bør være:
1. barracks construction,
2. farm construction,
3. road works,
4. rail construction,
5. train movement,
6. road/supply wagons,
7. military camp ambience,
8. day/night + simple smoke/fire,
9. ports/ferries,
10. seasonal agriculture.

Denne rækkefølge giver høj synlig værdi tidligt og tester samtidig den fælles state-driven architecture.

## Performance-regel

Living-world visuals er sekundære til simulation og UI. Hvis en animation ikke kan vises uden mærkbar performance- eller readability-regression, skal den culles, simplificeres eller erstattes af et billigere visual. Simulation state må aldrig afhænge af, om den visuelle animation er aktiv.
