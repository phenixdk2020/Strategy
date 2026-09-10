# PROJECT 1864 Campaign v00.00.13n1 — CESIUM UGUI COMPILE HOTFIX

Status: DEV / afventer Unity compile + runtime QA.

## Problem

Cesium for Unity 1.25.1 kompilerede ikke i Unity 6000.6, fordi projektmanifestet ikke eksplicit havde Unity UI (uGUI). `CesiumCreditSystemUI.cs` bruger `UnityEngine.EventSystems` og kan derfor ikke kompileres uden `com.unity.ugui`.

Observeret fejl:

`CS0234: The type or namespace name 'EventSystems' does not exist in the namespace 'UnityEngine'`

## Rettelse

- Tilføjet `com.unity.ugui` version `2.0.0`, som er Unity 6-generationens uGUI-pakke.
- Tilføjet `com.unity.modules.ui` version `1.0.0` eksplicit som lavniveau UI-module.
- Cesium for Unity forbliver på `1.25.1`.
- Ingen Campaign-simulation, geografi, map-only eller tactical kode er ændret.

## QA

1. Unity Package Manager resolver Cesium + uGUI uden dependency-fejl.
2. `CesiumCreditSystemUI.cs` kompilerer uden CS0234 for `UnityEngine.EventSystems`.
3. Campaign build overlay viser `v00.00.13n1 CESIUM UGUI COMPILE HOTFIX DEV`.
4. Hvis Unity fortsat viser stale PackageCache-fejl efter manifest-opdateringen, luk Unity, slet projektets `Library/PackageCache` eller hele `Library` og åbn projektet igen.
