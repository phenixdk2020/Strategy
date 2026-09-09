# Campaign v00.00.13h — Denmark Premium Visual DEV

**Status:** IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA  
**Branch:** `work/v00.00.13h-denmark-premium-visual-pass`

## Formål

v13h er en presentation-only premium pass oven på den korrigerede Denmark-first campaign foundation. Målet er at flytte kortet væk fra debug/prototype-udtryk og hen imod **Historic Miniature Grand Strategy Diorama** uden at ændre simulationen.

## Implementeret

- Premium land palette på det korrigerede v13f Denmark landmesh.
- Deterministiske markfelter og hedgerows placeret på sikker dansk landgeometri.
- Deterministiske woodland clusters med miniature-træer og city-avoidance.
- Mørkere, glattere clean-sea materiale med meget subtil time-based colour breathing.
- Legacy Denmark settlement miniatures skjules og erstattes af richer city dioramas.
- City dioramas skaleres efter byens betydning og kan indeholde houses, church/tower, station/platform og port piers efter node data.
- Denmark-only road/rail/ferry links får separate premium materialer og breddehierarki.
- Aalborg barracks og Aarhus farm beholder staged campaign-time construction; premium fence/material/field dressing tilføjes omkring sites.
- Warm directional light, soft shadows, restrained fog og strammere Denmark Home composition.
- v13h overtager label ownership: Denmark-only semantic zoom, light text + subtle shadow, ingen heavy black debug boxes.
- Foreign nodes/settlements/formations/links forbliver presentation-hidden; simulation data bevares.

## Ikke ændret

- campaign movement
- ETA / route distance
- campaign clock
- logistics
- strategic state
- tactical AI
- tactical navigation
- battle systems

## QA

Kontrollér i Unity:

1. Build label viser `v00.00.13h DENMARK PREMIUM VISUAL DEV`.
2. Ingen svenske, norske, finske eller tyske labels/settlements er synlige.
3. Ingen danske labels vises dobbelt.
4. Danmark er stadig korrekt udfyldt og genkendeligt.
5. Markfelter, hedges og skovklumper ligger på land og ikke ude i havet.
6. København/Aalborg/Aarhus/Odense har tydeligt rigere miniature-settlements ved tæt zoom.
7. Road/rail/ferry er visuelt adskilt.
8. Aalborg barracks og Aarhus farm er fortsat synlige og staged.
9. Close zoom fungerer ned til den nye premium inspection range.
10. Console har ingen compile errors.

## Kendte prototypegrænser

v13h bruger fortsat procedural Unity-primitives og genererede materialer; der er endnu ikke importeret et dedikeret historisk art asset pack, DEM, orthophoto eller færdige shader-pakker. Denne version etablerer premium composition, palette, hierarchy og diorama-retning som visuel baseline før eventuel asset-pipeline-opgradering.
