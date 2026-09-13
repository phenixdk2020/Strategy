# Del 13: 48 — National AI: økonomi, byggeri, hær, udstyr og mobilisering

**Designstatus:** BESLUTTET / IKKE IMPLEMENTERET ENDNU  
**Designbaseline:** v00.02.14  
**Scope:** Grand Campaign / alle ikke-spillerstyrede nationer

## 48.1 Formål

Alle lande skal kunne fungere som selvstændige strategiske aktører. National AI må ikke kun flytte eksisterende hære; den skal drive samme statslige og militære kredsløb som spilleren:

**befolkning → økonomi → budget → byggeri/industri → våben og materiel → rekruttering → træning → mobilisering → operationer → tab → replacements/genopbygning.**

AI-landene skal bruge de samme authoritative systemer, ressourcer, byggetider, lagre, manpower-pools, transportbegrænsninger og militære institutioner som spilleren. De må ikke få gratis regimenter, gratis våben, skjulte produktionsbonusser eller teleporteret mobilisering for at kompensere for svag planlægning.

## 48.2 National AI Director

Hvert AI-styret land får en `NationalAIDirector`, som arbejder over en længere strategisk tidshorisont og fordeler mål og budgetter til underliggende systemer. Directoren skal mindst koordinere:

- udenrigs- og sikkerhedsmål
- threat assessment og sandsynlige krigsskuepladser
- statsbudget og gældstolerance
- civil kontra militær investering
- militært byggeri
- industriel kapacitet og våbenproduktion
- import og udenlandske indkøb
- forskning, doktrin og adoption
- rekruttering og manpower-reserve
- fredstidshær, mobiliseringshær og replacements
- træningsniveau og readiness
- depoter, ammunition, heste og transportkapacitet
- fortifikationer, jernbane, havne og andre strategiske anlæg
- theatre priorities og overførsel af styrker mellem fronter

National AI udsteder mål til Theatre/Army AI; den skal normalt ikke micromanage regimenternes taktiske bevægelser.

## 48.3 Nationale profiler — samme regler, forskellig adfærd

Landene skal ikke være kopier af hinanden. Hver nation får en datadrevet `NationAIProfile`, som beskriver historiske institutioner og strategiske præferencer uden at låse landet til et fast manuskript.

Profilen kan blandt andet indeholde:

| Profilområde | Eksempler på data |
| --- | --- |
| RecruitmentSystem | stående hær, værnepligt, reserve, Landwehr/tilsvarende systemer, frivillige |
| ForceStructureBias | infanteri, kavaleri, artilleri, ingeniører, garnisoner, flåde |
| DoctrineBias | offensiv/forsigtig, concentration, skirmish, artillery emphasis, reservebrug |
| TrainingPriority | drill, marksmanship, fieldcraft, staff work, live-fire, manøvrer |
| EquipmentPolicy | moderniseringstempo, standardisering, accept af ældre materiel, importvillighed |
| IndustrialPolicy | våbenproduktion, arsenal, jern/kul, maskinværktøj, ammunition, skibsbygning |
| InfrastructurePolicy | jernbane, veje, havne, depoter, telegraf |
| FortificationPolicy | permanente fæstninger, kystforsvar, frontier defence |
| FinancialPolicy | fredstidsbesparelse, gældstolerance, krigsbudget, importbudget |
| RiskTolerance | hvor tidligt landet mobiliserer og hvor aggressivt det accepterer tab/økonomisk pres |

Historiske profiler fungerer som **priors**, ikke som en tvungen build order. Hvis campaignen udvikler sig anderledes end historien, skal AI kunne ændre prioriteringer.

## 48.4 Hvad bygger AI-landene?

National AI skal evaluere konkrete kapacitetsbehov frem for at følge en universel bygge-liste. Hvert muligt projekt får en utility-score ud fra strategisk behov, pris, byggetid, arbejdskraft/materialer, geografi og forventet nytte.

AI kan blandt andet beslutte at bygge eller udvide:

- kaserner og træningsanlæg
- artilleri-/specialistfaciliteter
- stalde og remount-depoter
- arsenaler og våbenværksteder
- ammunitionsproduktion og krudtlagre
- større militære depoter
- hospitaler og sanitetskapacitet
- permanente fortifikationer og kystforsvar
- havne og militær havnekapacitet
- veje, broer og jernbaner
- telegraf og kommunikationsinfrastruktur
- våben-, metal-, tekstil- og anden relevant industri
- lager- og transportkapacitet

Byggevalget skal reagere på konkrete mangler. Eksempler:

