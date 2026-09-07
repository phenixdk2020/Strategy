# PROJECT 1864 — Designmanual

**Aktuel designbaseline: v00.02.08**  
**Aktuel prototype-workbranch: P0A v00.00.09 TACTICAL COMMAND TEST**

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

## Versionshistorik

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
