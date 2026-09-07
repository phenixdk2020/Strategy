# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles nu i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09a`, `v00.00.09b`, `v00.00.09c` ...
- Fokus: battlefield controls, formations, Officer AI, combat, melee, navigation, obstacles, river/bridge, UI og tactical QA.
- Decorative trees er fra `v00.00.09b` pass-through; buildings/fences/water forbliver tactical obstacles.
- Kendt blocker: robust route finding omkring buildings skal fortsat stabiliseres, men må ikke blokere campaign-sporet.

## Track B — Campaign

- Branch: `campaign`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Campaign`
- Aktuel revisionsserie starter ved `v00.00.10a`.
- Fokus: campaign scene/map, strategic formations, movement/time, battle trigger, tactical handoff og state return.
- Campaign-kode/scener skal så vidt muligt ligge separat fra tactical navigation/combat-filer for at minimere merge-konflikter.

## Stable

- Branch: `main`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy`
- Kun accepterede/promoverede gates.

## Integration rule

Campaign må ikke vente på mindre tactical polish. Kun tactical fixes der er nødvendige for campaign-state continuity eller som er accepteret gennem QA, integreres senere i campaign-sporet. Ingen destructive Git-operationer, force-push, reset --hard eller clean anvendes.
