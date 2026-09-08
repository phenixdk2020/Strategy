# PROJECT 1864 — Campaign v00.00.13 3D DEV Notes

**Branch:** `work/v00.00.13-3d-campaign-map`  
**State:** `IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA`  
**Promotion:** Ingen. Denne DEV-branch omgår ikke v00.00.12/B-409-afhængigheden.

## Første implementerede 3D-slice

- procedural campaign terrain mesh over den eksisterende geospatiale projection
- coarse land/sea mask
- stærkere relief i Norge/Sverige
- hydrology-root med sea base
- L1-L10 layer roots
- eksisterende strategic links resampled over terrain
- sandsynlige rail-links visuelt adskilt fra roads
- country outlines løftet op på 3D-overfladen
- node markers og moving formation tokens terrain-snapped
- små 3D settlement-miniaturer
- station/depot/port/fortification-miniaturer
- terrain-aware campaign camera
- PageUp/PageDown pitch
- campaign-time sunlight, ambient light og fog
- F6 infrastructure
- F7 settlements
- F8 living world
- F9 overlays
- F10 hydrology
- staged barracks QA project near Aalborg
- staged farm QA project near Aarhus
- construction stages følger `CampaignSession.CurrentDateTime`
- to simple worker loops per aktiv construction
- overlay `PROJECT 1864 CAMPAIGN | v00.00.13 3D DEV`

## Unity 6.6 compile-fix batch — 2026-09-08

Efter første lokal compile-test er følgende API-kompatibilitetsrettelser implementeret:

- `CampaignMapController.cs`: `Object` er eksplicit kvalificeret som `UnityEngine.Object`, så `System.Object`/`UnityEngine.Object`-ambiguiteten fjernes.
- `CampaignMapController.cs`: deprecated `FindObjectsSortMode.None` er fjernet og erstattet med Unity 6.6 parameterless `FindObjectsByType<T>()`.
- `CampaignTerrainV013.cs`: tre deprecated `FindObjectsByType<T>(FindObjectsSortMode.None)`-kald er opdateret til Unity 6.6 API.
- `PrototypeNavigationRecoveryManager.cs`: deprecated object-query API opdateret mekanisk; navigation logic er ikke ændret.
- `PrototypeBattlefieldNavigationV3.cs`: deprecated object-query API opdateret mekanisk; navigation logic er ikke ændret.
- `PrototypeBattlefieldNavigationManager.cs`: deprecated object-query API opdateret mekanisk; navigation logic er ikke ændret.

Compile-fix er committed, men **Unity compile/runtime QA er stadig ikke markeret bestået**, før den er testet lokalt i Unity 6.6.

## Prototypebegrænsninger

- terrain er procedural/coarse, ikke DEM/GIS
- coastlines er coarse
- strategic links bruger nuværende graph, ikke historiske route polylines
- road/rail classification er delvist inferred
- construction QA er ikke koblet til fuld economy/resource queue
- ingen production troop-train lifecycle i denne slice
- ingen chunk streaming/floating origin
- ingen final land-cover/forest/agriculture system
- ingen final weather simulation

## QA-checkliste

1. Unity 6.6 compile med 0 errors.
2. Bekræft terrain relief og sea under land.
3. Bekræft nodes/formations snapper til terrain.
4. Bekræft strategic links følger terrain.
5. Bekræft 3D settlements vises.
6. Test F6-F10 layer toggles.
7. Test PageUp/PageDown camera pitch.
8. Bekræft Aalborg/Aarhus construction stages ændres med campaign-time.
9. Pause skal stoppe construction/worker movement.
10. x5/x20 skal accelerere campaign-time construction progression.
11. Bekræft at tactical navigation-adfærd ikke er ændret af compile-fix batchen.
