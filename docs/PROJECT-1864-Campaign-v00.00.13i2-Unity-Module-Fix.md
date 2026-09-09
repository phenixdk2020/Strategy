# PROJECT 1864 — Campaign v00.00.13i2 Unity Module Fix

## Purpose

Restore compile support for the GIS terrain tile loader on the Campaign branch under Unity 6.6 without reintroducing tactical dependencies.

## Required built-in Unity modules

The GIS terrain loader depends on three built-in modules:

- `com.unity.modules.imageconversion` for PNG decode / `Texture2D.LoadImage` extension support.
- `com.unity.modules.unitywebrequest` for `UnityWebRequest`.
- `com.unity.modules.unitywebrequesttexture` for `UnityWebRequestTexture` and `DownloadHandlerTexture`.

These are render/data-loading dependencies only. They do not affect campaign simulation, tactical navigation or AI.

## QA gate

- Unity package resolve completes without package errors.
- `CampaignDenmarkGisFoundationV013I.cs` compiles without the previous five missing-type/member errors.
- GIS tile download/cache still works at runtime.
- MAP-ONLY remains active.
