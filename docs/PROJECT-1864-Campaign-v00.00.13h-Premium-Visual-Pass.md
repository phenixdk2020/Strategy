# PROJECT 1864 — Campaign v00.00.13h Premium Visual Pass

**Version:** v00.00.13h  
**Branch:** `work/v00.00.13h-denmark-premium-visual-pass`  
**Status:** DEV / IMPLEMENTERET / AFVENTER UNITY COMPILE + RUNTIME QA  
**Scope:** Campaign presentation only — Denmark-first premium visual milestone

## Designmål

v00.00.13h er den første campaign-version, hvor det visuelle mål ikke blot er teknisk korrekt geografi og fungerende debug-præsentation, men et bevidst **premium grand-strategy look**. Den valgte visuelle retning er:

> **Historic Miniature Grand Strategy Diorama**

Kortet skal føles historisk, seriøst, læsbart og som en lille levende miniatureverden. Det må ikke ligne rå Unity-prototypegeometri, men det skal heller ikke være cartoon eller fotorealistisk. Strategisk information og gameplay-læsbarhed har højere prioritet end dekorativ realisme.

## Fast versionsregel

v13h må ændre rendering, terrænmaterialer, vand, vegetation, settlement-miniaturer, infrastrukturpræsentation, construction-dressing, lys, fog, kamera-composition og labels. Versionen må **ikke** ændre campaign movement, ETA, route distance, campaign time, logistics, tactical AI, battlefield navigation eller andre authoritative simulation-systemer.

Danmark-first-reglen fortsætter: kun noder med `CampaignMapRegion.Denmark` må være synlige som byer, labels, formationsmarkører og strategiske forbindelser i denne QA-slice. Sverige, Norge, Finland og Tyskland forbliver i simulationen, men deres præsentation er skjult indtil den kontrollerede geografiske udvidelse.

## B-13h-01 — Premium terrain material pass

- Bevar det korrigerede v13f Denmark landmesh.
- Erstat ensartet prototype-grøn med en dæmpet palette for græs, eng og hede.
- Tilføj deterministic presentation-only markfelter i forskellige jord-/afgrødetoner.
- Tilføj små levende hegn omkring en del af markfelterne.
- Felt- og hedge-objekter må kun placeres på sikker indland-geometri og må ikke ændre strategisk terrain state.

**Acceptance:** Danmark læses som et varieret landskab frem for én grøn plade.

## B-13h-02 — Premium water & coastline pass

- Bevar den rene v13e sea-surface.
- Brug mørkere blå tone og højere smoothness.
- Tillad meget subtil tidsbaseret farvevariation for at undgå helt statisk vand.
- Kystlinjen gøres finere og mindre debug-lignende.

**Acceptance:** vandet giver dybde og kontrast uden at dominere kortet.

## B-13h-03 — Denmark silhouette & composition pass

- Home-kamera skal frame Danmark tydeligt og centralt.
- Jylland, Fyn og Sjælland skal kunne aflæses straks.
- Close zoom bevares og legacy terrain floor må ikke blokere kameraet.
- Kameraændringer er kun presentation.

**Acceptance:** Danmark er det tydelige visuelle fokus fra første frame.

## B-13h-04 — Premium settlement miniatures

Legacy Denmark settlement-miniaturer skjules. v13h bygger nye små by-dioramaer direkte ved authoritative campaign-nodepositioner.

Visuelle elementer kan omfatte:
- flere huse afhængigt af byens betydning,
- kirke og tårn som silhouette-anchor,
- station + platform ved rail-noder,
- små piers ved port-noder,
- større miniature-footprint ved København, Aalborg, Aarhus og Odense.

**Acceptance:** byer kan genkendes som settlements ved close zoom uden at være afhængige af sorte debug-labels.

## B-13h-05 — Premium road / rail / ferry pass

