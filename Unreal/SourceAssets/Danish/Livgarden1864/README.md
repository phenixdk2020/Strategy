# PROJECT 1864 — Danish Livgarden 1864 source assets

This folder is the local staging location for the first real animated infantry visual in the Unreal port.

Expected local source layout:

```text
Unreal/SourceAssets/Danish/Livgarden1864/
  Mesh/
    DK_Livgarden_1864_Apose_textured_skeleton.fbx
  Weapons/
    Rifle_1_textured.glb
    Rifle_Bayonet_1_textured.glb
  Animations/
    *.fbx
```

The user-supplied animation package currently contains 65 FBX clips.

## Skeleton preflight

A static FBX metadata/name audit of the supplied files found the same 41 core `mixamorig:` motion-bone names in the body mesh and every animation clip, including `mixamorig:Hips`, spine, head, arms, hands, legs and feet.

The FBX source metadata label differs:

- body source label: `mixamorig:Hips_skin`
- animation source label: `mixamorig:Hipsf`

Those two strings occur in Mixamo source metadata while the actual `mixamorig:Hips` motion bone is present in both sets. This is a strong compatibility indication, but the Unreal import result remains authoritative.

## Unreal import

Run:

```text
Unreal/Content/Python/import_livgarden_1864.py
```

from Unreal Editor's Python execution environment.

The importer creates:

- `/Game/Units/Danish/Livgarden1864/Mesh/SK_DK_Livgarden_1864`
- `/Game/Units/Danish/Livgarden1864/Weapons/SM_Rifle_1`
- `/Game/Units/Danish/Livgarden1864/Weapons/SM_Rifle_Bayonet_1`
- one `A_*` animation asset for every FBX animation, bound to the imported Livgarden skeleton.

The first Danish QA company can then enable `UStrategyInfantryVisualComponent`, which renders the company at 1:1 by default and samples the full formation footprint when 1:2 / 1:5 / 1:10 visual scale is selected.

## Known first-pass gap

The supplied package has kneeling/sitting and prone reload clips, but no clearly named standing reload clip. Until one is supplied, the first-pass runtime visual falls back to the standing aim pose during a standing reload. Gameplay reload timing remains authoritative and unchanged.
