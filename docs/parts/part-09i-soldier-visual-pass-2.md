# PROJECT 1864 — Designmanual supplement — v00.00.09i Soldier Visual Pass 2

## Formål

`v00.00.09i` er et rent presentation-layer løft af tactical infantry og regimentsfaner. Revisionen må ikke ændre movement, combat resolution, morale, cohesion, fire policy, selection state eller Officer AI. Den eksisterende `v00.00.09h4` manual-route/V3 movement baseline er fortsat autoritativ.

## Repræsentative soldater

Simulationen forbliver formationsbaseret. De synlige soldater er repræsentative render-entities og må aldrig blive selvstændige gameplay-agenter.

09i fastlægger følgende visuelle minimum for infantry-repræsentanter:

- menneskeligere proportioner med separat torso, coat skirts, arme, hænder, ben, støvler, hals og hoved,
- tydelig rifle med stock, barrel og bayonet,
- backpack, cartridge box, straps og udvalgte equipment-varianter,
- faction-specifik headgear-silhuet,
- officer med sash, shoulder boards og sword,
- standard-bearer med særskilt sash/pole-grip og uden almindelig rifle,
- flere deterministiske variationer i skala/udstyr, så formationer ikke læses som perfekte kloner.

Visuals skal fortsat bruge eksisterende regiment formation slots. De må ikke eje pathfinding, collision, target acquisition eller combat timing.

## Visuel posing

09i må bruge letvægts-procedural posing uden Animator-controller som prototype:

- march: modgående arm-/bensving,
- stationary/ready: rolig våbenklar positur,
- firing: kort rifle-/arm pose knyttet til eksisterende volley-feedback.

Posing er kosmetisk og må ikke forsinke, fremskynde eller trigge simulation events.

## Uniform Designer integration

09h uniformprofilen forbliver farve-source-of-truth. 09i skal live-synkronisere mindst:

- coat,
- trousers,
- headgear,
- trim,
- straps,
- equipment,
- skin,
- flag primary,
- flag secondary,
- ribbon/accent.

Uniform/customisation er presentation-only. Ingen uniformfarve giver gameplay bonus eller penalty.

## Faction readability

Fra normal tactical zoom skal Danmark og Preussen kunne skelnes uden at åbne UI. Ved close zoom skal headgear og equipment styrke samme identitet.

- Danmark: tydelig cap/visor/band-silhuet oven på den valgte uniformprofil.
- Preussen: tydelig Pickelhaube dome/band/spike/visor-silhuet oven på den valgte uniformprofil.

Historisk præcise uniform- og regimentsdetaljer skal senere flyttes til source-validerede data/assets. 09i er visuel prototypebaseline, ikke endelig historisk art.

## Regimentsfaner / standards

09i standarden skal være markant mere levende end den tidligere QA-cube-fane:

- større og tydeligere pole/finial silhouette,
- procedural two-sided cloth mesh,
- let wind deformation,
- cords,
- animated ribbons,
- fringe/detail,
- dansk/preussisk prototype-device,
- live-sync med Uniform Designer flag/ribbon colors.

Flag wind animation er kosmetisk. Endelig historisk flag art kræver særskilt research/asset-gate.

## Performance / LOD

Et visuelt kvalitetsløft må ikke gøre formationskamp unødigt dyr.

09i bruger derfor første simple close-detail LOD: små equipment-detaljer kan skjules ved stor kameraafstand, mens torso, hoved, ben og weapon silhouette fortsat vises. Senere erstattes dette af shared meshes/materials, GPU instancing og egentlige LOD assets.

## Movement isolation

09i må ikke ændre følgende accepterede/aktive regler:

- Navigation V3 er eneste lokale steering-owner.
- 09h4 manual routes bruger Column under travel og bounded route recovery ved dokumenteret no-progress.
- Decorative trees og enkelte FencePosts er soft/pass-through i nuværende prototype.
- Buildings/barns er hard obstacles.
- River/water krydses kun via bridge-reglen.
- 09h3 march/deploy policy gælder AI-enheder separat.
- 09h2 sticky frontage gælder AI multi-regiment attack planning separat.

## 09i acceptance-gate

09i kan først accepteres som ny tactical visual baseline når:

1. Unity 6000.6.0f1 compiles uden røde errors.
2. Build marker viser `PROJECT 1864 | v00.00.09i TEST`.
3. Close zoom viser tydeligt mere menneskelige infantry silhouettes end 09h.
4. Officer og standard-bearer kan identificeres visuelt.
5. Danmark/Preussen kan skelnes på headgear/uniformsilhuet.
6. March/ready/fire posing fungerer uden at påvirke simulationen.
7. Uniform Designer ændrer 09i soldier- og flagfarver live.
8. Nye regimentsfaner bølger uden renderer-fejl eller synlige gamle QA-faner oveni.
9. LOD skjuler kun små detaljer, ikke formationens grundsilhuet.
10. Den samme AI-OFF manual movement QA fra 09h4 giver samme eller bedre movement-adfærd; graphics-pass må ikke introducere nye stalls.
