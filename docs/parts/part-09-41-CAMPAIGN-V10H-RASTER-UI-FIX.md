# PROJECT 1864 — Campaign v00.00.10h Raster/UI Hotfix

## Formål

v10h retter to konkrete QA-problemer observeret i Unity 6.6 på TRUE 11 BASEMAP-laboratoriet:

1. Rasterbasemaps kunne ende permanent delvist indlæst, fx `LOADING 18/30`.
2. Den gamle v10e campaign-IMGUI og den nye v10g basemap-IMGUI blev tegnet oven i hinanden.

## Observeret fejl

QA-screenshot viste provider 9 (Imagery), hvor de nordlige rasterrækker var indlæst, mens resten af Game view stadig viste den blå campaign-baggrund. Samtidig var v10e-versionstekst, Natural Earth-mapinfo og v10g basemap-paneler synlige samtidigt.

## Teknisk årsag

`CampaignRasterBasemapV010G` kørte én sekventiel coroutine over alle XYZ-tiles. `CampaignMapStyleSwitcher` deaktiverer ikke-valgte provider-roots. Unity stopper coroutines på deaktiverede GameObjects, men v10g nulstillede ikke `loadStarted`. En provider, der blev forladt midt i indlæsningen, kunne derfor blive stående i en permanent deltilstand og nægte at starte igen.

Rastertiles blev samtidig oprettet synligt én for én. Det gav en hård kant mellem allerede downloadede tile-rækker og det tomme baggrundsområde.

## v10h løsning

### Raster-loader

- Maksimalt seks samtidige tile-requests.
- Aalborg/Limfjord prioriteres først i køen som QA-region.
- Successful tiles registreres separat og bevares på tværs af provider-skift.
- Hver load-session får en generation-ID. Deaktivering invaliderer den gamle session.
- Provider-reselection starter en ny session og genbruger allerede succesfulde tiles/cache.
- Raster-rooten er skjult under loading og vises først samlet efter overview-loaden.
- Fejlede source-tiles får kun en neutral `missing tile`-flade; der bruges aldrig fallback-data fra en anden provider.
- Status viser antal færdige tiles samt samtidige aktive requests.

### UI

- Build badge viser `v00.00.10h`.
- Det gamle v10e-buildbadge maskeres uden at fjerne campaign clock/speed controls.
- Basemap-toolbar får en reserveret uigennemsigtig bundzone.
- Den gamle Natural Earth-mapinfo kan derfor ikke længere tegnes gennem basemap-knapperne.
- Loading-state vises centralt, mens rasterkortet endnu ikke er komplet.
- Provider source/requirements/attribution samt Aalborg/Limfjord QA-gate forbliver synlige.

## Ingen gameplay-ændringer

v10h ændrer ikke campaign clock, zonegraf, WGS84-identitet, army movement eller battle/navigation-adfærd. Hotfixet ligger på presentation/provider-laget.

## QA

1. Start provider 9 Imagery.
2. Bekræft at status går fra `LOADING` med flere `ACTIVE` requests til `READY`.
3. Bekræft at et halvt rasterkort ikke længere vises under loading.
4. Skift væk fra provider 9 midt i loading og tilbage igen.
5. Bekræft at den fortsætter/genstarter uden at fryse på det gamle tile-tal.
6. Gentag på provider 3 ArcGIS og provider 8 OSM.
7. Kontroller UI ved 1280x720-ish vinduesbredde: build badge, campaign clock, selection panel, provider info og bundtoolbar må ikke ligge oven i hinanden.
8. Kontroller Aalborg/Limfjord og WGS84 marker alignment.

## Rollback

Pre-hotfix v10g er gemt på:

`backup/channel-campaign3-v10g-before-raster-ui-fix-20260912`
