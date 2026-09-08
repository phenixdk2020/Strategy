# B-290–B-299 — Campaign Map v00.00.11 Extended Core Work Package

## Purpose

Denne pakke fortsætter samme campaign-version `v00.00.11 WORK`. Efter udvidelsen til B-300–B-359 er B-299 ikke længere den endelige versionspromotion; den fungerer som **intermediate core usability gate**. Den endelige promotion-gate er B-359.

Tactical AI er fortsat uden for scope på campaign-branchen.

## B-290 — Campaign search / find location
**Status:** IMPLEMENTERET / AFVENTER QA

Krav: søgning på location/formation navn og ID, partial match, camera focus, ingen ændring af simulation state.

Acceptance: København, Berlin, Stockholm, Dybbøl og mindst én formation kan findes og fokuseres.

## B-291 — Map filters og layer toggles
**Status:** PLANLAGT

Minimumslayers: location labels, strategic links, political control, formation tokens, depots, ports, rail og fortifications.

Acceptance: hvert lag kan toggles uden at ændre authoritative campaign state.

## B-292 — Route-type legend og route cost breakdown
**Status:** PLANLAGT

Vis leg type (`Road`, `Rail`, `Sea/Ferry`), distance, terrain modifier, beregnet leg time og total ETA. Arkitekturen skal have hooks til weather, congestion, supply og fatigue.

## B-293 — Strategic importance / node role
**Status:** PLANLAGT

Datadrevne roller: capital/major city, regional centre, fortress, depot, port, rail junction og crossing/ferry point.

Acceptance: mindst 15 prototype-locations har en tydelig strategisk rolle uden byspecifik UI-hardcode.

## B-294 — Depot og supply overlay seed
**Status:** PLANLAGT

Depot-noder kan fremhæves og formationsdiagnostik kan finde en relevant friendly depot-kandidat via campaign data. Ingen gratis automatisk resupply i denne opgave.

## B-295 — Port, rail og fortification symbols
**Status:** PLANLAGT

Separate symboler for port, rail og fortified; følger node-position og kan toggles.

## B-296 — Formation hover/OOB summary
**Status:** IMPLEMENTERET / AFVENTER QA

Hover viser formation-navn, nation, total strength, regiment count, state og destination hvis moving.

## B-297 — Label culling og performance
**Status:** PLANLAGT

Labels uden for viewport renderes ikke; zoom reducerer clutter; major/selected locations prioriteres. Retningen skal kunne skaleres til synthetic 100+ node QA.

## B-298 — Campaign QA scenario presets
**Status:** PLANLAGT

Presets: multi-leg Danish movement, 3 formations stacked, Danish/Prussian contact, long-distance camera navigation, ferry/sea route og rail-capable corridor.

Acceptance: mindst fire presets kan reproduceres deterministisk efter reset.

## B-299 — Intermediate core usability gate
**Status:** PLANLAGT

B-299 validerer campaign-map kernens brugbarhed før de bredere B-300–B-359 systempakker samles.

Acceptance:
1. Unity compiler: 0 blocking errors.
2. Campaign overlay viser `v00.00.11 WORK`.
3. 50 canonical nodes validerer uden broken links.
4. Location labels og node selection fungerer.
5. Political control er separat fra geographic region.
6. Formation selection og stacking fungerer.
7. Multi-leg pathfinding, route ghost og ETA fungerer.
8. Link types kan skelnes som minimum Road/Rail/Sea-Ferry.
9. Camera pan/zoom/rotate/focus/reset fungerer.
10. Search/find location fungerer.
11. Map layer arkitektur kan fortsættes uden state mutation.
12. Route cost breakdown/data hooks er konsistente.
13. Depot/port/rail/fortification information kan repræsenteres.
14. Formation hover/OOB summary fungerer.
15. QA presets kan reproducere centrale tests.
16. Diagnostics rapporterer version, node count, links, selection, route, ETA og campaign time.
17. Tactical AI-filer er ikke ændret af campaign work.

B-299 åbner **ikke** v00.00.12. Efter B-299 fortsætter samme version gennem B-300–B-359. Kun B-359 kan promote campaign til v00.00.12.

## Implementation status

Implementeret / afventer QA:
- B-281: 50-node runtime validation + broken-link diagnostics.
- B-282: eksisterende location labels, afventer læsbarheds-QA.
- B-283: første Road/Rail/SeaFerry classification.
- B-284: ETA over eksisterende multi-leg pathfinding.
- B-285: node selection + info panel.
- B-286: political-control indicator.
- B-287: stationary formation stacking offsets.
- B-288: `F` focus selected + `Home` reset camera.
- B-289: runtime diagnostics.
- B-290: location/formation search + camera focus.
- B-296: formation hover/OOB summary.

Ingen af disse kaldes godkendte før Unity 6.6 compile/runtime QA er udført.
