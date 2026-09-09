# PROJECT 1864 Designmanual — v00.02.13 Historical 3D Map Addendum

## Strategisk kortretning

Campaign-kortets langsigtede presentation baseres på en **Historical 3D Map**-model: troværdig geografi/elevation som fysisk 3D-base, historisk kort/raster/vector som cartographic surface og små 3D facilities/settlements som semantic close-zoom detail.

Den tidligere prototype med store procedural primitives, forskellige terrain-tile farver og overdrevne city dioramas er ikke længere målstandarden.

## Lagkontrakt

1. Simulation geography: WGS84 latitude/longitude og explicit route geometry.
2. Elevation: DEM/DHM render data, aldrig authoritative distance.
3. Physical water/coast: GIS geometry.
4. Historical cartography: scenario-date raster/vector med provenance.
5. Infrastructure: road/rail/ferry/bridge semantics med korrekt route geometry.
6. Settlements/facilities: små LOD-styrede 3D miniatures.
7. Strategic entities/overlays/UI: separat fra basemap.

Et visuelt kortskift må ikke ændre movement, ETA, logistics eller strategic outcomes.

## Cartographic texture

v13j fastlægger global UV/draping contract. En historisk raster kan ligge oven på det samme 3D-terrain uden at ændre simulation. Development fallback må bruges til renderer-QA, men skal tydeligt markeres som ikke-historisk.

## Skala og LOD

Byer, kaserner, farms, havne og andre facilities skal være map-scale miniatures og ikke repræsentere bogstavelig geografisk footprint ved overview zoom. Identitet bæres af semantic labels/symboler langt væk; 3D-detail kommer frem ved tæt zoom.

## Kamera

Strategisk map camera skal understøtte smooth overview-to-local inspection med terrain-aware close zoom og cursor-centric interaction. Målet er en kortoplevelse i retning af moderne 3D-kortnavigation, men uden at gøre campaign-kortet til et flight/first-person camera.

## Historisk dataintegritet

Moderne GIS-data må bruges til fysisk terræn/geografi, hvor det er hensigtsmæssigt. Moderne road/building state må aldrig automatisk blive 1864 scenario-state. Historiske features skal valideres mod periodekilder og senere have provenance/confidence metadata.

## Input migration

Unity Input Manager deprecation behandles separat fra map renderer. Projektet skifter først til New Input System, når tactical og campaign controls er migreret og regressionstestet samlet. Visual-map builds må ikke bryde tactical controls for at fjerne en editor-warning.

## v13j implementation baseline

- ét smooth mainland Denmark terrain mesh,
- bilinear DEM sampling + reduced relief,
- seamless global cartographic material,
- optional local historical raster contract,
- smaller Denmark-only city/facility miniatures,
- Aalborg harbour/barracks and Aarhus farm in map scale,
- thinner corrected road/ferry presentation,
- semantic Denmark-only labels,
- terrain-aware cursor zoom + middle mouse pan,
- MAP-ONLY combat gate maintained during map QA.
