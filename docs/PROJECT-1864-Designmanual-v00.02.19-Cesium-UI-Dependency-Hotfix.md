# PROJECT 1864 — Designmanual v00.02.19 Addendum

## Cesium UI dependency baseline

Campaign v00.00.13n1 fastlægger Unity 6 UI-afhængigheden for Cesium-integrationen.

Cesium for Unity 1.25.1 bruger `UnityEngine.EventSystems` i sit credit-system. På PROJECT 1864s Unity 6000.6-baseline skal Campaign-projektet derfor eksplicit inkludere Unity UI (`com.unity.ugui` 2.0.0) samt `com.unity.modules.ui` 1.0.0.

Dette er en compile/runtime dependency og ændrer ikke campaign simulation, tactical code eller den geografiske model.
