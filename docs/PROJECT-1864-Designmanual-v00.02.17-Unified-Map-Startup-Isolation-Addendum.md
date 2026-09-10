# PROJECT 1864 Designmanual Addendum v00.02.17

## Unified map startup isolation

CampaignMap må kun have én synlig kort-owner ad gangen. Ældre campaign-map systemer må gerne leve kortvarigt som interne datakilder under initialisering, men deres renderers, labels og GUI må ikke være synlige.

For v00.00.13m1 gælder:

- v13m er eneste synlige map renderer.
- v13j bruges kun som midlertidig DEM/MeshCollider-kilde og må aldrig være synlig.
- v13k/v13l må ikke vise tiles, status eller labels.
- v13m base-surface må ikke vises uden et gyldigt stitched basemap-atlas.
- mens atlas bygges, vises kun neutral loading-state.
- legacy renderers fra v13a-v13l samt GIS/prototype presentation holdes skjult.
- build/version-overlay placeres under top-controls for at undgå overlap.

Dette ændrer ikke simulation, lat/lon, distance, ETA, campaign time, construction state, logistics eller tactical kode.
