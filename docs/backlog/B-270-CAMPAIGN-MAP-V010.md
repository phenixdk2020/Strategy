# B-270–B-279 — Campaign Map / Strategic Layer v00.00.10

## Purpose

Efter P0A v00.00.09 Tactical Command TEST forbindes de taktiske slag med det strategiske lag gennem første komplette loop:

`3D campaign map -> movement/contact -> tactical battle -> battle result -> tilbage til campaign map`

## Fast geografisk scope

Campaign-kortet skal dække **Danmark, Sverige, Norge, Finland og Tyskland** på ét sammenhængende strategisk kort.

Dette er ikke et lokalt Slesvig-only kort og ikke et rent skematisk nodekort.

Grundprincipper:

- geografien lagres i reel latitude/longitude,
- strategisk visning projekteres til et interaktivt 3D-kort,
- kortet skal kunne panoreres, zoomes og roteres,
- byer, terræn, veje, jernbaner, floder, skove og befæstninger bygges som separate datalag,
- formationer og strategiske objekter ligger oven på samme geografiske koordinatsystem,
- node-/rutenet bruges til strategisk navigation og state, men må ikke erstatte det geografiske kort visuelt.

Historisk-politisk model holdes adskilt fra geografisk region. Eksempel: Finland ligger geografisk i `Finland`, men kontrolleres i 1864 af det Russiske Imperium; Norge og Sverige modelleres under Sverige-Norge; Tyskland er geografisk map-region, mens politisk kontrol kan være Preussen eller andre tyske stater/forbund.

## Gate fra v00.00.09

Tactical v00.00.09 skal fortsat runtime-testes for de seneste navigation/melee-rettelser. Campaign-arbejdet udvikles parallelt på `work/v00.00.10-campaign-map` og må ikke promoveres til TEST/main, før tactical-baseline er godkendt.

## v00.00.10 MVP scope

### B-270 Campaign scene

- Ny separat `CampaignMap` scene.
- Interaktiv 3D strategisk visning med pan/zoom/rotation.
- Kortets koordinater dækker Tyskland til Finland og Norge.
- Tactical battle scene forbliver separat.
- Editor-menu skal kunne åbne Campaign Map og Tactical Battle separat.

### B-271 Geografisk datamodel

Strategiske locations lagres med:

- stabil ID,
- navn,
- geografisk region: Danmark/Sverige/Norge/Finland/Tyskland,
- latitude/longitude,
- projiceret map-position,
- politisk controller,
- terrain type,
- road links,
- rail capability,
- river/crossing flags,
- depot/supply capability,
- port capability,
- fortification flag,
- tactical battlefield template reference senere.

Første kodebaserede geographic network indeholder større strategiske locations i alle fem map-regioner. Den visuelle coast/relief-layer starter grov og opgraderes senere til højere opløsning uden at ændre formations-/nodekoordinater.

### B-272 Strategic formations

Campaign map viser formationer som tokens/stacks med stabil ID.

State:

- nation/team,
- formation name,
- regiments,
- manpower,
- ammunition,
- morale/cohesion summary,
- officer/HQ reference,
- current location,
- destination/route,
- movement state.

Samme regiment-ID skal leve videre fra campaign til tactical og tilbage igen.

### B-273 Strategic movement og tid

- Campaign time kører i realtid med pause og speed controls.
- Formationer flyttes gennem det geografiske road/node network.
- Travel time beregnes fra reel geografisk distance + terrain/route modifiers.
- Ghost route vises på kortet.
- Prussian QA formations kan få automatiske syd->nord angrebsruter for at teste contact-loopet.
- Senere kobles weather, fatigue, supply, bridge damage, rail transport og staff quality på.

### B-274 Battle trigger

Ved fjendtlig kontakt vises battle panel med:

- location,
- angriber,
- forsvarer,
- deltagende formationer,
- estimated strength,
- `KÆMP TAKTISK`,
- senere `Auto Resolve`.

Campaign-time pauses under battle prompt.

### B-275 Campaign -> Tactical transfer

Følgende state skal overføres:

- participating regiment IDs,
- strength,
- ammunition,
- morale/cohesion,
- officer profiles,
- attacker/defender role,
- battle location/template,
- campaign date/time.

Det hårdkodede QA tactical bootstrap skal kunne erstattes af `CampaignBattleContext`, når slaget startes fra campaign map.

### B-276 Tactical -> Campaign result

Efter slag returneres:

- surviving strength,
- ammunition,
- morale/cohesion,
- routed/destroyed state,
- winner/control result,
- senere officer state,
- senere captured equipment/prisoners.

Der må ikke ske midnight/full reset mellem lagene.

### B-277 Campaign UI MVP

- Topbar: dato/tid + pause/speed.
- Compact selection panel.
- Klik formation = selection/stats.
- Højreklik strategisk location = movement order.
- Ghost route/path.
- City/location labels.
- ownership/control skal kunne aflæses.
- WASD pan, Q/E rotation, mouse wheel zoom.

### B-278 Map layers / supply hook

Map-arkitekturen skal have separate lag for:

- coastline/landmass,
- relief/height,
- forests,
- rivers/lakes,
- roads,
- railways,
- ports,
- fortifications,
- depots/supply,
- political control,
- later fog of war/intelligence.

Supply starter simpelt, men dataarkitekturen skal støtte depot, road/rail route, ammunition, food/fodder, wagons og cut-off state.

### B-279 Acceptance for first campaign loop

1. Campaign scene åbner uden blocking errors.
2. Kortet viser det aftalte geografiske område Danmark + Sverige + Norge + Finland + Tyskland.
3. Kortet kan pan/zoom/roteres.
4. Locations ligger på latitude/longitude-baserede positioner.
5. Dansk formation kan vælges og få movement order.
6. Campaign clock kan pause/accelereres.
7. Formation bevæger sig mellem locations og ghost route følger ordren.
8. Enemy contact udløser battle prompt.
9. Tactical scene åbnes med attacker/defender context.
10. Tactical battle kan afsluttes og resultat returneres til campaign.
11. Strength/ammunition efter slag matcher tactical result.
12. Campaign kan fortsætte efter slaget.

## Ikke i første v00.00.10 slice

Følgende må ikke blokere første loop:

- fuld diplomacy,
- national economy,
- production chains,
- research trees,
- recruitment/training depth,
- komplet strategic fog of war,
- historical OOB completeness,
- komplette rail timetables,
- fuldt naval campaign system,
- engineer/pontoon construction UI,
- save-game completeness,
- final high-resolution coastline/DEM artwork.

## Design principle

Første campaign prototype skal bevise **geografisk korrekt coordinate foundation + state continuity** mellem strategisk og taktisk lag. Det visuelle kort må starte groft, men må ikke være et skematisk lokalkort; alle senere højopløselige kortlag skal kunne monteres oven på samme latitude/longitude model.
