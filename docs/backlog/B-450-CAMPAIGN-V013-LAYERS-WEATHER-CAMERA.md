# B-450–B-459 — Campaign v00.00.13 Map Layers, Weather, Camera & Readability

## Status

**Target version:** `v00.00.13`  
**State:** PLANLAGT

## B-450 — Strategic map layer manager
**Status:** PLANLAGT

One manager controls visibility/LOD of data/render layers without changing simulation.

Core layers:
- Terrain,
- Hydrology,
- LandCover,
- Roads,
- Rail,
- Telegraph,
- Settlements,
- Facilities,
- Fortifications,
- Formations,
- Logistics traffic,
- Living world,
- Political overlay,
- Supply/logistics overlay,
- Intelligence overlay,
- Weather/atmosphere,
- Labels/UI/debug.

Acceptance:
- layers can toggle independently,
- sensible presets exist,
- visibility state can be saved as UI preference,
- disabling a layer never changes authoritative campaign state.

## B-451 — Political control and occupation overlay
**Status:** PLANLAGT

Political/military control is projected over the 3D terrain.

Acceptance:
- geography and controller remain separate data,
- overlay conforms to terrain without obscuring relief,
- occupation/contested states supported,
- opacity can be adjusted,
- no z-fighting at normal zoom.

## B-452 — Logistics and supply overlay
**Status:** PLANLAGT

3D-readable visualization of depots, supply sources, active flows and shortages.

Acceptance:
- route throughput/capacity can be color/intensity coded without changing route state,
- selected formation shows supply source/route,
- bottleneck and interruption markers supported,
- overlay respects fog/intelligence where applicable.

## B-453 — Road, rail and transport-capacity overlay
**Status:** PLANLAGT

Infrastructure mode emphasizes operational transport capability.

Acceptance:
- road class/condition,
- rail operational/damaged/under-construction,
- crossings/stations/ports,
- congestion/capacity indicators,
- troop train/freight movement can remain visible when relevant.

## B-454 — Intelligence/fog-of-war terrain overlay
**Status:** PLANLAGT

Knowledge state must remain readable on a 3D map.

Acceptance:
- unknown/suspected/contact/identified/stale states supported,
- terrain itself remains navigable even when intelligence is limited,
- last-known formation markers have age/confidence treatment,
- enemy ambient traffic does not leak hidden state,
- overlay can coexist with political/logistics modes.

## B-455 — Weather volumes and strategic weather visual
**Status:** PLANLAGT

Weather becomes spatial/temporal map state with scalable 3D visuals.

Initial set:
- cloud cover,
- rain,
- fog/mist,
- snow later,
- wind direction,
- wet/muddy state hook.

Acceptance:
- simulation weather state distinct from particle/cloud renderer,
- weather can affect movement/construction only through documented campaign modifiers,
- effects LOD/cull aggressively,
- weather does not make labels/routes unreadable by default.

## B-456 — Day/night, sun, season and atmosphere
**Status:** PLANLAGT

Campaign time affects strategic lighting and seasonal presentation.

Acceptance:
- sun/light follows campaign time approximately,
- night has readable strategic UI and restrained settlement/camp lights,
- seasonal land-cover variation supported,
- sunrise/sunset can use geographic/date hooks,
- time acceleration does not produce unstable flicker.

## B-457 — Terrain-aware 3D camera/navigation
**Status:** PLANLAGT

Camera becomes suitable for real relief and layered 3D objects.

Acceptance:
- pan/rotate/zoom,
- terrain collision/minimum height protection,
- focus selected node/formation/train/project,
- optional follow selected moving entity,
- Home/overview reset,
- camera bounds derive from map extent,
- no clipping below terrain/sea under normal control.

## B-458 — Depth-aware labels, selection and route preview
**Status:** PLANLAGT

2D UI must remain stable over 3D terrain.

Acceptance:
- labels anchor to 3D positions but remain screen-readable,
- occlusion/prioritization rules prevent label walls,
- selected route can conform visually to terrain/infrastructure,
- hover/selection uses appropriate raycast layers,
- major cities/selected targets receive priority.

## B-459 — Readability presets and layer-combination QA
**Status:** PLANLAGT

Provide player presets such as:
- Political,
- Military,
- Logistics,
- Infrastructure,
- Intelligence,
- Economy/Construction,
- Clean/Immersive.

Acceptance:
- each preset has documented layer configuration,
- player can customize afterward,
- important selected-object information survives preset changes,
- terrain relief remains legible,
- QA checks common overlay combinations for z-fighting, clutter and severe frame loss.