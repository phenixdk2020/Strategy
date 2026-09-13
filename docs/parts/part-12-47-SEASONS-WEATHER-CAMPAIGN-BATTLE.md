# Del 12: 47 — Fælles sæson-, vegetation- og vejrsystem for Campaign + Battle

**Designstatus:** BESLUTTET / IKKE IMPLEMENTERET ENDNU  
**Designbaseline:** v00.02.13  
**Foreslået første runtime-slice:** Campaign3 v00.00.10o — Seasonal Visual Foundation

## 47.1 Formål

Campaign map og taktiske 3D-slag skal vise **samme geografiske sted på samme dato og under samme overordnede sæson-/vejrsituation**. Et slag ved Aalborg i juli må derfor ikke ligne samme sted i januar, og et campaign map med vintertilstand må ikke åbne et grønt sommer-battle map.

Campaign-simulationens dato/tid er autoritativ. Battle-systemet modtager et snapshot af den aktuelle miljøtilstand gennem `BattleContext`, så begge skalaer bruger samme source of truth.

## 47.2 Fælles SeasonState

Første fælles miljømodel skal mindst kunne bære:

- `DateTime`
- `Season` = `Winter | Spring | Summer | Autumn`
- `SeasonProgress` = 0–1 inden for den aktuelle sæson
- `SnowCoverage` = 0–1
- `GroundWetness` = 0–1
- `VegetationState`
- `FieldState`
- senere temperatur, nedbør, vind, sigtbarhed og frosthistorik

Årstiden må ikke være den eneste styrende værdi. **Vinter betyder ikke automatisk fuld sne.** `SnowCoverage` skal være separat, så en dansk vinterdag kan være bar, våd, let snedækket eller kraftigt snedækket afhængigt af den aktuelle og nylige vejrsituation.

## 47.3 Visuel sæson på Campaign map

Campaign map skal kunne skifte visuelt uden at ændre zone-, city- eller gameplay-identitet. Første visuelle lag omfatter:

### Forår
- friskere grøn vegetation
- mørkere/vådere jord
- marker i tidlig vækst
- mere afdæmpet løv end midsommer

### Sommer
- kraftigt grønt landskab
- blanding af grønne marker og **gule/modne kornmarker**
- tæt vegetation og fuldt løv
- tørrere jord i tørre perioder

### Efterår
- grøn/gul/brun vegetation
- høstede eller delvist høstede marker
- mere synlig bar jord
- gradvis løvfaldsprofil
- mulighed for vådere/mudret terræn

### Vinter
- bare eller stærkt reducerede løvprofiler
- brune/grå marker ved snefri vejr
- variabelt sne-overlay styret af `SnowCoverage`
- frost-/vintertone uden krav om konstant sne

Sæsonlaget må ikke blive et kraftigt farvefilter, der skjuler World Imagery, zonegrænser, byer, hære eller anden vigtig information. Målet er en læsbar, naturlig overgang mellem årstider.

## 47.4 Landbrugsmarker

Marker skal være en selvstændig visuel og senere økonomisk datatype. Første visuelle `FieldState` kan bestå af:

- `BareField`
- `SpringGrowth`
- `SummerGreen`
- `SummerYellowGrain`
- `Harvested`
- `WinterDormant`
- `SnowCovered`

Sommerlandskabet skal ikke være ensfarvet grønt. Især juli/august skal kunne vise en mosaik af grønne områder og gule/modne kornmarker. Sen sommer og efterår skal gradvist kunne vise høstede marker og mere bar jord.

Senere kan `FieldState` kobles til faktiske afgrøder, gårde, høsttid, fødevareproduktion, forsyning og ødelæggelse under militære operationer.

## 47.5 Battle map arver Campaign-miljøet

Når `CampaignToBattleGenerator` opretter et taktisk kort, skal `BattleContext` mindst overføre:

- kampens WGS84-center
- campaign-dato og klokkeslæt
- `Season`
- `SeasonProgress`
- `SnowCoverage`
- `GroundWetness`
- `VegetationState`
- `FieldState`
- senere aktivt vejr og nylig vejrhistorik

Det taktiske kort genererer derefter den samme årstid i højere detalje:

