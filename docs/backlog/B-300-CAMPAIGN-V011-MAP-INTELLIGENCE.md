# B-300–B-309 — Campaign v00.00.11 Map Intelligence & UX

## Scope

Disse backlogpunkter fortsætter samme campaign-version `v00.00.11 WORK`. De udvider kortets læsbarhed, søgning, information og operationelle overblik uden at ændre tactical AI.

## B-300 — Semantic zoom tiers
**Status:** PLANLAGT

Kortinformation skal ændre detaljegrad efter zoomniveau.

Acceptance:
- nær zoom viser fulde labels og formationsdetaljer,
- mellemzoom prioriterer større byer, formationer og strategiske forbindelser,
- lang zoom reducerer clutter og viser højere-echelon information,
- selected location/formation forbliver synlig.

## B-301 — Major-city priority labels
**Status:** PLANLAGT

Locations får label-prioritet, så hovedbyer og strategiske centre ikke forsvinder i tæt label-culling.

Acceptance:
- København, Berlin, Hamburg, Stockholm, Christiania/Oslo og centrale 1864-frontsteder har høj prioritet,
- prioritet er datadrevet og ikke hardcoded i GUI-logik.

## B-302 — Hover location tooltip
**Status:** PLANLAGT

Hover over location viser kompakt tooltip med navn, controller, region, terrain, port/rail/fortification og eventuelle formationer.

Acceptance:
- tooltip kræver ikke click,
- tooltip ændrer ikke authoritative campaign state.

## B-303 — Selected-node highlight
**Status:** PLANLAGT

Valgt location skal have tydelig world-space highlight/ring.

Acceptance:
- highlight følger node position,
- highlight kan ses over political-control overlay,
- kun én primary selected-node highlight ad gangen.

## B-304 — Selected-formation route emphasis
**Status:** PLANLAGT

Når en formation er valgt, fremhæves dens aktive route tydeligere end resten af netværket.

Acceptance:
- completed legs, current leg og future legs kan skelnes,
- destination er tydeligt markeret,
- route emphasis skjuler ikke kontakt/battle markers.

## B-305 — Map legend panel
**Status:** PLANLAGT

Samlet legend for route types, political control, ports, rail, fortifications, depots og formation states.

Acceptance:
- legend kan åbnes/lukkes,
- legend svarer til de faktisk aktive visual layers.

## B-306 — Minimap/overview prototype
**Status:** PLANLAGT

Lille overview af hele campaignområdet med kameraets aktuelle viewport.

Acceptance:
- viewport rectangle opdateres ved pan/zoom,
- click i overview kan fokusere omtrentligt område,
- ingen alternativ simulation state.

## B-307 — Bookmark/favorite locations
**Status:** PLANLAGT

Spilleren kan markere centrale locations som bookmarks under en session.

Acceptance:
- mindst 5 bookmarks,
- hurtig kamera-focus,
- bookmarks påvirker ikke campaign simulation.

## B-308 — Formation list / order of battle panel
**Status:** PLANLAGT

Sidepanel med kendte egne formationer og deres location/state.

Acceptance:
- sortering efter nation/echelon/navn,
- click fokuserer formation,
- moving/engaged/idle fremgår,
- styrke vises hvor informationen er kendt.

## B-309 — Contact/event marker layer
**Status:** PLANLAGT

Kortet får særskilte markers for contact, battle pending, recent battle og andre campaign events.

Acceptance:
- marker har timestamp,
- marker kan fokuseres,
- recent-event markering kan udløbe uden at slette campaign history.
