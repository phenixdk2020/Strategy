# PROJECT 1864 — Campaign v00.00.13g
## Denmark-only labels & close zoom

Status: DEV / clean campaign lineage

### Purpose
v00.00.13g corrects two presentation faults observed during v13f QA:
1. city names were drawn twice because both CampaignMapController and the v13f Denmark label layer rendered labels;
2. Sweden, Norway, Finland and Germany were still visible through the generic all-Europe label layer, while the current milestone is Denmark-only.

The same QA also showed that the v13f maximum zoom remained too far from the map because CampaignMapCameraController clamped against the hidden legacy CampaignTerrainV013 height floor.

### Rendering ownership
- v13f still builds the corrected filled Denmark landmesh.
- after v13f Start, v13g disables the v13f behaviour so its OnGUI/LateUpdate no longer competes with v13g.
- CampaignMapController's generic nodeLabelStyle is replaced with a transparent 1x1 style.
- v13g becomes the single visible city-label owner.

### Denmark-only rule
For this milestone, visible geographic presentation is restricted to nodes with:

`node.Region == CampaignMapRegion.Denmark`

Sweden, Norway, Finland and Germany remain in CampaignSession simulation data, but their city labels and legacy presentation objects are hidden. Flensburg and Schleswig are also hidden for now even though they remain strategically relevant to 1864; they return only when the geographic scope is deliberately expanded beyond Denmark.

### Infrastructure rule
Clean v13e links are shown only when both endpoints are Danish-region nodes. This prevents the Denmark-first QA view from leaking links into Germany or Scandinavia.

### Label LOD
- overview: primary Danish strategic cities only;
- middle zoom: primary + regional Danish cities;
- close zoom: all Danish-region nodes;
- overlap avoidance tests alternate positions around the projected node.

### Camera correction
CampaignMapCameraController now exposes:
- `UseLegacyTerrainFloor`
- `TerrainClearance`

The Denmark clean presentation sets:
- `UseLegacyTerrainFloor = false`
- `MinHeight = 8`
- `TerrainClearance = 5.5`

This removes the hidden v13 terrain from the minimum-height calculation and permits meaningful close inspection of the clean Denmark map.

### Gameplay isolation
No intentional changes to:
- campaign state
- route distance
- ETA/march time
- campaign clock
- combat
- tactical AI/navigation
- logistics

### QA gate
v13g passes its visual QA when:
- Danish cities appear once only;
- no Swedish, Norwegian, Finnish or German city labels are visible;
- Flensburg/Schleswig are hidden in the Denmark-first view;
- close zoom is substantially closer than v13f;
- corrected v13f filled Denmark landmesh remains visible;
- Aalborg barracks and Aarhus farm remain available;
- Unity compiles without errors.
