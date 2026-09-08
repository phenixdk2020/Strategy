# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles nu i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09e`.
- Fokus: battlefield controls, formations, Officer AI, combat, melee, navigation, obstacles, river/bridge, UI og tactical QA.
- `v00.00.09e` gør **Navigation V3** til eneste autoritative movement/steering-lag igen. V4 samt 09A/09B beholdes som sammenligningskode, men deaktiveres før deres første runtime-Update af `PrototypeNavigationV3Authority`.
- Tactical obstacle-baseline følger igen V3: træer, bygninger og fence posts behandles som diskrete forhindringer, mens river/water fortsat er bridge-only.
- `v00.00.09e` reducerer samtidig HUD-occlusion: range fans vises som tynde transparente ghost-linjer; selected-unit data samles i ét fast kompakt panel, og hover-info er kun en lille to-linjers label.
- Primær QA: regimentet yderst/mod højre må ikke længere stoppe permanent, regiment nr. 2 fra højre må ikke lave gentagne unødige kurskorrektioner, og Console skal vise `NAV-AUTH|...|Authority=V3` efter start.

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
