# PROJECT 1864 — Designmanual Addendum v00.02.11

**Designbaseline:** v00.02.11  
**Emne:** Campaign Denmark Premium Visual Baseline  
**Tilknyttet build:** Campaign v00.00.13h — DENMARK PREMIUM VISUAL DEV

Dette addendum er en del af PROJECT 1864-designmanualen og fastlægger den visuelle baseline for campaign-kortet. Den eksisterende hovedmanual v00.02.10 bevarer de tidligere system- og simulationsbeslutninger; v00.02.11 tilføjer den kontrollerede premium presentation-retning nedenfor.

## Historic Miniature Grand Strategy Diorama

Campaign-kortets visuelle mål er en **historisk miniatureverden i premium grand-strategy-stil**. Udtrykket skal være seriøst, roligt, læsbart og stemningsfuldt. Det må ikke være cartoon, og fotorealisme er ikke et krav. Verdenen skal kunne læses strategisk på afstand og samtidig belønne close zoom med by-, terræn-, vegetation- og construction-detaljer.

Presentation er aldrig authoritative simulation. Visual relief, materialer, vegetation, fog, vand, shadows, city miniatures og labels må ændres uden at ændre route distance, ETA, campaign state eller AI.

## Denmark-first visual milestone

Indtil Danmark-baselinen er visuelt godkendt, må den aktuelle premium QA-view kun vise `CampaignMapRegion.Denmark` som settlements, labels, formationsmarkører og aktive presentation-links. Sverige, Norge, Finland og Tyskland forbliver i simulationens datamodel, men skjules visuelt.

Det sikrer, at Danmark kan færdiggøres som én sammenhængende quality bar før samme render-principper udvides geografisk.

## Premium terrain

Det korrigerede Denmark landmesh fra v13f er fortsat fundament. Premium-laget må tilføje dæmpede grass/meadow/heath-materialer, markfelter, hedgerows og woodland clusters. Disse elementer skal være deterministic presentation dressing og placeres på dansk landgeometri. De må ikke skabe eller ændre strategic terrain state.

## Premium water og coastline

Havet skal understøtte Danmark-silhuetten gennem mørkere, rolig blå palette, højere smoothness og diskret visuel variation. Kystlinjen skal være fin og sekundær i forhold til selve landfladen. Vand-animation og shader-look er presentation-only.

## Premium settlements

Byer skal være små dioramaer frem for debug-markører. Strategic importance må påvirke miniature-footprint. Node-data kan styre presence af station/platform, port/piers og fortified cues. Kirke/tårn bruges som historisk silhouette-anchor, men konkrete bygninger i prototypefasen er ikke claims om præcis byarkitektur i 1864.

## Premium infrastructure

Road, rail og ferry skal have tydeligt forskelligt visuelt hierarki. Infrastructure rendering følger authoritative node/link data, men det visuelle materiale og line/spline-look må forbedres frit. Foreign links forbliver skjult i Denmark-first QA.

## Construction og living-world presentation

Aalborg barracks og Aarhus farm er fortsat staged, campaign-time-bundne QA-projekter. Premium dressing kan tilføje hegn, materialestakke, arbejdsareal, marker og andre sceneelementer, men må ikke vise projektet som færdigt før authoritative build progress siger det.

Living-world visuals følger fortsat reglen fra v00.02.09: animation kan visualisere state, men må aldrig være state.

## Lighting og atmosphere

Warm directional light, soft shadows, restrained fog og coherent ambient light er godkendte premium-værktøjer. Atmosphere skal skabe dybde uden at skjule strategiske informationer eller gøre labels/formationer ulæselige.

## Premium labels og semantic zoom

Kun ét label-system må være synligt. Heavy black debug boxes udfases til fordel for let tekst, diskret shadow og semantic zoom. Overview viser kun strategiske centre; regional/local detail kommer frem ved tættere zoom. Denmark-first-reglen gælder også labels.

## Camera composition

Home view skal straks kommunikere Danmark som fokus. Close zoom skal være tæt nok til at inspicere byminiaturer, roads/rail, construction og vegetation. Legacy hidden terrain må ikke kunstigt blokere close zoom.

## v00.02.11 acceptance

Den visuelle baseline er opfyldt når:

- Danmark er genkendeligt og sammenhængende fra Home view,
- ingen udenlandske settlements/labels vises i Denmark-first QA,
- ingen duplicate labels findes,
- land, water og coast har et samlet premium palette-system,
- byer har miniature-identitet ved close zoom,
- roads/rail/ferry kan skelnes,
- vegetation og fields giver dybde uden at spilde ud i havet,
- barracks/farm progression stadig er authoritative,
- UI understøtter kortet i stedet for at dominere det,
- Unity compile/runtime QA består.

## Versionshistorik-delta

- **v00.02.11 — Denmark Premium Visual Baseline** — Fastlægger Historic Miniature Grand Strategy Diorama som campaign-kortets visuelle retning; Denmark-first presentation, premium terrain/water/settlement/infrastructure/vegetation/lighting/labels, semantic zoom og presentation-only-reglen. Knyttet til Campaign v00.00.13h DEV.
