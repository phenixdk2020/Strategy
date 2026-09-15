# PROJECT 1864 — Designmanual

**Aktuel designbaseline: v00.02.13**  
**Aktuel prototype-workbranch: P0A v00.00.09f29e REGIMENTAL DEFENSE + OBJECTIVE VISUALS + COORDINATED ATTACK TEST**

Grand Strategy i realtid + taktiske 3D-slag. Denne GitHub-udgave er opdelt i dele for overskuelig versionsstyring. Den layoutede Word-master opdateres parallelt som projektartefakt, mens GitHub-Markdown er den løbende designmæssige source of truth.

Projektets centrale intake-log for besluttede men endnu ikke implementerede funktioner, planlagte opgaver, research-emner og løse idéer ligger i [PROJECT-BACKLOG.md](PROJECT-BACKLOG.md). Større emner kan have detaljerede backlog-supplementer, som senere konsolideres ind i hovedbackloggen.

Den aktuelle tactical-command/regiment-control baseline ligger på **v00.00.09f29e**. Det centrale design for regimentschef, cavalry, charge shock og infantry square er samlet i [Designsupplement v00.02.09 — Regimentskommando, cavalry og square](design-supplements/v00.02.09-regimental-command-cavalry-square.md). Map/attack-AI-hardening er dokumenteret i [v00.00.09f29b — Battlefield + Attack AI Hardening](design-supplements/v00.00.09f29b-battlefield-attack-ai-hardening.md), den fælles Major/Company command UI i [v00.00.09f29c — Unified Command HUD](design-supplements/v00.00.09f29c-unified-command-hud.md), semantic zoom/HQ visibility/NATO-symboler i [v00.00.09f29d — Semantic Zoom + NATO Symbols](design-supplements/v00.00.09f29d-semantic-zoom-nato-symbols.md), og den aktuelle regimental defense/objective/attack-coordination hardening i [v00.00.09f29e — Regimental Defense, Objective Visuals & Coordinated Attack](design-supplements/v00.00.09f29e-regimental-defense-attack-coordination.md).

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
- [Designsupplement v00.02.09 — Regimentskommando, cavalry, charge shock og infantry square](design-supplements/v00.02.09-regimental-command-cavalry-square.md)
- [Designsupplement v00.00.09f29b — Battlefield + Attack AI Hardening](design-supplements/v00.00.09f29b-battlefield-attack-ai-hardening.md)
- [Designsupplement v00.00.09f29c — Unified Command HUD](design-supplements/v00.00.09f29c-unified-command-hud.md)
- [Designsupplement v00.00.09f29d — Semantic Zoom + NATO Symbols](design-supplements/v00.00.09f29d-semantic-zoom-nato-symbols.md)
- [Designsupplement v00.00.09f29e — Regimental Defense, Objective Visuals & Coordinated Attack](design-supplements/v00.00.09f29e-regimental-defense-attack-coordination.md)
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
- [Backlog B-240–B-249 — udvidet kavaleri-, Gardehusar- og dragonmodel](backlog/B-240-CAVALRY-DRAGOONS-EXPANDED.md)
- [Backlog B-250–B-259 — fog of war, scouts og HQ command effectiveness](backlog/B-250-FOG-SCOUTS-COMMAND-EFFECTIVENESS.md)
- [Backlog B-280–B-291 — Charge, melee, Regiments-HQ, Dragoons og artilleri](backlog/B-280-CHARGE-REGIMENT-HQ-HIERARCHY.md)

## v00.00.09f28 Regiment Control Test — implementeringsstatus

F28 er første samlede regiment-control MVP. Den danske prototypekæde er nu designmæssigt og runtime-mæssigt:

`Oberstløjtnant → Major A / Major B → 4 kompagnier pr. Major → 8 kompagnier i regimentet`

