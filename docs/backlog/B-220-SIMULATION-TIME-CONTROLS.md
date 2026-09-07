# PROJECT 1864 — B-220–B-229: Simulationstid, pause og klokke

**Status: AKTIV / IMPLEMENTERET I P0A v00.00.09 TEST**  
**Designbaseline: v00.02.08**  
**Target prototype: P0A v00.00.09 TEST**

## B-220 — Fælles simulation clock

**BESLUTTET / IMPLEMENTERET.** BattleManager ejer den aktuelle battle time. P0A starter 1. februar 1864 kl. 10:20. Klokken vises permanent i topbaren.

Tiden er simulationstid og skal ikke være bundet til wall-clock. Valgt speed skalerer simulationens forløb.

## B-221 — Time-control bar

**IMPLEMENTERET i v00.00.09 TEST.** Topbaren har klikbare controls:

`PLAY | PAUSE | x2 | x5 | x20 | dato/klokkeslæt`

- `PLAY` = x1.
- `PAUSE` = Time.timeScale 0.
- `x2` = Time.timeScale 2.
- `x5` = Time.timeScale 5.
- `x20` = Time.timeScale 20.

Aktiv state markeres i UI.

## B-222 — Tastaturgenveje

**IMPLEMENTERET.**

- `Space` = pause/resume med senest valgte speed.
- `1` = Play/x1.
- `2` = x2.
- `3` = x5.
- `4` = x20.

## B-223 — Pause semantics

**BESLUTTET / IMPLEMENTERET.** Pause stopper den authoritative taktiske simulation samlet:

- Officer AI think/reaction timers.
- Regiment movement.
- Reload og Time.time-baseret fire cadence.
- Combat resolution der kræver simulation progress.
- Battle clock.

Følgende skal fortsat være brugbart under pause:

- kamera pan/rotate/zoom,
- UI og hover/inspection,
- selection,
- analyse af officerstats/reason codes.

Senere command-model kan tillade, at spilleren udarbejder ordrer under pause; hvornår de bliver effective afhænger stadig af valgt order-delay/realism model.

## B-224 — Input isolation

**IMPLEMENTERET.** Klik på time-control-panelet må ikke samtidig ramme slagmarken bag UI. PlayerCommander blokerer derfor selection/order-input, når pointeren ligger over simulation controls.

## B-225 — High-speed determinism guard

**BESLUTTET / QA.** x5/x20 skal kun accelerere tid. De må ikke med vilje ændre:

- weapon accuracy,
- reload-sekunder i simulationstid,
- movement speed i simulationstid,
- morale/cohesion-regler,
- AI officer stats,
- difficulty bonuses,
- casualty resolution.

Diskrete Update-timere kan dog være følsomme over for store simulation-deltaer. Det skal derfor testes specifikt ved x20, og senere authoritative systems bør bruge robuste timers/state transitions frem for frame-afhængige antagelser.

## B-226 — UI readability ved speed

Combat-feedback som `Ramte N` må gerne bruge unscaled realtime for at forblive læsbar ved x5/x20. Det ændrer ikke simulationens combat-resultat; kun hvor længe feedbacken er synlig for spilleren.

## B-227 — Battle result

Når slaget afsluttes, går simulationen terminalt i pause. Time controls må ikke genstarte et afsluttet slag. Kun `R`/restart må starte en ny battle state.

## B-228 — v00.00.09 acceptance-test

1. Topbaren viser dato og klokkeslæt.
2. PLAY sætter simulation til x1.
3. PAUSE stopper battle clock, AI, movement og reload/fire progression.
4. Space pauser og genoptager den tidligere speed.
5. x2, x5 og x20 accelererer battle clock og simulation i korrekt relativ retning.
6. Kamera/UI fungerer under pause.
7. Klik på time controls ændrer ikke unit selection bag panelet.
8. `Ramte N` forbliver læsbart ved høj speed.
9. Officer-AI telemetry/reason state kan inspiceres under pause.
10. Terminal battle result kan ikke unpauses via Play/x2/x5/x20.

## B-229 — Senere strategisk tid

Når strategilaget kobles på, skal samme princip udvides til kampagneklokken. Strategisk og taktisk tid må ikke være to uafhængige sandheder. Ved manuelt taktisk slag fryses strategisk world simulation efter den allerede besluttede model og fremskrives kontrolleret til slagets sluttid ved return til strategilaget.
