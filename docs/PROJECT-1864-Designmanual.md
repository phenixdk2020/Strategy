# PROJECT 1864 — Designmanual

**Aktuel designbaseline: v00.02.11**  
**Aktuel tactical prototype baseline: P0A v00.00.09 TACTICAL COMMAND TEST**  
**Aktuel aktive campaign-version: v00.00.11 WORK**  
**Aktuel campaign 3D development branch: `work/v00.00.13h-denmark-premium-visual-pass` — IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA**

Grand Strategy i realtid + taktiske 3D-slag. Denne GitHub-udgave er opdelt i dele for overskuelig versionsstyring. Den layoutede Word-master opdateres parallelt som projektartefakt, mens GitHub-Markdown er den løbende designmæssige source of truth.

Projektets centrale intake-log for besluttede men endnu ikke implementerede funktioner, planlagte opgaver, research-emner og løse idéer ligger i [PROJECT-BACKLOG.md](PROJECT-BACKLOG.md). Større emner kan have detaljerede backlog-supplementer, som senere konsolideres ind i hovedbackloggen.

Den komplette dokumentation af **hvad der implementeres og skal testes i P0A v00.00.09**, officerstats, AI difficulty, skydning/fire policy, tactical command-menu, kendte begrænsninger og den fulde Unity acceptance-test ligger i [P0A v00.00.09 Release Notes](releases/P0A-v00.00.09-RELEASE-NOTES.md).

## Indhold

- [Del 1: 1–6 — Executive summary, slutvision, strategisk realtid, kort, nationer og OOB](parts/part-01-01-06.md)
- [Del 2: 7–12 — Enheder, officerer, ordrer, march, logistik og fog of war](parts/part-02-07-12.md)
- [Del 3A: 13–19 — Taktiske 3D-slag, kamp, våbenarter, casualties, retreat og flåde](parts/part-03a-13-19.md)
- [Del 3B: 20 — Befolkning, økonomi, byudvikling, industri, handel, forskning, rekruttering, træning, sanitet/fanger, regimentshistorik og perks](parts/part-03b-20-20.md)
- [Del 3C: 20.16 — Strategisk landudvikling: veje, jernbane, gårde, hesteopdræt, våbenindustri og regionale projekter](parts/part-03c-20-16-strategic-development.md)
- [Del 3D: Battle supply, skumring/nat, overnight resupply, kavaleri og dragoner](parts/part-03d-night-supply-cavalry.md)
- [Del 4: 21–27 — Strategisk/taktisk AI, terræn, performance, UI, save/modding og historisk datamodel](parts/part-04-21-27.md)
- [Del 5A: Feature-arkitektur F00–F15](parts/part-05a-F00-F15.md)
- [Del 5B: Feature-arkitektur F16–F31](parts/part-05b-F16-F31.md)
- [Del 5C: Feature-arkitektur F32–F47](parts/part-05c-F32-F47.md)
- [Del 6: 29–36 — Milepæle, immediate prototype sequence, vertical slice, risici, datarelationer, historisk grounding og designbeslutninger](parts/part-06-29-36.md)
- [Del 7: 37 — Implementeringsstatus P0A Unity 3D Battle Prototype](parts/part-07-37-P0A.md)
- [Del 8: 38 — P0A v00.00.08 reload, experience, salve-feedback og enkel casualty-visual](parts/part-08-38-P0A-v08.md)
- [Release Notes — P0A v00.00.08 TEST](releases/P0A-v00.00.08-RELEASE-NOTES.md)
- [Release Notes — P0A v00.00.09 TACTICAL COMMAND TEST](releases/P0A-v00.00.09-RELEASE-NOTES.md)
- [Projekt-backlog — beslutninger, planlagte funktioner, research og idéer](PROJECT-BACKLOG.md)
- [Backlog B-160–B-169 — strategisk landudvikling](backlog/B-160-STRATEGIC-DEVELOPMENT.md)
- [Backlog B-170–B-179 — Officer AI, delegeret kommando og AI Unit ON/OFF](backlog/B-170-OFFICER-AI-DELEGATION.md)
- [Backlog B-180–B-189 — symmetrisk fjende-AI, sværhedsgrad og v00.00.09-prioritet](backlog/B-180-AI-DIFFICULTY-AND-V009.md)
- [Backlog B-190–B-199 — officerstats, Composure/Nerve og AI decision model](backlog/B-190-OFFICER-STATS-MODEL.md)
- [Backlog B-200–B-209 — Close/Medium/Long range bands, HQ hierarchy, semantic zoom og couriers](backlog/B-200-COMMAND-VISUALS-RANGE-HQ-COURIERS.md)
- [Backlog B-210–B-219 — fire eligibility, skudkegle og højere formation templates](backlog/B-210-FIRE-ELIGIBILITY-AND-HIGHER-FORMATIONS.md)
- [Backlog B-220–B-229 — Pause/x0,5/x1/x2/x5/x20, simulationstid og klokke](backlog/B-220-SIMULATION-TIME-CONTROLS.md)
- [Backlog B-230–B-239 — battle supply, skumring/nat og overnight resupply](backlog/B-230-BATTLE-SUPPLY-NIGHT-OPERATIONS.md)
- [Backlog B-240–B-249 — udvidet kavaleri- og dragonmodel](backlog/B-240-CAVALRY-DRAGOONS-EXPANDED.md)
- [Backlog B-250–B-259 — fog of war, scouts og HQ command effectiveness](backlog/B-250-FOG-SCOUTS-COMMAND-EFFECTIVENESS.md)
- [Backlog B-360–B-369 — levende campaign-kort, systemisk construction og state-drevne ambient-animationer](backlog/B-360-CAMPAIGN-LIVING-WORLD.md)
- [Campaign v00.00.12 Planning — infrastruktur, logistics, transport, society/events og march/mobility](PROJECT-1864-Campaign-v00.00.12-Planning.md)
- [Campaign v00.00.13 Planning — 3D Strategic World & Geospatial Foundation](PROJECT-1864-Campaign-v00.00.13-Planning.md)
- [Campaign v00.00.13 — Layered 3D Map Architecture](PROJECT-1864-Campaign-v00.00.13-3D-Map-Architecture.md)
- [Campaign v00.00.13 3D DEV — Implementation Notes](releases/CAMPAIGN-v00.00.13-3D-DEV-NOTES.md)
- [Campaign v00.00.13h — Denmark Premium Visual Pass](PROJECT-1864-Campaign-v00.00.13h-Premium-Visual-Pass.md)
- [Designmanual addendum v00.02.11 — Premium Visual Baseline](PROJECT-1864-Designmanual-v00.02.11-Premium-Visual-Addendum.md)
- [Campaign v00.00.14 Planning — Government, Research, Doctrine & Strategic AI](PROJECT-1864-Campaign-v00.00.14-Planning.md)
- [Campaign v00.00.14 Design Supplement](PROJECT-1864-Campaign-v00.00.14-Design-Supplement.md)
- [Campaign v00.00.15 Planning — Multi-Nation AI, Trade, Diplomacy & Alliances](PROJECT-1864-Campaign-v00.00.15-Planning.md)