**Kaptajn** er company-chef, **Major** bataljonschef, **Oberstløjtnant** regimentschef, **Oberst** brigadechef og **Generalmajor** divisionschef som standardnavne i spillet. Konkrete historiske OOB-data kan afvige ved tab, fravær eller midlertidig kommando.

Oberstløjtnanten giver missionsordrer til Majorerne — ikke direkte til kompagnierne. Regiments-AI kan i første MVP disponere de to bataljoner side om side, med én bataljon i reserve eller med én bataljon på flankemission. Majorerne omsætter regimentsintentionen til company-slots og lokal udførelse.

Authority er hierarkisk. Direkte manuel company-ordre giver spiller-authority. AI ON bagefter betyder lokal Kaptajn-AI; gammel Major-ordre må ikke genopstå. En ny Major-ordre kan reclaim'e kompagniet. Manuel flytning af Major giver spiller-authority over HQ, mens en ny Oberstløjtnant-ordre kan reclaim'e Majoren og give en frisk bataljonsmission.

HQ'er flytter frem i bounds under angreb i stedet for at stå permanent på startpositionen. Command reach påvirker primært order/reaction delay, coordination, reserve/flank reassignment, recovery og senere information quality — ikke direkte musket accuracy eller damage.

## v00.00.09f29b Battlefield og Attack AI — hardening

Det taktiske battlemap er nu **5760 × 3840 m**, dvs. dobbelt bredde og dobbelt dybde i forhold til den foregående 2880 × 1920 m testflade. Road, river og vegetation fortsætter over den større flade, mens den centrale bridge/farm testzone bevares til regressionstest.

Det aktive QA-scenario beholder nu to fjendtlige Prussian company-scale formationer, `8th Regiment` og `18th Regiment`, så target-selection og fler-target angreb kan testes i samme slag. Begge behandles som company-scale testformationer.

Attack-authority følger den eksplicitte regel **én fysisk movement owner ad gangen**. Når en Major placerer et company og derfor har disabled den lokale `OfficerAIController`, må frontage-, approach- eller andre hjælpe-AI-systemer ikke skrive ny route eller formation. Under-fire reaction har tilsvarende midlertidig authority og må ikke bekæmpes af andre movement layers.

Et eksplicit `AttackTarget` er sticky og må ikke overskrives af "nearest enemy". `AttackNearest` kan revurdere under approach, men låses til et konkret target ved contact, så formationen ikke oscillerer mellem to næsten lige nære fjender.

F29a-visualiseringen bevares: selected company viser connection til egen Major, selected Major viser connection til Oberstløjtnanten, command-lines følger terrænet, og officer target-area cirklen vises under point-orders. Formation endpoint safety kontrollerer hele company-footprint mod åen, så en formation ikke kan godkendes med dele af linjen i åbent vand.

## v00.00.09f29c Unified Command HUD — UI-baseline

Major/Bataljon og Kaptajn/Kompagni bruger samme compact bottom-HUD visual language. HUD'en opdeles efter command level i **ENHEDSINFO**, **AI/DOKTRIN**, relevante ordregrupper og ved Major-selection en separat oversigt over subordinate companies.

State-farver er standardiseret: **grøn = aktiv/valgt**, **rød = inaktiv/ikke valgt**. Det bruges på AI ON/OFF, DEF/BAL/OFF doctrine, company fire policy, formationer og relevante movement states.

Major HUD viser aggregate mænd, tab, morale, cohesion og ammunition samt fire subordinate company rows med mænd/tab/morale/ammo. Bataljonsordrerne er samlet som `ANGRIB HER / FORSVAR HER / RYK FREM / TILBAGETRÆK / SAML / STOP-HOLD`.

Company HUD viser de samme statusprincipper og grupperer `HOLD / CLOSE / MED / LONG` under skydning, `→MED / →LONG / →UD / TVANG / CHARGE / STOP` under movement/orders og `LINJE / KOLONNE / SQUARE` under formation. **SQUARE er synlig i HUD'en og bruger direkte F29 square-state**, ikke en parallel implementering.

