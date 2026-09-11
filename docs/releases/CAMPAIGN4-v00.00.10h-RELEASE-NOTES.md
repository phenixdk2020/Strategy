# CAMPAIGN4 v00.00.10h — Danmark 1851 / Top-40 bylag

## Branch

`channel-campaign4`

Base: `channel-campaign3` v00.00.10g TRUE 11 BASEMAPS.

## Ændringer

- Bevarer Campaign3 urørt som rollback/reference.
- Bevarer provider 1 (DEM/hydrologi) som standard 3D-basemap.
- Tilføjer `Campaign4CityLayer1850.cs` som separat runtime-lag.
- Erstatter de 10 gamle city scaffold-markører med de 40 største danske købstæder efter den officielle folketælling 1. februar 1850.
- Befolkning bruges til rank og markerstørrelse.
- Semantic zoom: top 5 / top 15 / top 40 labels.
- Esbjerg fjernes fra 1851-bylaget som anakronistisk byvalg.
- Unity `.meta` er committed sammen med scriptet.

## Historisk kilde

Danmarks Statistik, *Statistisk Tabelværk, Ny Række, Bd. 1 — Folkemængden 1850*, publikation 19933. Tabellen rangerer Kongerigets 68 købstæder efter befolkningen 1. februar 1850.

## QA-gates

- Unity 6.6 compile uden fejl.
- Provider 1 starter som default.
- Limfjorden skal være synlig ved Aalborg/Nørresundby.
- Præcis 40 `CITY1850_*` roots.
- Ingen aktiv `CITY_Esbjerg`.
- København størst marker.
- Alle 40 labels ved close zoom.
