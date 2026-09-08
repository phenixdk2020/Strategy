# PROJECT 1864 — Campaign v00.00.13 Layered 3D Map Architecture

## Decision

The strategic map will become a true layered 3D world. `Layer` here means primarily a **data/render architecture layer**, not automatically a Unity physics layer. Only categories that need separate raycasts, collision masks, camera culling or selection should consume Unity LayerMask slots.

The terrain is one common geospatial surface. Roads, rail, rivers, settlements, units and overlays are placed onto/above that same coordinate system. We do not build the map as eleven unrelated flat planes.

## Layer stack

| Layer | Purpose | Authoritative? | Typical representation |
|---|---|---|---|
| L0 Geospatial reference | lat/lon, projection, map extent, route geometry | Yes | data/service only |
| L1 Elevation/Terrain | true elevation + render relief | terrain data authoritative; render Y derived | chunked mesh/heightfield |
| L2 Hydrology | sea, coastline, rivers, lakes, wetlands | data authoritative | water meshes/splines/polygons |
| L3 Land cover | forest, agriculture, grass, urban, marsh, rock | classification authoritative where gameplay uses it | terrain materials/instances |
| L4 Infrastructure | roads, rail, bridges, tunnels, ferry, telegraph | Yes | splines + assets |
| L5 Settlements/Facilities | cities, farms, depots, barracks, ports, stations, forts | Yes | footprints + semantic 3D assets |
| L6 Strategic Entities | formations, trains, ships, convoys, projects | Yes | state-driven visual entities |
| L7 Living World | workers, camp life, traffic, smoke, animals, aftermath | No; derived from state | pooled/LOD ambient visuals |
| L8 Strategic Overlays | political, logistics, infrastructure, intelligence | No; derived from state | terrain-conforming overlays/lines/icons |
| L9 Atmosphere | day/night, weather, clouds, fog, wind | weather/time state may be authoritative; renderer derived | lights/volumes/particles |
| L10 UI/Debug | labels, selection, route preview, ruler, diagnostics | No | screen-space/world-space UI |

## Suggested Unity physics/render layer use

Do not map every architecture layer 1:1 to Unity layers. A practical first allocation is:

- `CampaignTerrain`
- `CampaignWater`
- `CampaignInfrastructure`
- `CampaignSettlement`
- `CampaignStrategicEntity`
- `CampaignAmbient`
- `CampaignOverlay`
- `CampaignInteraction`

UI remains normal UI. Layer masks should primarily simplify picking/culling and prevent decorative workers/smoke from stealing clicks intended for a formation, city or route.

## Map-scale contract

The current prototype projection covers 47.0–71.5 N and 4.0–32.5 E but displays it in approximately 520 x 620 Unity units. This is presentation space only.

Rules:

1. Latitude/longitude are canonical for geographic identity.
2. Explicit route polylines are canonical for road/rail/ferry route geometry.
3. `DistanceKm` is computed from geographic route geometry, never Unity `Vector3.Distance`.
4. Unity scale can change for readability without changing ETA.
5. Geographic projection can be replaced behind the coordinate service without rewriting campaign state IDs.
6. Vertical exaggeration is visual; true elevation remains separately stored.

## 3D elevation

At strategic scale, true relief in Denmark/northern Germany would appear very flat compared with Norway. Therefore rendering may apply configurable vertical exaggeration. Example modes can later be tested such as 1x / 2x / 4x, but no multiplier is canon until visual QA.

Movement uses real slope/route data. A visually exaggerated hill must not create an artificial movement penalty.

## Route geometry

Roads and rail should be stored as geographic polylines. Rendering converts them into terrain-conforming 3D splines. This gives one route definition to support:

- accurate route kilometers,
- march ETA,
- rail train position,
- convoy position,
- bridge/crossing attachment,
- construction progress along an edge,
- congestion/interdiction segments,
- selected-route highlighting.

For each route, normalized progress `0..1` maps simulation progress to a precise 3D position on the visual spline.

## Terrain interaction

- Roads generally drape/follow terrain but can use visual grading/cuts.
- Rail should use stricter grade constraints and may require cuts/embankments/bridges.
- Rivers are terrain/hydrology data; bridge objects reference specific crossing state.
- Ports require coastline/sea access.
- Settlements use footprint zones instead of one generic marker.
- Fortifications and facilities are placed relative to real settlement/terrain state.

## Living-world rule

Living-world animation is subordinate to campaign state. Examples:

- a moving troop train corresponds to a real `RailTransit` state,
- a supply wagon can correspond to a real convoy,
- a construction crew corresponds to an active project,
- a camp corresponds to a stationary formation state,
- smoke/aftermath corresponds to timestamped battle/damage state.

Ambient entities may disappear because of LOD/culling without affecting the underlying campaign.

## Strategic overlay rule

Overlays must conform to terrain while preserving relief readability. Political color should tint rather than replace terrain. Logistics and intelligence overlays should emphasize routes/nodes without turning the world into opaque flat panels.

Overlays can use small vertical/depth offsets, projected decals or terrain-aware shaders to avoid z-fighting.

## Weather and time

Campaign time/weather can influence simulation through explicit modifiers, while visual effects remain scalable. Examples:

- campaign rain state -> road/mud modifier through simulation data,
- rain particles/clouds -> presentation only,
- campaign night/time -> march/night policy and visibility rules where designed,
- lighting -> presentation derived from the same time.

## Camera

The 3D map camera must support:

- rotate/pitch,
- pan,
- strategic zoom from near diorama to continental overview,
- terrain-aware minimum height,
- focus/follow formation, train, city or project,
- layer-aware picking,
- semantic LOD transitions,
- overview/Home reset.

## Precision/streaming

Geographic state must not depend on one huge Unity coordinate plane. The design permits chunk-local coordinates and later floating-origin behavior. Save data stores geographic/route progress state, not floating-origin-adjusted render transforms.

## Acceptance direction

The 3D map is successful when a player can visually follow a formation marching over terrain or a troop train traveling station-to-station while the ETA remains identical if the visual map scale, vertical exaggeration or rendering detail is changed.