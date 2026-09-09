# PROJECT 1864 Designmanual — v00.02.13 GIS Module Dependencies

The GIS-first Campaign render pipeline requires Unity built-in support for PNG image conversion and HTTP texture retrieval. The Campaign project manifest must therefore retain:

- `com.unity.modules.imageconversion`
- `com.unity.modules.unitywebrequest`
- `com.unity.modules.unitywebrequesttexture`

These modules are technical data-loading dependencies for the strategic map and are not tactical-system dependencies. Removing them requires replacing the GIS tile loader with another decode/download pipeline before the manifest is changed.
