# PROJECT 1864 Campaign v00.00.13l — PREMIUM CARTOGRAPHIC VISUAL

Status: DEV / MAP-ONLY / afventer Unity compile + runtime QA.

## Implementeret

- Nyt `CampaignPremiumCartographicVisualV013L` presentation-layer.
- v13k overview map bevares.
- z12/z13 high-detail cartography ved tæt zoom.
- 32x32 conforming mesh pr. close-detail tile.
- Trilinear + anisotropic filtering.
- Premium daylight, soft shadows og restrained atmospheric fog.
- Kamera tættere på terræn: MinHeight 0.14 / clearance 0.09 / near clip 0.003.
- Gamle GIS road/ferry lines og prototype city blocks holdes skjult.
- v13k landmark root skjules.
- Ny mindre Aalborg Kaserne med pitched roofs, parade ground, fence, trees og Dannebrog.
- Ny mindre Aarhus Gård med farmhouse, barn, field, fence og trees.
- Landmarks kun ved tæt zoom.
- High-detail requests er fortsat kun 3x3 viewport og caches 14 dage.
- High-detail OSM slås fra, hvis lokal historical raster eller custom provider.json findes.

## Ikke ændret

- Strategic lat/lon.
- Route distance/ETA.
- Campaign time.
- Logistics.
- Battle/contact gate.
- Tactical AI/navigation.

## QA

1. Ingen nye compile errors.
2. Overlay: `PROJECT 1864 CAMPAIGN | v00.00.13l PREMIUM CARTOGRAPHIC VISUAL DEV`.
3. Overview fungerer som v13k.
4. Ved tæt zoom bliver kortet tydeligt skarpere med z12/z13.
5. Tile-mesh følger terræn uden store synlige flader/facetter.
6. Ingen brun prototypevej over vand.
7. Aalborg Kaserne ligger på land syd for Limfjorden og er markant mindre end tidligere prototype.
8. Aarhus Gård er lille og close-zoom-only.
9. Ingen duplicate city labels/prototype city blocks.
10. MAP-ONLY forbliver aktiv.
