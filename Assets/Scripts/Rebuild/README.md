# PROJECT 1864 — Rebuild Scripts

This folder contains the clean tactical rebuild runtime.

## v00.01.00b ownership

- `TacticalRebuildTypes00B.cs` — stable UnitID and OOB records.
- `TacticalCompanyEntity00B.cs` — first-class Company tactical entity and QA footprint.
- `TacticalCompanySelection00B.cs` — the single Company selection input owner.
- `TacticalRebuildBootstrap00B.cs` — isolated rebuild world/bootstrap and legacy runtime gate.

## Rule

Do not add a second owner for selection, movement or combat. New gates extend the Company entity architecture directly rather than overriding older behaviour with later execution-order patches.