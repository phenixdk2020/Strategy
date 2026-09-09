# PROJECT 1864 — Campaign v00.00.13l Premium Cartographic Visual

Status: DEV / MAP-ONLY / afventer Unity compile + runtime QA.

## Formål

v13l er en ren presentation-version. Den bygger videre på v13j 3D-terrain og v13k live cartography, men fokuserer på at få campaign-kortet til at ligne et færdigt strategikort frem for en GIS/debug-prototype.

## Visuel pipeline

1. v13j leverer kontinuerligt DEM-baseret 3D-terrain.
2. v13k leverer live overview-cartography.
3. v13l tilføjer z12/z13 detail tiles tæt på terrænet.
4. Detail tiles bygges som 32x32 conforming meshes og følger terræn/water surface.
5. Prototype road/ferry/city renderers forbliver skjult.
6. Små 3D-landmarks vises kun ved tæt zoom.

## Close zoom

- z12 under camera height 5.6.
- z13 under camera height 1.55.
- Kun 3x3 tiles omkring aktuel viewport.
- 14 dages lokal cache.
- Trilinear + anisotropic filtering.
- Camera minimum height 0.14, clearance 0.09 og near clip 0.003.

## Premium presentation

- Neutral kartografisk farvebalance.
- Soft directional shadows.
- Restrained warm daylight.
- Long-range atmospheric fog.
- Ingen store prototypebyer.
- Ingen direkte prototypeveje hen over vand.
- Aalborg Kaserne og Aarhus Gård er små LOD-landmarks med terrain anchoring.

## Provider/historical isolation

OpenStreetMap Standard er fortsat kun DEV-basemap. Hvis `denmark_1864.png` eller en lokal `provider.json` findes, slår v13l sin ekstra OSM detail-layer fra, så forskellige kartografiske kilder ikke blandes.

## Simulation isolation

v13l ændrer ikke lat/lon, route distance, ETA, campaign time, logistics, combat eller tactical code. MAP-ONLY gate forbliver aktiv.

## QA gate

- Unity compiler uden nye errors.
- Overlay viser v00.00.13l.
- Overview er stabilt som v13k.
- Close zoom skifter til z12/z13 og er tydeligt skarpere.
- Detail tiles følger terræn uden tydelige firkanter/seams.
- Aalborg Kaserne står på land syd for Limfjorden.
- Ingen gamle city blocks eller GIS road/ferry debug-lines vises.
