# PROJECT 1864 — Designmanual addendum v00.02.20
## Cesium terrain physics under MAP-ONLY QA

Under Campaign MAP-ONLY udvikling er Cesium terrain en visuel/geospatial streaming-kilde, ikke et globalt PhysX-kollisionslandskab.

`Cesium3DTileset.createPhysicsMeshes` skal derfor være `false` på CampaignMap, så Unity ikke bager coarse world-terrain tiles med meget lange triangle edges til MeshColliders. Det reducerer runtime-støj og unødigt physics-arbejde uden at ændre terrain-rendering, imagery, LOD, georeference eller campaign simulation.

Når terrain collision senere bliver nødvendigt for units, construction placement eller ray/ground queries, skal det implementeres som en afgrænset high-detail/local collision-løsning eller en særskilt geospatial terrain-query mekanisme. Global collider generation for alle streamede Cesium tiles må ikke genaktiveres som standard.

Denne regel gælder Campaign-v00.00.13n2 og efterfølgende Cesium-baserede map-only builds, indtil terrain-interaction får sin egen gameplay-arkitektur.
