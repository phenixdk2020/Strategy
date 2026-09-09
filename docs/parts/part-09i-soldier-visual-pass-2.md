# PROJECT 1864 — Designmanual supplement — v00.00.09i2 Soldier Visual Pass 2

## Formål

`v00.00.09i` introducerer Soldier Visual Pass 2. `v00.00.09i2` skærper samme presentation-layer baseline, fordi QA viste at det første pass ikke var visuelt tydeligt nok ved normal tactical zoom. Revisionen må ikke ændre movement, combat resolution, morale, cohesion, fire policy, selection state eller Officer AI.

## Repræsentative soldater

Simulationen forbliver formationsbaseret. De synlige soldater er repræsentative render-entities og må aldrig blive selvstændige gameplay-agenter.

Visual baseline:

- menneskeligere proportioner med separat torso, coat skirts, arme, hænder, ben, støvler, hals og hoved,
- tydelig rifle med stock, barrel og bayonet,
- backpack, cartridge box, straps og equipment-varianter,
- faction-specifik headgear-silhuet,
- officer med sash, shoulder boards og sword,
- standard-bearer med særskilt sash/pole-grip og uden almindelig rifle,
- deterministiske variationer så formationer ikke læses som perfekte kloner.

Visuals skal bruge eksisterende regiment formation slots. De må ikke eje pathfinding, collision, target acquisition eller combat timing.

## Tactical readability invariant

Et visuelt kvalitetsløft er ikke accepteret, hvis det kun kan ses ved ekstrem close zoom. Ved normal tactical zoom skal spilleren kunne læse:

- at markørerne repræsenterer mennesker, ikke tynde pinde,
- formationens retning og densitet,
- faction-identitet,
- regimentsfanen som et klart center-/identitetspunkt.

For at opnå dette må render-rigs bruge bounded presentation-only distance scaling omkring deres eksisterende formation slot. Dette må aldrig ændre slot-position, BoxCollider, unit footprint eller pathfinding-clearance.

09i2 baseline:

- soldier render scale ca. 1.04x nær kameraet til ca. 1.24x ved lang tactical afstand,
- små equipment-/uniformdetaljer må bevares til ca. 285 m i prototype-LOD,
- headgear, rifle/bayonet, hænder, støvler og torso-silhuet prioriteres ved medium zoom,
- scaling er ren rendering og må ikke skabe større fysisk formation.

## Visuel posing

Letvægts-procedural posing uden Animator-controller er tilladt som prototype:

- march: modgående arm-/bensving,
- stationary/ready: rolig våbenklar positur,
- firing: kort rifle-/arm pose knyttet til eksisterende volley-feedback.

Posing er kosmetisk og må ikke forsinke, fremskynde eller trigge simulation events.

## Uniform Designer integration

09h uniformprofilen forbliver farve-source-of-truth. 09i/09i2 synkroniserer mindst coat, trousers, headgear, trim, straps, equipment, skin, flag primary, flag secondary og ribbon/accent.

Uniform/customisation er presentation-only. Ingen uniformfarve giver gameplay bonus eller penalty.

## Faction readability

Fra normal tactical zoom skal Danmark og Preussen kunne skelnes uden at åbne UI. Ved close zoom skal headgear og equipment styrke samme identitet.

- Danmark: tydelig cap/visor/band-silhuet.
- Preussen: tydelig Pickelhaube dome/band/spike/visor-silhuet.

Historisk præcise detaljer skal senere flyttes til source-validerede data/assets. 09i2 er visuel prototypebaseline, ikke endelig historisk art.

## Regimentsfaner / standards

Standarden skal være markant mere levende og læsbar end den tidligere QA-cube-fane:

- tydelig pole/finial silhouette,
- procedural two-sided cloth mesh,
- let wind deformation,
- cords,
- animated ribbons,
- fringe/detail,
- dansk/preussisk prototype-device,
- live-sync med Uniform Designer flag/ribbon colors.

09i2 tillader bounded distance-based render scaling af hele procedural-standarden fra ca. 1.18x tæt på til ca. 1.52x ved lang tactical afstand, så fanen forbliver synlig som regimentsidentitet. Dette har ingen fysisk eller gameplay-mæssig effekt.

## Runtime verification

Visual pass må ikke fejle stille. Startup telemetry skal gøre det tydeligt om det aktive graphics layer faktisk er installeret.

09i2 forventer mindst:

- `SOLDIER-09I2|Installed=True...`
- `STANDARD-09I|Installed=True...`
- `VIS-09I2|Installed=True...`

Hvis en procedural 09i-standard mangler, skal der logges en specifik `VIS-09I2 ... Standard09I=False` warning frem for at lade QA tro at den gamle 09g-fane er den nye grafik.

## Performance / LOD

Et visuelt kvalitetsløft må ikke gøre formationskamp unødigt dyr. 09i2 prioriterer silhouette readability over tidlig detail-culling. Senere erstattes prototype-primitives med shared meshes/materials, GPU instancing og egentlige LOD assets.

## Movement isolation

Visual pass må ikke ændre aktive movement-regler:

- Navigation V3 er lokal steering-owner.
- 09h4 manual routes bruger Column under travel og bounded route recovery.
- Decorative trees og enkelte FencePosts er soft/pass-through.
- Buildings/barns er hard obstacles.
- River/water krydses via bridge-reglen.
- 09h3 march/deploy policy gælder AI-enheder separat.
- 09h2 sticky frontage gælder AI multi-regiment attack planning separat.
- 09i1 hard-obstacle continuation kan korrigere V3's eksisterende Farmhouse/Barn detour-state, men graphics-pass må ikke kende eller skrive denne state.

## 09i2 acceptance-gate

09i2 kan først accepteres når:

1. Unity 6000.6.0f1 compiles uden røde errors.
2. Build marker viser `PROJECT 1864 | v00.00.09i2 TEST`.
3. Normal tactical zoom viser klart mere menneskelige infantry silhouettes end 09h.
4. Close zoom viser full articulated/equipment pass.
5. Officer og standard-bearer kan identificeres visuelt.
6. Danmark/Preussen kan skelnes på headgear/uniformsilhuet.
7. March/ready/fire posing fungerer uden simulationseffekt.
8. Uniform Designer ændrer soldier- og flagfarver live.
9. Procedural standards er synlige og læsbare ved medium/long tactical zoom.
10. Startup Console viser `SOLDIER-09I2`, `STANDARD-09I` og `VIS-09I2`.
11. Den samme AI-OFF manual movement QA giver samme eller bedre movement-adfærd; graphics-pass må ikke introducere stalls.