- Denmark-only links fra den rene v13e-infrastruktur genbruges.
- Road, rail og ferry får hver sit materiale og breddehierarki.
- Foreign links forbliver skjult.
- Linjerne må støtte læsbarhed frem for at ligne debug-vectors.

**Acceptance:** vej, jernbane og færgeforbindelse kan skelnes visuelt uden forklaring.

## B-13h-06 — Barracks & farm premium rebuild

Aalborg-kasernen og Aarhus-farmen forbliver staged campaign-time construction projects. v13h må ikke erstatte deres authoritative build progress, men kan lægge presentation-dressing omkring byggepladserne:

- hegn/perimeter,
- tømmer- og materialestakke,
- arbejdsareal,
- markdetalje omkring Aarhus-farmen.

**Acceptance:** begge byggeprojekter opleves som steder i verdenen, samtidig med at deres eksisterende byggeprogression fortsat er authoritative.

## B-13h-07 — Living world / worker polish

Denne version etablerer visual foundation for senere workers/ambient animation. v13h må bruge let presentation-motion, men må ikke skabe selvstændig authoritative simulation. Større worker lifecycle og logistiske actor flows hører fortsat under living-world/backlog-systemerne.

## B-13h-08 — Vegetation & field pattern pass

- Deterministiske woodland clusters placeres kun inde i Danmark.
- Clusters består af små miniature-træer med trunk/crown variation.
- Major city centres friholdes.
- Vegetation må ikke repræsentere fog-of-war eller skjulte gameplaydata.

**Acceptance:** kortet får visuel struktur og dybde uden at blive overfyldt.

## B-13h-09 — Lighting & atmosphere premium pass

- Varmere directional light.
- Soft shadows.
- Dæmpet ambient light.
- Restrained linear fog til dybde og afstandslæsning.
- Atmosphere må ikke reducere strategisk UI-læsbarhed.

**Acceptance:** kortet føles som en sammenhængende scene frem for isolerede primitive objekter.

## B-13h-10 — Premium label & UI pass

v13h overtager label-rendering som eneste synlige label owner:

- legacy `CampaignMapController` labels suppresses,
- v13g/v13f GUI-lag må ikke tegne yderligere labels,
- kun Denmark-region labels vises,
- semantic zoom reducerer antallet af labels ved overview,
- labels bruger let tekst + diskret shadow i stedet for tunge sorte bokse.

**Acceptance:** ingen duplicate labels, ingen svenske/norske/finske/tyske labels og markant mindre UI-støj.

## Runtime-arkitektur

Presentation stack for v13h:

```text
Corrected Denmark landmesh (v13f)
  + clean sea/infrastructure base (v13e)
  + Denmark-only/close-zoom rules (v13g start state)
  + v13h premium presentation owner
      ├─ terrain palette
      ├─ fields + hedgerows
      ├─ vegetation
      ├─ premium settlements
      ├─ infrastructure styling
      ├─ construction dressing
      ├─ lighting + fog
      └─ elegant semantic labels
```

v13h disables the active v13g behaviour after its initial state has been applied, then maintains Denmark-only presentation itself. The v13f landmesh GameObjects remain alive and are not rebuilt again.

## QA gate

v00.00.13h passes visual QA when all of the following are true:

1. Unity compiles without errors.
2. Denmark is filled and immediately recognizable.
3. No foreign city labels or foreign settlement miniatures are visible.
4. No Danish city label appears twice.
5. Fields/hedges/vegetation stay visually on Danish land.
6. Water looks materially better than the flat prototype sea.
7. Major Danish cities have visibly richer miniatures at close zoom.
8. Road/rail/ferry can be distinguished.
9. Aalborg barracks and Aarhus farm remain present and staged.
10. Home view looks materially more polished than v13g while preserving strategic readability.

## Promotion rule

v13h is a DEV presentation build only. The official active campaign remains v00.00.11 until formal campaign gates are satisfied. Visual QA success does not promote v13h into the official active campaign version by itself.