## v00.00.09 Tactical Command Test — implementeringsstatus

P0A v00.00.09 på arbejdsbranchen implementerer den første sammenhængende tactical-command slice oven på v00.00.08. Danske testregimenter er **forsvarere**, mens de preussiske testregimenter er **angribere**. Battlefield er udvidet fra 180x120 til **360x240**, startafstanden er forøget, og kameraets bounds/zoom er udvidet, så spilleren har reel tid og plads til at pause, inspicere, udstede ordrer og manøvrere før kontakt.

Alle fire regimenter får `OfficerProfile` + `OfficerAIController`. Danske regimenter starter AI OFF, mens preussiske regimenter starter AI ON gennem samme shared decision core. Officerprofilen består af Leadership, Inspiration, Tactical Skill, Initiative, Staff/Command Skill, Discipline/Obedience, Aggressiveness/Caution og Composure/Nerve samt separat Officer Experience. Composure påvirker stress-relateret reaction delay og decision noise; alle profiler i v00.00.09 er QA-data og ikke historiske ratings.

Valgte danske regimenter kan toggles med **`I` = AI UNIT ON/OFF**; `A` er fortsat kamera-left i WASD. En contextual command-menu giver `DEFENSIVE / BALANCED / OFFENSIVE` doctrine, en 0–100 commander `forsigtig ↔ aggressiv` intent og fire policy `HOLD / CLOSE / MEDIUM / LONG`. Commander intent biaser execution, men officerens egen Aggressiveness er fortsat dominerende i første model. Højreklik går direkte til Regiment, når AI er OFF, og bliver Officer AI Move/Attack mission, når AI er ON.

Fjendens Officer AI må ikke blot løbe frem. Preussiske angribere starter Offensive med OrderAgg 65 og Medium fire policy. AI beregner preferred engagement range ud fra officer/order/doctrine, kan bruge column på længere approach, deployer til line ved engagement, kan stabilisere under morale pressure og lukker nu faktisk til sin beregnede preferred range i stedet for at blive maskeret af den gamle `OrderAttack` stopafstand. 18th Regiment har desuden et flank/approach waypoint før det reassesserer angrebet.

Easy/Normal/Hard påvirker kun den computerstyrede preussiske sides ekstra reaction/noise layer. Difficulty må ikke ændre weapon accuracy, reload, range, movement, morale, cohesion, casualties, officer stats eller skjult viden. Player-delegerede danske officerer bruger deres faktiske QA-profil på referenceindstillinger uanset enemy difficulty.

