# PROJECT 1864 — Backlog B-200–B-209: Range bands, HQ, semantic zoom og couriers

**Status: BESLUTTET / PLANLAGT**  
**Designbaseline: v00.02.08**  
**Implementering: efter første P0A v00.00.09 Officer AI-gate**

Dette supplement samler fire tæt forbundne UI-/command-systemer: visuel opdeling af våbenrækkevidde, fysiske HQ-enheder, hierarkiske command-links, semantic zoom og synlige budbringere/ordrer. De skal bygge oven på officer-AI, order lifecycle og fog-of-war i stedet for at være kosmetiske overlays uden simulation bag sig.

## B-200 — Close / Medium / Long range bands

**BESLUTTET.** Når spilleren viser en enheds skudfelt, opdeles det visuelt i tre bands:

- **Close**: ca. 0–50 % af våbnets `EffectiveRange`.
- **Medium**: ca. 50–100 % af `EffectiveRange`.
- **Long**: fra `EffectiveRange` til `MaximumRange`.

De præcise grænser er data pr. weapon profile og kan senere justeres historisk. Ovenstående er første designbaseline.

UI:

- Range overlay følger terrænet så vidt praktisk muligt.
- De interne grænser vises som tynde stiplede/dashed linjer.
- Den yderste `MaximumRange`-grænse skal være tydeligere end de interne bands.
- Close / Medium / Long kan vises med diskrete labels ved valgt enhed, men må ikke fylde hele slagmarken med tekst.
- Ved multi-select vises som standard kun den aktive/fokuserede enheds detaljerede bands for at undgå visuel støj.

## B-201 — Accuracy er kontinuerlig, bands er UI

**BESLUTTET.** Close/Medium/Long er ikke tre hårde hit-chance-trin. Den underliggende hit probability skal ændre sig kontinuerligt med afstand.

Det betyder:

- Close giver typisk den højeste hit probability.
- Medium er våbnets normale effektive kampafstand.
- Long er markant mindre sikker, men stadig mulig op til `MaximumRange`.
- Morale, cohesion, training, experience, stance, target density, cover, smoke, weather og weapon quality kan senere modificere samme continuous curve.
- Der må ikke opstå kunstige spring i accuracy, blot fordi målet krydser en stiplet UI-linje.

Første prototype kan bruge simple piecewise/lerp-kurver, men arkitekturen skal være en data-driven `AccuracyByRange`-curve pr. våbenprofil.

## B-202 — HQ som fysisk command-entity

**BESLUTTET.** Højere formationsniveauer som brigade, division, korps og army får et egentligt HQ på kortet.

Et HQ repræsenterer mindst:

- commander,
- staff,
- HQ position,
- command state,
- couriers/order capacity,
- communication links,
- eventuel escort,
- senere horses/wagons/telegraph access og HQ fatigue/disruption.

HQ-positionen er simulation. Hvis HQ flyttes langt væk, afskæres, angribes eller mister forbindelser, skal det kunne påvirke order delay, reporting og officer-AI.

## B-203 — Klik på HQ viser kommandokæden

**BESLUTTET.** Når et HQ vælges, vises command-links til dets direkte underenheder.

Standardvisning:

- vis kun **immediate children** for at undgå line-spaghetti,
- brigade-HQ → regimenter/bataljoner,
- division-HQ → brigader og direkte attached assets,
- corps-HQ → divisioner og corps assets,
- attached artillery/engineers/supply kan have særskilt attachment-link.

En udvidet command-overlay kan vise hele kæden nedad eller opad efter spillerens valg.

Command relationship og en aktiv courier/order må ikke bruge samme visuelle sprog:

- **command-link** = organisatorisk relation,
- **order-route** = konkret besked, der fysisk er på vej.

## B-204 — Semantic zoom og NATO/APP-6-lignende symboler

**BESLUTTET.** Enhedsgrafikken skifter med kameraets zoomniveau i stedet for blot at blive mindre og mindre.

Foreslået semantic zoom:

1. **Tæt zoom** — fulde 3D-formationer, soldater, kanoner, heste, HQ-visuals og casualty visuals.
2. **Mellemzoom** — forenklet formationsgrafik/banners og færre individuelle modeller.
3. **Lang zoom** — NATO/APP-6-lignende taktiske symboler med unit type, echelon og retning.
4. **Meget lang strategisk zoom** — formationer kan aggregeres på brigade/division/corps-niveau afhængigt af valgt command layer.