`STOP` er defineret som en direkte company-order: local AI OFF, manual route ryddes, withdrawal/charge afsluttes, forced march slås fra og company holder position. Formation-knapper er tilsvarende direct company authority, så AI ikke straks kan overskrive spillerens formation.

De otte danske company-scale enheder skal synligt hedde **1. KOMPAGNI–8. KOMPAGNI**. De ældre strings `1. Regiment`, `5. Regiment`, `2. Regiment` og `3. Regiment` bevares midlertidigt som interne compatibility-id'er, fordi ældre prototype-lag fortsat bruger dem til lookup; display/GameObject naming korrigeres i F29c. Den langsigtede datamodel skal adskille permanent unit-id, display name, parent regiment og battalion/company index.

## v00.00.09f29d Semantic Zoom og NATO-symboler — designforslag + første implementation

Taktisk visualisering skifter informationsform med zoomniveauet i stedet for kun at gøre de samme 3D-modeller mindre. Målet er at bevare fysisk nærkamp i close view og samtidig gøre command structure og formationer læsbare i operational/strategic view.

Semantic zoom bruger fire niveauer: **CLOSE → MEDIUM → OPERATIONAL → STRATEGIC**. HQ'er har højere informationsprioritet end companies og får derfor screen-space markører allerede ved moderat zoom. Major/Battalion vises som `II + HQ`, Oberstløjtnant/Regiment som `III + HQ`, mens company-scale infantry vises som `I` med NATO-style infantry `X`.

I **OPERATIONAL** beholdes 3D-modellerne, mens alle companies får 2D tactical counters med affiliation, strength, morale, facing og for danske enheder ammunition. Friendly er blå, enemy rød, selected gul/guld og routed grå. HQ-counters har fast minimumsstørrelse i screen-space og en beacon/stem ned til den faktiske world-position, så officererne kan lokaliseres selv over terræn og formationer.

I **STRATEGIC** skjules MeshRenderers og ParticleSystemRenderers under companies og HQ'er. Terrain, simulation, colliders, AI, LineRenderers, command links og ordre-state fortsætter uændret. Dermed bliver zoomet reelt en ren taktisk 2D-symbolvisning oven på det eksisterende 3D-terrain, og command-chain lines kan stadig læses.

Første QA bruger camera height som proxy: HQ marker ca. 72 m, Medium ca. 135 m, Operational ca. 235 m og Strategic ca. 390 m. Den langsigtede model bør kunne bruge projected screen footprint i pixels, fordi det er mere robust over for FOV, opløsning og kameraændringer. Smooth 3D↔2D crossfade, counter-click selection, overlap avoidance, very-far battalion aggregation og Brigade/Division echelons (`X`/`XX`) ligger som næste designtrin.

## v00.00.09f29e Regimental defense, objective visuals og coordinated attack — aktuel baseline

Oberstløjtnanten bruger nu samme kompakte 90 px HUD-sprog som Major og Company: **ENHEDSINFO / AI**, **REGIMENTSORDRER** og **BATALJONER UNDER OBERSTLØJTNANT**. AI ON/OFF og DEF/BAL/OFF følger samme grøn-aktiv / rød-inaktiv regel. HUD'en viser total styrke/tab/morale/cohesion/ammunition samt status for begge Majorer/bataljoner.

En committed Regimental point-order forsvinder ikke længere efter klikket. Mens Oberstløjtnanten er valgt, vises en terrain-following objective circle med centre cross og ordrelabel ved det faktiske `ANGRIB HER`, `FORSVAR HER`, `RYK FREM`, `TILBAGETRÆK` eller `SAML` objective. `FORSVAR HER` har den største første QA-radius, fordi markøren repræsenterer en defensive area og ikke kun et punkt.