v00.00.09 har en fælles simulation time-control bar med **PAUSE / x0,5 / PLAY x1 / x2 / x5 / x20** og synligt dato/klokkeslæt. x0,5 er slow tactical mode til ordreafgivelse og observation under pres. Pause stopper AI, movement, reload/fire progression og battle clock samlet, mens kamera/UI fortsat er brugbart.

## Directional fire, fire discipline og accuracy

Den gamle 360° infantry range-ring er erstattet af en **120° fremadrettet fire fan (±60°)**, som følger regimentets facing og åbner ud fra formationens frontage. Det repræsenterer, at soldater i en linje kan traverse våbnet til siderne, men at regimentet ikke kan skyde lige effektivt bagud uden at vende/reformere.

Ved valgt regiment vises tre nested grænser:

- **Close:** op til 50 % af `EffectiveRange`.
- **Medium:** op til `EffectiveRange`.
- **Long:** op til `MaximumRange`.

Fire policy bestemmer **hvornår** enheden må åbne ild: `HOLD`, `CLOSE`, `MEDIUM` eller `LONG`. Et mål skal både være inden for fire-policy-afstanden og inden for den forward fire arc. Close/Medium/Long er ordre-/UI-grænser; den underliggende accuracy er en **kontinuerlig distancekurve**, så tættere mål gradvist bliver lettere at ramme uden kunstige hit-chance spring ved band-grænserne. Weapon-profile reload, Experience-reload, 0-hit og positiv `Ramte N` feedback fra v00.00.08 bevares.

Næste combat-fase udbygger samme retning med formation-segmenteret `eligibleFiringFraction`, LOS, friendly obstruction, terrain/smoke og target exposure, så kun den del af regimentets frontage der reelt kan skyde bidrager til salven.

## Command-visualisation efter v00.00.09

Efter v00.00.09 runtime-gaten fortsætter command-UI-retningen med fysiske HQ-entities, command-links, courier/order lifecycle, fog-of-war reports og semantic zoom. Valg af et HQ viser relationer til direkte underenheder. Ved udzoomning skifter enheder via semantic zoom fra 3D-formationer til forenklet formation display og derefter NATO/APP-6-lignende taktiske symboler uden at ændre simulation state.

Ordrer transporteres senere gennem et egentligt courier/order-lifecycle-system. En aktiv ordre kan vises som en stiplet route fra afsender-HQ til modtager med en bevægelig courier-markør, hvis position svarer til faktisk simulation progress. Command relationship lines og konkrete order routes er to separate overlays, og de vises primært ved valgt HQ/enhed eller aktiv Command Overlay for at undgå visuelt rod.

Courier-interception håndteres primært som **område-/risikomodel**, ikke som manuel jagt på en enkelt lille rytter. Enemy presence, cavalry/scouts, screening, roads, terrain, mørke og command quality kan føre til reroute, delay, searching eller i sjældnere tilfælde lost/intercepted. Fjendens couriers er selv underlagt fog of war, så courier-markører ikke bliver en skjult radar til enemy HQ.

## Fire eligibility og højere formationer efter v00.00.09

Combat resolution skal senere beregne **hvor stor en del af formationens frontage der faktisk kan skyde på målet**. En fjende inden for range betyder derfor ikke automatisk, at hele regimentet deltager. Formationens frontage opdeles i et begrænset antal fire groups/segmenter, som testes mod fire arc, LOS, range, friendly obstruction, terrain/smoke og target exposure. Resultatet bliver en `eligibleFiringFraction` mellem 0 og 1, der indgår i salveberegningen sammen med den kontinuerlige accuracy-by-range-kurve.

Højere HQ'er skal samtidig kunne opstille deres underenheder efter data-drevne formation templates, fx **4 abreast**, **3 + 1 reserve**, **2 + 2**, echelon, march column og kombinationer med artilleri i centre/wing/rear-high-ground slots. Reserve er en faktisk rolle/state, og Officer AI skal senere kunne vælge og tilpasse template ud fra mission, terræn, frontage, artilleri, flanker, reservebehov og officerens stats. Templates giver målpositioner; terrain fitting må justere dem til brugbart terræn uden at bryde enhedsidentitet eller command relation.

## Battle supply, nat og flerdagsslag

Forsyning under et taktisk slag skal være fysisk og begrænset. Enheder forbruger konkret ammunition og kan kun genforsynes fra kompatible wagons/caissons/field trains/depots med reel beholdning, transportkapacitet og en brugbar rute. Resupply under aktiv kamp er muligt, men kan være langsomt, delvist eller blokeret af enemy fire, terrain, manglende wagons/horses eller afskårne forbindelser.

