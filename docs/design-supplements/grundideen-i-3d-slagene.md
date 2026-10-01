# Grundidéen i 3D-slagene (beslutninger)

> Tilføjet til designmanualen 1. oktober 2026 fra "Grundideen i 3D slagene.docx". Kun 3D-kampdelen. Unity er facit for testet adfærd, indtil Unreal-funktionen er bygget og testet.


## 1. Grundidéen for 3D-slagene
Vi har besluttet, at slagene ikke bare skal være en Total War-lignende visuel minigame-del. De skal være en egentlig taktisk simulation, hvor kommandostruktur, formation, terræn, våben, moral, LOS, officerer og fysisk placering har betydning.
Den grundlæggende kommandokæde er:
DIVISION
   │
BRIGADE
   │
REGIMENT
   │
 ┌─┴─────────────┐
MAJOR A        MAJOR B
   │               │
Kompagnier      Kompagnier
HQ'erne er fysiske enheder på slagmarken. Division, Brigade, Regiment og Major er altså ikke bare menupunkter.
En ordre fra Divisionen flytter heller ikke direkte hvert enkelt kompagni. Den delegeres ned gennem kommandokæden.
Det er en meget central beslutning:
Spiller
   ↓
Division
   ↓
Brigade
   ↓
Regiment
   ↓
Major
   ↓
Kompagni
Det betyder senere, at vi kan modellere officerers evner, ordreforsinkelse, kurérer, tab af HQ, dårlig kommunikation, lokal initiativ osv. uden at skulle redesigne kampsystemet.


## 2. Én autoritet må styre en bevægelse
Det var faktisk en af de største ting, vi lærte i Unity.
Vi havde flere perioder, hvor forskellige AI- og formationsscripts forsøgte at bestemme destination samtidig. Det gav bl.a.:
kompagnier der gik over en bro og derefter tilbage igen,
HQ'er der flyttede frem og tilbage,
enheder der stoppede før deres destination,
formationer der skiftede Line/Column igen og igen,
en ordreknap der blev ved med at være blå, selv om alle stod stille.
Derfor fastlagde vi princippet:
Der må kun være én fysisk movement-authority ad gangen.
Parent-missionen kan bestemme hvor enheden skal hen. Formation-, bro- og avoidance-systemer må bestemme hvordan den fysisk kommer derhen, men de må ikke omskrive missionens egentlige slutmål.
Det princip er taget direkte med over i Unreal.


## 3. Ordrer og ordrestatus
Vi endte i Unity med et fælles taktisk ordresprog for HQ'er:
ANGRIB HER · FORSVAR HER · RYK FREM · TILBAGETRÆK · SAML · STOP/HOLD
En meget vigtig beslutning var betydningen af farverne.
Blå betyder ikke længere "denne ordre er valgt".
Blå betyder:
Ordren bliver fysisk udført lige nu.
Når sidste relevante enhed er på plads, HQ'et er faldet på plads, eventuelt kavaleri er færdigt med at reformere osv., går knappen tilbage til rød.
Men FORSVAR HER kan stadig være formationens gældende standing intent.
Så vi skelner mellem:
MISSION / INTENT
        og
PHYSICAL EXECUTION
Det løste en del af problemet med ordrer, der så ud til aldrig at blive afsluttet.
Vi strammede endda Unity-testen ned til, at et kompagni skal være inden for cirka 0,50 m af sin endelige slot, før missionen regnes som fysisk afsluttet.


## 4. Selection og klassisk RTS-styring
Vi besluttede tidligt, at slaget skal kunne styres nogenlunde intuitivt som et RTS.
Venstreklik vælger en enhed.
Venstreklik + træk giver box selection.
Shift/Ctrl kan senere bruges til at ændre selection.
Fjendtlige enheder skal ikke vælges sammen med egne.
HQ'er og kavaleri kan også vælges fysisk på slagmarken eller gennem OOB-panelet.
En vigtig regel fra Unity-testene er desuden:
Når man vælger Division, Brigade, Regiment eller Major og giver en ordre, skal selection ikke forsvinde, når man placerer målet.
Det var tidligere en irritation, fordi ordreklikket kunne blive fortolket som et almindeligt klik på terrænet.
Det blev rettet og er nu en designregel.


