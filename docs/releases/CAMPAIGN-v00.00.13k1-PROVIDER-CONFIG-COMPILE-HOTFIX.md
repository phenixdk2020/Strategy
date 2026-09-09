# PROJECT 1864 Campaign v00.00.13k1 — Provider Config Compile Hotfix

Status: DEV / MAP-ONLY / afventer Unity compile + runtime QA.

## Problem

Unity compile fejlede i `CampaignLiveCartographicDrapeV013K.cs` med `CS0103` fordi `JsonUtility` ikke var tilgængelig i den aktuelle Unity 6.6 modulkonfiguration.

## Rettelse

- Tilføjet `JsonUtilityCompatibilityV013K1.cs`.
- Den minimale reader håndterer kun string-felter, hvilket er tilstrækkeligt til `ProviderConfig` (`UrlTemplate`, `Attribution`, `UserAgent`).
- Ingen ekstra Unity JSON-module dependency kræves.
- Live cartographic drape, viewport tiles, cache, camera, MAP-ONLY og simulation isolation er uændret.

## QA

1. Ingen `CS0103 JsonUtility` compile error.
2. Overlay viser `v00.00.13k1 LIVE CARTOGRAPHIC 3D DRAPE HOTFIX DEV`.
3. Default OpenStreetMap provider virker uden `provider.json`.
4. En lokal `provider.json` med de tre string-felter kan fortsat override provider-indstillinger.
5. Ingen tactical eller campaign simulation-adfærd er ændret.
