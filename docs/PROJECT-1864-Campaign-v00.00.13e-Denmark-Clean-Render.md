# PROJECT 1864 — Campaign v00.00.13e Denmark Clean Render Foundation

**Status:** DEV / IMPLEMENTERET / AFVENTER UNITY QA  
**Branch:** `work/v00.00.13e-denmark-clean-render`  
**Runtime:** `Assets/Scenes/CampaignMap.unity`

## Formål

v00.00.13e stopper den tidligere strategi med at lægge nye grafiske reparationslag oven på de gamle v13-renderlag. Danmark-QA får i stedet én isoleret render-foundation, mens den autoritative campaign-simulation, nodes, formationer, tid, ETA og rutedata bevares uændret.

Renderarkitekturen i denne QA-slice er:

`CLEAN SEA -> DIRECT LAT/LON DENMARK -> CLEAN INFRASTRUCTURE -> CLEAN SETTLEMENTS -> FORMATIONS/CONSTRUCTION -> LABELS`

## E-01 — Legacy render suppression

Alle renderere under de gamle campaign-lag L1 Terrain, L2 Hydrology, L3 LandCover, L4 Infrastructure, L5 Settlements og L8 Overlays skjules under v13e Denmark QA. De gamle objekter slettes ikke fra simulationen; de er blot ikke længere en del af den synlige Denmark render-path.

Følgende ældre presentation helpers deaktiveres i v13e QA: v13a visual polish, v13b visual polish, v13c Denmark cleanup, v13d Denmark rebuild/gate, v11 usability/search presentation og v13 layer panel.

## E-02 — Clean sea

Den gamle `Campaign Sea Base` renderer skjules. v13e bygger en ny tynd havflade centreret omkring Danmark, stor nok til at fylde Denmark-kameraets QA-view uden de synlige tykke kube-/kortkanter fra de tidligere builds.

## E-03 — Direct Denmark geography

Natural Earth 1:50m Denmark-ringe læses som longitude/latitude og projiceres direkte med `CampaignGeoProjection.Project3D(latitude, longitude, height)`. Den gamle lokale Denmark-projektion og efterfølgende remap er ikke en del af v13e.

Jylland, Fyn, Sjælland, Lolland/Falster/Møn, Langeland, Amager, Ærø, Als, Samsø, Læsø, Bornholm og øvrige ringe i den eksisterende Denmark-datakilde renderes som separate landdele.

## E-04 — Low-relief Denmark

Danmark bruger en meget lav amplitude presentation-height. Formålet er at få et troværdigt fladt/let bølget dansk landskab uden klippevægge eller plateauer. Dette er ikke en DEM-model og må ikke bruges som autoritativ elevation.

## E-05 — Clean infrastructure

De gamle strategic-link renderere skjules. v13e bygger nye road/rail/ferry lines kun mellem aktive Denmark-focus nodes. Højden beregnes fra samme v13e Denmark-surface; sea-ferry renderes ved vandniveau.

Strategiske afstande og ETA ændres ikke.

## E-06 — Clean settlements

De gamle `Settlement3D_*` renderere skjules. v13e bygger mindre by-/fort-ikoner direkte ved node-positionerne. Underliggende `CampaignNode_*` colliders bevares til selection/raycast, men deres gamle renderere skjules.

## E-07 — Construction continuity

`QA-BARRACKS-AALBORG` og `QA-FARM-AARHUS` bevares som staged campaign-time construction. De groundes og skaleres på den nye Denmark presentation-surface.

## E-08 — Formation presentation

Campaign formations beholder autoritativ state og movement. Kun formationer i Denmark-focus renderes i denne QA-slice. Deres Y-position groundes efter de ældre compatibility helpers, så v13e presentation vinder visuelt.

## E-09 — Label LOD

Legacy all-node labels skjules. v13e label-Lod:

- overview/høj kamera: primære danske byer
- mellemzoom: primære + sekundære danske locations
- close zoom: alle Denmark-focus nodes

Labels anvender simpel overlap avoidance.

## E-10 — Denmark camera

Start/Home strammes ind omkring Danmark. `CampaignMapCameraController` beholder styringen, men dens interne Home transform opdateres, så Home ikke hopper tilbage til det gamle brede Europa-view.

## Acceptance gate

v13e kan kun betragtes som bestået Denmark-render QA når:

1. ingen gigantiske grønne rektangulære terrain slabs er synlige,
2. Danmark er umiddelbart genkendeligt ved start/Home,
3. havet fylder baggrunden uden synlige tykke map-cube kanter,
4. danske settlements ligger på de rigtige landdele,
5. road/rail/ferry graphics følger de synlige danske nodes,
6. labels er læsbare på standardzoom,
7. Aalborg-kasernen og Aarhus-farmen fortsat eksisterer og er groundet,
8. tactical PrototypeBattle-indhold ikke findes på CampaignMap,
9. campaign movement/ETA/time ikke er ændret af renderpasset,
10. Unity compiles uden errors.

## Ikke i scope

- autoritativ DEM/elevation,
- endelige historiske 1864 road/rail polylines,
- vegetation/land-cover polish,
- endelige bymodeller,
- strategic AI/logistics/research/diplomacy,
- tactical AI/navigation.
