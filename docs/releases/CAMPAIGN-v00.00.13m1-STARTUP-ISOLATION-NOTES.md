# PROJECT 1864 Campaign v00.00.13m1 — Startup Visual Isolation

Status: DEV / MAP-ONLY / afventer Unity compile + runtime QA.

## Problem

v13m brugte v13j som DEM/collider-kilde, men lod ældre campaign-renderere være synlige under initialisering. Resultatet var et synligt første terrænkort, hvorefter det samlede v13m-kort blev lagt ovenpå. Det gav indtryk af to forskellige kort/render stacks.

## Rettelse

- Legacy renderers skjules fra første frame, mens de fortsat må initialisere nødvendige data internt.
- v13k/v13l presentation-komponenter og v13j historical labels deaktiveres straks.
- v13j-terrain renderer holdes skjult; v13j deaktiveres, så snart `V013J_SmoothTerrain` har en `MeshCollider` som v13m kan sample.
- `V013M_UnifiedDenmarkSurface` holdes skjult, indtil det stitched z8 atlas faktisk er sat på materialet.
- Under load vises kun en neutral `Indlæser samlet 3D Danmark-kort …` besked.
- Build/version-overlay flyttes til y=88 under top-controls.

## QA

1. Ved Play må intet gammelt grønt/proceduralt Danmark-kort blinke frem.
2. Der må ikke komme et andet kort oven på et allerede synligt kort.
3. Kun loading-tekst må ses, indtil v13m-atlas er klar.
4. Når atlas er klar, må kun den unified v13m renderer være synlig.
5. Versionen skal stå som `v00.00.13m1 UNIFIED MAP STARTUP ISOLATION DEV` under topbaren.
6. MAP-ONLY skal fortsat være aktiv.
7. Tactical TEST-kode må være urørt.
