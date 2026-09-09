# PROJECT 1864 — v00.00.09k Full-Scale OOB, officer command og special-arms pilot

## Formål

09k ændrer tactical prototype fra små 570–620 mands QA-regimenter til et første full-scale 1864-scenarie med fire regimenter i realistisk størrelsesorden og 1:1 visuel manpower rendering. Regimentet er fortsat spillerens normale command-enhed, mens bataljoner og kompagnier etableres som interne OOB-niveauer.

## Fire full-scale regimenter

Pilot-scenariet bruger:

- Danmark — 1. Regiment: ca. 1.620 mand, 2 bataljoner, 8 kompagnier.
- Danmark — 5. Regiment: ca. 1.587 mand, 2 bataljoner, 8 kompagnier.
- Preussen — 8th Regiment: ca. 2.460 mand, 3 bataljoner, 12 kompagnier.
- Preussen — 18th Regiment: ca. 2.440 mand, 3 bataljoner, 12 kompagnier.

Tallene er QA full-scale styrker baseret på den aftalte 1864-organisationsstørrelse; de er endnu ikke erklæret som kildeverificerede dagsstyrker for de konkrete regimenter.

Hvert regiment får ca. 25 mand regimentsstab i OOB-data. Resten fordeles over kompagnierne, så det interne OOB summerer til regimentets fulde manpower.

## 1:1 rendering uden 1 GameObject pr. soldat

1:1 betyder én synlig soldat pr. nuværende manpower, men ikke én autonom Unity-entity pr. mand.

- Regiment er simulation/movement/combat owner.
- Battalion/company er structural/kommende officer-allocation state.
- Menige tegnes med GPU instancing i batches på op til 1023.
- Ingen individuel MonoBehaviour, NavMeshAgent, collider eller AI pr. menig.
- Existing 09h/09i representative soldier renderer deaktiveres i 09k for at undgå dobbelte formationer.
- Formation slots opbygges på company/battalion-niveau.
- Column bruger en kompakt marchkolonne; Line viser bataljonsblokke i dybden.
- Reload visual følger stadig den faktiske regiment reload state gennem et simpelt 1:1 rifle-pose lag.

Dette er den performance-arkitektur, der senere benchmarkes mod 10k/20k/40k synlige soldater.

## Regimental HQ

Hvert regiment får en mounted command group på tre faktiske personer:

1. Regimentschef.
2. Adjudant.
3. Ordonnans/stabsrytter.

De tre ryttere erstatter tre af stabens visuelle manpower-slots, så HQ-gruppen ikke lægges oven i regimentets styrketal. Gruppen er i 09k visuel og følger regimentet. Senere skal den være command-origin for courier/order lifecycle, officer casualty og command effectiveness.

## Officer-order retning

Spillerens ønskede command model fastlægges som:

- Spilleren giver en officer et mission/objective, ikke individuelle company-waypoints.
- Minimum missions: `DEFEND AREA` og `ATTACK / CAPTURE AREA`.
- Officer AI analyserer objective, fjendekontakt, frontage, terræn, egne styrker, doctrine og officerstats.
- Officer AI fordeler underenheder mellem `ENGAGED`, `SUPPORT` og `RESERVE`.
- Reserve er en reel state, ikke bare en formation placeret bagest.
- Officer kan committe yderligere bataljoner/kompagnier hvis første indsats ikke er tilstrækkelig eller situationen ændres.
- Spilleren kan stadig override på lavere niveau, men normal play bør ikke kræve company-micromanagement.

09k etablerer OOB-data og HQ-visuals. Den fulde objective/reserve allocation AI implementeres som næste command-system gate og må ikke fakes med tilfældige bevægelser.

## Dragoons

09k tilføjer én QA dragoon squadron pr. side:

- ca. 160 ryttere pr. squadron.
- 1:1 visuel mounted strength.
- distinct horse/rider/carbine silhouette.
- ingen infantry reskin.

Fremtidig functional gate:

- mounted movement,
- dismount,
- horse holders,
- timed remount,
- horse casualties/fatigue,
- recon/screening/pursuit/raid,
- mounted vs dismounted combat.

## Feltartilleri

09k tilføjer ét QA field battery pr. side:

- 6 kanoner.
- ca. 120 crew.
- kanon, hjul, carriage/barrel og crew vises særskilt.

Fremtidig functional gate:

- limbered/unlimbered,
- horses/limbers,
- deploy time,
- ammunition type/stock,
- LOS/fire arc,
- crew casualties,
- counter-battery,
- abandon/capture.

Artilleri behandles ikke som et regiment med en anden mesh.

## Performance gate

09k skal først bevise, at ca. 8.100 full-scale infantry plus HQ, cavalry og artillery visuals kan køre stabilt på den aktuelle testmaskine.

Senere benchmark-serie:

- 10.000 synlige mænd,
- 20.000,
- 40.000,
- optional 60.000 stress test.

Målinger: FPS, main-thread ms, GPU ms, draw calls, memory og frame-time ved close/medium/full battlefield samt movement + combat + smoke.

## Acceptance 09k

1. Kun fire infantry-regimenter i hovedscenariet.
2. Build marker viser v00.00.09k TEST.
3. OOB-09K telemetry viser 2 bataljoner/8 kompagnier på danske regimenter og 3/12 på preussiske.
4. SCALE-09K viser VisualRatio=1:1 og PerSoldierGameObject=False.
5. Visuelt antal infantrymen svarer omtrent til CurrentStrength; mounted HQ erstatter tre stabsslots.
6. Hvert regiment har commander + adjutant + orderly på hest.
7. Begge sider har én dragoon squadron og ét seks-kanoners field battery.
8. 09j reload cadence ændres ikke; 09k 1:1 rifle-pose afspejler reload state kosmetisk.
9. V3/manual movement/combat baseline ændres ikke af renderer/OOB-laget.
10. Ingen regression i box selection, range cone eller F10 Uniform Designer.
