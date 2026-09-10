# PROJECT 1864 Campaign v00.00.13n2 — Cesium Physics Warning Cleanup

Status: DEV / afventer lokal Unity runtime QA.

## Problem

Unity 6.6 logger gentagne advarsler fra `Physics.BakeMesh` om Cesium-terrain triangles med kanter over 500 Unity-enheder. Advarslerne opstår, fordi `Cesium3DTileset.createPhysicsMeshes` er `true` som standard og derfor opretter PhysX MeshColliders for streamede world-terrain tiles.

## Rettelse

- `CampaignCesiumDenmarkV013N.BuildCesiumWorld()` sætter nu `terrainTileset.createPhysicsMeshes = false` umiddelbart efter `Cesium3DTileset` oprettes og før ion source/asset ID sættes.
- Cesium World Terrain og Bing Maps Aerial fortsætter som render/LOD-kilder.
- Ingen globale Cesium MeshColliders bages i MAP-ONLY QA.
- Statusloggen rapporterer `PhysicsMeshes=False`.

## Hvorfor det er korrekt nu

Campaign-kortet er stadig i MAP-ONLY udviklingsfase. Kamera, georeference, imagery og terrain-rendering kræver ikke globale PhysX-colliders. Derfor giver det ingen værdi at bage grove, meget store terrain tiles til MeshCollider endnu.

Når gameplay senere kræver terrain-hit tests, unit grounding eller objektplacering, skal collision genindføres kontrolleret som lokal/high-detail collision eller via en anden bounded terrain-query løsning. Vi skal ikke genaktivere global physics-bagning på hele Cesium-verdenen uden behov.

## QA

1. Start CampaignMap i Play Mode.
2. Bekræft overlay `v00.00.13n2 CESIUM PHYSICS WARNING CLEANUP DEV`.
3. Pan og zoom over Danmark i mindst 1–2 minutter.
4. Console må ikke længere spammes med `Detected one or more triangles where the distance between any 2 vertices is greater than 500 units` fra `CesiumForUnity.Helpers.BakeMeshFromId`.
5. Cesium terrain + imagery skal stadig renderes og streames normalt.