## 5. Facing er en del af ordren
Vi besluttede, at:
Klik = destination
Klik + træk = destination + facing
Det gælder både angreb, forsvar og efterhånden også kavaleri.
Det betyder eksempelvis, at:
FORSVAR HER
      ↓
   [mål]
      ↘ drag
        facing
ikke bare betyder "gå hen til dette punkt", men også hvordan formationen skal vende, når den kommer frem.
Facing skal fastlægges før company-slots beregnes. Vi testede den modsatte løsning i Unity, hvor facing blev korrigeret bagefter, og det gav formationer, der stod anderledes end den pil, spilleren havde tegnet.


## 6. Infantry: 1:1 soldater
En af de helt store beslutninger er:
Én synlig soldat repræsenterer som udgangspunkt én virkelig mand.
Det startede allerede i Unity.
Et kompagni på 190 mand skal derfor kunne vise cirka 190 soldater.
Det samme gælder kavaleri:
Gardehusar: 120 mand → 120 ryttere
Dragon:     140 mand → 140 ryttere
Vi har haft bugs i Unity, hvor gamle proxy-systemer kun viste f.eks. 12/14 eller 24/28 ryttere. Det blev specifikt rettet.
I Unreal har vi nu lavet samme arkitektur for infanteriet med UStrategyInfantryVisualComponent.
Det kan vise:
1:1
1:2
1:5
1:10
uden at ændre den simulerede formation.
Det er vigtigt: LOD må reducere rendering, men må aldrig ændre den egentlige styrke eller formation.


## 7. Infantryformationer
Vi har fastlagt tre grundformationer:
LINE
COLUMN
SQUARE
Line
Line er kampformation.
I vores nuværende QA-model har et 190-mands kompagni cirka 48 m frontage.
Vi har arbejdet med tre geledder i QA-prototypen, selv om geledantal senere skal kunne være nation-/periode-/doktrinafhængigt.
Column
Column er march- og passageformation.
En af de fejl vi faktisk så i Unity var, at kompagnierne gik for langt frem i Column og først begyndte at reformere, når de allerede var inden for fjendens ild.
Derfor endte vi med reglen:
Infantry begynder Line-reform ved nærmeste fjendes MaximumRange + 35 m.
Med vores nuværende testafstand:
Enemy MaximumRange = 100 m
Safety/reform buffer = 35 m

Deploy omkring 135 m
Så Line skal være dannet før enheden kommer under effektiv fjendtlig ild.
Det er allerede porteret til Unreal.


## 8. Kompagnier må ikke stå oven i hinanden
Det har vi også konkret set under Unity-test.
Et kompagni kunne få en matematisk korrekt slot, men ende fysisk oven i et andet kompagni.
Derfor har vi nu:
190 mand / 3 geledder
≈ 48 m company frontage

Nominal center spacing = 72 m
Minimum reserved spacing = 68 m
Hvis der opstår konflikt, foretrækker formation planner først en lateral forskydning, frem for at stable kompagnier dybt oven i hinanden.
Det samme princip bruges ved friendly-fire lanes.
Hvis et kompagni står direkte foran et andet kompagni og blokerer dets skudfelt, kan den taktisk mindst nyttige formation lave et Side Step.


## 9. Range, cones og LOS
Vi har testet et meget tydeligt QA-system i Unity.
Nuværende testafstande er:
Fire policy
QA-afstand
CLOSE
35 m
MEDIUM
70 m
LONG
100 m
Fire cone
±35°
Den aktive range skal være tydelig.
De øvrige ranges skal stadig være synlige, men svage.
Eksempel:
CLOSE       svag
MEDIUM      KRAFTIG  ← aktiv
LONG        svag
HOLD viser kun de svage reference-ranges.
Vi havde gentagne problemer med, at fjendens cone ikke var synlig fra battle-start. Derfor fastlagde vi, at under TEST skal alle levende preussiske infantry cones være synlige fra starten.
Men det er meget vigtigt:
Den synlige cone er kun QA-grafik.
Den giver ikke AI'en magisk LOS.
Reel skydning kræver stadig:
Target known/contact
+
LOS
+
range
+
fire cone


