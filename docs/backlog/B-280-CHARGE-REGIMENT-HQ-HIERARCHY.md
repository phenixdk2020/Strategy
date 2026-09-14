# PROJECT 1864 — B-280–B-291 Charge, Melee, Regiments-HQ og næste våbenarter

**Status:** AKTIV implementeringssekvens — v00.00.09f26 har CHARGE/Melee-baseline, Command-Zone MVP og første Under-Fire Reaction  
**Branch:** `channel-test`

## B-280 — Infantry CHARGE

**Status:** AKTIV / første MVP implementeret i v00.00.09f25

CHARGE er en eksplicit ordre/state, ikke almindelig `OrderAttack()` med kortere afstand. Enheden skal kunne lukke gennem normal fire-distance til fysisk kontakt uden at ranged AI stopper fremrykningen ved Close/Medium/Long.

Første MVP:
- `CHARGE [V]` + enemy target-pick;
- HOLD FIRE under charge approach;
- Line i åbent terræn, bridge-routing må midlertidigt kræve Column;
- ekstra charge-speed/cohesion-cost;
- transition til melee-manager ved fysisk kontakt.

## B-281 — Infantry melee completion

**Status:** AKTIV / basal melee-core eksisterer og er koblet til CHARGE

Videre arbejde:
- bedre contact frontage;
- tydelig engaged state;
- break-off / winner / loser / rout;
- mere visuel melee feedback;
- ingen formation overlap/teleport gennem modstanderen.

FRONT/FLANK/REAR og kort charge-momentum findes allerede som første testlag.

## B-282 — Under-fire reaction

**Status:** AKTIV / første MVP implementeret i v00.00.09f26

AI ON company under movement that receives confirmed hostile fire from a valid enemy inside own Long range should normally suspend movement, deploy/hold Line, face attacker and return fire. Mission is suspended, not deleted.

v00.00.09f26 MVP:
- kun AI ON reagerer automatisk;
- parent mission bevares;
- Line + face local threat + return fire;
- HOLD FIRE respekteres;
- kortere fire policy kan midlertidigt bruge LONG under self-defence og gendannes bagefter;
- reaktionen frigives efter en rolig periode;
- bridge passage er kritisk undtagelse: reaktion deferred til passagen er fri;
- AI OFF er fortsat direkte spiller-authority.

## B-283 — Second battalion

**Status:** NÆSTE HIERARKI-MILEPÆL

Tilføj fire yderligere danske company-scale infanterienheder samt en anden fysisk Major HQ.

Kanonisk struktur:
- Major A → 1. Bataljon → 4 kompagnier.
- Major B → 2. Bataljon → 4 kompagnier.

Begge Majorer skal bruge samme AI/UI/authority model; ingen parallel specialimplementering hvis det kan undgås.

## B-284 — Regimental HQ / Oberstløjtnant

**Status:** PLANLAGT umiddelbart efter B-283

Opret én fysisk Regimental HQ ledet af **Oberstløjtnant**. Det er kommandoniveauet over de to Majorer.

Kanonisk dansk prototype-hierarki:
- Kaptajn → kompagni;
- Major → bataljon (4 kompagnier);
- Oberstløjtnant → regiment (2 bataljoner / 8 kompagnier);
- Oberst → brigade;
- Generalmajor → division.

## B-285 — Oberstløjtnant commands Majors

**Status:** PLANLAGT

Regiments-HQ giver mission intent til de to Majorer i stedet for direkte company destinations.

Første missioner:
- FORSVAR HER;
- ANGRIB HER;
- RYK FREM;
- TILBAGETRÆK;
- HOLD;
- SAML.

Majorer oversætter regimentsintentionen til company roles/slots og beholder taktisk skøn inden for doctrine og kendt information.

## B-286 — Regiment AI tactical allocation

**Status:** PLANLAGT

Regiments-AI beslutter anvendelse af de to bataljoner ud fra kendt information, styrke, cohesion, terræn og mission. Den kan fx vælge:
- begge bataljoner side om side;
- én fremme + én reserve;
- én fixing + én flanking;
- forskudt forsvar;
- koncentration på én sektor.

Reserve eller flanke er aldrig obligatorisk blot fordi to bataljoner findes.

## B-287 — Authority chain

**Status:** DESIGNBESLUTTET / delvist valideret Major→kompagni

Authority:

