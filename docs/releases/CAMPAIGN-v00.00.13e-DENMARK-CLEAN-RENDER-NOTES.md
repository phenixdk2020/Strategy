# CAMPAIGN v00.00.13e — DENMARK CLEAN RENDER DEV

## Build target

`PROJECT 1864 CAMPAIGN | v00.00.13e DENMARK CLEAN RENDER DEV`

## Hovedændring

Denne version erstatter den synlige Danmark-QA render-path med én ny clean hierarchy og skjuler de gamle v13 terrain/hydrology/land-cover/infrastructure/settlement/overlay renderere. Formålet er at fjerne de store grønne slabs og stoppe lag-på-lag reparationsarkitekturen.

## Implementeret

- `V013E_DENMARK_CLEAN_RENDER` top-level render hierarchy.
- Clean thin sea surface omkring Danmark.
- Natural Earth Denmark direkte broad lat/lon projection.
- Meget lav Denmark relief.
- Clean road/rail/ferry renderers mellem Denmark-focus nodes.
- Clean settlement icons ved autoritative node-koordinater.
- Legacy node/control/settlement renderere skjult, colliders bevaret.
- Formationer groundet og skaleret for Denmark QA.
- Aalborg barracks + Aarhus farm bevaret og groundet.
- Label LOD med færre labels ved overview.
- Denmark start/Home camera strammet ind.
- v13a/b/c/d presentation helpers deaktiveret i v13e QA.
- v11 diagnostic/search/layer UI deaktiveret i clean QA-slicen.

## Simulation

Der er ingen tilsigtet ændring i campaign state, campaign time, movement, ETA, route distance, battle transition, logistics eller tactical AI/navigation.

## QA

Første lokale Unity-test skal især kontrollere:

- ingen grønne rektangulære slabs,
- Danmark fylder hovedparten af viewet,
- kyst/øer er læsbare,
- Aalborg/Aarhus/Fredericia/Odense/København ligger korrekt,
- labels er færre og mere læsbare,
- kaserne/farm eksisterer,
- ingen compile errors.
