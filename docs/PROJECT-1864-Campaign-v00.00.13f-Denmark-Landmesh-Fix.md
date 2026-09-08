# PROJECT 1864 — Campaign v00.00.13f Denmark Landmesh Fix

Status: DEV IMPLEMENTATION / CLEAN CAMPAIGN LINEAGE  
Branch: `work/v00.00.13f-denmark-landmesh-fix`  
Base: v00.00.13e Denmark Clean Render Foundation

## Purpose

v00.00.13f corrects the specific render defect exposed by v00.00.13e: Denmark coastline geometry was correctly positioned, but the generated land triangles were wound for a 2D XY polygon and then rendered in Unity X/Z space. The orientation inversion made the visible top side point downward and caused the land to be back-face culled from the campaign camera.

The version preserves the v13e clean-render architecture. It does not reintroduce the old stacked terrain/polish/remap layers and does not change campaign simulation.

## Implementation rules

1. Denmark Natural Earth rings remain authoritative presentation geometry for this QA slice.
2. Rings are still projected directly through `CampaignGeoProjection.Project3D(latitude, longitude, height)`.
3. Triangle orientation is validated after projection into Unity X/Z space.
4. If aggregate triangle normal Y is negative, triangle indices 1 and 2 are swapped for every triangle.
5. The generated mesh is accepted only when runtime `AverageNormalY > 0.05`.
6. v13e clean sea, clean settlements and clean focus infrastructure remain in use.
7. The v13e Denmark landmesh renderer is suppressed and replaced by the corrected v13f landmesh.
8. Denmark remains deliberately low-relief; no DEM/elevation accuracy is claimed.
9. Land uses restrained grass/field/heath material variants for visual separation only.
10. Strategic distance, movement, ETA and route geometry remain independent of Unity render dimensions.

## Label semantic zoom

At overview/Home zoom only the principal Danish strategic centres are shown: Aalborg, Aarhus, Fredericia, Odense and Copenhagen.

At regional zoom, Hjørring, Viborg, Vejle, Kolding, Korsør and Roskilde may appear.

At close zoom, Haderslev, Dybbøl, Sønderborg and the southern 1864 context Flensburg/Schleswig may appear. Southern-context labels are smaller and lower contrast than the Danish strategic-centre labels.

Label placement tests several offsets around the projected node before falling back to a vertical displacement. This replaces the v13e behaviour that could build tall label stacks around central Jutland.

## Infrastructure

v13e road/rail/ferry renderers remain tied to authoritative node endpoints. v13f applies only a presentation clarity pass: rail is slightly stronger than road and ferry retains a separate blue visual identity.

## Construction

The existing QA construction projects remain part of the test slice:

- `QA-BARRACKS-AALBORG`
- `QA-FARM-AARHUS`

They are continuously re-grounded against the same low-relief Denmark presentation height so construction state does not float when the map presentation is updated.

## QA gate

v00.00.13f passes this development gate only when all of the following are true:

- Denmark land is visibly filled from the Home camera.
- Coastline is visible but is not the only visible country geometry.
- Runtime log reports `AverageNormalY > 0.05`.
- No giant legacy green terrain slabs are visible.
- Aalborg/Aarhus/Fredericia/Odense/Copenhagen overview labels are readable without major overlap.
- Regional/local labels appear progressively on zoom-in.
- Flensburg/Schleswig do not dominate overview presentation.
- Road/rail/ferry lines remain aligned to Denmark.
- Aalborg barracks and Aarhus farm remain present and grounded.
- Unity compiles without errors.

Formal campaign promotion gates remain unchanged; v00.00.11 remains the active campaign work version until those gates are satisfied.
