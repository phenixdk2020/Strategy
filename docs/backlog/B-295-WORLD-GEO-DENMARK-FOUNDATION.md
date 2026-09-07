# B-295 — World geography foundation / Denmark first

## Status

**IMPLEMENTERING STARTET i v00.00.10e CAMPAIGN TEST**

## Formål

Grand Campaign skal kunne vokse fra Danmark til Europa og i princippet hele verden uden at Danmark senere skal flyttes eller omskrives geografisk. Derfor er den permanente geografiske identitet WGS84 longitude/latitude. Unity X/Z er kun et lokalt render-space.

## v00.00.10e vertical slice

Første aktive detaljeregion er Danmark. Versionen erstatter den tidligere rektangulære QA-landvisualisering med reel kyst-/øgeometri fra Natural Earth 1:50m for DNK.

Dataregler:

- fysisk geografi og historisk politik er separate lag
- Natural Earth er geographic scaffold, ikke 1851 politisk facit
- historiske grænser for den danske helstat, Slesvig, Holsten og Lauenburg kræver særskilt kildearbejde
- moderne admin-geometri må ikke bruges som 1851 territorial simulation uden historisk overlay
- DEM/elevation er et separat lag og er ikke implementeret i 10e

## Global arkitektur

Planlagt kæde:

`WGS84 master world -> geographic tiles -> physical terrain/DEM -> historical 1851 political layer -> settlements/infrastructure -> gameplay zone polygons -> formations/economy/supply`

Hvert geografisk objekt skal kunne have:

- stable ID
- longitude/latitude eller polygon i geographic coordinates
- source/provenance
- valid-from / valid-to hvor data er historisk tidsafhængige
- render tile / local projection metadata

Det gør det muligt senere at indlæse eksempelvis Preussen eller Sverige-Norge med samme projection/data contract uden at ændre danske ZoneId/FormationId.

## Danmark i 10e

Reel landgeometri:

- Jylland
- Sjælland
- Fyn
- Lolland
- Langeland
- Falster/Møn-gruppe i Natural Earth-geometrien
- Amager
- Ærø
- Als
- Samsø
- Læsø
- Bornholm

Første gameplay-zonecentre:

- DK-VEN — Vendsyssel
- DK-NJ — Nordjylland
- DK-MJ — Midtjylland
- DK-VJ — Vestjylland
- DK-OJ — Østjylland
- DK-SJ — Sydjylland
- DK-FYN — Fyn
- DK-NSJ — Nordsjælland
- DK-KBH — København
- DK-SSJ — Sydsjælland
- DK-LF — Lolland-Falster
- DK-BOR — Bornholm

Disse er gameplay scaffolding. De er endnu ikke polygoner og skal ikke tolkes som historiske amts-/stifts-/administrative grænser.

## Byer i første geografiske lag

10e placerer geografiske ankermarkører for Aalborg, Viborg, Aarhus, Esbjerg, Kolding, Fredericia, Odense, København, Næstved og Rønne.

Placering er geografi. Befolkning, industri, garnison, havn, jernbane, fortifikation og økonomisk værdi skal research-valideres for 1851 før de får simulationsværdier.

## Startdato

Canonical Grand Campaign start er **1. januar 1851 kl. 08:00**.

1851 bliver baseline for:

- politisk state
- OOB/mobiliseringsstate
- våben og ammunition
- officerskorps
- befæstninger
- statsfinanser/produktion
- veje/jernbaner/havne
- diplomatiske relationer

## Semantic zoom foundation

10e etablerer tre første kamera-bands:

- Close — bylabels + zoneinformation
- Operational — zonelabels + formationer
- Strategic — detaljerede labels reduceres

Senere udskiftes formationer gradvist med NATO/APP-6-lignende symboler i både Campaign og Battle mode. Det er kun visual LOD; simulationens IDs og state ændres ikke.

## Næste geografiske trin

1. Runtime compile/Play gate for v00.00.10e.
2. Indlæs DEM/højder for Danmark og valider projection mod kysten.
3. Byg kildebaseret 1851 politisk layer for den danske helstat og de relevante hertugdømmer.
4. Erstat zonecentre med polygoner, der følger fysisk geografi og gameplay-behov.
5. Læg historiske 1851 veje, jernbaner, broer, havne, færger og befæstninger ind som egne lag.
6. Tilknyt population, rekruttering, industri, landbrug, depoter og supply til zonepolygonerne.
7. Når Danmark-loopet er stabilt, genbrug samme pipeline for næste land.

## Acceptance v00.00.10e

- Unity compiler uden blocking errors.
- Visible build marker viser `v00.00.10e CAMPAIGN TEST`.
- Startpanel viser 1. januar 1851.
- Danmark vises som reel kyst-/øgeometri, ikke de tidligere firkantede landtiles.
- WGS84-koordinater vises for valgt zone.
- Danmark har 12 klikbare gameplay-zonecentre.
- City markers vises ved tæt zoom.
- En dansk QA-formation kan vælges og flyttes mellem sammenkoblede zoner.
- Bornholm kan ikke nås via land-march.
- Kamera kan pan/zoomes og skifter semantic zoom band.
- Ingen af ovenstående må præsenteres som runtime-valideret før Unity Play-test er rapporteret.
