# Del 17: 52 — Campaign3 v00.00.10n5: taktisk tilbagetrækning og BRYD KONTAKT

**Designbaseline:** v00.02.18  
**Runtimeversion:** v00.00.10n5  
**Work branch:** `work/channel-campaign3-v10n5-withdrawal-orders`  
**Status:** IMPLEMENTERET / AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA

## 52.1 Formål

v10n5 indfører kontrollerede tilbagetrækningsordrer som noget andet end morale-betinget flugt/rout.

Der skelnes mellem:

- **TILBAGETRÆK HERTIL** — overordnet ordre med et valgt samlepunkt,
- **KÆMPENDE TILBAGETRÆKNING** — automatisk adfærd for enheder, som stadig er i kampkontakt,
- **BRYD KONTAKT** — nødordre hvor enheden stopper kampen og forsøger hurtigst muligt at komme fri,
- **ROUT / FLUGT** — eksisterende ukontrolleret morale-state, som fortsat er separat.

## 52.2 TILBAGETRÆK HERTIL

Spilleren vælger en eller flere danske regimenter, aktiverer `TILBAGETRÆK HERTIL` og klikker derefter et punkt i terrænet. Punktet bliver et fælles `WithdrawalRallyPoint` for de berørte enheder.

Første runtime-slice anvender de aktuelt valgte regimenter som command scope. Når HQ-/brigade-/major-hierarkiet er implementeret, skal samme ordre kunne udstedes fra et HQ og automatisk propagere til dets underenheder efter command/order-delay-reglerne.

Enhedens reaktion afhænger af kampkontakt:

- ikke i kampkontakt → `RallyMarch` direkte mod samlepunktet,
- i kampkontakt → `FightingWithdrawal` indtil enheden er fri, derefter `RallyMarch`.

## 52.3 Kæmpende tilbagetrækning

`FightingWithdrawal` er en kontrolleret frigørelsesadfærd.

Enheden:

1. beholder front mod den nærmeste relevante fjende,
2. bevæger sig et kort stykke baglæns/udad,
3. stopper kort,
4. kan afgive ild efter sin eksisterende fire policy,
5. fortsætter derefter endnu et tilbagetrækningsstep,
6. gentager indtil den er sikkert ude af kampkontakt,
7. går derefter mod det valgte samlepunkt.

Første prototype bruger en step/fire-cycle frem for en kontinuerlig avanceret retrograde-animation. Senere kan dette erstattes af formation-specifik baglæns bevægelse, rear-guard detachments og officerstyret leapfrog withdrawal uden at ændre den overordnede ordre.

## 52.4 BRYD KONTAKT

`BRYD KONTAKT` er den kontrollerede nødordre, som erstatter idéen om en spillerknap kaldet “løb”.

Ordren betyder:

> Stop med at føre ildkamp. Vend væk fra fjenden og kom hurtigst muligt ud af engagement range.

Runtimeadfærd:

- fire policy sættes midlertidigt til `HOLD`,
- formationen går til column som prototype for mindre ordnet hurtig bevægelse,
- enheden bevæger sig direkte væk fra nærmeste trussel,
- der gives et ekstra speed-lag oven på normal movement for at repræsentere løb/hastig frigørelse,
- når enheden har været sikkert uden for kontakt i et kort confirmation-vindue, afsluttes nødfrigørelsen,
- tidligere fire policy gendannes,
- findes et tidligere rallypunkt, fortsætter enheden dertil,
- uden rallypunkt stopper enheden efter frigørelsen.

BRYD KONTAKT skal senere koste mere fatigue/cohesion og give højere sårbarhed mod ild, pursuit og cavalry end en kæmpende tilbagetrækning. Første n5-slice fokuserer på command/state/movement-adfærd.

## 52.5 Rout/Flugt er ikke det samme

Eksisterende `Regiment.IsRouted` og morale-triggeret `Route()` fortsætter uændret som ukontrolleret flugt.

| State | Kontrolleret | Skyder under frigørelse | Formation | Destination |
| --- | --- | --- | --- | --- |
| FightingWithdrawal | Ja | Ja, periodisk | Line/front mod fjende | Først fri, derefter rallypunkt |
| BreakContact | Ja, nødordre | Nej | Hurtig/column prototype | Ud af kontakt, derefter evt. rallypunkt |
| Rout/Flugt | Nej | Nej | Kollapset | Automatisk væk fra slaget |

## 52.6 Officer AI

Hvis `AI UNIT` er aktiv på regimentet, tager withdrawal-controlleren midlertidigt direkte kontrol, så Officer AI ikke samtidig forsøger at genoptage attack/defend/move missions.

Efter en kontrolleret withdrawal-order er afsluttet, genaktiveres Officer AI, hvis den var aktiv før ordren og regimentet ikke er routed.

Dette er en overgangsarkitektur. Den endelige HQ-/order-lifecycle-model skal repræsentere withdrawal som en native officer mission med ordre-delay, acknowledgement, officer interpretation, discipline og initiative.

## 52.7 UI

`PrototypeWithdrawalCommandManagerV010N5` viser en kompakt ordrelinje over den eksisterende Officer AI-kommandobar.

Kontroller:

- `TILBAGETRÆK HERTIL` → armer næste terrænklik som rallypunkt,
- `[KLIK SAMLEPUNKT]` → vises mens destination afventes,
- `BRYD KONTAKT` → udføres straks på de aktuelt valgte danske regimenter,
- statusfelt viser antal armed targets og aktive tilbagetrækninger.

## 52.8 Første runtime-begrænsninger

- Command scope er valgte regimenter; egentlig Major/Brigade/HQ propagation kommer med HQ-hierarkiet.
- Engagement afgøres i første slice primært af afstand til nærmeste aktive fjende og begge enheders weapon ranges.
- Fighting withdrawal er step/fire, ikke endnu kontinuerlig backwards formation movement.
- Break-contact speedboost er en prototype og er endnu ikke koblet til den fulde fatigue/cohesion-model.
- Artilleri, cavalry, limbering, abandoned guns og specialist withdrawal kræver senere armspecifik adfærd.

## 52.9 QA

1. Unity 6000.6.0f1 compiles uden errors.
2. Buildbadge viser `v00.00.10n5`.
3. TILBAGETRÆK HERTIL kan armes og modtage et terræn-rallypunkt.
4. Ikke-engageret regiment går direkte mod rallypunktet.
5. Engageret regiment går i FightingWithdrawal.
6. FightingWithdrawal holder front mod fjenden under movement-step.
7. Firing-step stopper enheden, så eksisterende fire policy kan afgive ild.
8. Efter sikker frigørelse skifter state til RallyMarch.
9. BRYD KONTAKT sætter midlertidigt HOLD FIRE.
10. BRYD KONTAKT vender enheden væk fra fjenden og øger frigørelseshastigheden.
11. Original fire policy gendannes efter frigørelse.
12. Eksisterende rallypunkt genbruges efter BRYD KONTAKT.
13. Rout/Flugt forbliver separat og ignorerer kontrollerede withdrawal-ordrer.
14. Officer AI genaktiveres efter afsluttet ordre, hvis den var aktiv før ordren.
15. Normal move/attack/hold/fire-policy/formation og campaign runtime regressionsbestår.
