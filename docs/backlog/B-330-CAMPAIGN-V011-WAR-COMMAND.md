# B-330–B-339 — Campaign v00.00.11 Warfare & Command

## Scope

Campaign-level warfare, command and battle-preparation hooks. Tactical AI må ikke ændres på denne branch; campaign skal kun levere state/context til tactical handoff.

## B-330 — Formation echelon hierarchy
**Status:** PLANLAGT

Campaign formations skal kunne organiseres som regiment/brigade/division/corps/army uden at UI eller logik er afhængig af GameObject-navne.

Acceptance:
- stable parent/child IDs,
- hierarchy kan vises i OOB panel,
- formation kan flyttes mellem parent formations via dataoperation.

## B-331 — Command radius / communication hook
**Status:** PLANLAGT

Campaign formation får command relation til nærmeste relevante HQ.

Acceptance:
- in-command/out-of-command kan beregnes,
- modellen kan senere bruge officer stats, couriers, rail/telegraph og weather,
- ingen tactical AI ændring.

## B-332 — Strategic order delay seed
**Status:** PLANLAGT

Orders kan have issued time, received time og activation time.

Acceptance:
- instant-order mode kan bruges til QA,
- delayed-order mode kan aktiveres senere,
- pending order er synlig i UI/diagnostics.

## B-333 — Enemy contact uncertainty
**Status:** PLANLAGT

Fjendtlige formationsdata skal kunne være ukendte, estimerede eller daterede.

Acceptance:
- last-known location/time kan lagres,
- styrke kan være range/estimate i stedet for eksakt tal,
- friendly truth og player intel holdes adskilt.

## B-334 — Strategic reconnaissance seed
**Status:** PLANLAGT

Formationer kan generere observation/intelligence omkring nærliggende nodes og routes.

Acceptance:
- reconnaissance radius/data hook,
- observation timestamp,
- senere cavalry/scout/officer modifiers kan tilføjes uden redesign.

## B-335 — Battle initiation rules
**Status:** PLANLAGT

Definér hvornår to fjendtlige formationsstates skaber battle/contact.

Acceptance:
- same-node hostile contact understøttes,
- crossing/opposing movement hook dokumenteres,
- battle trigger er deterministisk i QA preset.

## B-336 — Reinforcement window
**Status:** PLANLAGT

Nærliggende formationer kan markeres som mulige reinforcements til et forestående slag.

Acceptance:
- ETA til battle node beregnes,
- reinforcement eligibility er campaign data,
- tactical handoff kan modtage reinforcement manifest senere.

## B-337 — Retreat destination selection
**Status:** PLANLAGT

Efter tactical result skal en retreating formation kunne finde gyldig friendly destination.

Acceptance:
- hostile-controlled node undgås,
- blocked/unreachable destination afvises,
- fallback state er sikkert og diagnostiserbart.

## B-338 — Battle aftermath state
**Status:** PLANLAGT

Campaign skal kunne modtage resultater som casualties, prisoners, fatigue, ammo og control change.

Acceptance:
- battle result har stable schema/version,
- result kan anvendes én gang idempotent,
- log viser før/efter state for QA.

## B-339 — Campaign battle history
**Status:** PLANLAGT

Slag gemmes som campaign history records.

Acceptance:
- dato/tid, location, deltagere og result summary lagres,
- historik kan vises uden at genberegne simulation,
- senere medals/flags/unit history kan linke til samme record.
