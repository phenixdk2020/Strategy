# CAMPAIGN v00.00.13b — Visual Polish Pass 1

Branch: `work/v00.00.13b-campaign-visual-polish`

State: **IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA**

This build is a presentation-only campaign pass. It does not intentionally alter strategic movement, ETA, route distance, tactical navigation or Officer AI.

## Implemented

1. Terrain color pass — darker/naturalized strategic terrain plus deterministic Denmark field patches.
2. Water/coastline pass — darker smoother sea and stronger land/water contrast; v13a detailed Denmark coastline retained.
3. Denmark recognition — Denmark-first Home/default camera retained and Natural Earth 1:50m Denmark remains the detailed focus area.
4. Road/Rail visual pass — road, rail and sea-ferry links receive distinct materials/widths; route ghost reduced.
5. Settlement visual pass — differentiated town ground, urban civic/church dressing, port quay/crane, rail platform/awning and depot stacks.
6. Construction art pass — Aalborg barracks and Aarhus farm receive worked-earth pads, timber/stone material stacks, carts, scaffold/detail props and richer completed-site dressing.
7. Worker animation polish — workers gain head/cap/tool dressing and a calmer non-spinning motion loop.
8. Vegetation pass — grouped deterministic tree clusters with varied crown silhouettes around Denmark nodes.
9. Lighting/atmosphere pass — softer shadows, warmer daylight transition and improved fog/ambient balance.
10. UI readability pass — legacy tactical build marker suppressed, strategic overlays remain OFF by default, line weights reduced, existing F5/F3 collapsed UI defaults retained.

## Existing construction QA retained

- `QA-BARRACKS-AALBORG` — starts 1864-02-01 10:00, duration 96 campaign hours.
- `QA-FARM-AARHUS` — starts 1864-02-01 18:00, duration 72 campaign hours.
- Both retain six visible stages from site layout to complete building.

## Unity QA checklist

- Compile in Unity 6000.6.0f1 with 0 errors.
- Campaign opens `CampaignMap`, never PrototypeBattle automatically.
- Overlay reads `PROJECT 1864 CAMPAIGN | v00.00.13b VISUAL POLISH DEV`.
- Home/default camera starts over Denmark.
- Denmark coastline and islands are recognizable.
- Terrain/sea palette is visually calmer than 13a.
- Roads, rails and ferry links are distinguishable.
- Settlement dressing appears without blocking node selection.
- F8 Living World ON shows Aalborg barracks and Aarhus farm construction.
- Campaign time advances construction stages; pause stops campaign-time progress.
- Workers no longer spin continuously and display head/tool details.
- Vegetation appears in grouped clusters and decorative colliders do not intercept selection.
- No tactical TEST AI/navigation changes are present.

## Known prototype limits

- Terrain remains procedural/coarse rather than DEM/GIS terrain.
- Only Denmark currently has the detailed Natural Earth 1:50m presentation layer.
- Land-cover and vegetation are deterministic presentation proxies, not historical 1864 GIS reconstruction.
- Road/rail route geometry is still based on the current strategic graph rather than final historical spline data.
- Settlement and construction art remains procedural geometry rather than final authored asset packs.
