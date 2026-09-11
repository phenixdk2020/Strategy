# Del 9 — Campaign4: Danmark 1851, 3D-kort og historisk bylag

## Formål

Campaign4 er en separat videreudviklingskanal fra `channel-campaign3`. Campaign3 bevares urørt som rollback/reference.

Campaign4 starter på den eksisterende TRUE 11 basemap-arkitektur, hvor provider 1 er standard: den genererede DEM/hydrologi-provider. Danmark vises i WGS84-projektion med 3D-relief, og Limfjorden er en obligatorisk hydrologisk QA-gate omkring Aalborg/Nørresundby.

## Historisk bylag for 1851-starten

Kortet skal **ikke** vise en moderne dansk byliste. Bylaget tager i stedet udgangspunkt i den officielle danske folketælling pr. **1. februar 1850**, som er den nærmeste komplette officielle størrelsesrangering før spilstarten 1. januar 1851.

Kilde: Danmarks Statistik, *Statistisk Tabelværk, Ny Række, Bd. 1 — Folkemængden 1850*, publikation 19933.

Campaign4 viser præcis **40 byer**: de 40 største købstæder i 1850-rangeringen. Befolkningstallet bruges som metadata og til visuel markeringshierarki.

### Top 40

1. København — 129.695
2. Odense — 11.122
3. Helsingør — 8.111
4. Aarhus — 7.886
5. Aalborg — 7.745
6. Randers — 7.338
7. Horsens — 5.827
8. Rønne — 4.717
9. Svendborg — 4.556
10. Fredericia — 4.326
11. Viborg — 4.039
12. Slagelse — 4.011
13. Roskilde — 3.805
14. Vejle — 3.300
15. Nyborg — 3.059
16. Ribe — 2.984
17. Assens — 2.965
18. Nakskov — 2.955
19. Kolding — 2.865
20. Næstved — 2.735
21. Holbæk — 2.638
22. Kalundborg — 2.490
23. Køge — 2.456
24. Thisted — 2.342
25. Rudkøbing — 2.333
26. Faaborg — 2.328
27. Nykøbing Falster — 2.123
28. Hillerød — 1.929
29. Hjørring — 1.914
30. Kerteminde — 1.833
31. Korsør — 1.819
32. Stege — 1.808
33. Varde — 1.774
34. Maribo — 1.667
35. Middelfart — 1.655
36. Nykøbing Mors — 1.598
37. Vordingborg — 1.579
38. Bogense — 1.497
39. Nexø — 1.403
40. Skagen — 1.400

## Historisk guardrail: Esbjerg

Esbjerg må ikke indgå i 1851-bylaget. Den moderne by blev først udviklet efter 1864 og havneanlægget fra slutningen af 1860'erne. Den tidligere prototype-markør var derfor anakronistisk og erstattes i Campaign4.

## Unity-implementation — historisk bylag

`Campaign4CityLayer1850.cs` er et separat runtime-lag, så Campaign3-koden ikke ændres.

Ved start:

- de gamle `CITY_<navn>` scaffold-markører fjernes;
- 40 `CITY1850_<rank>_<navn>`-objekter oprettes fra WGS84-ankre;
- markerstørrelse skaleres efter 1850-befolkningen;
- markørerne er reelle 3D-objekter med base + centerstykke;
- semantic zoom viser færre labels på strategisk niveau og flere ved tæt zoom;
- bylaget er gameplay/presentation-data og er fortsat adskilt fra selve basemap-providerne.

## Campaign4 ægte 3D-kamera

Fra v00.00.10i må Campaign4 ikke længere være låst til et lodret orthographic-kamera.

`Campaign4Camera3DController.cs` overtager den visuelle kamera-position i `LateUpdate` og konverterer campaign-kameraet til perspective rendering. Den eksisterende gameplay-selection kan fortsat bruge samme `Camera.main` og raycasts.

### Kamera-funktioner

