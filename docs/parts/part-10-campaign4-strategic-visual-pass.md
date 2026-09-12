# Del 10 — Campaign4 v00.00.10l: Strategic Visual Pass

## Formål

v00.00.10l flytter Campaign4 fra teknisk 3D-prototype mod det godkendte Danmark-1851-look på strategisk zoom.

Referencebilledet bruges **kun som art direction**. Det er ikke en geografisk eller historisk datakilde.

Det betyder eksplicit:

- bylisten kopieres ikke 1:1 fra referencebilledet;
- vejene kopieres ikke 1:1 fra referencebilledet;
- kystdetaljer, marker, skove og bebyggelse kopieres ikke 1:1;
- PROJECT 1864-data, historisk research og gameplay-systemer er facit for indhold;
- referencebilledet er facit for ønsket palette, dybde, tæthed, stemning, markerhierarki og generel diorama-følelse.

## Strategisk terræn-look

`Campaign4StrategicVisualPassV010L.cs` tilføjer et runtime visual layer oven på provider 1 DEM.

### Landcover

Alle streamede DEM-meshes får globale UV-koordinater ud fra den eksisterende WGS84-projektion. Et deterministisk 512x512 procedural landcover-map genereres ved runtime og deles af terræn-tiles.

Teksturen blander visuelt:

- græs/lavland;
- grønne marktoner;
- gyldne/jordfarvede marktoner;
- hede/brun vegetation;
- mørkere skovområder;
- let fin variation for at bryde den tidligere ensartede DEM-grønne flade.

Dette er visuel dressing og **ikke** en rekonstruktion af præcise 1851-markskel eller skovparceller.

## Vand og lys

v10l gør havet mørkere og mere mættet, mens fjorde/indre vand får en separat lidt lysere blå tone.

Campaign Sun justeres mod varmt skråt lys med bløde skygger. Ambient light dæmpes, så DEM-relief og skovmasser læses tydeligere.

## Skovmasser

Et begrænset antal procedurale 3D-skovmasser placeres deterministisk på faktiske DEM-højdesamples. De er strategiske volumener, ikke individuelle historiske træpositioner.

## By-markører

De eksisterende 40 historiske 1850-byankre bevares. Strategiske `Marker`/`Centre` renderers får rust-rød/orange styling i retning af det godkendte reference-look.

City Detail LOD overtager fortsat ved tæt zoom.

## Vej-scaffold — vigtig guardrail

v10l introducerer et separat `CAMPAIGN4_v10l_StrategicRoads_NONCANON` runtime-lag.

Vejene:

- er **ikke** traced fra referencebilledet;
- er ikke facit for det historiske 1851-vejnet;
- bruges kun som strategisk præsentations- og pipeline-scaffold;
- forbinder udvalgte historiske byankre i et manuelt PROJECT 1864-linknet;
- får en lille deterministisk kurve i stedet for perfekte rette linjer;
- følger det streamede DEMs sampled højde;
- skal senere kunne erstattes af source-backed historiske vejdata uden at ændre resten af visual-pipelinen.

Referencebilledets konkrete vejforløb må derfor ikke reproduceres 1:1.

## Kamera

Danmark-overblikket ændres i v10l til:

- perspective FOV 38;
- overview distance 94;
- pitch 68°;
- yaw -3°;
- fortsat fri zoom/pan/orbit;
- `Home` genskaber Danmark-overblikket;
- `F` bevarer Aalborg/Limfjorden som close-zoom QA.

Målet er et mere kort-lignende strategisk overview uden at gå tilbage til et fladt orthographic map.

## Ikke med i v10l endnu

v10l er første visual pass og er ikke slutkvalitet. Følgende ligger senere:

- source-backed historiske vejnet;
- præcise 1851-skove/marker;
- authored 3D-byer og havne;
- broer/færger som særskilte transportdata;
- nabolande med fuld terrain dressing;
- specialbygninger, kaserner, forter og industri;
- avanceret vandshader/kystskum;
- endeligt UI i reference-look.

## QA

1. Unity 6.6 skal compile uden errors.
2. Provider 1 skal fortsat være default.
3. DEM-relief skal fortsat være synligt efter landcover-materialet lægges på.
4. Land skal have tydelig grøn/brun variation og ikke være ensfarvet.
5. Hav skal være mørkere end v10k.
6. Fjorde/hydrologi skal stadig kunne aflæses.
7. De 40 historiske city markers skal være rust-røde/orange.
8. Skovmasser skal være begrænsede og ligge på terrænet.
9. Road-scaffold skal være diskret og følge terrænhøjde.
10. Vejene må ikke præsenteres som historisk facit eller som kopier af referencebilledet.
11. Danmark-overblik skal være mere top-down og map-like.
12. `F` skal stadig fokusere Aalborg/Limfjorden.
13. Army/zone selection og RMB marchordre skal fortsat fungere.
14. Nyt Danmark-overbliksscreenshot skal sammenlignes visuelt med art-direction-referencebilledet.
