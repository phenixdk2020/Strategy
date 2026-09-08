# B-440–B-449 — Campaign v00.00.13 Strategic Entities & Living World in 3D

## Status

**Target version:** `v00.00.13`  
**State:** PLANLAGT  
**Depends on:** v00.00.12 simulation state + B-430 infrastructure geometry.

## B-440 — Semantic 3D formation representation
**Status:** PLANLAGT

Replace one-size formation cubes with zoom-aware strategic representations.

Acceptance:
- far zoom: clear symbol/icon,
- medium zoom: formation base/flag/compact representation,
- near zoom: optional small representative marching/camped group,
- representation never changes authoritative strength/state,
- stacking and selection remain usable.

## B-441 — Marching-column visual along route geometry
**Status:** PLANLAGT

A moving formation visually follows the actual road/route spline.

Acceptance:
- normalized movement progress maps to route geometry,
- visual heading follows path tangent,
- column may be visually elongated at near zoom,
- B-400 mobility/ETA controls progress,
- normal vs forced march can have distinct but restrained visual state,
- animation may cull without pausing movement simulation.

## B-442 — Troop train full-route 3D visual
**Status:** PLANLAGT

B-381 troop-train state gets a 3D train that follows the whole rail route.

Acceptance:
- train appears after train assembly/loading lifecycle reaches departure,
- follows each rail spline segment continuously,
- stops at simulated waits/congestion/disruption,
- formation is marked `RailTransit` rather than simultaneously marching on land,
- arrival/unloading hides/releases train visual only after state transition,
- camera can optionally follow selected troop train.

## B-443 — Freight/supply train visual
**Status:** PLANLAGT

Freight rail flow may be represented by state-driven trains.

Acceptance:
- train corresponds to real transfer/allocated capacity where available,
- freight and troop trains share rail congestion/capacity hooks,
- visual cargo category can be abstracted,
- empty/repositioning stock can optionally be represented,
- no visual train creates resources or capacity.

## B-444 — Ships, ferries and sea-transport movement
**Status:** PLANLAGT

State-driven vessels follow sea/ferry routes.

Acceptance:
- vessel visual tied to transport state,
- embark/transit/disembark lifecycle respected,
- port approach/departure points used,
- blockade/disruption can stop/reroute appropriately,
- far zoom reduces vessels to cheap symbols.

## B-445 — Wagon convoys, couriers and scouts
**Status:** PLANLAGT

Road network gains selective moving state visuals.

Acceptance:
- supply wagon represents convoy state when applicable,
- courier/scout visuals respect fog of war,
- route progress follows actual state,
- no thousands of individual agents required,
- near zoom uses pooling and random phase offsets for natural movement.

## B-446 — Camps, bivouacs and stationary army life
**Status:** PLANLAGT

Stationary formations can develop a camp state visually over time.

Acceptance:
- camp requires minimum stationary duration/state,
- tent/fire/horse/wagon density derived from formation class/size within budget,
- camp disappears/reduces after movement starts,
- camp does not grant bonuses unless separate simulation state says so,
- nighttime lighting is LOD controlled.

## B-447 — Construction crews and staged 3D work sites
**Status:** PLANLAGT

B-360 construction state gains terrain-aware 3D worksites.

Acceptance:
- barracks/farm/road/rail/depot/fortification stages supported,
- workers/materials reflect active progress,
- road/rail crews can move progressively along an edge,
- pause stops campaign progress while cosmetic idle animation may freeze or use approved presentation behavior,
- completed asset reconstructs from save without replaying all construction.

## B-448 — War aftermath, medical and civilian visual state
**Status:** PLANLAGT

Campaign events leave temporary 3D traces.

Examples:
- smoke/fire after battle or destruction,
- damaged wagons/infrastructure,
- hospital/ambulance activity,
- refugee/civil displacement indicators,
- occupation checkpoints/flags.

Acceptance:
- visual state is derived from timestamped authoritative event/state,
- lifetime/fade rules bounded,
- fog-of-war/intelligence rules respected,
- visuals do not expose hidden enemy losses/positions.

## B-449 — Strategic entity/living-world 3D QA gate
**Status:** PLANLAGT

Acceptance:
1. marching formation follows route geometry,
2. troop train follows full rail route and lifecycle,
3. freight train/convoy can represent actual logistics transfer,
4. ship/ferry follows sea state,
5. camps/worksites reconstruct from campaign state,
6. living-world visuals obey zoom/LOD/culling,
7. fog of war prevents hidden-state leakage,
8. disabling all ambient visuals leaves identical simulation results.