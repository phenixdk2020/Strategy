# PROJECT 1864 — P0A v00.00.09i1 — V3 hard-obstacle detour continuation

## Purpose

Denne revision dokumenterer en konkret navigation-fejl observeret under direkte spillerstyring med Officer AI OFF. Fejlen er uafhængig af 09i Soldier Visual Pass 2.

## Bekræftet QA-fejl

1. Regiment modtog en normal PlayerCommander move-route.
2. Navigation V3 identificerede et `Farmhouse` som hard obstacle og valgte et persistent detour-punkt.
3. Regimentet nåede det første detour-punkt, men Farmhouse blokerede stadig den direkte vej til route-goal.
4. V3 afsluttede den nåede detour-leg og valgte derefter samme eller praktisk talt samme detour-punkt igen.
5. QA-loggen viste tusindvis af identiske `NAV-V3 ... Detour=True ... Obstacle=Farmhouse ... Via=...` entries.
6. 09h4 manual route recovery opdagede senere no-progress og indsatte et bypass, men route sanitisation/goal adjustment kunne ændre bypasset, hvorefter base-loopet fortsatte.

## 09i1 regel

Et reached detour-point betyder **ikke**, at obstacle-detouren er færdig, hvis samme hard obstacle stadig ligger mellem formationen og route-goal.

Når V3 allerede ejer en Farmhouse/Barn-detour og formationen står ved det aktuelle detour-point:

- den lokale route skal fortsætte rundt om samme obstacle,
- næste continuation-point skal ligge på en sikker outer ring omkring obstacle clearance,
- den lokale leg må ikke skære gennem samme hard obstacle,
- V3's egne destination/path-penalty queries bruges til validation,
- en lokal continuation må ikke opfinde en ny river crossing,
- continuation er bounded og telemeteret,
- regimentet må aldrig teleporteres som normal løsning.

`PrototypeNavigationV3DetourContinuation09I1` implementerer denne compatibility-regel umiddelbart efter V3 og skriver det valgte continuation-point tilbage i V3's eksisterende private NavigationState. V3 forbliver steering-owner næste frame.

## Obstacle semantics

- `Farmhouse`: hard obstacle; continuation aktiv.
- `Barn`: hard obstacle; continuation aktiv.
- Decorative `Tree`: soft/pass-through i den nuværende prototype.
- Individual `FencePost`: soft/pass-through i den nuværende prototype.
- River/water: hard constraint; bridge-only crossing.

## Telemetry

Succes:

`NAV-V3-09I1|Unit=...|Continuation=True|Obstacle=Farmhouse|FromVia=...|NextVia=...|Goal=...|Count=...`

Failure/guard:

- `Reason=NoSafeContinuation`
- `Reason=ContinuationLimit`

Det gamle mønster med samme obstacle + samme Via gentaget frame efter frame er en QA-failure.

## Architecture invariant

09i1 er ikke et nyt selvstændigt navigation-system. Det må kun korrigere V3's allerede aktive hard-obstacle detour-state. PlayerCommander ejer player route/intent; Navigation V3 ejer steering; 09h4 manual route recovery er bounded higher-level fallback.

## Graphics isolation

Hele `v00.00.09i Soldier Visual Pass 2` bevares uændret: articulated procedural infantry, uniform designer integration, officer/standard-bearer distinction, close-detail LOD og procedural animated regimental standards må ikke påvirke movement state.

## Acceptance test

- AI OFF på de danske regimenter.
- Box-select de fire danske regimenter.
- Giv samme gruppeordre som i QA-fejlen gennem Farmhouse-området.
- 1. Regiment skal fortsætte rundt om bygningen i stedet for at parkere ved første Via.
- `NAV-V3-09I1 Continuation=True` må gerne forekomme 1–få gange.
- Tusindvis af identiske `NAV-V3 Detour=True` for samme Via må ikke forekomme.
- Regimentet må ikke passere gennem Farmhouse/Barn.
- River crossing skal fortsat gå over bridge.