Skumring skal være en **phase transition**, ikke et universelt hard battle stop. Battle state kan fortsætte som `DAYLIGHT -> DUSK -> NIGHT -> DAWN`. Normal organiseret formation combat reduceres kraftigt i mørke, mens hold, withdrawal, reorganisation, casualty collection, fieldworks, patrols, courier traffic og resupply fortsat kan ske. Begrænset night fighting er muligt, men skal være risikabelt og afhænge af mission/officer/doctrine.

Natten bliver et naturligt resupply-vindue. En formation får dog kun overnight resupply, hvis den stadig har eller kan genetablere en fysisk supply route tilbage til egne lines/field train, og der faktisk findes stock og transportkapacitet. **Afskårne enheder får ingen automatisk ammunition om natten.** Flerdagsslag fortsætter med persistent casualties, ammo, fatigue, positions, fieldworks, officer/equipment state og supply connectivity.

Sunrise/sunset/dusk skal senere beregnes ud fra dato, geografisk position og relevant weather/light state. UI kan auto-pause ved skumring/daggry og give command-valg som `Hold positions`, `Withdraw`, `Continue night operations`, `Reorganize/resupply` eller `Prepare dawn attack`.

## Kavaleri og dragoner

Kavaleri skal være langt mere end en charge-knap. Det får roller inden for **reconnaissance, screening/counter-recon, flank security, courier support/escort, pursuit, raids mod supply/courier/telegraph routes og exploitation**.

Dragoner kan bevæge sig mounted og sidde af til sustained fire/combat. Ved dismount efterlades horses og horse holders som en faktisk tactical state; remount tager tid og kan reduceres eller mislykkes, hvis heste/holdere er tabt eller spredt. Mounted mobility påvirkes af horse fatigue, casualties og terrain.

Charge-resultater skal afhænge af target formation/state, facing, terrain, surprise, cavalry cohesion/fatigue/momentum og officer quality. Et frontalt charge mod steady formed infantry med god fire discipline skal være meget risikabelt, mens cavalry kan være særdeles effektivt mod routed/disordered infantry, exposed skirmishers, retreating artillery/transport og isolerede rear-area targets.

Cavalry raids kobles direkte til supply- og command-systemet: en cavalry formation kan true eller skære en supply/courier route uden at erobre hele regionen. Det kan dermed forhindre overnight resupply og øge command delay.

## Fog of war, scouts og command effectiveness

Fog of war er en **knowledge-state model**, ikke blot skjult grafik. En fjendtlig formation kan være `Unknown`, `Suspected`, `Contact`, `Identified`, `Fresh observation` eller `Stale`. Når kontakt mistes, kan last known position blive stående med faldende confidence i stedet for perfekt live-tracking.

Reconnaissance kommer fra faktiske kilder som cavalry patrols, dragoner, skirmishers/scout detachments, line units, HQ, observation points og senere civilians/telegraph/naval reports. Spotting og identification påvirkes bl.a. af afstand, terrain, vegetation, elevation, daylight/night, weather, smoke, target size/movement, firing signature, scout quality og enemy screening.

Information skal **rapporteres gennem command-nettet**. En scout kan derfor se en fjende før Division HQ ved det. Reports kan forsinkes eller gå tabt efter samme grundprincipper som couriers/orders. En lokal Officer AI kan reagere på frisk lokal information, mens overordnet HQ stadig arbejder med ældre knowledge state.

HQ får et visuelt **command effectiveness envelope**, men ikke en hård magisk radius. Første niveauer er `Command Core -> Supported -> Extended -> Detached -> Isolated`. Command effectiveness falder gradvist med afstand, terrain og communication quality og følger den hierarkiske chain `Division HQ -> Brigade HQ -> Regiment/Battalion`.

Dårlig command connectivity påvirker primært order delay, acknowledgement, reporting, coordination, reserve/support reaction og hvor meget formationen må stole på lokal Initiative/Tactical Skill/Composure. Den giver **ikke** en vilkårlig direkte accuracy- eller damage-penalty. Roads og gode courier routes kan senere udvide den effektive command reach, mens woods, rivers uden crossings, svært terræn og enemy interdiction kan skabe svage sektorer i envelope-visningen.

Screens/counter-recon bliver en rigtig mission. Cavalry og skirmishers kan beskytte HQ/courier/supply approaches, opdage enemy scouts tidligere og reducere modstanderens observation confidence uden nødvendigvis at skulle destruere hver enkelt scout fysisk.

## Campaign-kort: levende verden og state-drevne ambient-animationer

Campaign-kortet skal ikke opleves som et statisk strategibræt. Når tiden går, skal verden vise diskrete tegn på aktivitet, byggeri, transport, årstid, krig og økonomi. Den godkendte retning er **systemisk/state-drevet visualisering** frem for kun dekorative loops: hvor der findes en reel campaign-state, skal animationen afspejle den state.

Eksempler på godkendt living-world retning:

