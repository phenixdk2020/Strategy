# PROJECT 1864 — Territorial Zones 1851

**Designbaseline: v00.02.10**  
**Register: ZONE-REG-01 — Kongeriget Danmark ved campaign-start 1. januar 1851**

## Formål

ZONE-REG-01 definerer det regionale simulationslag mellem staten og de enkelte city/settlement-noder. Zonerne er ikke moderne regioner og ikke de gamle grove QA-zoner fra Campaign3-prototypen. Første kanoniske baseline følger så vidt muligt den **administrative amtsstruktur omkring 1851**, fordi den giver en historisk men gameplay-egnet ramme for regional befolkning, skat, rekruttering, produktion, infrastruktur, forsyning, kontrol og besættelse.

København behandles som særskilt hovedstadszone. Københavns Amt med Roskilde Amtsrådskreds abstraheres som én territorial simulation-zone i første version. Skanderborg Amt er selvstændig ved campaign-start og kan først senere fusioneres administrativt med Aarhus gennem en historisk ændring.

## Kerneprincipper

- Hver city/settlement-node tilhører præcis én territorial `ZoneId` i en given campaign-state.
- Zoneejerskab og city-ejerskab er separate state-værdier, så belejrede/besatte byer senere kan afvige fra resten af zonen.
- Zoner indeholder også landbefolkning, gårde, skove, råstoffer, veje og anden regional kapacitet uden for købstæderne.
- Byklasse A/B/C bestemmer fortsat byens egne byggeoptioner; en by bliver ikke Development City blot fordi dens zone er rig eller strategisk vigtig.
- En zone kan skifte controller gennem krig uden at dens historiske navn eller `ZoneId` ændres.
- Administrative reformer håndteres som data/events og må ikke kræve nye koordinater for eksisterende byer.
- Øer og søforbindelser skal kunne have maritime adjacency-links, som er forskellige fra landmarch-links.

## Zone data model

Hver zone skal mindst kunne indeholde:

- `ZoneId`
- `ZoneName1851`
- `CountryId`
- historical administrative reference
- polygon/border geometry
- capital/admin centre
- owner
- controller
- neighbour zone ids
- maritime neighbour ids
- urban population
- rural population
- workforce
- recruitment pool
- tax base
- agriculture/food production
- horses/remount capacity
- raw materials
- industry capacity
- supply throughput
- road/rail/port infrastructure
- loyalty/unrest
- occupation/resistance state
- source/confidence

# ZONE-REG-01 — 20 startzoner

| ZoneId | Zone 1851 | Primære registrerede købstæder | Købstæder |
| --- | --- | --- | ---: |
| **DK-Z01-KBH** | København Stad | København | 1 |
| **DK-Z02-KBH-AMT** | Københavns Amt / Roskilde Amtsrådskreds | Roskilde, Køge | 2 |
| **DK-Z03-FRB** | Frederiksborg Amt | Helsingør, Hillerød, Frederikssund | 3 |
| **DK-Z04-HOL** | Holbæk Amt | Holbæk, Nykøbing Sjælland, Kalundborg | 3 |
| **DK-Z05-SOR** | Sorø Amt | Ringsted, Sorø, Slagelse, Korsør, Skælskør | 5 |
| **DK-Z06-PRA** | Præstø Amt | Næstved, Store Heddinge, Præstø, Vordingborg, Stege | 5 |
| **DK-Z07-MAR** | Maribo Amt | Stubbekøbing, Nykøbing Falster, Nysted, Sakskøbing, Maribo, Rødby, Nakskov | 7 |
| **DK-Z08-BOR** | Bornholms Amt | Rønne, Hasle, Allinge, Sandvig, Svaneke, Neksø, Åkirkeby | 7 |
| **DK-Z09-ODE** | Odense Amt | Odense, Assens, Kerteminde, Middelfart, Bogense | 5 |
| **DK-Z10-SVE** | Svendborg Amt | Svendborg, Nyborg, Fåborg, Rudkøbing | 4 |
| **DK-Z11-VEJ** | Vejle Amt | Kolding, Fredericia, Vejle | 3 |
| **DK-Z12-SKA** | Skanderborg Amt | Horsens, Skanderborg | 2 |
| **DK-Z13-AAR** | Aarhus Amt | Aarhus | 1 |
| **DK-Z14-RAN** | Randers Amt | Ebeltoft, Grenaa, Randers, Mariager, Hobro | 5 |
| **DK-Z15-VIB** | Viborg Amt | Viborg, Skive | 2 |
| **DK-Z16-AAL** | Aalborg Amt | Aalborg, Nibe | 2 |
| **DK-Z17-HJO** | Hjørring Amt | Hjørring, Sæby, Frederikshavn, Skagen | 4 |
| **DK-Z18-THI** | Thisted Amt | Thisted, Nykøbing Mors | 2 |
| **DK-Z19-RIN** | Ringkøbing Amt | Lemvig, Holstebro, Ringkøbing | 3 |
| **DK-Z20-RIB** | Ribe Amt | Varde, Ribe | 2 |