Valg af Oberstløjtnanten viser hele command chain: `Oberstløjtnant → begge Majorer → alle otte kompagnier`. Samtidig vises alle aktuelle company destination footprints samt aktive routes, så spilleren kan se hvordan regimentsordren er dekomponeret til bataljons- og company-slots uden at klikke hver Major separat.

Den gamle F27 arrival tolerance på ca. 4.5 m kunne få en route til at forsvinde, mens company-centret stadig stod uden for det viste destination rectangle. F29E har en snæver final-arrival correction, der kun arbejder i den gamle tolerance-zone, respekterer F26 under-fire authority og først accepterer slotten omkring 0.85 m fra centre. Der teleporteres ikke.

`FORSVAR HER` betyder nu, at det klikkede point faktisk skal dækkes. F28's generiske to-battalion split på omtrent ±170 m kunne efterlade en stor corridor gennem centrum. F29E bruger som første QA omtrent **±84 m** battalion half-separation omkring det defensive objective. Med lokal reserve og tre front companies pr. battalion mødes de indre frontage-slots omkring regimentscentrum; terrain/river legality kan stadig flytte individuelle slots.

Ved `ANGRIB HER` er tactical role assignment runtime-korrigeret: ved FRONT+RESERVE eller FRONT+FLANK er den battalion, der allerede er nærmest objective, FRONT. Den anden får reserve/flank-rollen. Rollen afgøres ikke længere af laveste samlede marchafstand mellem Major-HQ og slots.

Når flere companies har samme eksplicitte `AttackTarget`, får de separate formation-safe assault slots omkring target i stedet for alle at lukke direkte mod target-centret. 2–4 companies fordeles på en frontal arc med bevaret left/right ordering; yderligere companies lægges som support. `OfficerAIController` er stadig den fysiske movement owner. F29E må ikke skrive gennem en disabled controller og suspenderes under F26 under-fire reaction.

Map enlargement fra F29B kræver også navigation, der faktisk arbejder på **5760 × 3840 m**. Det gamle V4-lag var stadig bygget omkring den tidligere ±176 × ±116 m QA-grid og kunne derfor behandle gyldige enheder på det større kort som værende uden for navigation-bounds. F29E deaktiverer de obsolete small-grid navigation writers på det store map og bruger dynamic `PrototypeBootstrap` battlefield bounds med persistent goals og let obstacle-detour; det eksisterende bridge-only phase system forbliver autoritet for river crossing.

## Directional fire, fire discipline og accuracy

Infantry fire policy bruger `HOLD / CLOSE / MEDIUM / LONG`. Et mål skal være inden for fire-policy-afstanden og relevant forward fire arc. Close/Medium/Long er ordre-/UI-grænser; accuracy ændres kontinuerligt med faktisk afstand.

Næste combat-fase fortsætter med formation-segmenteret `eligibleFiringFraction`, LOS, friendly obstruction, terrain/smoke og target exposure, så kun den del af frontage der faktisk kan skyde bidrager til salven.

## Command-visualisation og order lifecycle

Fysiske HQ-entities, command-links, courier/order lifecycle, fog-of-war reports og semantic zoom er den fælles retning. Valg af et HQ viser relationer til direkte underenheder. Valg af et company viser desuden relationen tilbage til dets direkte Major, og valg af en Major viser relationen op til den aktuelle Oberstløjtnant, så command chain kan læses begge veje under QA og senere semantic zoom.

Ved Regimental HQ selection vises i F29E også det fulde downstream hierarchy og aktuelle company mission destinations. Command relationship lines, persistent HQ objective markering og konkrete future courier/order routes er tre forskellige overlays og må ikke semantisk blandes sammen.

Ordrer transporteres senere gennem et egentligt courier/order-lifecycle-system. En aktiv ordre kan vises som en route fra afsender-HQ til modtager med en bevægelig courier-markør, hvis position svarer til faktisk simulation progress.

