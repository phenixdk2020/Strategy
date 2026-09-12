# CAMPAIGN4 v00.00.10k — Unity 6.6 API compatibility

## Fix

Unity 6.6 reported:

`Assets/Scripts/Campaign/Campaign4DemMeshRepairV010J.cs(59,22): error CS0619: 'Object.GetInstanceID()' is obsolete: 'Use GetEntityId instead.'`

`Campaign4DemMeshRepairV010J.cs` no longer calls `GetInstanceID()`.

The DEM repair layer now stores processed mesh references directly in `HashSet<Mesh>`.

## Why

The code only needs to know whether a specific streamed DEM mesh has already been checked. A direct `Mesh` reference is sufficient and avoids unnecessary dependency on Unity's newer EntityId API.

## Behaviour unchanged

- streamed `DEM_*` meshes are scanned;
- downward-facing meshes have triangle winding reversed;
- normals and bounds are recalculated;
- each mesh is processed only once;
- Campaign3 remains untouched.

## QA

1. Pull/update `channel-campaign4`.
2. Confirm Unity 6.6 compiles without the CS0619 error.
3. Enter Play Mode.
4. Verify DEM terrain appears above the sea board.
5. Verify `CAMPAIGN4-DEM-REPAIR|Version=v00.00.10k` logging when tiles are repaired.
6. Test Denmark overview and `F` focus at Aalborg/Limfjorden.
