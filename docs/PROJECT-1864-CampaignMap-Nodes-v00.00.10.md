# PROJECT 1864 – Campaign Map Nodes v00.00.10

## Status

Work branch: `work/v00.00.10-campaign-map-nodes50`

This supplement documents the first 50-point strategic campaign-map network. It is deliberately isolated from the current tactical AI work so campaign-map development can continue without altering the tactical TEST baseline.

## Node count

- Existing geographic network: 32 nodes.
- Added in node pack v00.00.10: 18 nodes.
- Target total at runtime: 50 nodes.

The runtime installer logs:

`CAMPAIGN-NODES|Pack=v010|Installed=True|Added=18|Nodes=50|Target=50`

## Added nodes

### Denmark – 9

1. Hjorring
2. Randers
3. Viborg
4. Horsens
5. Vejle
6. Odense
7. Nyborg
8. Korsor
9. Roskilde

### Sweden – 3

10. Helsingborg
11. Halmstad
12. Jonkoping

### Norway – 2

13. Fredrikshald
14. Drammen

### Finland – 2

15. Bjorneborg
16. Tavastehus

### Germany / Schleswig-Holstein – 2

17. Rendsburg
18. Altona

## Strategic-network intent

The new points densify the map where movement choices matter most while preserving the existing full-map coverage of Denmark, Sweden, Norway, Finland and Germany.

Main additions:

- denser north-south Jutland corridor,
- additional Danish central/ferry chain through Funen and Zealand,
- Great Belt strategic link via Nyborg–Korsor,
- Swedish west-coast corridor,
- inland Swedish Jonkoping branch,
- southeastern Norway access around Christiania,
- additional Finnish coastal/inland routes,
- denser Schleswig-Holstein approach through Rendsburg and Altona.

## Implementation rule

The node pack is implemented in `Assets/Scripts/CampaignMapNodePackV010.cs` and runs before the campaign scene is loaded. It calls the existing `CampaignSession` node/link helpers, so all added points use the same latitude/longitude projection and route representation as the original 32 nodes.

The code is intentionally isolated rather than changing the tactical AI scripts. This makes it possible to evaluate or roll back the current AI work separately.

## Historical-data caveat

Coordinates and period place names are suitable for prototype map placement. Depot, bridge, port and rail flags are strategic seed data for gameplay and should be historically source-validated before they are treated as final 1864 infrastructure data.

## QA gate

Before merging this work into the main campaign branch:

- Unity must compile without blocking errors.
- Campaign Map must report 50 nodes.
- All 18 added nodes must render as map markers.
- Added nodes must be selectable by the existing campaign UI.
- Route links must be visible and usable for formation movement.
- Tactical AI behaviour must remain unchanged by this branch.
