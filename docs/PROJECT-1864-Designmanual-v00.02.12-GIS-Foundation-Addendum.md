# PROJECT 1864 Designmanual — v00.02.12 GIS Foundation Addendum

## Campaign map rendering decision

The strategic world shall use a **GIS-first 3D render pipeline**. Hand-built polygon maps may remain as fallback/debug scaffolds, but they are not the long-term visual foundation.

### Authoritative separation

- **Simulation geography:** latitude/longitude, route geometry and geodesic distance.
- **Render geography:** projected Unity X/Z plus terrain/water/settlement meshes.
- **Elevation:** presentation value attached to geographic samples; never strategic distance.
- **Historical state:** separate scenario layer controlling which roads, railways, bridges, ports, settlements and facilities exist at the scenario date.

Changing DEM resolution, map LOD or vertical exaggeration must not change ETA or strategic outcomes.

## Data-source hierarchy

Preferred final Denmark pipeline:

1. **DHM/Terræn or equivalent official elevation** for final Danish relief.
2. **GeoDanmark / authoritative Danish vector data** for coastline, water, roads, buildings and other physical geometry where suitable.
3. **Historical maps/sources close to 1864** for scenario-date infrastructure, settlement extent, land cover and military features.
4. **Natural Earth** as coarse fallback coastline/overview scaffold.
5. **Mapzen Terrain Tiles / AWS public terrain dataset** as v13i runtime DEM pilot and fallback development source.

Modern geographic data must never silently become 1864 historical state. Physical geography and historical infrastructure are separate concepts.

## v00.00.13i runtime pilot

The v13i pilot establishes the implementation contract:

- tile-based elevation loading,
- local cache,
- offline fallback,
- broad campaign projection,
- Denmark land mask,
- explicit hydrology,
- terrain-conforming presentation,
- Denmark-only settlements/labels,
- explicit road/ferry rendering,
- MAP-ONLY development gate.

## Water and harbour rule

Major fjords, straits and harbour relationships are first-class map geometry. A strategic harbour city must visually touch its navigable water context. Aalborg is the initial QA case: Limfjorden, south-bank city/harbour and north-bank Nørresundby context must be readable at close zoom.

## Infrastructure rule

Infrastructure renderer must distinguish:

- road,
- rail,
- ferry/sea crossing,
- bridge,
- navigable water route.

A graph connection is not automatically a drawable straight segment. Route geometry must follow land/water reality. Cross-water links require explicit crossing semantics.

## Historical validation rule

Unknown or unverified 1864 infrastructure must remain hidden/placeholder rather than being inferred from modern `HasRail`, modern road networks or present-day bridges. Source/provenance confidence is part of the data model planned for later campaign versions.

## Visual target

The visual direction remains **Historic Miniature Grand Strategy Diorama**, but the diorama shall sit on credible GIS terrain/water geometry. Diorama styling may simplify buildings/vegetation; it must not distort coastline, water topology or strategic route meaning.

## Development gate

Until the Denmark map foundation passes visual QA, CampaignMap may run in MAP-ONLY mode. Strategic combat/tactical transition is restored only after map presentation is stable enough that combat testing does not mask geography/render defects.
