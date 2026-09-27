# PROJECT 1864 Campaign v00.00.15 — LAEA-PROJEKTION (klar til Europa)

Status: DEV / MAP-ONLY / kanal `channel-campaign5`. Batch-rendereret QA i Unity 6000.6.0f1.

## Hovedændring

Kortet skifter fra en simpel lige-bredde-projektion (lon/lat skaleret med cos(bredde)) til
**Lambert Azimuthal Equal-Area** på en kugle med centrum 52° N, 10° Ø, samme opsætning som
EPSG:3035 (ETRS89-LAEA Europe). Dermed kan samme kort senere vokse fra Danmark til Nordeuropa eller Europa
uden at forvrænge Skandinavien og Østeuropa.

## Implementeret

- `tools/map1851/build_map.py`
  - `project()` / `unproject()` (LAEA) og `box_extent()`; rasteren arbejder i projicerede km.
  - Navngivne udsnit i `REGIONS` (i dag `denmark1851`), valgt med `--region=`; `--out=` til output-mappe.
  - JSON: `projection`, `extentKm` og `bornholmKm` (km). `extent`/`bornholm` i lon/lat er bevaret som information.
  - Udsnittet `denmark1851` er nu 399 × 517 km (3164 × 4096 px, 126 m/px).
- Unity: `Map1851Projection`, `Map1851ExtentKm`; `Project()` og Bornholm-indsatsen bruger projektionen.
- Samme projektion er implementeret i Unreal-porten (`FCampaign1851Projection`).

## Ikke ændret

- Maling, byer, stednavne, UI og kamera.
- `CampaignMap` (v13n Cesium), campaign simulation og Tactical TEST.

## Næste trin (ikke startet)

Et `northern_europe`-udsnit kræver fliser/virtual textures i stedet for én tekstur samt historiske
grænser for 1851 (Det Tyske Forbund, Preussen, Østrig, Sverige-Norge m.fl.).
