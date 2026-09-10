# PROJECT 1864 — Tactical Rebuild v00.01.00b

**Gate:** A+B  
**Status:** TEST  
**Branch:** `work/tactical-rebuild-v00.01.00`

## What this gate proves

This gate validates the clean architecture boundary before movement, combat or 1:1 rendering are introduced.

### Gate A

- Stable UnitID identity.
- OOB data separated from display names.
- Explicit ParentUnitId relationships.
- Danish and Prussian Regiment/Battalion/Company records.
- Historical/traditional identity stored separately where available.

### Gate B

- Eight independent Company tactical entities.
- Four Danish and four Prussian Companies.
- OOB parent relationship does not create Transform parenting.
- One and only one selection owner.
- LMB select.
- Shift add.
- Ctrl toggle.
- LMB drag box select.
- Esc clear.

## QA manpower

```text
Denmark I Battalion: 759
Prussia I Battalion: 812
Total active manpower: 1571
```

The rest of each Regiment remains present in OOB data but is not tactical-active in this gate.

## Deliberate exclusions

00.01.00b has no Company movement owner and no Company combat owner.

RMB does not move anything. It logs that movement is deferred to Gate D.

This is intentional. The previous 09l2–09l5 failure came from adding later authority layers before lower ownership was stable. The rebuild does not repeat that sequence.

## Runtime isolation

The old 09j four-Regiment `PrototypeBootstrap` is blocked before scene load. Legacy runtime MonoBehaviours are disabled before normal Update, except the retained RTS camera controller and visible build marker.

No legacy PlayerCommander is used by Company selection.

## Acceptance before Gate C

- Unity compiles without red errors.
- Eight Company footprints are visible.
- Each Danish Company is independently selectable.
- Shift/Ctrl/box selection work.
- No old Regiment formations or command overlays appear.
- Stable UnitIDs are visible in Company labels/logs.
- No Company movement occurs on RMB.

Only after this gate passes does Gate C add 1:1 GPU-instanced soldiers.