Courier-interception håndteres primært som område-/risikomodel. Enemy presence, cavalry/scouts, screening, roads, terrain, mørke og command quality kan føre til reroute, delay, searching eller lost/intercepted. Fjendens couriers er selv underlagt fog of war.

## Fire eligibility og højere formationer

Combat resolution skal senere beregne hvor stor en del af formationens frontage der faktisk kan skyde på målet. Formationens frontage opdeles i fire groups/segmenter, som testes mod fire arc, LOS, range, friendly obstruction, terrain/smoke og target exposure.

Højere HQ'er opstiller underenheder efter data-drevne formation templates, fx 4 abreast, 3 + 1 reserve, 2 + 2, echelon og march column. Reserve er en faktisk rolle/state, ikke en fast bonus.

## Battle supply, nat og flerdagsslag

Forsyning under taktiske slag er fysisk og begrænset. Enheder forbruger konkret ammunition og kan kun genforsynes fra kompatible wagons/caissons/field trains/depots med beholdning, transportkapacitet og brugbar rute.

Skumring er en phase transition, ikke et universelt hard stop. Battle state kan fortsætte som `DAYLIGHT -> DUSK -> NIGHT -> DAWN`. Natten er et naturligt resupply-/reorganisation-vindue, men afskårne enheder får ingen magisk ammunition.

## Kavaleri, Gardehusarer, dragoner og infantry square

Danske **Gardehusarer og dragoner er separate cavalry-typer**, men bygges på en shared cavalry core. Dansk cavalry modelleres ikke som kun sabel; data-driven weapon profiles kan omfte **sabel, karabin og pistol**, mens de konkrete regimentsspecifikke 1864-profiler research-verificeres før endelige stats låses.

Dragoner kan bevæge sig mounted og sidde af til sustained fire/combat. Ved dismount efterlades horses og horse holders som tactical state; remount tager tid og afhænger af hestenes tilstand.

Cavalry charge er directional. FRONT mod steady, facing infantry er den mindst fordelagtige contact; FLANK giver større shock; REAR kan give severe casualties/stragglers, cohesion- og morale shock og høj break/rout probability, især hvis infantry allerede er engaged forfra.

Et mounted charge er ikke garanteret at nå melee. `Charge Confidence / Charge Momentum` påvirkes af casualties, horse hesitation, cohesion, morale, terrain og defensive fire. En vel-timet **close-range volley** fra steady infantry skal kunne få et frontalt charge til at `FALTER`, `ABORT` eller i ekstreme tilfælde `ROUT` før fysisk kontakt.

Infantry får formationerne:

`LINE / COLUMN / SQUARE`

**SQUARE / KARRÉ** er en specifik anti-cavalry formation. Den tager tid at danne, har lav mobility og er stærk mod mounted charge fra flere retninger, men bliver et tættere og mere sårbart mål for artilleri og koncentreret infantry fire. Square må ikke give 360° full-strength volley; fire eligibility fordeles på relevante sider. Cavalry der rammer mens square stadig dannes kan skabe kraftig disorder og høj break/rout risk.

Cavalry kan også skabe combined-arms pressure uden at charge: en troværdig mounted threat kan tvinge infantry i square og dermed gøre det mere sårbart for artilleri eller infantry manoeuvre.

Efter stabil F29E defense/objective/attack/navigation regression er næste systemrækkefølge fortsat: **Gardehusar/Dragoon prototype → mounted/dismounted AI → Kanonbatteri/Artilleri → Combined Arms QA → senere højere Brigade/Division HQ.** F30 forbliver reserveret til den første rigtige mounted cavalry prototype.

## Fog of war, scouts og command effectiveness

Fog of war er en knowledge-state model. En fjendtlig formation kan være `Unknown`, `Suspected`, `Contact`, `Identified`, `Fresh observation` eller `Stale`. Når kontakt mistes, bevares last known position med faldende confidence i stedet for perfekt live-tracking.

