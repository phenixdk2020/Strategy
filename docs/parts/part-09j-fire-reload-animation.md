# PROJECT 1864 — Designmanual supplement — v00.00.09j Fire & Reload Animation Pass

## Formål

`v00.00.09j` kobler de repræsentative infantry-soldaters animation til den eksisterende combat cadence uden at ændre combat-resultater, reload-tider, ammunition, movement eller AI.

## Autoritativ simulation vs. visual state

`Regiment` forbliver autoritativ for, hvornår en volley faktisk afgives og hvornår næste skud må afgives. Visual-laget må kun aflæse denne timing og visualisere den.

Den tidligere 09i fire-pose brugte `HasHitFeedback`, som beskriver at regimentet er blevet ramt. Det er ikke et korrekt signal for at regimentet selv har affyret en volley. 09j bruger derfor ændringen i regimentets eksisterende `nextFireTime` som edge-trigger for en faktisk udgående volley.

## Fire → Reload → Ready

Efter en faktisk volley skal de repræsentative riflemen gennem følgende visuelle state-sekvens:

1. **Fire pose** — kort shoulder/firing hold umiddelbart efter volley.
2. **Reload** — våbentype-specifik kosmetisk reload-sekvens, skaleret til den faktiske aktuelle reload-periode.
3. **Ready** — returnerer til våbenklar stilling når reload-perioden er afsluttet.

Standard-bearer uden aktiv rifle deltager ikke i rifle-reload-animationen.

## Rifled muzzle-loader

Den danske muzzle-loader-sekvens skal som prototype mindst læses som:

- sænk/rejs geværet,
- arbejd ved cartridge/charge,
- ladning/ramrod-bevægelse,
- tilbage til ready.

Dette er en visuel prototype og ikke endnu en frame-for-frame historisk drill-rekonstruktion. Endelig drill-animation kræver source-validering og egentlige riggede animations-assets.

## Dreyse needle rifle

Den preussiske Dreyse-sekvens skal visuelt være kortere og anderledes:

- sænk geværet,
- bolt/cartridge-arbejde,
- luk/recover,
- tilbage til ready.

Forskellen skal styrke weapon/faction readability uden at ændre den eksisterende `BaseReloadSeconds`/experience-baserede cadence.

## Formation-wide variation

Volley-firing må læses som koordineret formation fire, men repræsentative soldiers må have en lille deterministisk phase/stagger under reload, så alle figurer ikke bevæger armene som perfekte kloner.

## Isolation rule

09j må ikke:

- ændre `nextFireTime`,
- ændre `CurrentReloadSeconds`,
- ændre accuracy/hits/shock,
- ændre movement destination eller formation,
- ændre Officer AI,
- ændre PlayerCommander input,
- ændre ammunition eller casualty resolution.

## Input platform

Unity 6.6 markerer legacy Input Manager som deprecated. Migration til det nye Input System er en separat platform/control-opgave og må ikke blandes ind i 09j, fordi LMB/RMB, box-select, keyboard hotkeys og UI pointer guards skal migreres og QA-testes samlet.

## 09j acceptance-gate

1. Build compiles uden røde errors.
2. Build marker viser `PROJECT 1864 | v00.00.09j TEST`.
3. Console viser `RELOAD-09J|Installed=True`.
4. En faktisk volley udløser `VolleyDetected=True` på skyttens regiment, ikke på den enhed der blot bliver ramt.
5. Danske muzzle-loader infantry viser en tydelig længere loading/ramming sekvens.
6. Preussiske Dreyse infantry viser en kortere bolt/cartridge-sekvens.
7. Reload-animation slutter omtrent samtidig med den eksisterende combat reload-window.
8. Standard-bearer uden rifle forsøger ikke at reload.
9. Ingen movement/combat/AI-semantik ændres.
10. Bridge/contact movement oddities fra QA-videoen følges separat og må ikke løses ved at ændre reload-laget.
