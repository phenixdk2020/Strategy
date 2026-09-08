# B-400–B-409 — Campaign v00.00.12 Formation Mobility & March Model

## Status

**Target version:** `v00.00.12`  
**State:** PLANLAGT  
**Scope:** strategic/campaign movement only  
**Tactical AI:** må ikke ændres af dette work package.

## Grundregel

En formation/hær må ikke have én kunstig fast map-speed. Strategisk marchhastighed beregnes ud fra de faktiske komponenter, der bevæger sig samlet.

**Formationens maksimale sammenhængende marchhastighed sættes som udgangspunkt af den langsomste nødvendige attached component.**

Eksempler:
- infanteri til fods begrænser en ellers mounted HQ-stab,
- fodartilleri/hestetrukket artilleri kan begrænse hurtigt kavaleri, hvis de marcherer samlet,
- en langsom supply train kan sætte tempoet for hele kolonnen, hvis den er ordre-bundet til formationen,
- en detached baggage/supply column må ikke begrænse hovedstyrken, hvis den faktisk får separat marchordre,
- rail/sea transport bruger transportmode i stedet for normal land-march speed under selve transitfasen.

Den langsomste enhed er derfor ikke bare laveste rå km/t; det er laveste **effective movement rate for den aktuelle route og movement mode** blandt de komponenter, der skal holde sammen.

## B-400 — Unit strategic mobility profile
**Status:** PLANLAGT

Hver campaign subunit/attached element får et datadrevet mobility profile.

Minimum data/hooks:
- movement class: Foot / Mounted / HorseDrawn / WagonTrain / RailEligible / SeaEligible,
- base road march rate,
- cross-country/terrain modifiers,
- horse requirement og available horses,
- equipment/load burden,
- artillery/vehicle type hook,
- normal daily march duration,
- fatigue sensitivity,
- forced-march eligibility.

Acceptance:
- profile er data og ikke UI-hardcode,
- infantry, cavalry, horse artillery, foot artillery og wagon train kan få forskellige profiler,
- manglende heste kan reducere horse-drawn mobility,
- historiske værdier kan senere udskiftes uden kodeændring.

## B-401 — Formation bottleneck speed
**Status:** PLANLAGT

Beregn formationens effektive marchfart ud fra de attached components, der er på samme movement order.

Princip:
`FormationEffectiveSpeed = min(EffectiveSpeed(component_1 ... component_n))`

Acceptance:
- langsomste required attached component identificeres eksplicit,
- diagnostics viser hvilken component der er bottleneck,
- detached units tæller ikke med efter gyldig separation,
- destroyed/immobile equipment håndteres uden NaN/negative speed,
- formation kan ikke outrunne en attached component uden særskilt ordre/state.

## B-402 — Horses, draught teams and artillery mobility
**Status:** PLANLAGT

Heste bliver en reel mobility resource for cavalry, artillery og transport.

Acceptance:
- cavalry speed afhænger af mounted availability og horse condition hooks,
- horse-drawn guns kræver tilstrækkelige teams for normal speed,
- manglende/udmattede heste reducerer march speed,
- artilleri kan have forskellig movement burden efter type/weight,
- wagons og artillery må ikke få cavalry speed bare fordi formationen også indeholder ryttere.

## B-403 — Route/terrain/weather movement modifiers
**Status:** PLANLAGT

Den samme formation skal bevæge sig forskelligt på hovedvej, lokal vej, dårligt terræn, bakker, skov, mudder, sne og crossings.

Acceptance:
- edge road class indgår i effective speed,
- terrain/weather modifiers er bounded og forklarlige,
- bridge/ferry/crossing kan have særskilt delay/capacity,
- UI kan vise base speed og vigtigste modifiers,
- route planner kan vælge hurtigste frem for blot korteste distance.

## B-404 — Column length, congestion and passage delay
**Status:** PLANLAGT

Store formationer har fysisk operationel længde og kan ikke behandles som et punkt uden friktion.

Acceptance:
- strength, wagons, artillery og march organization kan påvirke column length,
- narrow roads/bridges/stations kan skabe passage delay,
- flere formationer på samme corridor kan skabe congestion,
- fronten af kolonnen kan ankomme før bagenden uden at formationen straks regnes fuldt deployable,
- diagnostics forklarer delay source.

## B-405 — March schedule, rest and daily distance
**Status:** PLANLAGT

Skeln mellem øjeblikkelig marchfart og realistisk distance pr. campaign-døgn.

Acceptance:
- normal march indeholder movement/rest windows,
- daily distance afhænger af effective speed og faktisk marching time,
- natmarch kan være særskilt policy/order,
- rest reducerer fatigue gennem hooks,
- ETA bruger hele marchplanen og ikke `distance / top speed` alene.

## B-406 — Forced march and straggling
**Status:** PLANLAGT

Forced march kan øge fremdrift mod en reel pris.

Acceptance:
- forced march kan øge march hours og/eller movement effort bounded,
- fatigue stiger hurtigere,
- straggler/casualty/horse-condition hooks findes,
- battle readiness kan være reduceret ved arrival,
- forced march kan ikke ophæve hard mobility bottlenecks som manglende heste eller impassable edge.

## B-407 — Split, detach and rejoin for mobility
**Status:** PLANLAGT

Spilleren/AI kan vælge at løsne en langsom component fra hovedstyrken i stedet for altid at marchere efter den langsomste.

Acceptance:
- artillery/train kan detach'es med egen formation/order hvis organisationen tillader det,
- hovedstyrkens bottleneck recalculates efter detach,
- detached component bevarer egen location/route/ETA,
- rejoin kræver faktisk co-location/contact state,
- systemet må ikke duplikere personel/equipment.

## B-408 — Movement ETA, planner and explanation UI
**Status:** PLANLAGT

Route preview skal vise realistisk ETA og hvorfor formationen bevæger sig med den givne fart.

UI-eksempel:
- Distance: 86 km
- Base formation rate: 4.2 km/h
- Bottleneck: 12-pdr artillery train
- Road modifier: x1.00
- Rain/mud: x0.82
- Congestion: +1h 40m
- Scheduled rest: 7h
- Estimated arrival: 3 Feb 1864 14:20

Acceptance:
- ETA recalculates ved route/state change,
- bottleneck component vises,
- player kan se effekt af detaching slow units eller rail transport,
- UI viser ikke falsk præcision hvis intelligence/state er ukendt.

## B-409 — Formation mobility QA and final v00.00.12 gate
**Status:** PLANLAGT

B-409 overtager rollen som endelig v00.00.12 promotion-gate efter tilføjelsen af formation mobility/march work package.

Acceptance:
1. Unity 6.6 compiler: 0 blocking errors.
2. B-399 society/events checkpoint er bestået eller eksplicit deferred med begrundelse.
3. Foot infantry, cavalry, horse-drawn artillery og wagon train har forskellige data-driven mobility profiles.
4. En mixed formation bruger den langsomste required component som effective speed bottleneck.
5. Detach/rejoin ændrer bottleneck uden state duplication.
6. Horse shortage kan reducere cavalry/artillery/transport mobility.
7. Road/terrain/weather/crossing modifiers påvirker ETA deterministisk.
8. March/rest/forced-march model giver bounded fatigue/ETA behavior.
9. Route planner kan forklare speed, bottleneck, delays og arrival time.
10. Rail troop transport bruger B-381 lifecycle og erstatter normal march speed under rail transit.
11. Tactical AI/navigation/formation steering er ikke ændret af campaign mobility work.
12. Full v00.00.12 regression har ingen blocking campaign-state errors.

Når B-409 er godkendt kan næste campaign-version promoveres/oprettes.
