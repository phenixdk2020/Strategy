# v00.00.09l5 — Independent Company Movement Authority

## Purpose

09l5 fixes the remaining rigid-regiment behaviour observed in the 09l4 QA video. Although infantry was visually rendered around company tactical centres, those centres were still transform-children of the parent Regiment and multi-company orders rebuilt selected companies into one continuous line. The result read as one monolithic unit rather than multiple subordinate formations.

## Permanent command hierarchy rule

`ParentRegiment` is an organisational/command relationship, not a physical transform-parent relationship.

A company must retain its Regiment/Battalion OOB parent in data while owning an independent world-space tactical transform on the battlefield.

## Independent company transforms

- Active company tactical centres are detached from `Regiment.transform` into a dedicated world-space company root.
- Detachment preserves current world position and rotation.
- Later movement/rotation of Regimental HQ must not mechanically drag company formations.
- Regimental HQ remains the command entity and OOB parent.
- Ordinary soldiers remain GPU-instanced around each company tactical centre.

## Group order semantics

A multi-company order is a command to multiple subordinate formations, not an instruction to merge them into one new geometric formation.

- Normal RMB group move translates the selection around its centroid.
- Each selected company retains its relative offset from the group centroid.
- Each company receives its own destination.
- A simple RMB move retains the company's existing facing.
- RMB drag may rotate the relative company offsets around the group centroid and establish a common explicit final facing.
- Single-company movement semantics remain unchanged.

## Regiment selection semantics during company QA

While company-control is authoritative, selecting the active Danish regiment resolves to selecting all of its subordinate companies. The Regiment transform itself is not used as the movement body.

Parent Officer AI movement is temporarily disabled and the Regiment pivot is held while independent-company QA is active. This prevents competing transform writers until officer AI is moved down to Battalion/Company command levels.

## Retained constraints

- 09l3 reduced battle remains active: 1 Danish + 1 Prussian regiment, approximately 4,000 infantry / 20 companies.
- 09l4 screen-space company selection remains active.
- 09l4 company guidons remain attached to each company.
- Parent Regiment/HQ fire remains suppressed until proper company combat exists.
- Independent company obstacle/path navigation is not yet implemented; 09l5 company movement remains direct movement.
- V3 remains the accepted older whole-regiment navigation implementation but is not allowed to mechanically move detached company transforms in this gate.

## Acceptance telemetry

`COMPANY-AUTH-09L5|Installed=True|TransformParent=IndependentWorldSpace|GroupOrders=PreserveRelativeCompanyOffsets|RegimentPivotMovement=False|WholeRegimentSelection=SelectAllCompanies`

Multi-company move:

`COMPANY-AUTH-09L5|Order=GroupMove|Companies=N|RelativeOffsetsPreserved=True|...`
