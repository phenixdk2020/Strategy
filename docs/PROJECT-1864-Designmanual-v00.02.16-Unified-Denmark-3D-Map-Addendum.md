# PROJECT 1864 Designmanual Addendum v00.02.16

## Unified Denmark 3D Map

Campaign map presentation follows a single-renderer rule from v00.00.13m onward.

### Rendering architecture

- One visible unified map surface owns the Denmark overview.
- The underlying v13j DEM mesh remains a hidden elevation/collider source only.
- A continuous country-scale cartographic atlas must always remain visible underneath higher-detail map data.
- Close-detail tiles may refine the current viewport but must never be the only visible map layer.
- Legacy terrain colours, prototype road/ferry lines, old city blocks and superseded map status overlays must not leak into the active presentation.

### Geographic presentation

- Land elevation is visual only; authoritative movement distance remains geographic/route based.
- Water is represented at a consistent sea level in the unified visual mesh.
- Cartographic UV mapping must follow Web Mercator when using slippy-map raster tiles.
- The current live provider is temporary QA cartography, not a historical authority for 1864.

### LOD

- Overview: permanent low-resolution full-country atlas.
- Medium/close zoom: viewport-only z10-z13 detail over the atlas.
- 3D landmarks appear only at close zoom and must remain geographically small relative to the strategic map.

### Current development branch

`work/v00.00.13m-unified-denmark-3d-map`

Official promoted campaign version remains v00.00.11 until normal QA/promotion gates are passed.