- kaserner, farms, depoter, veje, jernbaner og fortifications bygges visuelt i stages som `SITE -> FOUNDATION -> FRAME/SCAFFOLD -> PARTIAL -> NEAR COMPLETE -> COMPLETE`, bundet til faktisk build progress,
- arbejdere, vogne og materialeaktivitet kan vises ved aktive bygge- og reparationsprojekter,
- små tog kører på aktive rail-links og stopper, hvis linket er lukket, ødelagt eller utilgængeligt,
- damp-/sejlskibe, færger og havneaktivitet kan afspejle gyldige port- og sea/ferry-links,
- hestevogne, diligencer og forsyningsvogne kan afspejle civil trafik, militær trafik og supply-flow,
- stationære formationer kan efter passende tid udvikle små lejre med telte, lejrbål, heste og aktivitet,
- couriers, scouts og supply columns kan visualisere eksisterende command-, recon- og logistics-state uden at lække skjult fjendeinformation,
- landbrug kan ændre sig med campaign-dato og sæson gennem pløjning, såning, voksende marker, høst og høstakke,
- større byer, fabrikker, depoter og værksteder kan have diskret røg og aktivitet, som senere kan kobles til faktisk produktion/stockflow,
- dag/nat, skyer, regn, tåge, senere sne samt fælles vindretning i flag/røg skal give tidslig og geografisk variation,
- battle aftermath kan give midlertidig røg, ødelagte vogne, hospital/ambulanceaktivitet, skader og andre state-baserede spor,
- kontrolskifte kan ændre flag, checkpoints og aktivitetsniveau,
- civil trafik/flygtninge kan senere afspejle frontnærhed, besættelse og campaign events.

Living-world visuals skal bruge **semantic animation LOD**. Ved langt zoom vises kun billige, vigtige tegn på aktivitet; ved mellemzoom kan construction stages, trafik, camps og større ambient aktivitet ses; ved tæt zoom kan workers, dyr, vogne, kajaktivitet, lejrbål og andre korte loops aktiveres. Off-screen og fjerne aktiviteter skal culles/pooles, og der må ikke opstå ubegrænset spawn accumulation.

Simulation state må aldrig afhænge af, om en ambient-animation renderer. Living-world layeren skal kunne slås helt fra til performance/QA uden at ændre campaign-resultater. Rent dekorative loops er tilladt, men må ikke kommunikere en gameplay-state, der ikke er sand. Det detaljerede godkendte work package ligger i [B-360–B-369 — Campaign Living World](backlog/B-360-CAMPAIGN-LIVING-WORLD.md).

## Campaign strategic-world baseline — beslutninger efter v00.02.09

### Infrastruktur, transport og rolling stock

Campaign-verdenens infrastruktur er persistent gameplay-state. Veje, jernbaner, broer, depoter, stationer, havne, telegraph, kaserner, farms og fortifications skal kunne bygges, opgraderes, beskadiges og repareres gennem campaign-tid og senere økonomi/manpower/resources.

Jernbaneinfrastruktur og rolling stock er separate ressourcer. En jernbane giver ikke ubegrænset transport. Nationer/regioner har begrænsede pools af lokomotiver, passagervogne, godsvogne, militære/specialvogne og relevant transportmateriel. Troppetransport og godstransport konkurrerer om kapaciteten. Rolling stock skal senere kunne produceres, importeres/købes, repareres, beskadiges, erobres og mistes.

En formation, der flyttes med tog, gennemgår et faktisk lifecycle som `WAIT FOR STOCK -> ASSEMBLE TRAIN -> LOAD -> DEPART -> IN TRANSIT -> WAIT/STOP -> ARRIVE -> UNLOAD -> RELEASE STOCK`. Det visuelle troop train følger den samme authoritative route progress hele vejen station-til-station og må culles/LOD-reduceres uden at ændre simulationen.

### Strategisk march, normal march og forced march

Strategiske formationer må ikke have én vilkårlig fast map-speed. Hver nødvendig attached component får en data-drevet mobility profile, fx `Foot`, `Mounted`, `HorseDrawn`, `WagonTrain`, `RailEligible` og `SeaEligible`.

Når enheder skal marchere samlet, gælder grundreglen:

`FormationEffectiveSpeed = min(EffectiveSpeed(required attached components))`

Den langsomste nødvendige component er bottleneck efter hensyn til heste, artilleri-/transportbyrde, vejklasse, terræn, vejr, fatigue, crossings og congestion. Mounted cavalry/HQ kan derfor ikke få samlet infanteri eller tungt artilleri til at bevæge sig med cavalry-fart. En langsom component kan kun ophøre med at være bottleneck, hvis den reelt detach'es og får separat movement/order state.