NATO-symbolerne er en moderne **UI-abstraktion**, ikke in-world 1864-grafik. Spilleren kan senere få mulighed for historiske alternative symbolsets.

Teknisk krav:

- overgang skal have hysteresis/fade, så grafik ikke flakker omkring en zoomgrænse,
- simulationen ændres ikke ved LOD/semantic zoom,
- selection og order targets skal bevare samme stable unit ID gennem alle visual modes.

## B-205 — Courier/order-route visual

**BESLUTTET.** En aktiv ordre skal kunne visualiseres som en stiplet/dashed rute fra afsenderens HQ til modtagende formation/HQ.

På ruten vises en bevægelig markør:

- lille courier/horse icon ved tættere zoom,
- simpel kugle/chevron/pil ved større afstand,
- markørens position langs ruten svarer til faktisk simulation progress, ikke en kosmetisk timer,
- markørens retning viser om ordren går ud til enheden eller en acknowledgement/report er på vej tilbage.

Når spilleren hover/selecter ruten eller ordren, vises fx:

`Sent 10:42 | Courier 63% | ETA ~1m40s | En route`

ETA er et estimat; actual delivery afhænger af terræn, vej, movement, vejr, enemy interference og HQ relocation.

## B-206 — Courier er en simulation entity, men ikke tung soldier-AI

**BESLUTTET.** Budbringeren skal have faktisk progress gennem verden, men må implementeres letvægtsmæssigt.

Minimum state:

- sender HQ,
- recipient / last known recipient position,
- order/report ID,
- route,
- current route progress,
- movement speed,
- status: dispatched / en route / searching / delayed / delivered / lost,
- direction: outbound order eller returning acknowledgement/report.

Senere kan courieren:

- blive forsinket af terræn/vej/vejr,
- søge efter et HQ, der har flyttet sig,
- blive afskåret, såret/fanget/dræbt,
- få hesten udmattet eller mistet,
- erstattes af ny courier ved timeout eller urgent resend.

Der er ikke behov for fuld individuel combat-AI. Tæt på kameraet kan en ryttermodel følge den samme lightweight route state; langt væk eksisterer kun data + UI-marker.

## B-207 — Command overlay og clutter control

**BESLUTTET.** Command-links og courier routes vises ikke permanent for hele hæren.

De vises når mindst én af følgende er sand:

- et HQ er valgt,
- en enhed med aktiv ordre er valgt,
- spilleren aktiverer `Command Overlay`,
- spilleren åbner en specifik ordre i message/order UI.

Standard skal prioritere læsbarhed frem for at vise hele kommunikationsnettet konstant.

## B-208 — Order status på HQ og unit card

**PLANLAGT.** HQ/unit UI viser samme order lifecycle som simulationen:

`Drafted → Sent → In transit → Delivered → Acknowledged → Executing → Superseded/Failed`

Det skal være muligt at skelne mellem:

- ordren er sendt,
- courieren er på vej,
- ordren er leveret,
- officeren har forstået/acknowledged,
- formationen udfører den faktisk.

Det er centralt for HQ-perspektivet: spilleren må ikke automatisk vide, at en modtager allerede handler på ordren.

## B-209 — Kobling til Officer AI

**BESLUTTET.** Courier/HQ-systemet bliver transportlaget for Officer AI.

Når `AI UNIT ON`:

- officeren fortsætter sin nuværende mission og lokale autonomi,
- en ny spiller-/overordnet ordre ændrer ikke missionen før ordren faktisk er delivered/understood efter command-modellen,
- officerens Staff/Command Skill, Discipline, Initiative og Composure kan påvirke acknowledgement, fortolkning og reaction efter levering.

Når `AI UNIT OFF` i høj realism mode gælder samme transportfriktion: direkte spillerinput repræsenterer en HQ-ordre og må ikke teleporteres. Assistance/arcade settings kan senere reducere eller fjerne denne delay.

## Prioritet efter v00.00.09

Anbefalet rækkefølge efter første Officer AI-gate:

1. **Range bands** som relativt lavrisiko battle-UI-forbedring.
2. **HQ entity + command relationship overlay**.
3. **Courier/order lifecycle MVP** med moving progress marker.
4. **Semantic zoom / NATO-symbol mode**.
5. Derefter dybere courier failure, reports/acknowledgements og højere-level brigade/division AI.

Denne rækkefølge holder P0A v00.00.09 ren, men bringer hurtigt projektet videre mod vertical-slice-målet med reelle HQ'er, order delay og hierarkisk officer-AI.
