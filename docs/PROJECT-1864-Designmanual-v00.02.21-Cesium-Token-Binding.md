# PROJECT 1864 — Designmanual addendum v00.02.21

## Cesium ion runtime credential rule

Campaign runtime må ikke stole implicit på editor-login for runtime-oprettede Cesium objects. `CesiumIonServer.defaultServer` skal bindes eksplicit, og en konkret ikke-tom token-værdi skal resolveres og sættes før ion asset IDs aktiveres.

Credential precedence er: `CESIUM_ION_TOKEN` environment variable, lokal PROJECT 1864 tokenfil under `Application.persistentDataPath`, derefter Cesium Project Default Token. Credentials må ikke commits til GitHub.

Hvis ingen token kan resolveres, må Campaign ikke sende Cesium ion requests med tom `access_token`; terrain/imagery startes ikke, og Game view skal vise en klar token-diagnose.

Cesium load failures skal eksponeres som HTTP-status og forklarende tekst i Campaign Game view under udviklings-QA.
