# PROJECT 1864 — B-240–B-249: Kavaleri og dragoner — udvidet model

**Status: BESLUTTET / PLANLAGT EFTER P0A v00.00.09**  
**Designbaseline: v00.02.08**

Dette supplement udvider de eksisterende B-040–B-043-beslutninger. Kavaleri skal ikke reduceres til en charge-knap. Dets værdi ligger i reconnaissance, screening, pursuit, communication, flank security, raids og exploitation, mens dragoner også kan kæmpe afsiddet.

## B-240 — Mounted combat state

**BESLUTTET.** Mounted cavalry/dragoon formationer har egne states for:

- mounted readiness,
- horse fatigue,
- horse casualties,
- formation cohesion,
- terrain suitability,
- charge momentum,
- remount availability.

Hestens state er separat fra rytterens personelstate.

## B-241 — Dragoner mounted/dismounted

**BESLUTTET.** Dragoner kan:

- marchere og manøvrere mounted,
- dismount for sustained fire/combat,
- efterlade horses ved horse holders,
- remount efter en tidskrævende reorganisering.

Dismounted dragoner behandles kampmæssigt som en let/kompakt infantry-like formation med deres konkrete weapon profile; de må ikke få mounted mobility samtidig.

## B-242 — Horse holders og remount risk

**BESLUTTET.** Når dragoner kæmper afsiddet, eksisterer hestene som en reel tactical asset bag/ved formationen.

- horse holders kan blive ramt,
- heste kan blive dræbt, såret, spredt eller panikke,
- remount tager tid,
- tab af heste reducerer efterfølgende strategic mobility,
- en afsiddet enhed kan i værste fald blive tvunget til at fortsætte som fodtropper.

## B-243 — Reconnaissance og screening

**BESLUTTET.** Cavalry er en vigtig sensor-/counter-sensor-komponent.

Roller omfatter:

- forward reconnaissance,
- flank reconnaissance,
- screening af egne march columns/HQ,
- counter-recon mod fjendens scouts/cavalry,
- observation af roads/bridges/fords,
- maintaining contact med en withdrawing enemy.

Resultatet feeds ind i den eksisterende fog-of-war/knowledge-state model.

## B-244 — Courier support og HQ communication

**BESLUTTET.** Mounted personnel understøtter command networket.

Kavaleri/dragon assets kan senere:

- stille couriers/escorts til rådighed,
- beskytte HQ/order routes,
- jage fjendtlige couriers,
- sikre forbindelse mellem detached formations.

Det giver en direkte kobling mellem cavalry, HQ-systemet og order delay.

## B-245 — Pursuit og exploitation

**BESLUTTET.** Cavalry er særlig effektiv mod:

- routed/disordered infantry,
- retreating artillery/transport,
- exposed supply columns,
- isolated skirmishers,
- broken formations uden støtte.

Pursuit kan øge prisoners, captured guns/wagons og missing casualties, men giver fatigue og risiko for overextension.

## B-246 — Charge model

**PLANLAGT.** Charge-effekt afhænger ikke af en universel cavalry bonus.

Vigtige inputs:

- target formation/state,
- cavalry cohesion/momentum/fatigue,
- terrain/obstacles,
- surprise,
- flanking/rear aspect,
- support,
- weapon/doctrine,
- officer stats,
- target fire discipline/morale.

Et frontalt charge mod steady, formed infantry med god fire discipline skal være meget risikabelt. Charge mod shaken/routed/exposed formations kan være ekstremt effektivt.

## B-247 — Terrain restrictions

**BESLUTTET.** Cavalry mobility og charge quality påvirkes kraftigt af:

- woods,
- ditches,
- fences/walls,
- marsh/soft ground,
- steep slopes,
- narrow roads/bridges,
- villages/buildings.

Terrain fitting/pathfinding skal derfor behandle mounted formations anderledes end infantry.

## B-248 — Raids, supply interdiction og rear-area operations

**BESLUTTET.** På operations-/strategikortet kan cavalry bruges til at:

- raid supply routes,
- capture/destroy wagons,
- threaten depots,
- cut couriers/telegraph links,
- force enemy escorts/screens,
- probe crossings and roads.

Det skal ikke kræve, at en hel provins er erobret for at påvirke fjendens logistics.

## B-249 — Cavalry/dragoon acceptance criteria

Senere prototype skal mindst verificere:

1. Mounted og dismounted state har reelt forskellig mobility/combat capability.
2. Dismount efterlader horses/holders som separat tactical state.
3. Remount tager tid og fejler/delvist reduceres hvis heste er mistet.
4. Cavalry recon forbedrer egen knowledge state uden omniscience.
5. Screen kan reducere enemy reconnaissance quality.
6. Pursuit mod routed enemy giver væsentligt større capture/straggler-risk end mod steady formations.
7. Charge-resultat varierer kraftigt efter target state, facing og terrain.
8. Woods/fences/ditches reducerer mounted maneuver/charge effectiveness.
9. Cavalry kan true/cut supply/courier routes.
10. Officer AI kan vælge mellem screen/recon/hold/charge/pursuit/dismount efter mission og officer stats.

## Historical research gate

De konkrete 1864-regimentstyper, styrker, weapons, remount practice, doctrine og nation-specifikke forskelle mellem dansk, preussisk og allieret kavaleri/dragoner skal research-verificeres, før endelige stats låses. Dette dokument fastlægger systemarkitekturen, ikke historiske numerical ratings.
