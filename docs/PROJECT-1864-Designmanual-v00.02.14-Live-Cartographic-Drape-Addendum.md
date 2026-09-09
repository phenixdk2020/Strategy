# PROJECT 1864 — Designmanual addendum v00.02.14

## Live Cartographic 3D Drape baseline

Campaign-kortets grafiske baseline udvides med en direkte **cartographic drape**-pipeline: et almindeligt georefereret kort må bruges som 2D-teksturlag, men lægges på et geospatialt 3D-terrain i stedet for at blive vist som en flad kortplane.

Dette bliver den foretrukne map-presentation arkitektur:

`geospatial projection -> DEM/elevation -> smooth terrain mesh -> cartographic raster/tile drape -> historical/gameplay overlays -> close-zoom 3D landmarks`

### Principper

- Kortets pixelgrafik er presentation; latitude/longitude og geodesiske/routed distances forbliver authoritative simulation state.
- Nutidige basemaps må bruges til visual QA og navigation, men de bliver aldrig automatisk historisk 1864-data.
- Et licenseret/georefereret historisk raster prioriteres frem for moderne live tiles, når det foreligger.
- Live tile providers må kun bruges efter deres vilkår, med attribution og uden skjult bulk/prefetch.
- Provider credentials/API keys må aldrig commits i repositoryet.
- Roads/rail/ferries i simulationen må senere renderes som egne historiske overlays, men kun fra valideret route geometry.
- En road-overlay må ikke visuelt krydse vand, medmindre route-segmentet eksplicit er bridge/ferry/sea crossing.
- Settlements og facilities skal bruge semantic LOD; cartographic footprint kan være nok på regionalt zoom, mens små 3D-landmarks aktiveres ved tæt zoom.
- Harbour cities skal have land-anchored settlement/facility objects og separate water-anchored harbour/ship objects.

### v00.00.13k reference implementation

v13k bruger v13j smooth DEM-terrain og draperer current-view raster tiles via subdividerede tile meshes. Tile meshes følger terrain-colliderens højder og falder tilbage til water level uden for landmesh.

Default DEV-provider er OpenStreetMap Standard med 3x3 viewport-only requests, 14-dages cache og synlig attribution. Dette er ikke en production/offline tile strategy. Et lokalt `denmark_1864.png` historical raster har højere prioritet og deaktiverer live requests.

Prototype node-to-node road/ferry lines og primitive city blocks skjules i v13k visual QA. Aalborg-kaserne og Aarhus-farm genintroduceres som små terrain-anchored close-zoom landmarks.

### Quality gate

Den nye map-baseline er først godkendt, når Danmark kan aflæses som et rigtigt kort ved overview, terrain relief kan opleves ved tilt/zoom, lokale detaljer forbliver skarpe ved nær-view, og 3D-landmarks ikke dominerer kortets geografiske skala.
