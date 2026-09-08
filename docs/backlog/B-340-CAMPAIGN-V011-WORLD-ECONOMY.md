# B-340–B-349 — Campaign v00.00.11 World, Economy & Manpower

## Scope

Foundation for den strategiske verden omkring hærene. Dette er prototype-data og systemhooks; historiske tal, produktionskapaciteter og politiske forhold skal source-valideres senere.

## B-340 — Population/manpower pool seed
**Status:** PLANLAGT

Nations/regions får manpower pools adskilt fra formation strength.

Acceptance:
- manpower er persistent campaign state,
- recruitment kan trække fra pool senere,
- negative værdier forhindres.

## B-341 — Recruitment policy hook
**Status:** PLANLAGT

Data model for voluntary/draft/conscription-style recruitment policy.

Acceptance:
- policy kan ændres uden at hardcode nation-specifik UI,
- policy kan have manpower, political og training modifiers senere.

## B-342 — Replacement flow foundation
**Status:** PLANLAGT

Formationer kan modtage replacements gennem campaign systemet.

Acceptance:
- replacements kommer fra gyldig pool,
- formation max strength respekteres,
- transfer logges med source/destination/count.

## B-343 — Equipment stockpile seed
**Status:** PLANLAGT

National/depot stock kan repræsentere våben, ammunition og basisforsyninger.

Acceptance:
- stock categories er datadrevne,
- depot kan have separat inventory,
- stock må ikke blive negativ.

## B-344 — Basic production tick
**Status:** PLANLAGT

Prototype production pr. campaign time interval.

Acceptance:
- production kan slås fra til QA,
- output går til defineret stockpile,
- production rates er data og ikke UI-hardcode.

## B-345 — Resource categories foundation
**Status:** PLANLAGT

Foundation for money, food, wood, iron og stone som senere economy inputs.

Acceptance:
- resources kan lagres nationalt/regionalt,
- samme schema kan udvides,
- diagnostics kan vise balance.

## B-346 — Treasury/budget seed
**Status:** PLANLAGT

Nation får treasury og simple income/expense records.

Acceptance:
- transaction log med reason,
- budget kan ikke ændres skjult af UI,
- senere army upkeep/trade hooks kan tilføjes.

## B-347 — Regional ownership/economic controller split
**Status:** PLANLAGT

Geographic region, political controller og economic contribution holdes som separate begreber.

Acceptance:
- occupation kan ændre controller uden at omskrive geografi,
- economic contribution kan reduceres separat,
- data understøtter contested state senere.

## B-348 — Trade route hook
**Status:** PLANLAGT

Strategiske links kan senere bære trade/resource flow.

Acceptance:
- route kan identificere origin/destination/resource/capacity,
- military movement og trade bruger fælles infrastructure data men separat flow state.

## B-349 — Economy diagnostics panel
**Status:** PLANLAGT

QA-panel for manpower, treasury, stockpile, production og depot summary.

Acceptance:
- read-only diagnostics,
- ændrer ikke simulation,
- kan kopieres til console/log som kompakt snapshot.
