# PROJECT 1864 — B-280–B-289 Charge, Melee og Regiments-HQ

**Status:** AKTIV næste sekvens efter f24-regressionsfixes  
**Branch:** `channel-test`

## B-280 — Infantry CHARGE

CHARGE skal være en eksplicit ordre/state, ikke bare almindelig `OrderAttack()` med kortere afstand. Enheden skal kunne lukke gennem normal fire-distance til fysisk kontakt uden at ranged AI stopper fremrykningen ved Close/Medium/Long.

Første scope:
- eksplicit CHARGE-knap/ordre;
- Line som udgangspunkt, med senere mulighed for andre assault formations;
- mål/retning låses som charge intent;
- cohesion/fatigue/momentum påvirkes;
- ranged fire og charge må ikke kæmpe om movement authority;
- charge afbrydes ved rout, umulig rute eller ugyldigt mål.

## B-281 — Infantry melee completion

Eksisterende `PrototypeMeleeCombatManager` er baseline. Udbyg med:
- charge-contact transition;
- kontakt frontage i stedet for ren centerafstand alene;
- facing/flank/rear modifiers;
- tydelig engaged state;
- break-off / winner / loser / rout;
- separate melee casualties og logs;
- ingen formation overlap/teleport gennem modstanderen.

## B-282 — Under-fire reaction

AI ON company under movement that receives confirmed hostile fire from a known enemy inside own Long range should normally suspend movement, deploy/hold Line, face attacker and return fire. Mission is suspended, not deleted. Exception: critical passage such as bridge crossing may require exiting the passage before deploying.

AI OFF remains literal player control and should not automatically cancel a direct movement order solely because it is fired upon.

## B-283 — Second battalion

Add four additional Danish company-scale infantry units plus a second physical Major HQ.

Canonical structure:
- Major A → Battalion 1 → 4 companies.
- Major B → Battalion 2 → 4 companies.

Each Major uses the same AI/UI/authority model.

## B-284 — Regimental HQ / Oberstløjtnant

Create one physical Regimental HQ commanded by **Oberstløjtnant**. It is the next command level above the two Majors.

Canonical Danish prototype hierarchy:
- Kaptajn → company;
- Major → battalion (4 companies);
- Oberstløjtnant → regiment (2 battalions / 8 companies);
- Oberst → brigade;
- Generalmajor → division.

## B-285 — Oberstløjtnant commands Majors

The Regimental HQ gives mission intent to the two Majors rather than issuing direct company destinations.

First missions:
- FORSVAR HER;
- ANGRIB HER;
- RYK FREM;
- TILBAGETRÆK;
- HOLD;
- SAML.

Majors translate regimental intent into company roles/slots and retain tactical discretion within doctrine/known information.

## B-286 — Regiment AI tactical allocation

Regimental AI decides how to use the two battalions from known information, strength, cohesion, terrain and mission. It may choose, for example:
- both battalions abreast;
- one forward + one reserve;
- one fixing + one flanking;
- staggered defense;
- concentration on one sector.

No reserve or flank is mandatory merely because two battalions exist.

## B-287 — Authority chain

Authority must be explicit:

`Oberstløjtnant order → Major mission → company mission`

A lower level may temporarily react to local contact, but must retain the parent mission intent unless a rule explicitly permits deviation. Direct player intervention at a lower level temporarily detaches that unit from its parent until a new parent order reclaims it.

## B-288 — Order delay/courier preparation

Do not require courier delay for first Regimental-HQ gameplay, but architecture must preserve a future order lifecycle between Oberstløjtnant and Majors. Later couriers physically carry delayed orders and can be delayed/lost/intercepted.

## B-289 — Dynamic HQ advance / Command Zone

HQ must not remain at its original start position while subordinate formations advance hundreds of metres. During movement and especially attack, HQ positions are dynamic tactical positions.

### Major / Battalion HQ movement

- The Major follows the battalion in bounds rather than continuously hugging the front line.
- During an attack the Major should normally remain behind the active company line, but advance when the battalion front moves far enough forward.
- HQ relocation is a tactical action: choose a new safe HQ anchor, move there, then reassess command links.
- Major should avoid known enemy frontage, open water, blocked terrain and obviously exposed positions.
- If the battalion halts to fight, the Major should normally halt behind/near its centre or reserve rather than continue forward through the line.
- If the battalion retreats, the Major should displace rearward as part of the mission.

### Oberstløjtnant / Regimental HQ movement

- Regimental HQ follows both battalions at a larger standoff than either Major.
- It should prefer a position from which both Majors remain inside useful command reach.
- If one battalion advances much farther than the other, the Regimental HQ must balance safety against maintaining communication to both.
- It may displace forward in bounds as the regiment advances and rearward during withdrawal.

### Command Zone is not primarily a weapon-stat bonus

Being outside command reach should mainly degrade **command and coordination**, not musket accuracy directly.

Suggested affected systems:
- increased order/reaction delay;
- slower Officer AI reassessment;
- slower reserve reassignment and flank coordination;
- reduced cohesion recovery / reorganisation rate;
- slower morale recovery after shock;
- higher chance of delayed/stale information later under fog of war;
- courier travel time / order delivery once courier lifecycle is enabled.

Do **not** use a crude immediate accuracy penalty simply because a company is outside HQ radius.

### Prototype command quality bands

Use continuous distance/quality later, but first implementation may expose three bands:

- `IN COMMAND` — normal command quality;
- `EXTENDED` — moderate delay/coordination penalty;
- `OUT OF COMMAND` — significant delay and weaker coordination, but unit remains functional under its local Captain.

A company outside Major range does not become helpless: the Kaptajn keeps local control and can defend, fire, react to contact and execute the last received mission. What is lost is fast coordination with higher HQ.

Likewise, a Major outside Regimental HQ range still commands its battalion locally, but receives new regimental intent more slowly/less reliably.

### Prototype positioning concept

Initial QA values, subject to tuning:

- Major preferred position during normal attack: roughly 100–220 m behind battalion fighting line/centre.
- Major command quality begins degrading progressively beyond roughly 300–400 m from subordinate companies.
- Oberstløjtnant preferred position: roughly 250–500 m behind/among the two battalion centres.
- Regimental command quality may begin degrading beyond roughly 700–900 m to a Major.

These are gameplay prototype values, not final historical measurements.

### Visualisation

When an HQ is selected:
- show direct subordinate links;
- optionally show a subtle command-zone ring/band;
- subordinate UI may show `IN COMMAND`, `EXTENDED` or `OUT OF COMMAND`;
- do not leave command-zone overlays permanently visible because that would clutter the battlefield.

### Important authority rule

Automatic HQ movement changes **HQ position**, not the commander mission. A Major ordered to defend/attack keeps the mission while relocating its physical HQ to remain useful. The same applies to the Oberstløjtnant relative to the two Majors.

## Gate before Dragoons

Dragoons remain blocked until these are stable enough to reuse:
1. infantry CHARGE;
2. melee resolution;
3. under-fire reaction;
4. two battalions / two Majors;
5. Oberstløjtnant → Major command chain;
6. basic dynamic HQ positioning / command reach.

Then proceed to mounted/dismounted Dragoon implementation, Dragoon AI and later artillery.
