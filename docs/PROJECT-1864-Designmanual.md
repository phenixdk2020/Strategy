# PROJECT 1864 — Designmanual

**Aktuel designbaseline: v00.02.09**  
**Aktuel tactical status: v00.00.09l5 FROZEN QA — CLEAN COMPANY REBUILD REQUIRED**

Grand Strategy i realtid + taktiske 3D-slag. Denne GitHub-udgave er opdelt i dele for overskuelig versionsstyring. GitHub-Markdown er den løbende designmæssige source of truth.

## Current consolidated reference

Efter 09l5-company-QA er den aktuelle tværgående arkitektur konsolideret i Strategy Tools:

- [Current consolidated design manual](../tools/strategy-tools/PROJECT-1864-CURRENT-DESIGN-MANUAL.md)
- [1864 Army / OOB working reference](../tools/strategy-tools/ARMY-1864-OOB-REFERENCE.md)
- [09l5 Company architecture review and recovery](parts/part-09l5-company-rebuild-recovery.md)

**Vigtig designbeslutning:** Company forbliver den mindste normale uafhængigt kontrollerbare infanteriformation. Det er 09l2–09l5 runtime-layeringen oven på en Regiment-centreret arkitektur, der er forkastet. Der må ikke lægges flere compatibility-/authority-lag oven på 09l5; næste tactical company implementation skal bygges rent med stable Unit IDs, uafhængige company world entities, én steering owner og company-owned combat.

Projektets centrale intake-log for besluttede men endnu ikke implementerede funktioner, planlagte opgaver, research-emner og løse idéer ligger i [PROJECT-BACKLOG.md](PROJECT-BACKLOG.md). Større emner kan have detaljerede backlog-supplementer, som senere konsolideres ind i hovedbackloggen.

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
- [09l5 — Company architecture review and recovery](parts/part-09l5-company-rebuild-recovery.md)
- [09m — OOB Designer and campaign integration](parts/part-09m-oob-designer-campaign.md)
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

## Tactical command baseline retained from v00.00.09

Den tidlige v00.00.09-serie etablerede en brugbar tactical-command slice. Danske testregimenter var forsvarere, mens de preussiske testregimenter var angribere. Battlefield blev udvidet, kameraets bounds/zoom blev øget, og spilleren fik reel tid og plads til at pause, inspicere, udstede ordrer og manøvrere før kontakt.

Officerprofilen består af Leadership, Inspiration, Tactical Skill, Initiative, Staff/Command Skill, Discipline/Obedience, Aggressiveness/Caution og Composure/Nerve samt separat Officer Experience. Composure påvirker stress-relateret reaction delay og decision noise; QA-profiler er ikke historiske ratings.

En contextual command-model bruger `DEFENSIVE / BALANCED / OFFENSIVE` doctrine, commander aggression intent og fire policy `HOLD / CLOSE / MEDIUM / LONG`. Commander intent biaser execution, mens officerens egne stats fortsat er afgørende.

Difficulty må ikke ændre weapon accuracy, reload, range, movement, morale, cohesion, casualties, officer stats eller skjult viden. Difficulty må primært påvirke computer-AI reaction/noise/decision quality.

Simulation time-control retningen omfatter **PAUSE / x0,5 / x1 / x2 / x5 / x20** med synligt dato/klokkeslæt. Pause stopper AI, movement, reload/fire progression og battle clock samlet, mens kamera/UI fortsat er brugbart.

## Directional fire, fire discipline og accuracy

Infantry fire er fremadrettet og skal følge formationens facing frem for at være en 360° ring. Working baseline bruger en 120° fire fan (±60°).

Fire policy:

- **HOLD**
- **CLOSE**
- **MEDIUM**
- **LONG**

Close/Medium/Long er ordre-/UI-grænser; underliggende accuracy skal være kontinuerlig med afstand frem for kunstige spring ved band-grænserne.

I den endelige Company-first arkitektur flyttes fire authority fra Regiment til Company. Hvert company skal eje ammo, target, fire eligibility, reload og casualties. Regimental HQ må ikke være den fysiske affyringskilde for alle underenheder.

Næste combat-fase beregner `eligibleFiringFraction` ud fra fire arc, LOS, range, friendly obstruction, terrain/smoke og target exposure.

## Command-visualisation

Command-UI-retningen omfatter fysiske HQ-entities, command-links, courier/order lifecycle, fog-of-war reports og semantic zoom. Valg af et HQ kan vise relationer til direkte underenheder.

Ved udzoomning skifter enheder via semantic zoom fra 3D-formationer til forenklet formation display og senere taktiske symboler uden at ændre simulation state.