## 10. Infantry fire og ammunition
Combat er ikke bare en visuel muzzle-flash.
Vi har allerede besluttet og i Unreal implementeret en egentlig combat-core med:
ammunition
reload
range
accuracy
fire cone
LOS
casualties
morale shock
cohesion shock
En salve kan godt give 0 hits. Det er stadig en gyldig salve, som bruger ammunition og kræver reload.
Positive hits reducerer den autoritative CurrentStrength.
Morale og Cohesion er separate værdier.


## 11. Den nye fire-drill progression
Det er vores seneste beslutning.
Vi undersøgte, hvordan det faktisk foregik omkring 1864, men besluttede bevidst at gøre starten lidt simplere for gameplayets skyld.
Progressionen er nu:
Niveau
Drill
0
Front Rank Fire
1
Two-Rank Fire
2
Fire by Rank
3
Controlled Volley
4
Independent Fire
5
Advanced Fire Drill
Det er eksplicit skrevet ind som en bevidst ahistorisk gameplay-abstraktion.
Front Rank Fire
Kun forreste geled skyder.
Two-Rank Fire
Forreste geled kan knæle, mens andet geled skyder over/forbi.
Fire by Rank
Geled 1 FIRE
   ↓
Geled 1 RELOAD

Geled 2 FIRE
   ↓
Geled 2 RELOAD

Geled 3 FIRE
   ↓
tilbage til geled 1
Soldaterne bytter ikke fysisk plads.
Controlled Volley
Relevante geledder afgiver koordineret salve.
Independent Fire
Soldater/sektioner skyder mere individuelt efter deres egen reload-cyklus.
Advanced Fire Drill
Enheden kan vælge passende ildform efter situationen.
En vigtig beslutning er også, at forskning ikke erstatter de gamle modes. En eliteenhed kan stadig vælge Volley, selv om den har Independent Fire.
Og forskning er ikke det samme som dygtighed.
DrillTraining, FireDiscipline, NCO'er, fatigue, morale, våbentype osv. afgør, hvor godt enheden udfører drillen.
Det er nu implementeret i Unreal-source som v00.02.79.


## 12. Standing, Kneeling og Prone
Vi har også besluttet, at infantry ikke kun skal have en abstrakt "formation".
En soldat/enhed kan være:
STANDING
KNEELING
PRONE
Det påvirker bl.a.:
target profile
movement
reload
fire drill
cover
Forladere og bagladere skal ikke nødvendigvis have samme muligheder.
En Dreyse/baglader kan eksempelvis fungere langt bedre liggende end en soldat, der skal håndtere en lang forlader og ladestok.


## 13. Square / karré
Square er ikke bare en damage-bonus.
Det er en fysisk formation mod kavaleri.
Den tager tid at danne.
Den har dårlig mobilitet.
Den er stærk mod cavalry charge, men er et tættere og dermed attraktivt mål for artilleri og infantry.
Vi fandt en konkret Unity-fejl, hvor Square stod som aktiv state, men soldaterne stadig visuelt stod i Line/Column. Det skyldtes, at formationen blev skrevet til en gammel renderer. Det blev rettet mod den aktive 1:1 renderer.
Vi har også besluttet, at Square ikke må skyde 360° med hele kompagniets firepower.
Den er opdelt i fire sider:
       25 %
   ┌─────────┐
25%│         │25%
   └─────────┘
       25 %
Hver side har egen fire sector/reload.
Unity-testen nåede også til retningskorrekt sortkrudtsrøg fra den side, der faktisk skyder.


## 14. Kavaleri
Kavaleri blev meget grundigt udviklet i Unity.
Vi har en shared cavalry core, men Gardehusar og Dragon er forskellige typer.
Den nuværende formation-standard er:
Situation
Formation
Normal Line
4 geledder
Charge
4 geledder
March Column
4 abreast
Bro/defile
2 abreast
Det erstattede en tidligere 2-geleds cavalry-line.
Det er en gameplay-standard for projektet, ikke en påstand om, at alt historisk kavaleri altid stod sådan.