`Oberstløjtnant order → Major mission → company mission`

Et lavere niveau må reagere lokalt på kontakt, men parent mission intent bevares, medmindre en regel specifikt tillader afvigelse. Direkte spillerintervention på lavere niveau løsriver midlertidigt enheden fra parent command indtil en ny parent order reclaim'er den.

Validerede regler:
- manuel ordre → AI OFF / spiller-authority;
- AI ON efter manuel ordre → lokal Kaptajn-authority, ikke gammel Major-destination;
- kun en NY Major-ordre reclaim'er kompagniet;
- v00.00.09f26 under-fire reaction suspenderer missionen, men sletter den ikke.

## B-288 — Order delay/courier preparation

**Status:** PLANLAGT

Første Regimental-HQ gameplay behøver ikke fuld courier delay, men arkitekturen skal bevare et fremtidigt order lifecycle mellem Oberstløjtnant og Majorer. Senere skal fysiske couriers kunne bære forsinkede ordrer og blive forsinket/tabt/intercepted.

## B-289 — Dynamic HQ advance / Command Zone

**Status:** AKTIV / første Major→kompagni MVP implementeret i v00.00.09f25

HQ må ikke blive på startpositionen mens underenheder rykker hundredvis af meter frem. Under movement/attack skal HQ forskydes i bounds.

Major MVP:
- review ca. hver 2,5 sek. når Major AI er ON;
- auto-relocation når underenheder er langt nok fremme;
- foretrukken position ca. 145–165 m bag bataljonscenter/frontretning;
- direkte HQ-rute skal være fri for hard blockers/åbent vand;
- automatisk relocation ændrer ikke Majorens mission.

Command bands:
- `IN COMMAND` — 0–320 m;
- `EXTENDED` — 320–450 m;
- `OUT OF COMMAND` — over 450 m.

Distance påvirker primært command/reaction/coordination, ikke musket accuracy eller weapon range.

Regimental HQ senere:
- følger begge bataljoner med større standoff;
- forsøger at holde begge Majorer inden for brugbar command reach;
- rykker frem i bounds under offensiv og bagud ved withdrawal.

## REGIMENT-CONTROL GATE

**Denne gate skal være stabil før nye våbenarter prioriteres:**

1. infantry CHARGE/melee er brugbar;
2. under-fire reaction er brugbar;
3. 2 bataljoner / 2 Majorer / 8 kompagnier findes;
4. Oberstløjtnant fysisk HQ findes;
5. Oberstløjtnant → Major → Kaptajn authority fungerer;
6. Regiments-AI kan give bataljonerne angreb/forsvar og vælge fornuftig reserve/flanke-disposition;
7. grundlæggende HQ-positionering/command reach fungerer.

Når denne gate er nået, fortsætter vi **ikke straks op til Brigade/Division**. Vi bruger først det fungerende system til nye våbenarter.

## B-290 — Dragoons

**Status:** PLANLAGT — første store system efter Regiment-control gate

Dragoons skal bygges og testes som næste våbenart.

Første scope:
- mounted movement;
- formationer for mounted movement;
- `SID AF` / dismount;
- dismounted fire;
- `SID OP` / remount;
- mounted charge/melee;
- evt. dismounted melee;
- heste som fysisk/visuel ressource;
- Dragoon casualty/state model;
- Kaptajn/Major/Regiments-AI skal kunne vurdere mounted vs dismounted anvendelse;
- samme authority-, destination-, command-zone- og order-delay-principper som infanteriet.

Dragoon AI skal testes både under direkte spillerkontrol og gennem HQ-kæden.

## B-291 — Kanonbatteri / Artilleri

**Status:** PLANLAGT — efter Dragoon-baseline

Artilleri er næste våbenart efter Dragoons.

Første scope:
- batteri som taktisk enhed;
- movement/limber;
- unlimber/deploy;
- facing/fire arc;
- range bands;
- ammunitionstyper;
- reload/cadence;
- crew/cannon casualties;
- solid shot/shell/shrapnel/canister når relevant for perioden;
- struktur-/building damage koblet til senere urban damage model;
- battery AI for position selection, target priority, displacement og ammunition policy;
- samme HQ/authority/command-zone-system som øvrige våbenarter.

Efter Dragoons + Artilleri testes kombineret våbenbrug under Regimental HQ, før vi prioriterer højere Brigade-/Divisions-HQ.
