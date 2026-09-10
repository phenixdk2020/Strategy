# v00.01.00b — Gate A+B implementation note

## Implemented now

- Stable UnitID OOB registry.
- Full reference data for one Danish and one Prussian Regiment.
- Only I Battalion on each side tactical-active.
- Eight independent Company entities.
- Company transforms are not OOB-parent transforms.
- One selection owner with LMB, Shift, Ctrl, drag box and Esc.
- Minimal QA battlefield and Company footprints.
- Legacy Regiment bootstrap/runtime blocked from becoming authoritative.

## Intentionally deferred

- 1:1 soldiers: Gate C.
- Movement/navigation: Gate D.
- Combat: Gate E.
- Full Regiment scale: Gate F.
- Officer/HQ AI: Gate G onward.

## Acceptance principle

Do not add Gate C until this Gate compiles and Company selection behaves correctly without any legacy Regiment formation appearing.