# PROJECT 1864 — Designmanual supplement — Campaign Map Visual Lab v00.00.10f

## 39. Campaign-map visual comparison

### 39.1 Formål

Før den endelige grafiske pipeline for Grand Campaign låses, skal PROJECT 1864 kunne sammenligne flere kortretninger direkte i Unity på **samme geografi og samme simulation state**. Visuel sammenligning må ikke forveksles med forskellige datamodeller.

Den eksisterende v00.00.10e Danmark-baseline bevares som fælles fundament:

- WGS84 longitude/latitude er geografisk identitet.
- Natural Earth 1:50m er foreløbig fysisk kyst-/landgeometri.
- Byer bruger geografiske koordinater.
- 12 aktuelle gameplay-zonecentre er scaffolding, ikke historiske provinsgrænser.
- Hær-ID, bevægelse, kampagneklokke og selection/order state er fælles på tværs af alle visual styles.

`CampaignMapStyleSwitcher.cs` er et isoleret visual-lab lag. Det ændrer ikke kampagnedata eller historiske claims.

### 39.2 De 11 kandidater

| # | Kandidat | Visuel intention | Endelig pipeline kræver |
|---|---|---|---|
| 1 | Dansk DEM → Unity Terrain | Naturtro dansk relief, marker, skov og fysisk landskab | Dansk DEM/elevation, terrain tiles/materialer |
| 2 | Cesium World Terrain | Global real-world terrain/aerial retning | Cesium for Unity + valgte terrain/imagery sources |
| 3 | ArcGIS Maps SDK | Professionel GIS-læsbarhed med historiske overlays | ArcGIS SDK + egne kildebyggede 1851-lag |
| 4 | MapTiler 3D Terrain | Klar moderne map-rendering med god global skalering | MapTiler terrain/tiles + styling/integration |
| 5 | Historisk kort på 3D-relief | 1850'er militært stabskort draperet over relief | Source-backed historisk raster/vector + DEM |
| 6 | QGIS/Blender → Unity | Håndkontrolleret high-end baked terrain | Offline GIS/Blender pipeline + baked meshes/textures |
| 7 | Procedural living world | Levende 3D-verden med marker, skove, gårde og byer | Procedural generators + historiske land-use inputs/assets |
| 8 | OpenStreetMap foundation | Ren, informationsstærk geometri som arbejdsgrundlag | OSM/import pipeline + omfattende historisk korrektion |
| 9 | Hybrid satellite-look + painted 1851 | Fotorealistisk terrain-feel, men tidskorrekt indhold | Base terrain + håndmalet/historisk 1851 surface pipeline |
| 10 | Modeljernbane / diorama | Fysisk miniature-verden med høj identitet | 3D asset library, vegetation, byer, landmarks, LOD |
| 11 | HOI4-lignende 2D/2.5D | Maksimal strategisk læsbarhed og performance | Historiske region/provinspolygoner, political map shader, overlays |

### 39.3 Runtime comparison controls

Alle 11 kandidater kan afprøves i samme campaign-runtime:

- Klik på kandidatknapper nederst i skærmen.
- `M` eller `]` går til næste kandidat.
- `[` går til forrige kandidat.
- `Shift+F1` til `Shift+F11` vælger kandidat direkte.

Der anvendes `Shift+F#` i stedet for bare F-tasterne for ikke at kollidere med taktiske testbindings som F6/F7/F8.

### 39.4 Hvad visual lab må ændre

En kandidat må i v00.00.10f ændre:

- kameraets pitch/position og orthographic size,
- hav-, land- og kystpalette,
- coastline width,
- belysning og ambient light,
- fog/atmosfærisk retning,
- by- og zone-marker emphasis,
- material gloss/smoothness hvor shaderen understøtter det,
- strategisk kontrast og generel art direction.

### 39.5 Hvad visual lab IKKE må ændre

Skift af kandidat må ikke ændre:

- WGS84-koordinater,
- landgeometri eller geografisk identitet,
- city coordinates,
- zone IDs eller adjacency,
- formation IDs,
- army strength/state,
- march orders/progress,
- campaign date/time,
- owner/control data,
- historiske data eller simulation rules.

### 39.6 Historisk og teknisk integritet

En preview-style må aldrig fremstå som om et manglende datalag allerede er historisk korrekt. UI viser derfor eksplicit PREVIEW-status for kandidater, der endnu mangler eksempelvis real DEM, Cesium, ArcGIS, MapTiler, historisk raster eller kildebyggede provinsgrænser.

Natural Earth-baselinen er fysisk/geografisk scaffold og er ikke bevis for 1851-politiske grænser. OpenStreetMap kan tilsvarende bruges som teknisk/geometrisk hjælpegrundlag, men moderne infrastruktur må ikke indlæses som 1851-historik uden kildekritisk korrektion.

### 39.7 Udvælgelsesgate

Før én retning vælges som primær pipeline, skal hver kandidat vurderes på:

1. Visuel kvalitet ved Strategic, Operational og Close zoom.
2. Læsbarhed af byer, enheder, ejerskab, supply og transportlag.
3. Mulighed for historisk 1851-korrekt data.
4. Performance i Unity 6.6.
5. Danmark → Europa → global skalering.
6. Asset-/dataarbejde pr. ny region.
7. Modding- og savegame-kompatibilitet.
8. Evne til at kombinere med semantic zoom.

Det er tilladt, og sandsynligvis ønskeligt, at slutløsningen bliver en **hybrid**: eksempelvis rigtig DEM/terrain i Close/Operational zoom kombineret med en mere HOI4-lignende political/strategic visualisation ved stor udzoomning. Simulation state skal fortsat være den samme under alle zoom- og render modes.

### 39.8 Acceptance for v00.00.10f

Builden kan først markeres runtime-valideret når Unity 6000.6.0f1 har:

- kompileret uden nye errors,
- startet Grand Campaign,
- vist alle 11 kandidatknapper,
- skiftet igennem alle 11 uden exception,
- bevaret samme valgte formation/zone og campaign time under skift,
- bevaret selection og marchordre efter mindst ét visual-style skift,
- vist build marker `v00.00.10f CAMPAIGN MAP VISUAL LAB`.

v00.00.10f vælger **ikke** en vinder. Formålet er at gøre valget visuelt og teknisk sammenligneligt i den rigtige Unity-runtime.