- perspective camera med FOV 42;
- glidende zoom fra Danmark-overblik til lokal byskala;
- zoomafstand ca. 115 Unity-enheder ned til 5,5;
- WASD/piletaster flytter kameraets pivot;
- musehjul zoomer ind/ud;
- midterste museknap + drag ændrer yaw/pitch;
- Q/E roterer omkring fokuspunktet;
- `Home` nulstiller til Danmark-overblik;
- `F` fokuserer Aalborg/Limfjorden som fast QA-område;
- pitch er begrænset, så kameraet bevarer strategisk læsbarhed og ikke går under terrænet.

Den nye controller er et Campaign4-lag og ændrer derfor ikke Campaign3.

## 3D-terræn og relief

Provider 1 er allerede et rigtigt DEM-runtime-system og må fortsat være Campaign4-standardfundamentet.

Den bygger terrain tiles fra Terrarium elevation data og konverterer højdemeter til Unity-Y. Hydrologilaget skærer bl.a. Limfjorden ud af terrænet. Det betyder, at perspective-kameraet faktisk ser et 3D-mesh og ikke blot et fladt baggrundsbillede.

Produktionsmålet er fortsat officielle danske DHM/GeoDanmark-data, men Terrarium-providerens rolle er at gøre den tekniske 3D-pipeline spilbar nu.

## City Detail LOD

`Campaign4CityDetailLOD.cs` indfører første nær-LOD for byerne.

På stor afstand anvendes de billige strategiske city markers. Når kameraet kommer tæt nok på en by:

- strategimarkøren skjules;
- et deterministisk 3D-bylag aktiveres;
- bygninger, hovedgader og kirke genereres som runtime-geometri;
- København/top-10 får større detail-radius og længere synsafstand end mindre byer;
- kun nærliggende byer aktiverer detailgeometri, så hele Danmark ikke renderer tusindvis af nær-objekter samtidig.

Den procedurale bebyggelse er **ikke** historisk bygningsfacit. Den er den tekniske LOD-placeholder. Senere udskiftes den gradvist med source-backed 1851 prefabs for centrale byer, havne, kaserner, kirker, stationer, fæstninger osv.

## Visuel retning

Målet for Campaign4 er det tidligere godkendte visuelle koncept: et levende, detaljeret 3D-strategikort, hvor Danmark kan ses samlet, men hvor spilleren kan zoome ned mod by-, havne- og infrastrukturniveau uden at skifte til et statisk billede.

Kortet skal derfor bygges af gameplay-objekter og LOD-lag — ikke af ét stort renderet kortbillede.

## QA

1. Branch skal være `channel-campaign4`.
2. Provider 1 skal fortsat være default ved Play.
3. Limfjorden skal være synlig ved Aalborg og adskille Aalborg-siden fra Nørresundby/Vendsyssel-siden.
4. Der skal oprettes præcis 40 `CITY1850_*` roots.
5. Esbjerg må ikke findes som aktiv bymarkør.
6. København skal være tydeligt største city marker.
7. Campaign camera skal være `orthographic = false` efter Campaign4-controlleren installeres.
8. Musehjul skal kunne zoome fra Danmark-overblik til tæt byniveau.
9. `F` skal fokusere Aalborg/Limfjorden.
10. Midterste museknap skal kunne ændre kameraets vinkel uden at påvirke højreklik-ordrer.
11. Nær en by skal `Detail3D` aktiveres og den strategiske markør skjules.
12. Når kameraet zoomer væk igen, skal `Detail3D` deaktiveres og markøren komme tilbage.
13. Provider 1 DEM-relief skal være synligt i perspective view.
14. Der må ikke komme compile warnings fra deprecated `FindObjectsByType(...FindObjectsSortMode...)` i Campaign4-lagene.
15. Før promotion skal Aalborg/Limfjorden kontrolleres visuelt i Unity Play Mode.
