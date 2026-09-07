# PROJECT 1864 — Designmanual v00.02.08 — Del 3D

## 19.1 Battle supply og resupply under kamp

Ammunition og andre kritiske forsyninger er konkrete beholdninger. En formation må ikke regenerere ammunition automatisk blot fordi den ikke skyder.

Battle resupply kræver en supply source med kompatibel beholdning, transportkapacitet, en brugbar rute og tid til fysisk distribution. Forward ammunition wagons/caissons kan placeres tættere på fronten for hurtigere resupply, mens større field trains normalt ligger længere tilbage og er mindre udsatte.

Resupply efficiency afhænger af distance, terræn/vej, enemy interdiction, formationens movement/combat pressure, available wagons/horses/drivers og staff/logistics quality. En formation i aktiv ildkamp kan derfor blive delvist genforsynet eller slet ikke modtage forsyninger, selv om en overordnet supply line stadig eksisterer.

## 19.2 Skumring, nat og flerdagsslag

Tactical battle har en lys-/tidsfase:

`DAYLIGHT -> DUSK -> NIGHT -> DAWN -> DAYLIGHT`

Skumring er normalt overgangen fra organiseret dagskamp til en natlig operationsfase, men er ikke et universelt hard stop for selve battle instance.

Under natten kan enheder:

- holde position,
- trække sig,
- gennemføre begrænset repositionering,
- samle/evakuere sårede,
- hvile og reorganisere,
- forbedre hasty fieldworks,
- sende couriers/reports,
- modtage ammunition, food, forage og medical supply,
- udføre patrols/screens,
- gennemføre begrænset night attack/infiltration, hvis order/doctrine/officer tillader det.

Normal formation combat reduceres kraftigt af dårlig LOS, command/control og cohesion. Night attack er derfor et specialvalg med stor friktion, ikke standard-adfærd.

## 19.3 Overnight resupply

Natten giver et naturligt resupply-vindue, men resupply er betinget af fysisk forbindelse til egne linjer.

En formation kan kun få overnight resupply, hvis:

- supply route til eget field train/depot er åben eller kan genetableres,
- source har relevante stocks,
- transportkapacitet eksisterer,
- fjenden ikke kontrollerer/interdict'er ruten effektivt,
- formationen kan modtage/distribuere forsyningen.

En afskåret formation får ingen magisk natlig ammunition. Den må fortsætte på carried stocks, erobret materiel eller genetablere forbindelsen.

Overnight resupply kan være delvis. Spilleren kan senere sætte resupply priority, og infanteri-/artilleriammunition behandles som separate beholdninger.

## 19.4 Daylight, dusk og auto-pause

Sunrise/sunset/dusk skal senere afledes af dato, geografisk position og relevant weather/light state. Et fast universelt “slag slutter kl. 18” er ikke ønsket.

Ved skumring kan UI auto-pause og præsentere eksempelvis:

`Hold positions | Withdraw | Continue night operations | Reorganize/resupply | Prepare dawn attack`

Auto-pause er en hjælp til command-level beslutninger og kan senere være en assistance-setting.

Hvis begge sider stadig er i kontakt efter natten, fortsætter samme battle næste dag med persistent casualties, ammunition, fatigue, morale/cohesion, damaged/captured equipment, fieldworks, officer state, positions og supply connectivity.

## 19.5 Kavaleri og dragoner

Kavaleri er en operations- og informationsressource, ikke kun en charge-enhed.

Centrale roller:

- reconnaissance,
- screening/counter-recon,
- flank security,
- courier support/escort,
- pursuit af routed enemy,
- raids mod supply/courier/telegraph routes,
- exploitation af gaps,
- beskyttelse af HQ, artillery og trains.

Mounted mobility afhænger af horse fatigue, horse casualties, terrain og cohesion. Woods, ditches, fences, villages, steep ground og narrow crossings kan gøre mounted maneuver eller charge ineffektiv.

## 19.6 Dragoner mounted/dismounted

Dragoner kan bevæge sig mounted og sidde af til sustained fire/combat. Ved dismount efterlades horses og horse holders som en reel tactical state. Remount tager tid og afhænger af, om hestene stadig er til rådighed og organiserede.

En dragoon formation må ikke samtidig få fordelene ved mounted mobility og dismounted fire capability. Dismounted combat bruger formationens konkrete weapon profile og relevante infantry-like combat rules.

## 19.7 Charge og pursuit

Charge-effekt skal afhænge af target state, facing, terrain, cavalry fatigue/cohesion, momentum, surprise, support og officer quality.

Et frontalt charge mod steady formed infantry med god fire discipline er meget risikabelt. Cavalry er langt farligere mod routed/disordered infantry, exposed skirmishers, retreating artillery/transport og isolerede rear-area targets.

Pursuit kan øge prisoners, captured guns/wagons og missing/straggler outcomes, men koster fatigue og kan føre til overextension.

## 19.8 Kobling til Officer AI og supply

Officer AI skal senere kunne vælge mellem screen, recon, hold, charge, pursuit, dismount/remount og raid efter mission, knowledge state, officer stats og unit condition.

Cavalry raids og counter-recon kobles direkte til supply- og courier-systemet: en cavalry formation kan true en supply route eller communication link uden at erobre hele regionen. Dette kan igen forhindre overnight resupply og skabe command delay.