Reconnaissance kommer fra faktiske kilder som cavalry patrols, dragoner, skirmishers/scout detachments, line units, HQ og observation points. Information rapporteres gennem command-nettet, så lokal Officer AI kan reagere på frisk information før overordnet HQ har modtaget rapporten.

HQ får et visuelt command effectiveness envelope, men ikke en hård magisk radius. Dårlig connectivity påvirker order delay, acknowledgement, reporting, coordination, reserve/support reaction og afhængighed af lokal Initiative/Tactical Skill/Composure. Den giver ikke en vilkårlig direkte accuracy- eller damage-penalty.

## Versionshistorik

- **v00.02.13 / P0A v00.00.09f29e REGIMENTAL DEFENSE + OBJECTIVE VISUALS + COORDINATED ATTACK TEST** — Unified Oberstløjtnant HUD; persistent regimental objective circle/cross; full Oberstløjtnant→Major→Company hierarchy and company destination visibility; precise final-slot arrival; continuous defensive frontage through ordered centre; nearest-battalion FRONT role enforcement; coordinated explicit AttackTarget deployment; enlarged-map navigation guard replacing obsolete ±176/±116 navigation bounds while retaining bridge-only river authority.
- **v00.02.12 / P0A v00.00.09f29d SEMANTIC ZOOM + NATO TACTICAL OVERLAY TEST** — Fire semantic zoom levels; HQ-prioriterede screen-space counters/beacons; NATO-echelon I/II/III; operational company overlays med strength/morale/ammo/facing; strategic view skjuler company/HQ meshes men bevarer simulation, colliders og LineRenderers; designet udvideligt til Brigade X, Division XX, screen-footprint thresholds, clickable counters og fog-of-war symbol states.
- **v00.02.11 / P0A v00.00.09f29c UNIFIED COMMAND HUD TEST** — Fælles kompakt Major/Company bottom HUD; rød/grøn state coding; Major subordinate overview med mænd/tab/morale/ammo; grouped company shooting, withdrawal, forced march, charge, STOP og LINJE/KOLONNE/SQUARE; F29 SQUARE gjort synlig i company HUD; danske company display names normaliseret til 1.-8. KOMPAGNI mens gamle Regiment strings bevares som interne compatibility-id'er.
- **v00.02.10 / P0A v00.00.09f29b BATTLEFIELD + ATTACK AI TEST** — Tactical battlefield 5760 × 3840 m; to Prussian company-scale QA-enheder; single physical movement owner-regel; explicit `AttackTarget` sticky ved contact; frontage-planner gjort movement-write-free; approach formation respekterer Major/under-fire authority; F29a command-chain visuals og river footprint safety retained.
- **v00.02.09 / P0A v00.00.09f28 REGIMENT CONTROL TEST** — Dansk command chain fastlagt som Kaptajn → Major → Oberstløjtnant → Oberst → Generalmajor; to bataljoner / to Majorer / otte kompagnier under fysisk Oberstløjtnant-HQ; hierarchical authority/reclaim og dynamic HQ command zones; Gardehusarer og dragoner separeret som cavalry-typer på shared core; sabel/karabin/pistol som data-driven cavalry weapon model; FRONT/FLANK/REAR charge shock; close-range volley kan FALTER/ABORT cavalry charge; infantry `SQUARE/KARRÉ` besluttet med reel formation time, multi-side defense og artilleri-trade-off.
- **v00.02.08 / P0A v00.00.09 TACTICAL COMMAND TEST work branch** — Shared `OfficerAIController`/`OfficerProfile`; delegation, doctrine, commander aggression intent, directional infantry fire, HOLD/CLOSE/MEDIUM/LONG, time control, battle supply/night/cavalry/fog-of-war retning.
- **v00.02.08 / P0A v00.00.08** — Våbenprofil/reload, experience, volley feedback, ammunition/casualty model og første expanded systems baseline.
- **v00.02.07** — P0A v00.00.07: statisk Unity 6.6 QA-hardening før runtime-validering.
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
