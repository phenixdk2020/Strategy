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

## Unity-implementation

`Campaign4CityLayer1850.cs` oprettes som et separat runtime-lag, så Campaign3-koden ikke ændres.

Ved start:

- de gamle `CITY_<navn>` scaffold-markører fjernes;
- 40 `CITY1850_<rank>_<navn>`-objekter oprettes fra WGS84-ankre;
- markerstørrelse skaleres efter 1850-befolkningen;
- markørerne er reelle 3D-objekter med base + centerstykke;
- semantic zoom viser top 5 ved strategisk zoom, top 15 ved operational zoom og alle 40 ved close zoom;
- bylaget er gameplay/presentation-data og er fortsat adskilt fra selve basemap-providerne.

## QA

1. Branch skal være `channel-campaign4`.
2. Provider 1 skal fortsat være default ved Play.
3. Limfjorden skal være synlig ved Aalborg og adskille Aalborg-siden fra Nørresundby/Vendsyssel-siden.
4. Der skal oprettes præcis 40 `CITY1850_*` roots.
5. Esbjerg må ikke findes som aktiv bymarkør.
6. København skal være tydeligt største city marker.
7. Ved operational zoom vises kun top 15 labels.
8. Ved close zoom vises alle 40 labels.
9. Der må ikke komme compile warnings fra deprecated `FindObjectsByType(...FindObjectsSortMode...)` i Campaign4-bylaget.