## 15. Kavaleri skal fysisk reformere
Vi besluttede, at formationen ikke må "poppe" mellem:
LINE → COLUMN → 2 abreast → LINE
Rytterne skal fysisk bevæge sig til deres nye slots.
En charge må heller ikke få fuld charge speed, mens formationen stadig reformerer.
Det har vi set fungere i Unity-prototypen.


## 16. Kavaleri og broer
Det her blev justeret flere gange efter vores Unity-tests.
Den endelige regel er:
Kavaleri må ikke gå i 2-abreast bare fordi det har fået en destination på den anden side af floden.
Det rider mod broen i normal formation.
Først cirka:
36 m fra near-bank bridge approach
går det i 2-abreast.
Derefter:
NearBank
   ↓
Bridge
   ↓
FarBank
   ↓
Exit clearance
   ↓
restore previous formation
Hele den lange 1:1 kolonne skal være fri af broen, før normal formation gendannes.
Vi testede netop problemet, hvor kavaleri gik i to geledder alt for tidligt.


## 17. Floder og routing
Åbent vand er hard blocker.
I den nuværende test skal man bruge broen.
Men vi fandt en anden vigtig fejl:
Hvis start og destination ligger på samme bred, men den rette linje mellem dem skærer en bugt i floden, må pathfinding ikke tro, at enheden skal:
hen til bro
→ over bro
→ tilbage igen
Det var netop en af de mærkelige bevægelser, vi observerede.
Derfor kræver bridge-routing nu et reelt bankskifte.
Same-bank movement bruger i stedet bank-follow/detour.


## 18. Cavalry må ikke ride gennem infantry
Det blev også fastlagt efter test.
Fjendtligt infantry fungerer som tactical avoidance-zone for kavaleri.
Autonom cavalry AI arbejder derfor efter:
SCREEN
   ↓
OPPORTUNITY
   ↓
CHARGE
Den skal ikke se en fjende og straks storme direkte igennem ham.
OPPORTUNITY kan eksempelvis opstå, når infantry allerede er bundet i kamp, har dårlig morale/cohesion eller præsenterer flank/rear.


## 19. Charge er retningsbestemt
Vi har besluttet:
FRONT  = mindst fordelagtigt
FLANK  = stærkere
REAR   = stærkest
Et frontalt charge mod steady infantry skal være risikabelt.
På længere sigt skal Charge Confidence / Momentum påvirkes af:
casualties
morale
cohesion
horse hesitation
terrain
defensive fire
formation
En close-range volley skal kunne få et charge til:
FALTER
ABORT
ROUT
Den komplette model er endnu ikke færdig.


## 20. Infantry reagerer på cavalry
Fjendtligt infantry må reagere på synligt kavaleri.
Men vi strammede reglen til:
LOS
+
range
+
cone
før infantry må skyde.
Square-threat kræver også reel LOS.
Det er vigtigt, fordi vores TEST-visible enemy cones ellers kunne have givet AI'en information, den egentlig ikke burde have.


## 21. Kavaleri som støtte til Major A/B
Det er en af vores mere interessante command-beslutninger.
Kavaleri kan organisatorisk høre til højere HQ, men under et angreb kan det midlertidigt tilknyttes:
Major A
eller
Major B
Vi ændrer ikke:
OrganicParent
Vi ændrer midlertidigt:
CurrentCommandParent
Hvis vi har to cavalry units og to Majorer, vælges kombinationen med laveste samlede travel cost, så de ikke unødigt krydser hinanden.
Når angrebet er afsluttet:
CAV → tilbage til tidligere HQ → RESERVE
Ved FORSVAR HER placeres cavalry i stedet som reserve/flankesikring cirka:
150 m bag støttet bataljon
45 m lateralt
De værdier er QA-tal og kan tunes.


