# PROJECT 1864 Campaign v00.00.13n3 — CESIUM TOKEN BINDING + DIAGNOSTICS

Status: DEV / MAP-ONLY / afventer Unity runtime QA.

## Fejl fundet i v13n2

Unity Console viste Cesium ion requests til asset 1 og asset 2 med en tom query parameter:

`.../endpoint?access_token=`

Begge requests returnerede HTTP 401. Cesium editor-login var aktivt, men de runtime-oprettede Cesium-komponenter modtog ikke en brugbar token-værdi.

## Rettelse

- `CesiumIonServer.defaultServer` bindes eksplicit til både `Cesium3DTileset` og `CesiumIonRasterOverlay`.
- Token resolveres før asset-ID 1/2 aktiveres.
- Token-prioritet: `CESIUM_ION_TOKEN` environment variable -> lokal PROJECT 1864 tokenfil -> Cesium Project Default Token.
- Den resolverede token sættes eksplicit på terrain og aerial overlay før første ion-request.
- Hvis token mangler, startes asset requests slet ikke; Game view viser `TOKEN MANGLER` i stedet for at generere 401-spam.
- Den lokale token-editor kan nu importere Cesium Project Default Token direkte til PROJECT 1864s lokale tokenfil.
- Terrain- og imagery-load failures vises med HTTP-code/message direkte i Game view.
- Cesium physics meshes forbliver deaktiveret under MAP-ONLY QA.

## QA

1. Ingen requests må indeholde `access_token=` med tom værdi.
2. Game view skal vise token-kilde som `Cesium Project Default Token`, `lokal ion-token` eller `CESIUM_ION_TOKEN env`.
3. Asset 1 skal kunne streame Cesium World Terrain.
4. Asset 2 skal kunne streame Bing Maps Aerial.
5. Ved 401/403 skal Game view vise konkret terrain/aerial HTTP-diagnose.
6. Tactical TEST-kode og campaign simulation må ikke ændres.
