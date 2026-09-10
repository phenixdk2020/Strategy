# PROJECT 1864 — Tactical ownership rules v00.01.00b

## Gate A+B owners

| Category | Owner |
|---|---|
| Persistent identity/OOB | `RebuildOOBRegistry00B` |
| Company tactical identity/world pose | `TacticalCompanyEntity00B` |
| Player Company selection | `TacticalCompanySelection00B` |
| Rebuild world/bootstrap | `TacticalRebuildBootstrap00B` |
| Camera | retained `RTSCameraController` |
| Build marker | retained `PrototypeBuildVersionOverlay` |
| Company movement | **NONE in 00.01.00b** |
| Company combat | **NONE in 00.01.00b** |

## Mandatory rule

Before a later gate introduces movement or combat, that gate must name exactly one owner. No later execution-order script may override another owner's state as a normal architecture pattern.