`NORMAL MARCH` og `FORCED MARCH` er separate strategiske marchformer. Normal march søger bæredygtig dagsdistance og battle readiness gennem march-/hvilevinduer. Forced march øger primært marcharbejde/marchtimer og dermed dagsdistance, men giver større fatigue, straggling, horse-condition/tab-hooks og lavere readiness ved ankomst. Forced march kan ikke ophæve hårde begrænsninger som manglende trækheste, ødelagt bro eller impassable terrain. Night march kan senere være separat policy/order.

ETA må ikke være simpel `distance / top speed`; den skal kunne medregne marchtimer pr. døgn, hvile, column length, passage delay, congestion, terrain/weather, bridge/ferry delay og forced-march konsekvenser. UI skal forklare både bottleneck og vigtigste modifiers.

### Geospatial distance og kortets skala

Unity world units er **aldrig authoritative kilometer**. Den nuværende prototypeprojektion dækker omtrent `47.0–71.5 N` og `4.0–32.5 E`, mens præsentationen er cirka `520 x 620` Unity units. Simulationen må derfor ikke bruge `Vector3.Distance` på render-objekter til marchtid, togtransport eller logistics ETA.

Geografisk identitet lagres som latitude/longitude, mens vej-/rail-/ferry-ruter senere skal have eksplicit geografisk polyline-geometry. Autoritativ routedistance er:

`RouteDistanceKm = sum(geodesic/polyline distance of traversed route geometry)`

Den visuelle kortprojektion og Unity-skala kan derfor ændres uden at ændre campaign-resultater. Real elevation/slope data holdes separat fra visual vertical exaggeration; et visuelt overdrevet bjerg må ikke skabe en falsk movement-penalty.

### Layered 3D strategic world

Campaign-kortet udvikles til en fælles geospatial 3D-verden og ikke til en stak uafhængige flade planes. Den godkendte arkitekturstak er:

1. geospatial reference/projection,
2. elevation/terrain,
3. hydrology/coast/water,
4. land cover/vegetation/agriculture,
5. infrastructure: roads/rail/bridges/ports/telegraph,
6. settlements/facilities/fortifications,
7. strategic entities: formations/trains/ships/convoys/projects,
8. living-world visuals,
9. political/logistics/intelligence overlays,
10. weather/lighting/atmosphere,
11. UI/labels/selection/debug/measurement.

Disse er primært data-/renderlag. Kun de kategorier, der har konkret behov for raycast, culling, collision eller selection masks, skal bruge Unity `LayerMask` slots.

Roads og rail skal på sigt være terrain-conforming 3D splines genereret fra geografiske route polylines. Samme route-definition skal drive kilometer, ETA, troop-train position, convoy position, bridge/crossing attachment, construction progress, congestion/interdiction og selected-route highlight.

### Campaign v00.00.13 3D DEV — første implementerede slice

På `work/v00.00.13-3d-campaign-map` findes nu en første implementeret 3D campaign-slice, som **afventer Unity compile/runtime QA og ikke er en promoveret campaign-version**. Den indeholder proceduralt campaign terrain, coarse land/sea mask, højere visuelt relief i Norge/Sverige end Danmark, layer roots `L1–L10`, terrain-conforming eksisterende strategic links, 3D settlement miniatures, station/depot/port/fortification miniatures, terrain-aware camera, campaign-time day/night/atmosphere samt map-layer visibility controls.

Der findes også QA-prototyper på staged construction: en kaserne ved Aalborg og en farm ved Aarhus bygges gennem flere visuelle stages med simple worker loops. Construction progress og workers følger `CampaignSession.CurrentDateTime`, så campaign pause stopper progressionen. Det demonstrerer designreglen om state-drevne bygningsanimationer.

DEV-begrænsningerne er eksplicitte: terrænet er endnu procedural/coarse og ikke DEM/GIS, coastlines er grove, strategic links er endnu node-to-node og ikke historiske route polylines, rail/road classification er foreløbig, construction QA-projekterne er endnu ikke koblet til fuld økonomi/resource queue, og production troop-train lifecycle, final land cover, final weather, chunk streaming/floating origin mangler stadig.

### Campaign v00.00.13h — Premium visual baseline

Den visuelle retning for campaign-kortet er nu **Historic Miniature Grand Strategy Diorama**: historisk, seriøs, læsbar og stemningsfuld 3D-miniaturepræsentation, uden cartoon-look og uden krav om fotorealisme. v13h er den første build, hvor grafisk kvalitet er et selvstændigt acceptance-mål frem for alene teknisk geometri/QA.

Danmark-first-reglen fastholdes under denne milestone: kun noder med `CampaignMapRegion.Denmark` må vises som settlements, labels, formationsmarkører og presentation-links. Sverige, Norge, Finland og Tyskland bevares i simulationen, men skjules visuelt indtil Denmark-baselinen er godkendt.