- for få rifler → prioriter arsenal/våbenproduktion eller import
- utilstrækkelig ammunition → øg ammunition/krudt-kapacitet
- mange rekrutter men for lidt træningskapacitet → udvid kaserner/træningspladser
- mangel på heste → remount/stutteri/importprioritet
- langsom mobilisering → depoter, jernbane og mødepladser
- truet frontier/by → fortifikation, depot og garnison
- forsyningsflaskehals → vej/jernbane/havn/lager frem for endnu et regiment

AI skal respektere CITY-REG-01-reglen om, at almindelig tung militær nybygning normalt kræver en kvalificeret A-by; historiske fixed/special buildings kan fortsat eksistere uden denne regel.

## 48.5 Hvilken hær træner AI-landet?

AI skal have et `ForcePlan` med ønsket fredstidsstyrke, mobiliseringsstyrke og force mix. Planen beregnes ud fra:

- befolkning og tilgængelig manpower
- historisk rekrutteringssystem
- statsbudget
- eksisterende OOB og cadre
- tilgængelige officerer/NCO'er
- træningskapacitet
- våben- og uniformslagre
- heste og transport
- artilleri og ammunition
- forventede fjender og terræn
- aktuelle war goals/theatre priorities
- losses og behov for replacements

AI må ikke oprette en formation blot fordi den ønsker den. En ny eller mobiliseret formation skal gennem samme pipeline som spilleren:

`rekruttering/indkaldelse → mødeplads → udrustning → officers/NCO-tildeling → organisatorisk indplacering → træning/sammenøvelse → readiness → deployment`.

ForcePlan skal kunne indeholde både historiske formation templates og situationstilpassede mål. En nation kan derfor ønske flere infanteribataljoner, artilleribatterier, kavaleri, ingeniører eller garnisoner, men faktisk opbygning begrænses af ressourcer og tid.

## 48.6 Træning og readiness

National AI styrer ikke kun antal soldater, men også hvor meget staten investerer i deres kvalitet.

AI fordeler træningsbudget og tid mellem blandt andet:

- Basic Training
- Drill
- Marksmanship
- Fire Discipline
- Fieldcraft
- Cohesion Training
- Endurance
- Assault
- Entrenchment
- officers-/staff training
- brigade/divisionsmanøvrer

Fredstidsbesparelser kan skabe en stor men dårligt øvet mobiliseringshær. Omvendt kan et mindre land vælge højere træningskvalitet og readiness på færre enheder. Replacements skal integreres og kan fortynde erfaring, NCO-kvalitet og cohesion præcis som hos spilleren.

## 48.7 Hvilket udstyr får hæren?

Udstyr skal være inventory-baseret, ikke en abstrakt AI-bonus. National AI vælger mellem indenlandsk produktion, eksisterende lagre, modernisering og import.

Ved anskaffelse af våben/materiel vurderes mindst:

- performance og militær nytte
- pris
- indenlandsk produktionskapacitet
- leveringstid
- kompatibel ammunition og logistisk standardisering
- eksisterende lagerbeholdning
- træningsbehov ved nyt system
- vedligeholdelseskapacitet
- udenrigspolitisk adgang/import-risiko
- om modernisering af hele hæren er realistisk eller kun udvalgte formationer

Derfor kan ældre og nyere våbentyper eksistere samtidigt. Elite/aktive enheder kan prioriteres først, mens reserve- og garnisonsenheder beholder ældre materiel. Det skal være en faktisk national policy, ikke en skjult combat modifier.

Udstyrsfordeling skal omfatte mere end rifler:

- infanterivåben og ammunition
- artilleripjecer, ammunition og caissons
- kavaleri-/dragoonudstyr
- heste
- vogne og forsyningsmateriel
- ingeniørmateriel
- uniformer, lædervarer og telte
- medicinsk/sanitetsmateriel
- kommunikations-/stabsudstyr hvor relevant

## 48.8 Mobilisering og krigsberedskab

National AI skal løbende vurdere mobiliseringsberedskab. Mobilisering må være gradueret frem for kun OFF/ON.

Eksempel på states:

`PEACETIME → HEIGHTENED_READINESS → PARTIAL_MOBILISATION → GENERAL_MOBILISATION → WAR_REPLACEMENT_MODE → DEMOBILISATION`.

Beslutningen påvirkes af:

- kendte trusler og diplomatiske kriser
- intelligence/fog of war
- allianceforpligtelser
- readiness og lagre
- mobiliseringstid
- økonomisk skade ved at trække arbejdskraft ud
- høst/sæson og transportforhold
- risikoen ved at mobilisere for tidligt kontra for sent

AI må ikke kende spillerens skjulte mobiliseringsordre med det samme; den reagerer på tilgængelig intelligence og observerbare tegn.

