# PROJECT 1864 Campaign v00.00.13m — UNIFIED DENMARK 3D MAP

Status: DEV / MAP-ONLY / afventer Unity compile + runtime QA.

## Implementeret

- Nyt `CampaignUnifiedDenmark3DMapV013M` runtime-lag.
- En enkelt synlig map-renderer erstatter den stablede v13j/v13k/v13l-præsentation.
- Hele det validerede Danmark-envelope bygges som ét sammenhængende 520x340 mesh.
- Land følger v13j DEM-collider; vand holdes på fast sea level.
- Komplet z8 overview-atlas stitches til én texture over hele Danmark-meshet.
- Web-Mercator-korrekte UV'er bruges, så atlas og geografisk projektion følger samme kortdata.
- Missing base tiles får neutral fallback i stedet for at afsløre gammel grøn terrain-rendering.
- 5x5 viewport-detail overlays anvender z10-z13 ved medium/tæt zoom; base-atlas forbliver altid synligt under dem.
- Eksisterende interactive/premium cache genbruges hvor muligt; nye tiles gemmes i `MapCache/unified`.
- v13j/v13k/v13l scripts/renderers/status GUI deaktiveres efter v13m er klar; v13j MeshCollider beholdes som elevation reference.
- Prototype GIS-road/ferry lines, city blocks og gamle landmarks forbliver skjult.
- Aalborg Kaserne og Aarhus Gård er genoprettet som små v13m-landmarks og terrain-anchored.
- MAP-ONLY beholdes.

## QA

1. Overlay viser `v00.00.13m UNIFIED DENMARK 3D MAP DEV`.
2. Der må kun være én map-statuslinje nederst, ikke v13j/v13k/v13l samtidigt.
3. Danmark overview må ikke bestå af en lys rektangulær tile-ø oven på grøn legacy terrain.
4. Hele det validerede Danmark-kortområde skal have sammenhængende cartographic coverage.
5. Zoom/pan må ikke fremkalde huller, mens detail tiles skifter.
6. Tæt zoom skal skifte til z10-z13 detail uden at fjerne base-atlas.
7. Aalborg Kaserne og Aarhus Gård skal kun vises tæt på og ligge på land.
8. Ingen tactical-kode må være ændret.
9. Unity skal compile uden errors.
