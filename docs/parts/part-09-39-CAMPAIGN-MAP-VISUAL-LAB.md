# PROJECT 1864 — Campaign Map Visual Lab v00.00.10f

## Formål
Denne version lægger et kontrolleret **11-kandidaters map visual lab** oven på den rene Campaign v00.00.10e-baseline. Alle kandidater bruger samme Danmark, WGS84-projektion, byankre, 12 gameplay-zoner, kampagneur, QA-formation og bevægelsesstate. Formålet er derfor at sammenligne **præsentation og læsbarhed**, ikke forskellige simulationer.

## Branch og baseline
- Aktiv testbranch: `channel-campaign3`
- Baseline: `backup/channel-campaign-v10e-before-mapstyle-20260911`
- Baseline commit: `07fec17980fad5709ce8d7e79b17d71714d108b0`
- Tidligere Campaign3 v13n3 er bevaret uændret på `backup/channel-campaign3-v13n3-before-maplab11-20260911`.

## De 11 kandidater
| # | Kandidat | v10f-preview |
|---|---|---|
| 1 | Dansk højdemodel -> eget Unity 3D Terrain | Naturlig palette, skråt kamera, tydeligt syntetisk preview-relief |
| 2 | Cesium World Terrain + egne 1851-lag | Højere relief, køligere atmosfære, diskrete markører |
| 3 | ArcGIS Maps SDK + historiske lag | GIS-look, koordinatnet, ren kartografisk læsbarhed |
| 4 | MapTiler 3D Terrain + Cesium | Terrain/vector-hybrid, tile-grid og moderat relief |
| 5 | Historisk 1850-kort draperet over 3D-relief | Sepia/papirkort art direction med let relief |
| 6 | QGIS/Blender -> terrain tiles til Unity | Kraftigere baked-terrain-look og tile-grid |
| 7 | DEM + procedural marker/skov/hede/by | Gameplay-fokuseret terræn med stærke strategiske markører |
| 8 | OpenStreetMap-geometri + historisk korrektion | Fladere geometrisk kortstil, grid og netværkslæsbarhed |
| 9 | Hybrid satellit/terrain-look + malet 1851-overflade | Mørkere, dramatisk painterly terrain-preview |
| 10 | Håndbygget modeljernbane/diorama-stil | Kraftigt relief, varm belysning og større fysiske markører |
| 11 | HOI4-lignende polygon/regionkort 2D/2.5D | Næsten fladt strategikort med høj kontrast og tydelige adjacency-links |

## Implementering
`Assets/Scripts/Campaign/CampaignMapStyleSwitcher.cs` auto-oprettes efter scene-load og finder den eksisterende v10e-geografi. Scriptet tager snapshots af landmeshes og markørstørrelser og anvender derefter den valgte map-profil uden at ændre campaign-state.

Profilerne kan ændre:
- orthographic kamera-vinkel og zoom
- syntetisk Perlin-baseret preview-relief på eksisterende Danmark-mesh
- land-, hav-, kyst-, zone-, by- og army-farver
- ambient/sun/fog
- kystlinjens tykkelse
- zone- og bymarkørstørrelse
- koordinat-grid, tile-grid og gameplay-adjacency overlays

## Kontroller
- Knapper nederst i Game view: vælg kandidat 1–11
- `M` eller `]`: næste kandidat
- `[`: forrige kandidat
- `Shift+F1` ... `Shift+F11`: direkte valg

## Datakvalitet og historiske guardrails
Vigtigt: kandidater 1–10 er **visuelle previews**. Denne version importerer ikke en rigtig dansk DEM, Cesium World Terrain, ArcGIS, MapTiler, OSM, satellitbilleder eller et historisk 1850/1851 rasterkort. Det syntetiske relief er kun art-direction til sammenligning.

Natural Earth 1:50m fra v10e er fortsat det geografiske fundament. De eksisterende v10e-zonecentre er gameplay-scaffolding og ikke historiske administrative grænser. Byernes koordinater er geografiske ankere; historiske befolkningstal, vejnet, jernbane, havne, industri og befæstning skal fortsat komme fra kildeunderbyggede 1851-data.

## QA-gate
1. Unity 6000.6.0f1 skal compile uden errors.
2. Alle 11 knapper/profiler skal kunne vælges i Play mode.
3. Kamera, markører, kyst og relief skal skifte tydeligt mellem profilerne.
4. Venstreklik-selection, højreklik-marchordre og kampagneur skal fortsat fungere.
5. Test mindst profil 1, 5, 7, 10 og 11 med både Close/Operational/Strategic zoom.
6. Der må ikke opstå ændringer i campaign-data eller WGS84-identiteter ved profilskift.
7. Først efter visuel QA udvælges 2–3 kandidater til egentlig DEM/GIS/historisk data-integration.

## Versionering
- v00.00.10e: ren world/geography baseline.
- v00.00.10f: 11-kandidaters Campaign Map Visual Lab på `channel-campaign3`.