- terrain-materialer
- sne-/frostlag
- træer og løvtilstand
- markmaterialer og afgrøder
- jord, mudder og fugt
- atmosfære, tåge og lys

Et battle map ved Aalborg i juli kan derfor have grøn vegetation, gule kornmarker og tørre veje, mens samme geografiske område i januar kan have bare træer, vintermarker og variabel sne/frost.

## 47.6 Samme sted, forskellig dato

Geografien og permanente bygninger skal være persistente, mens miljøprofilen beregnes ud fra den aktuelle campaign-dato.

Et cached eller genbrugt battle map må derfor ikke gemme en permanent sommer- eller vintertilstand. Det skal genindlæse den aktuelle `SeasonState` ved hvert slag.

Eksempel:

- Juli 1851: grønne træer, gule kornmarker, tør jord.
- Oktober 1851: høstede marker, gul/brun vegetation, vådere jord.
- Januar 1852: bare træer og 0–100 % snedække afhængigt af vejret.
- April 1852: våd jord og tidlig grøn vækst.

## 47.7 Vejr og sne

Sæson og vejr er separate systemer.

Første miljøversion kan bruge sæsonbaserede visuelle profiler, men arkitekturen skal senere understøtte dynamisk:

- regn
- snefald
- tåge/dis
- frost
- temperatur
- vind
- jordfugtighed
- akkumuleret sne
- optøning

`SnowCoverage` skal kunne ændre sig gradvist over flere campaign-timer/dage. Sne skal ikke forsvinde øjeblikkeligt, blot fordi aktivt snefald stopper.

## 47.8 Senere gameplay-effekter

Første runtime-slice skal primært være **visuel**. Gameplay-effekter tilføjes senere gennem samme state i stedet for at bygge et separat system.

Mulige effekter:

- våd/mudret jord reducerer bevægelse for artilleri, forsyningsvogne og tunge formationer
- sne og frost påvirker march og fatigue
- høje afgrøder kan påvirke concealment og observation
- bare vintertræer kan give anden LOS end tæt sommerløv
- regn/tåge/snefald påvirker visibility og spotting
- dårlige veje og jordbund kan påvirke supply throughput
- frosne overflader kan ændre passageforhold, men må kun aktiveres ved en separat verificeret frostmodel

Der må ikke indføres vilkårlige direkte combat-bonuser alene fordi en bestemt årstid er aktiv. Effekter skal komme gennem konkrete mekanismer som movement, visibility, fatigue, terrain traction og supply.

## 47.9 Arkitekturprincip

Systemet opdeles i tre lag:

1. **Global/Regional Environment State** — campaign-dato, sæson, sne, jordfugtighed og senere vejr.
2. **Campaign Visual Adapter** — oversætter miljøstate til campaign map-materialer, vegetation, marker og overlays.
3. **Battle Environment Adapter** — bruger samme state til det genererede 3D battle map.

Campaign og Battle må ikke have hver sin uafhængige sæsonberegning.

## 47.10 Første foreslåede implementation

`Campaign3 v00.00.10o — Seasonal Visual Foundation` kan som første slice implementere:

1. fælles `CampaignSeasonState`
2. fire basisårstider
3. `SnowCoverage`
4. `GroundWetness`
5. 5–7 marktilstande
6. campaign terrain/vegetation tint
7. visuelle gule sommermarker
8. sæsondata i `BattleContext`
9. samme material-/vegetationsprofil på battle map
10. debug-override til Spring/Summer/Autumn/Winter og SnowCoverage for QA

Gameplay-effekter skal først aktiveres, når det visuelle state-system er stabilt og testet.

## 47.11 Acceptance-principper

- Campaign og Battle viser samme sæson for samme dato.
- Sommer kan tydeligt vise både grønne og gule marker.
- Efterår kan vise høstede marker og løvfald uden at gøre hele kortet orange.
- Vinter kan være både snefri og snedækket.
- `SnowCoverage=0` giver ingen kunstig fuld sne.
- `SnowCoverage=1` giver sammenhængende snedækning, hvor terræn/materiale tillader det.
- Battle map må ikke nulstille sæsonstate.
- Samme geografiske battle map skal kunne genbruges under forskellige sæsoner.
- Sæsonlaget må ikke ændre WGS84, ZoneId, CityId, bygningernes identitet eller campaign simulation state.