Premium-laget må tilføre dæmpede terrain-materialer, deterministic fields/hedgerows/woodland clusters, mørkere og glattere vand, richer settlement-miniatures, tydelig road/rail/ferry-hierarki, construction-site dressing, warm light, soft shadows, restrained fog samt elegant semantic label rendering. Alt dette er **presentation-only** og må ikke ændre route distance, ETA, movement, campaign time, logistics, AI eller battle state.

Aalborg-kasernen og Aarhus-farmen bevarer deres eksisterende staged campaign-time construction som authoritative state. Premium dressing må gøre sites visuelt rigere, men må ikke vise et projekt som færdigt før dets build progress tillader det. Den detaljerede version-/QA-spec ligger i [Campaign v00.00.13h — Denmark Premium Visual Pass](PROJECT-1864-Campaign-v00.00.13h-Premium-Visual-Pass.md).

### Government, ministre og granular AI-delegation

Et senere nationalt decision layer gør det muligt at styre landet direkte eller delegere konkrete ressortområder til AI. Baseline modes er `MANUAL`, `ADVISORY`, `ASSISTED` og `AUTO`. Delegation er granular: spilleren kan eksempelvis automatisere economy/logistics, men selv styre General Staff plans, eller omvendt.

Engine-roller er semantic portfolios, fordi historiske titler varierer mellem lande og år. Scenario-data leverer de konkrete historiske navne/titler. De planlagte kerneportfolios omfatter Head of Government/Cabinet, War Ministry, Chief of General Staff, Quartermaster, Finance, Public Works, Rail/Transport, Industry/Trade, Agriculture, Interior, Foreign Affairs og Intelligence/Reconnaissance.

AI bruger samme lovlige campaign actions som spilleren og får ikke gratis penge, manpower, våben, heste, rolling stock, instant construction, transport capacity eller skjult intelligence. Spilleren kan definere guardrails som national posture, theatre priorities, minimum treasury reserve, spending/debt limits, recruitment severity, reserve manpower floor, infrastructure weights, protected resources, prohibited projects og risk tolerance. AUTO betyder delegation, ikke cheat-mode.

Betydelige AI-beslutninger skal være forklarlige gennem audit/decision records med actor, positive/negative reason factors, constraints, cost, ETA og result. Spilleren skal kunne se hvorfor AI prioriterede fx en jernbane, depot, regiment, artilleriproduktion eller research project.

### Research, teknologi og militær doctrine

Research skal være capability-/institution-driven frem for primært et arcade tech tree med generiske procentbuffs. Research kan åbne eller forbedre equipment, procedures, institutions, staff work, logistics, railways, telegraph, medicine, industry, agriculture, weapons og doctrine. Research completion er ikke det samme som øjeblikkelig feltimplementering; prototype, adoption, procurement, production og deployment kan være separate livscycles.

Militær doctrine opdeles i tre niveauer:

- **Strategic doctrine** — national war posture, mobilization/concentration, fortress strategy, railway concentration, attrition/manoeuvre.
- **Operational doctrine** — corps/division concentration, reserves, marches, screening, supply bases, crossings og pursuit.
- **Tactical doctrine** — skirmisher use, artillery preparation, line/column transitions, flanking, defensive positions, counterattack reserves og cavalry roles.

Doctrine ændrer planlægning, preferences og tilgængelige handlingsmønstre; den giver ikke hidden vision, gratis movement eller magiske damage-bonusser. General Staff AI skal planlægge ud fra reel geography, march/forced-march ETA, finite rail/rolling stock, depots, supply, mobilization og faktisk knowledge-state.

Historisk data/research skal kunne markeres med source/provenance, confidence og status som `historical`, `estimated` eller `QA-placeholder`, så verificerede fakta ikke blandes sammen med balancetal eller midlertidige testdata.

### Multi-nation AI parity

Alle lande skal på sigt bruge samme nationale systemarkitektur. Et ikke-player land er ikke en statisk baggrund: det kan udvikle roads/rail/infrastructure, styre economy/transport, bygge og mobilisere militær, prioritere research/doctrine, allokere supply og reagere strategisk gennem de samme legal actions og resource/time constraints som spilleren.

`PlayerControl` er en controller assignment, ikke en særlig nationstype. Dette gør det muligt senere at skifte spillernation uden at omskrive simulationen. AI-nationer må have forskellig competence, doctrines, personalities, goals og information, men ikke alternative regler eller gratis resources.

### International handel, diplomati og alliancer

International trade skal være **contract + physical flow**. En handelsaftale teleporterer ikke varer mellem nationale inventories. Food, raw materials, weapons, horses, rolling stock og andre goods skal flyttes gennem faktisk road/rail/port/sea transport capacity og kan påvirkes af delay, congestion, disruption, blockade, tariffs, embargo og shortages.