Ordrer transporteres senere gennem courier/order lifecycle. Command relationship lines og konkrete order routes er separate overlays og vises primært ved valgt HQ/enhed eller aktiv Command Overlay for at undgå visuelt rod.

Courier-interception håndteres primært som område-/risikomodel. Enemy presence, cavalry/scouts, screening, roads, terrain, mørke og command quality kan føre til reroute, delay, searching eller lost/intercepted. Fjendens couriers er underlagt fog of war.

## Higher formation templates and reserves

Højere HQ'er opstiller uafhængige underenheder efter data-drevne templates, fx:

- 4 abreast
- 3 + 1 reserve
- 2 + 2
- echelon
- march column
- kombinationer med artilleri og cavalry attachments

Reserve er en faktisk rolle/state. Officer AI vælger og tilpasser template ud fra mission, terræn, frontage, artilleri, flanker, reservebehov og officerstats.

**Permanent regel efter 09l5:** templates giver target centres til uafhængige company entities; templates må ikke implementeres ved at transform-parente alle companies under et regiment og flytte dem som ét rigid body.

## Battle supply, nat og flerdagsslag

Forsyning under taktisk slag er fysisk og begrænset. Enheder forbruger konkret ammunition og kan kun genforsynes fra kompatible wagons/caissons/field trains/depots med reel beholdning, transportkapacitet og brugbar rute.

Battle state kan fortsætte `DAYLIGHT -> DUSK -> NIGHT -> DAWN`. Normal formation combat reduceres kraftigt i mørke, mens hold, withdrawal, reorganisation, casualty collection, fieldworks, patrols, courier traffic og resupply kan fortsætte.

Afskårne enheder får ingen automatisk overnight ammunition. Flerdagsslag fortsætter med persistent casualties, ammo, fatigue, positions, fieldworks, officer/equipment state og supply connectivity.

## Kavaleri og dragoner

Kavaleri har roller inden for reconnaissance, screening/counter-recon, flank security, courier support/escort, pursuit, raids mod supply/courier routes og exploitation.

Dragoner kan bevæge sig mounted og sidde af til sustained combat. Dismount efterlader horses og horse holders som tactical state; remount tager tid og påvirkes af tabte/spredte heste.

Cavalry manpower og horse availability er separate værdier. Charge-resultater afhænger af target formation/state, facing, terrain, surprise, cohesion/fatigue/momentum og officer quality.

## Artillery

Dansk working 1864 field-battery baseline:

- **8 guns**
- **4 gun divisions/sections × 2 guns**
- approximately **190 personnel**

Artillery state skal skelne mellem guns, crews, ammunition, horses, drivers, caissons/wagons og limbered/unlimbered state. Preussisk battery structure forbliver data-driven/working indtil date-specific source lock.

## Fog of war, scouts og command effectiveness

Fog of war er en knowledge-state model, ikke blot skjult grafik. En fjendtlig formation kan være `Unknown`, `Suspected`, `Contact`, `Identified`, `Fresh` eller `Stale`.

Recon kommer fra cavalry patrols, dragoner, skirmishers/scouts, line units, HQ, observation points og senere øvrige rapportkilder. Spotting påvirkes af afstand, terrain, vegetation, elevation, daylight/night, weather, smoke, target size/movement og enemy screening.

Information rapporteres gennem command-nettet. Lokal Officer AI kan reagere på frisk lokal information, mens højere HQ stadig arbejder med ældre knowledge state.

Command effectiveness skal være gradvis, ikke en hård magisk radius. Dårlig command connectivity påvirker primært order delay, acknowledgement, reporting, coordination og reserve/support reaction — ikke vilkårlig direkte accuracy/damage.

## Company-first tactical architecture — v00.02.09 decision

Den ønskede tactical hierarchy er:

```text
Brigade HQ
→ Regiment HQ
→ Battalion
→ Company tactical entity
→ 1:1 GPU-instanced soldiers
```

Company er den mindste normale direkte kontrollerbare infanteriformation.

### Stable identity

Hver formation skal have stable `UnitID`. Display name må ikke være technical identity.

### OOB parent vs transform parent

`ParentFormationID` er data. Et company må ikke være afhængig af `Regiment.transform` for world position. Regimental HQ er command-parent, ikke movement-body.

### Movement ownership

Company ejer company movement state. Én tactical entity har én authoritative steering writer ad gangen. AI vælger mission/goal; navigation løser bevægelse; højere HQ må ikke overskrive subordinate transforms direkte.

### Combat ownership

Company ejer company combat state: ammo, target, fire policy, LOS, reload, casualties og local morale/cohesion. Regiment summerer og kommanderer, men er ikke den fysiske salveenhed.