## 48.9 Produktion, lager og erstatningstab

National AI skal planlægge fremad og ikke kun reagere, når lageret er tomt. Den vedligeholder minimumsmål for eksempelvis:

- small-arms reserve
- ammunition pr. aktiv/mobiliserbar formation
- reserveartilleri
- heste/remounts
- vogne
- uniformer
- medicinske forsyninger
- strategisk fødevare-/kornreserve

Ved krig sammenholdes faktisk forbrug og losses med produktionsrate. Hvis attrition overstiger replacement capacity, skal AI vælge mellem fx:

- øget produktion
- import
- reduceret operationsintensitet
- prioritering af bestemte hære
- brug af ældre lagre
- udsættelse af nye formationer
- kannibalisering/opløsning af svage formationer til replacements

## 48.10 Strategisk feedback-loop

National AI skal periodisk revidere sin plan gennem et eksplicit feedback-loop:

1. **Observe:** økonomi, lagre, manpower, diplomacy, intelligence, fronter og losses.
2. **Assess:** threats, shortages, readiness, opportunities og constraints.
3. **Plan:** budget, build queue, force plan, equipment policy, mobilisation og theatre priorities.
4. **Execute:** send ordrer til økonomi-, construction-, recruitment-, production- og Theatre AI-systemer.
5. **Review:** mål faktisk effekt mod planen og juster.

Store nationale beslutninger evalueres med længere interval end taktiske beslutninger. AI skal ikke omprioritere en flerårig jernbane eller våbenfabrik hvert sekund på grund af små frontændringer.

## 48.11 Historisk plausibilitet uden historisk tvang

Målet er et troværdigt 1851–1860'er-forløb, ikke et script der reproducerer 1864 uanset spillerens handlinger.

- historiske OOB'er, institutioner, teknologi og økonomi er startbetingelser
- historiske doktriner og politiske mål er priors
- faktiske campaign-hændelser kan ændre prioriteringer
- AI skal kunne lære, modernisere eller fejlvurdere inden for realistiske institutionelle begrænsninger
- et lille land må ikke pludselig opbygge en stormagtsindustri uden tid, kapital, arbejdskraft, råvarer og teknologi
- et rigt land må stadig kunne have organisatoriske, doktrinære eller politiske begrænsninger

## 48.12 AI difficulty og fairness

Strategisk difficulty skal primært ændre kvaliteten af beslutninger — ikke verdensreglerne.

Den kan påvirke:

- planning horizon
- hvor ofte planer revideres
- graden af decision noise
- hvor godt AI vægter supply og reservebehov
- koordinering mellem theatres
- hvor effektivt den undgår åbenlyse build-/production-flaskehalse

Den må som udgangspunkt **ikke** give AI:

- gratis manpower
- gratis våben/ammunition
- kortere byggetider
- skjult ekstra indkomst
- perfekt intelligence
- teleportation
- kunstige combat-stat bonuses

Alle sådanne afvigelser skal, hvis de nogensinde anvendes som særskilt accessibility/arcade-option, være eksplicitte og ikke del af normal historisk difficulty.

## 48.13 Debug og forklarbarhed

National AI skal kunne inspiceres. Debug/telemetry bør vise fx:

- aktuelle nationale mål
- top threats
- budgetfordeling
- aktive construction priorities
- force-plan target versus actual
- vigtigste equipment shortages
- mobilisation state
- reserve/manpower-status
- production bottlenecks
- importbehov
- theatre priorities
- årsagen til større beslutninger

Eksempel:

`PRUSSIA|Decision=ExpandRifleProduction|Reason=MobilisationRequirement|Required=82000|Stock=51000|MonthlyOutput=4200|ImportRisk=Low`

Det er vigtigt for balance, fejlsøgning og for at kunne forklare, hvorfor et AI-land handler som det gør.

## 48.14 Første implementeringsslice

Første runtime-version behøver ikke simulere alle stormagter fuldt ud. En kontrolleret prototype bør starte med 2–3 lande og følgende minimum:

1. `NationAIProfile`
2. `NationalAIDirector`
3. budgetprioriteter
4. simple construction priorities
5. `ForcePlan`
6. rekrutterings-/mobiliseringsmål
7. equipment shortage detection
8. production/import priority
9. theatre force allocation
10. debug panel/log

Danmark og Preussen er naturlige første modparter, fordi de allerede er centrale for projektets 1864-fokus. Et tredje land kan derefter bruges til at sikre, at arkitekturen ikke bliver specialkodet til kun to nationer.

Den endelige model skal kunne anvendes på alle campaignens nationer gennem data-profiler frem for særskilt hardcoded AI pr. land.
