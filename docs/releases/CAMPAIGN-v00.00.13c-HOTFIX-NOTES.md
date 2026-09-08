# CAMPAIGN v00.00.13c — projection/terrain hotfix

The first v13c terrain-cleanup implementation was rejected during runtime QA after video review.

## Root cause

The detailed Denmark Natural Earth layer was originally created through the legacy Denmark-local projection while the v11+ strategic nodes and terrain use the broad Europe projection. The first v13c pass then tried to use that local-projection geometry as a mask against the broad strategic terrain mesh. In addition, the broad mesh is too coarse to carve the detailed Danish coastline without large stepped terrain bands.

## Corrected rule

- Do not carve the global strategic terrain with a detailed Denmark mask.
- Do not infer a Denmark polygon from a mesh and apply it across another coordinate system.
- Remap the existing Natural Earth Denmark vertices from legacy local projection to the broad campaign projection explicitly.
- During the Denmark-focus QA slice, disable the coarse global terrain renderer and let the detailed Denmark mesh be the visible land surface over the strategic sea.
- Ground Danish settlements, nodes, formations, land-cover, construction sites and Danish infrastructure against the same gentle Denmark presentation height function.
- Hide non-Danish 3D settlement/node context during the Denmark-focus slice to prevent floating context geometry.

Simulation coordinates, route distances, ETA and campaign state remain unchanged.