### 1:1 rendering

Ordinary soldiers er renderer instances, ikke heavy GameObjects. 1 real soldier kan vises som 1 visual soldier, men simulationen er echelon-based.

## 09l2–09l5 architecture review

09l2–09l5 demonstrerede ønsket om company-level control, men implementeringen blev ustabil, fordi company-lag blev lagt oven på en Regiment-centreret runtime.

Observerede konflikter:

- companies parentet under `Regiment.transform`
- parent rotation/movement trak companies som én rigid enhed
- group orders kunne blive én gigantisk lang linje
- Regiment forblev combat authority
- mounted HQ kunne fremstå som salveenhed
- selection blev fordelt mellem colliders, GPU instances og screen-space patches
- successive authority scripts maskede ownership-konflikter

### Freeze

`v00.00.09l5` er frosset som failed QA architecture. Den videre tactical implementation må ikke fortsætte med flere compatibility authority scripts oven på samme struktur.

Clean rebuild order:

1. stable Unit IDs + OOB data
2. 1 dansk + 1 preussisk regiment
3. independent Company world entities
4. 1:1 renderer driven by Company state
5. company movement with one steering owner
6. company combat
7. Battalion/Regiment AI
8. Brigade AI + second regiment
9. OOB Designer + campaign handoff

Se [09l5 recovery supplement](parts/part-09l5-company-rebuild-recovery.md) og [consolidated current design manual](../tools/strategy-tools/PROJECT-1864-CURRENT-DESIGN-MANUAL.md).

## OOB Designer

OOB Designer er et core campaign tool.

- drag/drop i OOB tree ændrer organisatorisk parent
- drag/drop på campaign map opretter movement/march order
- organisatorisk reassignment teleporterer ikke enheden
- map movement ændrer ikke automatisk OOB-parent
- stable Unit IDs forbinder campaign og tactical state

OOB hierarchy understøtter Army → Corps → Division → Brigade → Regiment → Battalion → Company samt attached artillery/cavalry/support.

## 1864 historical working baseline

Se [Army/OOB reference](../tools/strategy-tools/ARMY-1864-OOB-REFERENCE.md).

Working baseline:

### Denmark

- company ~180–220
- battalion 4 companies
- regiment 2 battalions / 8 companies
- regiment ~1,500–1,700

### Prussia

- company ~190–210
- battalion 4 companies
- regiment 3 battalions / 12 companies
- campaign regiment ~2,400–2,500

Historisk dansk working brigadepilot: **7. Brigade = 1. + 11. Infanteri-Regiment**.

Historical records support confidence states such as `SOURCE_LOCKED`, `WORKING`, `QA_PLACEHOLDER`, `INFERRED` and `UNKNOWN` so prototype values cannot silently become canon.

## Versionshistorik

- **v00.02.09 / post-09l5 architecture consolidation** — Company-first design fastholdes, men 09l2–09l5 runtime-layering fryses som failed QA architecture. Stable Unit IDs, data-only OOB-parenting, independent Company world entities, one steering owner og company-owned combat gøres til permanente arkitekturregler. Strategy Tools får konsolideret current design manual og 1864 Army/OOB working reference.
- **v00.02.08 / P0A v00.00.09 TACTICAL COMMAND TEST work branch** — Shared Officer AI/profile, doctrine/aggression, directional fire, fire policy, time controls og senere command/courier/fog/supply/cavalry systems.
- **v00.02.08 / P0A v00.00.08** — Weapon-profile reload, Experience reload modifier, volley feedback, casualty visual og udvidet systems baseline.
- **v00.02.07** — P0A v00.00.07 static Unity QA hardening.
- **v00.02.06** — Unity compile-gate cleanup.
- **v00.02.05** — Built-in IMGUI, Particle System, Physics og Audio moduler aktiveret.
- **v00.02.04** — Unity baseline flyttet til 6000.6.0f1.
- **v00.02.03** — Repository-roden fastlåst som Unity project root.
- **v00.02.02** — Unity Editor metadata/version rettet.
- **v00.02.01** — Første konkrete P0A implementation koblet til roadmap.
- **v00.02.00** — Expanded systems baseline: økonomi, udvikling, handel, forskning, rekruttering, træning, sanitet/fanger, regimentshistorik, faner og traits.
- **v00.01.00** — Første samlede designbaseline.

## Projektregel

Designmanualen skal opdateres i GitHub, når designbeslutninger eller implementeringsbaselines ændres. Git-historikken bevarer tidligere udgaver. Nye beslutninger og idéer registreres desuden i backloggen. Hver testbuild skal have tydelig release-dokumentation, der adskiller **implementeret nu** fra **besluttet senere**.