# CAMPAIGN v00.00.13i2 — UNITY MODULE FIX

## Status

IMPLEMENTERET / afventer Unity package resolve + compile + runtime QA.

## Problem

GIS terrain loaderen bruger PNG decoding og UnityWebRequestTexture. Campaign-repositoryets minimale Packages/manifest.json havde kun Audio, IMGUI, ParticleSystem og Physics aktiveret. Derfor manglede de assemblies, der leverer Texture2D.LoadImage, UnityWebRequest, UnityWebRequestTexture og DownloadHandlerTexture.

## Rettelse

Packages/manifest.json aktiverer nu:

- com.unity.modules.imageconversion 1.0.0
- com.unity.modules.unitywebrequest 1.0.0
- com.unity.modules.unitywebrequesttexture 1.0.0

Ingen tactical dependencies eller Strategy-Test scripts er tilføjet.

## Forventet resultat

Efter updater + Unity package resolve skal CS1061/CS1069/CS0103 fejlene fra CampaignDenmarkGisFoundationV013I.cs vedrørende LoadImage/UnityWebRequest/UnityWebRequestTexture/DownloadHandlerTexture være væk.
