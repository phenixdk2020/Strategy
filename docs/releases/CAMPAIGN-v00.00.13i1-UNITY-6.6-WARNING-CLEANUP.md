# CAMPAIGN v00.00.13i1 — Unity 6.6 Warning Cleanup

Status: DEV / afventer lokal Unity compile/runtime QA.

## Formål

Rydde de gule CS0618-deprecation warnings fra Unity 6.6 væk fra den aktive GIS campaign-build uden at ændre gameplay, GIS-terrain, MAP-ONLY-gate eller tactical TEST-adfærd.

## Rettet direkte

- `CampaignMapOnlyModeV013H1`: `Object.FindObjectsByType<T>(FindObjectsSortMode.None)` -> `Object.FindObjectsByType<T>()`.
- `CampaignDenmarkV013DLegacyGate`: samme Unity 6.6 API-migration.

`FindObjectsSortMode.None` bad allerede om usorteret resultat, så den nye overload bevarer den relevante adfærd uden deprecated API.

## Legacy presentation-lag

v13c/v13d/v13e og andre ældre presentation-filer ligger stadig i projektet af kompatibilitetsgrunde, men er deaktiveret af v13i GIS-foundation. De indeholder fortsat gamle Unity API-kald. For at holde Console ren under GIS-QA anvender campaign-branchen midlertidigt `Assets/csc.rsp` med `-nowarn:0618`.

Dette er en midlertidig migration-policy, ikke en permanent erstatning for at fjerne/migrere gamle legacy-lag. Når GIS-foundation er stabil, skal overflødige legacy presentation-scripts udfases og compiler suppression fjernes igen.

## QA

- Ingen CS0618 `FindObjectsSortMode` warnings fra de viste campaign-filer efter Unity recompile.
- Ingen nye compile errors.
- v13i GIS terrain og MAP-ONLY fungerer uændret.
