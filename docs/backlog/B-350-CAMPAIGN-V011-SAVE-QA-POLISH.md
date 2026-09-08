# B-350–B-359 — Campaign v00.00.11 Save, QA & Polish

## Scope

Stabilitet, reproducerbar test og campaign-session continuity. B-359 bliver den nye samlede promotion-gate for v00.00.11.

## B-350 — Campaign save snapshot schema
**Status:** PLANLAGT

Definér versioneret snapshot af campaign state.

Acceptance:
- current time, node control, formations, routes og battle history kan serialiseres,
- schema har version,
- save-format er adskilt fra scene/GameObject state.

## B-351 — Load/restore campaign state
**Status:** PLANLAGT

Campaign state kan gendannes fra et gyldigt snapshot.

Acceptance:
- stable IDs bruges til restore,
- ugyldige/manglende refs rapporteres,
- load må ikke duplikere formations eller events.

## B-352 — Autosave checkpoint hook
**Status:** PLANLAGT

Hooks til autosave ved sikre campaign transitions.

Acceptance:
- campaign start, battle handoff og battle return kan udløse checkpoint,
- autosave kan deaktiveres i QA,
- ingen save midt i delvist anvendt state mutation.

## B-353 — Deterministic campaign seed
**Status:** PLANLAGT

QA kan køre med kendt random seed.

Acceptance:
- samme seed + samme orders giver samme prototype event sequence,
- seed vises i diagnostics,
- normal play kan vælge ny seed.

## B-354 — Campaign integrity validator
**Status:** PLANLAGT

Udvid validator til hele campaign state.

Acceptance:
- duplicate IDs,
- missing node refs,
- broken parent/child formation refs,
- invalid route legs,
- impossible negative strength/stock,
- inconsistent battle pending state rapporteres samlet.

## B-355 — Stress test 100+ locations
**Status:** PLANLAGT

Syntetisk performance-test over det nuværende 50-node target.

Acceptance:
- mindst 100 synthetic locations kan genereres til QA,
- pan/zoom/selection diagnosticeres,
- synthetic data må ikke blive permanent campaign canon.

## B-356 — Stress test formation count
**Status:** PLANLAGT

Test campaign UI/state med væsentligt flere formation tokens end standard QA OOB.

Acceptance:
- mindst 50 formation states i synthetic test,
- stacking/list/search forbliver funktionelt,
- performance metrics kan aflæses.

## B-357 — Campaign error overlay
**Status:** PLANLAGT

Blocking campaign-state problemer skal være synlige uden at åbne Unity Console.

Acceptance:
- compact error counter,
- sidste blocking diagnostic kan vises,
- overlay må ikke skjule normal UI når der ingen fejl er.

## B-358 — Full v00.00.11 regression preset
**Status:** PLANLAGT

Samlet reproducerbar QA-runde for alle centrale campaign-funktioner.

Acceptance:
- 50-node validation,
- search/focus,
- node/formation selection,
- movement + ETA,
- stacking,
- political control,
- contact/battle handoff,
- battle return,
- save/load hook når implementeret,
- 0 blocking console errors.

## B-359 — Final v00.00.11 promotion gate
**Status:** PLANLAGT

Dette er den endelige campaign gate før `v00.00.12`.

Acceptance:
1. Unity 6.6 compiler: 0 blocking errors.
2. Campaign overlay viser `v00.00.11 WORK` under QA.
3. 50-node canonical network validerer uden broken links.
4. Core B-280–B-299 acceptance er bestået.
5. Map intelligence/UX B-300–B-309 er enten bestået eller eksplicit deferred med dokumenteret begrundelse.
6. B-310–B-319 forbliver separat regimental-standards work package og må ikke skabe tactical regression.
7. Movement/logistics B-320–B-329 har ingen state corruption.
8. Warfare/command B-330–B-339 kan levere campaign context uden tactical AI rewrite.
9. World/economy B-340–B-349 er data-safe og kan deaktiveres uden at bryde map loop.
10. Save/QA B-350–B-358 er valideret i det omfang de er implementeret.
11. Tactical AI-filer er ikke ændret af campaign branchens work package.
12. Alle deferred items er registreret til v00.00.12+ før promotion.

Når B-359 godkendes kan næste campaign-version oprettes som `v00.00.12`.