## 22. Dragoner
Dragoner er ikke bare cavalry med en anden uniform.
De kan:
mounted
↓
SID AF
↓
dismounted combat
↓
STIG OP
↓
mounted
Ved afsidning har vi i Unity testet cirka:
75 % combat group
25 % horse holders
Horse holders og heste bliver ved dismount-positionen.
Combat group går frem og danner en to-geleddet skydelinje.
Hvis spilleren trykker STIG OP, mens combat group er væk fra hestene, betyder det:
return-to-horses
→ samling
→ remount
ikke magisk teleportering af hestene.
Afsiddede dragoner har også:
HOLD
CLOSE
MED
LONG
og faktisk carbine fire/reload/ammunition i Unity-prototypen.


## 23. HQ'er skal følge slaget
Vi havde problemet med, at Division og andre HQ'er stod alt for langt bagved.
Derfor skal HQ følge formationen inden for passende rear-depth.
Men:
HQ-follow er command housekeeping og må ikke overtage selve combat missionen.
Vi har QA command-zones omkring HQ'er.
De nuværende Unity-testværdier var omtrent:
Major       320 / 450 m
Regiment    800 / 1100 m
Brigade    1350 / 1850 m
Division   2100 / 2850 m
De er primært visual/QA-bands endnu.
Senere skal command effectiveness påvirke ordreforsinkelse, rapporter, coordination osv. — ikke give en magisk damage-penalty uden for en cirkel.


## 24. Officer AI
Vi har fastlagt, at Officer AI kan slås:
ON / OFF
på command levels.
Men AI ON betyder ikke "find selv en fjende og angrib".
Det betyder, at officeren er klar til at udføre/delegere en mission inden for sin authority.
Direkte player orders har højere prioritet.
Det var nødvendigt, fordi vi i Unity oplevede AI'en overskrive manuelle ordrer.
Vi har også doctrine-retninger som:
DEF
BAL
OFF
og officerprofiler skal senere påvirke initiative, tactical skill, composure osv.


## 25. ANGRIB HER er ikke "jagt nærmeste fjende"
Det var endnu en konkret fejl, vi fandt.
Et kompagni kunne nå sit attack-slot og derefter få lokal AI til straks at vælge nærmeste fjende og løbe videre.
Det er ikke længere meningen.
ANGRIB HER på company-niveau er en finite positioning mission.
Når kompagniet når sin tildelte position:
ankom
→ behold facing
→ bekæmp mål gennem normal fire policy
Det skal ikke automatisk starte en ny chase.


## 26. Terræn og dækning
Unity havde allerede terrain/pathfinding/crop concealment.
I Unreal er vi gået længere og lavet et fælles authoritative tactical-terrain system.
Det kan arbejde med:
Hill
Ridge
Depression
Slope
Crest
Reverse slope
Dead ground
Elevation advantage
LOS og artillery skal bruge samme terrænsandhed.
En bakke må altså ikke eksistere for artilleriets LOS, men ignoreres af infantry LOS.
Elevation kan forbedre observation, men kan aldrig ophæve fysisk LOS.


## 27. Sortkrudtsrøg
Røg skal ikke kun være Niagara-grafik.
Unreal-systemet har allerede simulationstilstand for black-powder smoke med density/lifetime.
Røg skal kunne påvirke:
LOS
accuracy
I Unity havde vi allerede directional smoke fra den formation/side, der faktisk skød.


## 28. Skirmishers og specialister
Vi besluttede, at vi ikke skal lave hundredvis af permanente unit types.
Et normalt infantry company skal kunne detachere specialister efter behov.
Det gælder bl.a.:
skirmishers
marksmen
engineers/working parties
andre specialistgrupper
Detached men skal trækkes fra parent unit.
De må altså ikke både eksistere i specialistgruppen og samtidig tælle som kampklare mænd i kompagniet.
Når de returnerer, kræver reform tid/cohesion.


