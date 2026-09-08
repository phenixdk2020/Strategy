# B-390–B-399 — Campaign v00.00.12 Society, Events & Final QA

## Status

**Target version:** `v00.00.12`  
**State:** PLANLAGT  
**Scope:** campaign society, events, occupation and v00.00.12 acceptance  
**Tactical AI:** må ikke ændres af dette work package.

## Retning

Kortet skal ikke kun vise hære og infrastruktur. Befolkning, mobilisering, besættelse, årstid og større hændelser skal kunne ændre campaign-state og samtidig afspejles visuelt uden at blive til en fuld city-builder.

## B-390 — Mobilization and reserve pool
**Status:** PLANLAGT

Nationer får mobilization/reserve state oven på manpower-systemet.

Acceptance:
- reserve pool er separat fra deployed formation strength,
- mobilization tager campaign time,
- policy/capacity hooks kan påvirke rate,
- formation creation/replacements kan senere trække fra samme authoritative pool.

## B-391 — Civilian displacement and refugee flow
**Status:** PLANLAGT

Frontnærhed, occupation og større slag kan skabe abstrakt civilian displacement state med diskrete living-world visuals.

Acceptance:
- displacement er aggregated campaign data, ikke tusindvis af individuelle AI-agenter,
- route/flee direction kan afledes af friendly/safe regions,
- visual refugees må LOD-culles uden state loss,
- systemet må ikke skabe skjult enemy-intelligence leakage.

## B-392 — Occupation and requisition policy
**Status:** PLANLAGT

Besatte områder kan have occupation policy og requisition hooks.

Acceptance:
- controller, geographic region og economic contribution forbliver separate,
- requisition kan give kortsigtet supply/resource effect med langsigtede costs hooks,
- policy kan påvirke unrest/civilian activity,
- ændringer logges med reason/time.

## B-393 — Public order and unrest
**Status:** PLANLAGT

Locations/regions får bounded public-order state.

Acceptance:
- occupation, shortages, requisition og battle aftermath kan påvirke unrest gennem hooks,
- unrest må ikke være ren random punishment,
- thresholds kan udløse events/production penalties senere,
- UI viser årsager og trend.

## B-394 — War weariness and national morale seed
**Status:** PLANLAGT

Nation-level state for længerevarende krigspres.

Acceptance:
- casualties, lost territory, shortages og victories kan bidrage gennem datadrevne modifiers,
- systemet ændrer ikke direkte weapon accuracy,
- morale/weariness kan senere påvirke politics/recruitment/AI priorities,
- diagnostics forklarer de største bidrag.

## B-395 — Seasonal, weather and disease campaign hooks
**Status:** PLANLAGT

Campaign date/weather kan påvirke movement, agriculture, disease risk og construction efficiency.

Acceptance:
- seasonal state kommer fra campaign date/location,
- modifiers er bounded og data-driven,
- weather visual og simulation state er adskilt men synkroniseret,
- disease behandles som formation/region state og ikke som tilfældig per-frame chance.

## B-396 — Newspaper and campaign event feed
**Status:** PLANLAGT

Spilleren får et kronologisk feed med større begivenheder.

Acceptance:
- battle results, control changes, construction completion, mobilization og større logistics events kan registreres,
- hvert event har campaign timestamp og stable type/id,
- feed kan filtreres,
- UI må ikke afsløre information spilleren ikke burde kende.

## B-397 — Historical/event trigger framework
**Status:** PLANLAGT

Generisk framework til scripted og conditional campaign events uden hardcoding i hovedloopet.

Acceptance:
- triggers kan bruge date, control, nation, resource og prior-event conditions,
- event kan være one-shot eller repeatable efter eksplicit regel,
- deterministic seed/QA understøttes,
- historiske hændelser kan senere valideres som dataindhold separat fra engine-kode.

## B-398 — Strategic AI priority hooks
**Status:** PLANLAGT

Campaign AI får et interface til at prioritere construction, mobilization, logistics og strategic objectives uden at røre tactical Officer AI/navigation.

Acceptance:
- priority decision kan forklares i diagnostics,
- AI bruger samme campaign actions/rules som player,
- ingen gratis resources, instant construction eller hidden transport,
- tactical AI-filer forbliver uberørte.

## B-399 — Final Campaign v00.00.12 acceptance gate
**Status:** PLANLAGT

B-399 er den planlagte endelige gate for campaign `v00.00.12`.

Acceptance:
1. Unity 6.6 compiler: 0 blocking errors.
2. v00.00.11/B-369 baseline er bevaret uden campaign regression.
3. Location facilities/construction queue kan save/loades deterministisk.
4. Damage/repair og crossings fungerer uden invalid route state.
5. Telegraph/depot/fortification data kan vises og diagnosticeres.
6. Road, rail og sea transport respekterer capacity og delay.
7. Convoy/stock transfer kan gennemføres uden duplication/loss bugs.
8. Congestion/disruption/reroute giver sikker state.
9. Occupation/mobilization/public-order state kan ændres og forklares.
10. Living-world visuals afspejler relevante states uden at blive authoritative.
11. Event feed og event framework respekterer fog of war.
12. Strategic AI priority hooks må ikke ændre tactical AI.
13. Save/load/integrity validator rapporterer ingen blocking campaign errors i regression preset.
14. Alle deferred items registreres eksplicit før promotion til næste campaign version.
