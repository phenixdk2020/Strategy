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

## B-289 — Gate before Dragoons

Dragoons remain blocked until these are stable enough to reuse:
1. infantry CHARGE;
2. melee resolution;
3. under-fire reaction;
4. two battalions / two Majors;
5. Oberstløjtnant → Major command chain.

Then proceed to mounted/dismounted Dragoon implementation, Dragoon AI and later artillery.