## 29. Forsvarsstillinger
Vi besluttede også, at infantry skal kunne etablere egentlige defensive positions.
Unreal har nu core for defensive positions med:
orientation
capacity
condition
owner
occupier
breach
capture
Det skal senere bruges til:
hasty cover
fieldworks
trenches
breastworks
redoubts/skanser
gun emplacements
Infantry skal kunne:
stå
knæle
ligge
bag relevant dækning.
Og artilleri skal kunne placeres bag eller i en forsvarsstilling.


## 30. Angreb på skanser
Vi har besluttet, at befæstninger ikke bare skal være en høj armor-værdi.
Der skal være assault equipment og fysisk breach/capture.
Det inkluderer bl.a. idéen om:
stiger
ingeniører
arbejdshold
breaches
når vi kommer til egentlige skanser.
Unreal har allerede fundamentet for fortification assault equipment og working parties, men final animation/grafik mangler.


## 31. Artilleri
Artilleri er nu langt mere udviklet i Unreal-koden end det nåede at blive i Unity.
Et batteri er en rigtig tactical unit med:
guns
crew
drivers
horses
ammunition
condition
States:
LIMBERED
   ↓
DEPLOYING
   ↓
DEPLOYED
   ↓
LIMBERING
plus kort manhandling.
Artilleriet kan ikke bare skyde, mens det transporteres.
Det skal have:
operational guns
crew
ammo
LOS
range
traverse
deployment
før det kan skyde.


## 32. Artilleriammunition
Vi har foreløbig:
Ammunition
Rolle
Round Shot
fladere bane, mulighed for ricochet
Shell
højere bane/impact
Shrapnel
air/burst-lignende virkning
Canister
kort range, bred pellet-cone
De endelige historiske værdier er ikke låst endnu.
Vi har bevidst adskilt gameplay-arkitekturen fra senere historisk ballistik-tuning.


## 33. Artilleri kan beskadiges og erobres
Hits mod et batteri kan ramme:
crew
horses
guns
En kanon kan være:
operational
disabled
destroyed
Disabled guns kan repareres.
Destroyed guns må ikke "genoplives".
Et batteri kan blive:
ABANDONED
CAPTURED
og captured guns kan senere potentielt genbruges, hvis man har kvalificeret crew, kompatibel ammunition og tid.


## 34. Morterer
Vi besluttede for nylig, at morterer skal være en egen artillery capability, ikke bare:
CannonDamage × 1.3 + HighArc = true.
De skal kunne angribe:
reverse slope
dead ground
trenches
redoubts
targets behind breastworks
uden almindelig direct-fire LOS.
Tunge morterer har states:
TRANSPORT
→ EMPLACING
→ DEPLOYED
→ PACKING
→ TRANSPORT
og skal være langsommere og mere logistiktunge end feltkanoner.


## 35. Fysisk ammunition og forsyning
Vi har også bevæget os væk fra magisk ammunition.
Unreal har en fysisk SupplyWagonUnit.
Den har bl.a.:
small-arms ammunition
artillery ammunition
drivers
horses
wagon condition
cargo
En enhed kan kun resupply, hvis:
wagon er tæt nok
+
rigtig ammunition findes
+
enhederne er i passende state
Transfer stopper ved movement eller under fire.
Captured ammunition skal heller ikke automatisk passe til alle våben.


## 36. Artilleriprojektiler og kamera
Unreal har allerede et presentation-system oven på den autoritative impact calculation.
Det betyder:
Grafikken må aldrig beregne et andet hit end simulationen.
Simulationen bestemmer impact.
Derefter visualiseres projektilet hen til samme impact point.
Vi har også lavet:
P = follow latest projectile
F9 = trajectory QA
Kameraet kan følge projektilet og returnere til den præcise tidligere battle-view position.
Canister er ikke egnet til projectile-follow.


## 37. Fog of War og rekognoscering
Vi har besluttet retningen, men det er ikke færdigt endnu.
Enemy knowledge skal senere være noget i stil med:
Unknown
Suspected
Contact
Identified
Fresh observation
Stale
Hvis LOS mistes, skal fjenden ikke fortsat være perfekt live-opdateret.
Man beholder en last-known position med faldende confidence.
Det hænger direkte sammen med cavalry.