**Kontrolsum: 20 zoner / 68 købstæder.**

## Foreløbig adjacency — designregel

Den endelige adjacency må udledes af historiske polygoner og samtidige transportforhold, men følgende principper gælder allerede:

- Landgrænse mellem to zoner giver ikke nødvendigvis høj throughput; vejnet, broer, færger, terræn og årstid bestemmer faktisk bevægelseskapacitet.
- Storebælt, Lillebælt, Øresund, Limfjorden, Langelandsbælt og farvandene omkring Bornholm behandles som maritime/ferry connections, ikke som gratis land-adjacency.
- København kan være tæt økonomisk forbundet til Sjælland uden at blive slået sammen med Københavns Amt som simulationszone.
- Bornholm har ingen land-adjacency til de øvrige danske zoner.
- Slesvig bliver et separat zone-register; grænseforbindelser fra Ribe/Vejle-zonerne skal derfor knytte til Slesvig-zoner, når ZONE-REG-02 etableres.

## Zoner og regional økonomi

Bybefolkningen i CITY-REG-01 er kun den urbane del. Den samlede zonebefolkning skal senere suppleres med landbefolkningen fra folketællingen 1850. Regional manpower og økonomi må derfor **ikke** beregnes alene ved at summere købstæderne.

Eksempelvis kan en zone med små købstæder have stor landbrugsproduktion, mange heste og betydelig værnepligtig befolkning. Omvendt kan København have ekstrem urban koncentration og statsfunktioner uden tilsvarende lokal fødevareproduktion.

## Zoner og militær udbygning

Zone-systemet ændrer ikke CITY-REG-01's byggebegrænsning:

- A-byer kan kvalificere til nye større militære byanlæg, hvis øvrige regler tillader det.
- B- og C-byer kan ikke få nye kaserner, arsenaler, større militære depoter, våbenfabrikker eller permanente fæstninger gennem normal city construction.
- Historisk dokumenterede anlæg kan eksistere som fixed/special buildings.
- Regionale projekter uden for byerne — fx vej, bro, jernbane, telegraf, gård, hestedepot eller særskilt militært anlæg — styres af deres egne placeringsregler og må ikke omgå byklassens begrænsninger.

## Visning på strategikortet

Zone-overlay skal kunne slås til/fra og bør understøtte mindst:

1. **Administrative/Political** — zonegrænse, owner/controller og zone-navn.
2. **Population/Recruitment** — regional befolkning, manpower og mobiliseringspres.
3. **Economy** — produktion, skat, fødevarer, heste og industri.
4. **Supply** — depoter, throughput, forbindelser og afskårne områder.
5. **Infrastructure** — veje, jernbane, havne, færger og telegraf.
6. **Occupation/Control** — besættelse, unrest, resistance og contested state.

På høj zoom skal zonegrænser være tydelige. På tæt city-zoom skal de tones ned, så veje, byer, hære og terræn forbliver læselige.

## Historiske ændringer

Zone-systemet skal være tidsafhængigt. Første konkrete eksempel:

- **1851:** Skanderborg Amt eksisterer som separat zone `DK-Z12-SKA`.
- **1867:** den historiske administrative sammenlægning med Aarhus kan repræsenteres som event/reform. Spillet kan enten sammenlægge de administrative overlays eller bevare de to simulationszoner under en fælles administrativ overbygning afhængigt af senere balance-test.

Samme mekanisme skal senere kunne håndtere territoriale ændringer efter krig, fredstraktater og administrative reformer.

## Implementeringsmål for næste Campaign3 city/zone-build

- Erstat de 12 nuværende grove QA-zone-centre som authoritative gameplay-data med ZONE-REG-01.
- Bevar WGS84 som koordinatkontrakt.
- Indlæs alle 68 CITY-REG-01-byer med bindende `ZoneId`.
- Vis zone-navn og city population/tier i selection UI.
- Gør zone selection og army routing datadrevet.
- Hold zonepolygoner adskilt fra moderne imagery/terrain; den moderne basemap må ikke definere 1851-grænserne.
- Polygondata kan komme i en senere underbuild, men `ZoneId`, membership og historisk zone-navn skal være authoritative først.

## Kilder og historisk kontrol

- Danmarks Statistik, folketællingen 1850, anvendes til population og senere regional demografi.
- Trap Danmark / Lex anvendes til historisk administrativ kontrol af amter, købstæder og senere ændringer.
- Skanderborg Amt blev genoprettet i 1824 og eksisterede ved campaign-start; det blev først nedlagt/sammenlagt med Aarhus i 1867.
- Kolding, Fredericia og Vejle registreres under Vejle Amt.
- Roskilde og Køge registreres under Københavns Amt / Roskilde Amtsrådskreds.

ZONE-REG-01 er startbaseline og skal udvides med særskilte registre for Slesvig, Holsten og Lauenborg.