Diplomati skal senere rumme bilateral relation, trade treaties, military access/transit rights, guarantees, influence/pressure, treaty duration/breach/consequences og explainable diplomatic AI. Relation må ikke reduceres til ét friendliness-tal; beslutninger skal kunne vægte national interests, commitments, military balance estimates, trade dependence, territorial goals, trust og kendt tidligere adfærd.

Alliancer/coalitions skal kunne have call-to-arms, neutrality, war aims, negotiated peace/separate peace og militær/economic cooperation. En alliance giver ikke automatisk perfekt fælles command eller intelligence. Allied forces beholder national ownership/command, medmindre en eksplicit expeditionary/command arrangement ændrer det, og intelligence sharing følger treaty scope og communication delay.

### Historisk plausibel replay variation

Historien fastlægger scenarioets startbetingelser og plausibility envelope, men er **ikke et fast fremtidigt script**. Landenes udvikling og hærens opbygning skal have bounded, weighted og seed-baseret variation, så flere campaigns fra samme startdato ikke nødvendigvis udvikler sig ens.

En national AI-beslutning kan konceptuelt vægte:

`Need + Threat + Geography + Economy + Resources + Ministers + Doctrine + PriorEvents + SeededVariation`

Variation må derfor ændre relative prioriteter mellem fx railways, fortifications, industry, artillery, cavalry, reserves, logistics, research og doctrine, men den må ikke tilsidesætte technology plausibility, manpower, resources, construction time, transport eller information constraints. Ministerpersonligheder og senere ministerudskiftninger kan skubbe et land i en anden retning midt i campaignen.

Samme campaign seed skal kunne reproducere samme variation for QA/save-load. Det præcise player-facing niveau for historical-vs-alternate variation fastlægges senere; designmålet er mærkbar replay value uden at verden bliver ren random eller ophører med at ligne midten af 1800-tallet.

## Versionshistorik

- **v00.02.11 — Denmark Premium Visual Baseline** — Fastlægger `Historic Miniature Grand Strategy Diorama` som campaign-kortets visuelle retning og gør Denmark-first premium presentation til en eksplicit quality gate. Godkender premium terrain palette, deterministic fields/hedgerows/woodland dressing, premium water, richer settlement miniatures, road/rail/ferry visual hierarchy, construction dressing, warm light/soft shadows/fog, elegant Denmark-only semantic labels og tættere kamera. Hele laget er presentation-only og må ikke ændre simulation state, ETA, route distance eller AI.
- **v00.02.10 — Strategic World, 3D Map, Government/AI & Multi-Nation baseline** — Konsoliderer beslutninger siden v00.02.09: persistent infrastructure, finite rolling stock og route-following troop trains; slowest-required-component mobility, normal/forced march og explainable ETA; geospatial distance contract hvor Unity units aldrig er authoritative km; layered 3D strategic world med terrain/hydrology/land cover/infrastructure/settlements/entities/living world/overlays/atmosphere/UI; første v00.00.13 3D DEV implementation med procedural terrain, 3D settlements, layer controls og campaign-time staged barracks/farm construction; government/ministers med MANUAL/ADVISORY/ASSISTED/AUTO, research/doctrine og explainable no-cheat strategic AI; multi-nation AI parity, fysisk international trade, diplomacy/alliance principles samt seed-baseret historisk plausibel replay variation.
- **v00.02.09 — Campaign Living World design baseline** — Godkender state-drevne ambient-animationer på campaign-kortet: systemisk staged construction for kaserner/farms/roads/rail/depots/fortifications, workers og road/rail works, tog, skibe/færger, road/supply traffic, camps, couriers/scouts/supply columns, sæsonbaseret landbrug, dag/nat/vejr, battle aftermath og control-change visuals. Fastlægger semantic animation LOD og reglen om, at living-world visuals aldrig må være authoritative simulation state eller lække skjult information. Detaljer registreret som B-360–B-369.
- **v00.02.08 / P0A v00.00.09 TACTICAL COMMAND TEST work branch** — Shared `OfficerAIController`/`OfficerProfile` på alle fire regimenter; `I` toggler player delegation; Defensive/Balanced/Offensive doctrine og 0–100 commander aggression intent; preussiske Officer AI-angribere med preferred-range/manoeuvre/stabilise adfærd; directional 120° infantry fire fan; HOLD/CLOSE/MEDIUM/LONG fire discipline; continuous closer-is-easier accuracy; battlefield 360x240; Pause/x0,5/x1/x2/x5/x20 og battle clock. v00.00.08 reload/0-hit/`Ramte N`/casualty-visual skal fortsat regressionsbestå. De senere besluttede systemer omfatter segmenteret fire eligibility, fysiske HQ-entities, semantic zoom, courier/order progress/interception, højere formation templates, fysisk battle resupply, night/overnight logistics, udvidet cavalry/dragoon model og fog of war/scouts/gradvis HQ command effectiveness. v00.00.09 er TEST og må først promoveres efter Unity compile/Play acceptance.
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