## 38. SPEJD HER
Vi har allerede designet en fremtidig cavalry-order:
SPEJD HER
Den skal ikke være en attack-order.
Kavaleri skal ride til et område, observere og ved kontakt:
RECON
→ CONTACT
→ SCREEN
→ REPORT
og holde en sikker afstand i stedet for automatisk at charge.
Observationen skal senere bevæge sig:
CAV
↓
CurrentCommandParent
↓
Regiment/Brigade
↓
Division
med informations-/ordreforsinkelse.
Vi har bevidst ikke aktiveret den som rigtig ordre endnu, fordi rigtig FOG/LOS først skal være autoritativ.


## 39. OOB, HUD og semantic zoom
Unity-testen lærte os også en masse om UI.
Vi har ét samlet OOB-træ:
XX Division
└─ X Brigade
   └─ III Regiment
      ├─ II Major A
      │  └─ I Companies
      ├─ II Major B
      │  └─ I Companies
      └─ ATTACHED/SUPPORT
         ├─ Cavalry
         └─ senere artillery
Single-click vælger.
Double-click fokuserer kameraet.
OOB skal have scrollbar.
AI skal konsekvent vises som:
ON / OFF
ikke nogle steder AI, andre steder ON.
Vi har semantic zoom:
Close
Medium
Operational
Strategic
Very Far
Ved stor afstand erstattes de detaljerede 1:1 modeller af NATO-/formationssymboler uden at stoppe simulationen.


## 40. Animationer og vores rigtige Livgarde-model
Nu er vi begyndt at gå fra QA-geometri til rigtige soldater i Unreal.
Vi har:
DK_Livgarden_1864_Apose_textured_skeleton.fbx
Rifle_1_textured.glb
Rifle_Bayonet_1_textured.glb
65 infantry animation FBX'er
Vi har kontrolleret filerne statisk og fundet de samme 41 centrale Mixamo motion bones i soldaten og animationerne.
Det er et meget godt tegn, men den endelige skeleton-kompatibilitet skal stadig verificeres gennem Unreal-importen.
Animationerne omfatter bl.a.:
Idle
Walk
Run
Aim
Fire
Crouch
Kneel
Prone
Prone movement
Prone fire
Prone reload
Bayonet charge
Bayonet stab
Deaths
Der mangler stadig en god stående historisk muzzle-loader reload.


## 41. Shared skeleton og uniformer
En vigtig Unreal-beslutning er, at vi ikke vil have:
Livgarden animation set
Dronningens animation set
Preussian animation set
Dragon animation set
...
hvis kroppene kan bruge samme skeleton.
Vi arbejder i stedet mod:
SK_Human_1864
       ↓
shared animations
       ↓
forskellige meshes/uniformer/materialer
Uniformfarver og regimentdetaljer skal være data-/materialestyrede.
Det betyder, at ændring af jakke/facings/bukser ikke kræver 65 nye animationer.


## 42. Rider og hest skal være separate
For cavalry har vi også fastlagt, at:
Rider SkeletalMesh
+
Horse SkeletalMesh
skal være separate, men synkroniserede.
Det giver os mulighed for:
rider bliver skudt
horse fortsætter
horse bliver skudt
rider falder
rider dismount
horse bliver tilbage
Mounted casualty skal derfor heller ikke nødvendigvis være én gigantisk animation.
Vi har talt om state-kæden:
Mounted
→ GetHit
→ FallOffLeft/Right
→ Ground Wounded/Death
Hesten har samtidig sin egen reaktion.


## 43. Unreal-porten er ikke et redesign
Det er måske den vigtigste samlede beslutning.
Vi skiftede fra Unity til Unreal, fordi battle-prototypen var blevet meget kompleks og bestod af mange lag af prototypescripts.
Men vi besluttede udtrykkeligt:
Vi starter ikke gameplay-designet forfra.
Unity er facit for de battle-beslutninger, der allerede er testet.
Unreal skal implementere dem med renere permanente systemer.
Derfor har Unreal nu selvstændige komponenter til bl.a.:
Formation
Orders
Officer AI
Fire control
Fire drill
Combat
Stance
Terrain awareness
Cavalry
Artillery
Supply
Specialists
Fortifications
Visuals
Animation state
Equipment
i stedet for den lange kæde af Prototype...F29/F30 scripts, vi endte med i Unity.
Unreal parity-backlog


## 44. Hvad har vi faktisk testet, og hvad er kun implementeret?
Det er vigtigt ikke at blande de to sammen.
Område
Unity
Unreal
Infantry movement/Line/Column
Testet visuelt
Implementeret core
1:1 infantry
Testet
Ny real skeletal renderer lavet
Company spacing
Testet og justeret
Implementeret
Early Line deployment
Testet og justeret
Implementeret
Attack/Defend
Testet mange gange
Implementeret
Order blue/red execution
Testet og rettet
Implementeret
Selection persistence
Testet
Implementeret
Facing via drag
Testet
Implementeret
River/bridge
Testet og fejlrettet
Implementeret
Cavalry formations
Testet
Implementeret core
Cavalry bridge 2-abreast
Testet og justeret
Implementeret
Dragon dismount
Testet
Core porteret
Dragon fire
Testet
Core porteret
Square
Testet og fejlrettet
Implementeret
Enemy cones
Testet gentagne gange
Implementeret QA
Cavalry AI/screen
Testet
Implementeret core
Higher HQ
Testet
Implementeret
Temporary CAV attachment
Testet
Implementeret
Artillery
Ikke fuldt implementeret
Stor code-core implementeret
Mortarer
Design
Code-core implementeret
Supply wagons
Design
Code-core implementeret
Advanced terrain/dead ground
Delvist
Code-core implementeret
Fire-drill research
Ny beslutning
Implementeret v00.02.79
Livgarden real skeletal mesh
Ikke denne model
Source integration lavet, importtest mangler
Det betyder, at vi ikke må kalde hele Unreal-porten testet endnu.


## 45. Præcis hvor vi står lige nu
Det screenshot, du lige sendte, beviser faktisk noget nyttigt:
Unreal starter.GameMode starter.QA battlefield spawner.Kameraet fungerer.De fysiske unit placeholders bliver spawned.
Men øverst stod:
PROJECT 1864 | UNREAL PORT | v00.02.65-dev
så du kører stadig en ældre kompileret binary.
Source i GitHub er nu:
Designbaseline v00.02.79
med den nye fire-drill progression.
Så næste egentlige milepæl er:
Hent v00.02.79
       ↓
Build Development Editor / Win64
       ↓
Start Unreal
       ↓
Se v00.02.79-dev
       ↓
Importér Livgarden
       ↓
Importér 65 animationer
       ↓
Test shared skeleton
       ↓
190 rigtige soldater
       ↓
Test formation/movement
       ↓
Test Standing/Kneeling/Prone
       ↓
Test Fire by Rank visuelt
Og derefter skal den visuelle FireByRank kobles helt ned på den enkelte soldier-slot, så vi faktisk ser:
GELED 1
🔥🔥🔥🔥🔥🔥🔥
↓ reload
GELED 2
🔥🔥🔥🔥🔥🔥🔥
↓ reload
GELED 3
🔥🔥🔥🔥🔥🔥🔥
↓ reload
GELED 1
🔥🔥🔥🔥🔥🔥🔥
i stedet for at hele kompagniets skeletal meshes spiller Fire-animationen samtidig.

Samlet set
Vi har altså allerede designet og i høj grad afprøvet selve måden et PROJECT 1864-slag skal fungere på i Unity: hierarchy, orders, formationer, 1:1 styrke, Line/Column/Square, range/cones, facing, broer, river routing, HQ'er, cavalry, dragoner, AI authority, selection, OOB og meget af combined-arms-logikken.
Unreal-porten handler nu primært om at gøre denne prototype til en ren, permanent og skalerbar simulation, og samtidig udvide den med de systemer Unity endnu ikke havde færdige: rigtig artillery/logistics, morterer, fortifications, specialister, mere avanceret terræn/LOS, shared skeleton/animation architecture og den nye fire-drill progression.
