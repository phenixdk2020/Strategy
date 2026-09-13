# PROJECT 1864 — City Register 1851

**Designbaseline: v00.02.10**  
**Register: CITY-REG-01 — Kongeriget Danmark ved campaign-start 1. januar 1851**

## Formål

Dette dokument er den kanoniske baseline for byer i **Kongeriget Danmark** ved PROJECT 1864's start i 1851. Registeret erstatter de hidtidige få QA-byer som historisk byliste.

Populationstallene bruger **folketællingen 1. februar 1850**, den nærmeste komplette samtidige opgørelse før campaign-start 1. januar 1851. Danmarks Statistiks samtidige tabel opregner **68 købstæder** i Kongeriget. København havde 129.695 indbyggere, mens de øvrige 67 købstæder tilsammen havde 160.870; samlet **290.565 personer**.

> Dette register dækker Kongeriget Danmark, ikke Hertugdømmerne Slesvig, Holsten og Lauenborg. De får særskilte registre.

## Udviklingsklasser

Alle byerne skal findes på strategikortet, men de må ikke alle udbygges ens.

| Klasse | Baseline | Tilladt udvikling | Tung militær udbygning |
| --- | --- | --- | --- |
| **A — Development City** | normalt mindst 4.000 indb. | Fuld by-/infrastrukturudvikling efter øvrige krav | Mulig, hvis nationale regler, ressourcer og historiske rammer tillader det |
| **B — Regional Town** | 2.000–3.999 indb. | Handel, lager, vej/havn, mindre industri og lokal logistik | **Nej** — ingen ny kaserne, arsenal, større militært depot, våbenfabrik eller permanent fæstning som normal spillerbygning |
| **C — Minor Town** | under 2.000 indb. | Synlig/klikbar; lokal handel, havn/færge, mindre lager/håndværk | **Nej** — ingen ny kaserne, arsenal, større militært depot, våbenfabrik eller permanent fæstning som spillerbygning |

### Bindende bygge-regel for mindre byer

**B- og C-byer skal være fuldt repræsenteret på kampagnekortet**, men deres tilstedeværelse gør dem ikke til frie militære byggepladser. Spilleren kan ikke vælge en B- eller C-by og opføre en ny kaserne, arsenal, større militært depot, våbenfabrik eller permanent fæstning dér gennem det normale byggeinterface.

De mindre byer kan fortsat have og udvikle civile/lokale funktioner som handel, havn/færge, lager, håndværk, lokal industri, vej- og senere jernbaneforbindelser, hvor de øvrige systemregler tillader det. **Kun A — Development City** kan som normal regel kvalificere til ny tung militær byudbygning, og også dér kræves de relevante nationale, økonomiske, teknologiske og historiske forudsætninger.

Historisk dokumenterede anlæg er en undtagelse: hvis en B- eller C-by faktisk havde fx fæstning, batteri, garnison, kaserne eller depot ved scenariestart, må det eksistere som et **fixed/special building**. Det giver ikke fri ret til at bygge flere tunge anlæg.

## Territorial zone model 1851

Fra v00.02.10 får hver by en bindende `ZoneId`. Zone-laget er det regionale niveau mellem den enkelte by/settlement og staten. For Kongeriget Danmark tager første kanoniske zoneinddeling udgangspunkt i den **historiske amtsstruktur omkring 1851**, fordi den passer naturligt til regional befolkning, skat, rekruttering, administration, forsyning og udvikling.

København behandles som særskilt hovedstadszone. Københavns Amt og Roskilde Amtsrådskreds abstraheres i denne første gameplay-model til én zone `DK-Z02-KBH-AMT`; Roskilde og Køge ligger derfor i denne zone. Skanderborg Amt er en særskilt 1851-zone og må først fusioneres administrativt med Aarhus efter den historiske ændring i 1867.

Zonegrænserne er **ikke bare dekorative streger**. En zone skal senere kunne have regional population, rural population, production, tax base, recruitment pool, supply throughput, infrastructure, unrest/loyalty, owner/controller og occupation state. Byer er noder inde i zonen og arver ikke automatisk alle zonens byggeoptioner.

Det samlede zoneregister og reglerne ligger i **Del 3F / ZONE-REG-01**.

## City data model

Hver by-node skal mindst have:

- `CityId`
- historisk/UI-navn
- WGS84-koordinat
- `Population1850`
- `DevelopmentTier`
- `ZoneId`
- `ZoneName1851`
- owner/controller
- port/ferry-status
- commerce/industry/infrastructure-baseline
- liste over dokumenterede special buildings
- byggebegrænsninger
- evt. victory/logistics value
- kilde/confidence

# CITY-REG-01 — 68 købstæder, folketal og zoner 1850/1851

## København
| By | Indbyggere 1850 | Klasse | ZoneId | Zone 1851 |
| --- | ---: | :---: | --- | --- |
| København | 129.695 | A | DK-Z01-KBH | København Stad |

## Sjælland og Møn
| By | Indbyggere 1850 | Klasse | ZoneId | Zone 1851 |
| --- | ---: | :---: | --- | --- |
| Helsingør | 8.111 | A | DK-Z03-FRB | Frederiksborg Amt |
| Hillerød | 1.929 | C | DK-Z03-FRB | Frederiksborg Amt |
| Frederikssund | 612 | C | DK-Z03-FRB | Frederiksborg Amt |
| Roskilde | 3.805 | B | DK-Z02-KBH-AMT | Københavns Amt / Roskilde Amtsrådskreds |
| Køge | 2.436 | B | DK-Z02-KBH-AMT | Københavns Amt / Roskilde Amtsrådskreds |
| Holbæk | 2.638 | B | DK-Z04-HOL | Holbæk Amt |
| Nykøbing Sjælland | 1.282 | C | DK-Z04-HOL | Holbæk Amt |
| Kalundborg | 2.490 | B | DK-Z04-HOL | Holbæk Amt |
| Ringsted | 1.380 | C | DK-Z05-SOR | Sorø Amt |
| Sorø | 901 | C | DK-Z05-SOR | Sorø Amt |
| Slagelse | 4.011 | A | DK-Z05-SOR | Sorø Amt |
| Korsør | 1.819 | C | DK-Z05-SOR | Sorø Amt |
| Skælskør | 1.134 | C | DK-Z05-SOR | Sorø Amt |
| Næstved | 2.735 | B | DK-Z06-PRA | Præstø Amt |
| Store Heddinge | 1.076 | C | DK-Z06-PRA | Præstø Amt |
| Præstø | 951 | C | DK-Z06-PRA | Præstø Amt |
| Vordingborg | 1.579 | C | DK-Z06-PRA | Præstø Amt |
| Stege | 1.808 | C | DK-Z06-PRA | Præstø Amt |

## Bornholm
| By | Indbyggere 1850 | Klasse | ZoneId | Zone 1851 |
| --- | ---: | :---: | --- | --- |
| Rønne | 4.717 | A | DK-Z08-BOR | Bornholms Amt |
| Hasle | 853 | C | DK-Z08-BOR | Bornholms Amt |
| Allinge | 609 | C | DK-Z08-BOR | Bornholms Amt |
| Sandvig | 298 | C | DK-Z08-BOR | Bornholms Amt |
| Svaneke | 1.009 | C | DK-Z08-BOR | Bornholms Amt |
| Neksø | 1.403 | C | DK-Z08-BOR | Bornholms Amt |
| Åkirkeby | 561 | C | DK-Z08-BOR | Bornholms Amt |

## Lolland-Falster
| By | Indbyggere 1850 | Klasse | ZoneId | Zone 1851 |
| --- | ---: | :---: | --- | --- |
| Stubbekøbing | 1.081 | C | DK-Z07-MAR | Maribo Amt |
| Nykøbing Falster | 2.123 | B | DK-Z07-MAR | Maribo Amt |
| Nysted | 1.082 | C | DK-Z07-MAR | Maribo Amt |
| Sakskøbing | 917 | C | DK-Z07-MAR | Maribo Amt |
| Maribo | 1.667 | C | DK-Z07-MAR | Maribo Amt |
| Rødby | 1.339 | C | DK-Z07-MAR | Maribo Amt |
| Nakskov | 2.955 | B | DK-Z07-MAR | Maribo Amt |

## Fyn og Langeland
| By | Indbyggere 1850 | Klasse | ZoneId | Zone 1851 |
| --- | ---: | :---: | --- | --- |
| Odense | 11.122 | A | DK-Z09-ODE | Odense Amt |
| Svendborg | 4.556 | A | DK-Z10-SVE | Svendborg Amt |
| Nyborg | 3.059 | B | DK-Z10-SVE | Svendborg Amt |
| Assens | 2.963 | B | DK-Z09-ODE | Odense Amt |
| Fåborg | 2.328 | B | DK-Z10-SVE | Svendborg Amt |
| Kerteminde | 1.833 | C | DK-Z09-ODE | Odense Amt |
| Middelfart | 1.633 | C | DK-Z09-ODE | Odense Amt |
| Bogense | 1.497 | C | DK-Z09-ODE | Odense Amt |
| Rudkøbing | 2.333 | B | DK-Z10-SVE | Svendborg Amt |

## Jylland
| By | Indbyggere 1850 | Klasse | ZoneId | Zone 1851 |
| --- | ---: | :---: | --- | --- |
| Kolding | 2.865 | B | DK-Z11-VEJ | Vejle Amt |
| Fredericia | 4.326 | A | DK-Z11-VEJ | Vejle Amt |
| Vejle | 3.300 | B | DK-Z11-VEJ | Vejle Amt |
| Horsens | 5.827 | A | DK-Z12-SKA | Skanderborg Amt |
| Skanderborg | 1.042 | C | DK-Z12-SKA | Skanderborg Amt |
| Aarhus | 7.886 | A | DK-Z13-AAR | Aarhus Amt |
| Ebeltoft | 1.112 | C | DK-Z14-RAN | Randers Amt |
| Grenaa | 1.099 | C | DK-Z14-RAN | Randers Amt |
| Randers | 7.338 | A | DK-Z14-RAN | Randers Amt |
| Mariager | 546 | C | DK-Z14-RAN | Randers Amt |
| Hobro | 1.173 | C | DK-Z14-RAN | Randers Amt |
| Nibe | 1.161 | C | DK-Z16-AAL | Aalborg Amt |
| Aalborg | 7.745 | A | DK-Z16-AAL | Aalborg Amt |
| Sæby | 895 | C | DK-Z17-HJO | Hjørring Amt |
| Frederikshavn | 1.374 | C | DK-Z17-HJO | Hjørring Amt |
| Skagen | 1.400 | C | DK-Z17-HJO | Hjørring Amt |
| Hjørring | 1.914 | C | DK-Z17-HJO | Hjørring Amt |
| Thisted | 2.343 | B | DK-Z18-THI | Thisted Amt |
| Nykøbing Mors | 1.398 | C | DK-Z18-THI | Thisted Amt |
| Skive | 1.256 | C | DK-Z15-VIB | Viborg Amt |
| Viborg | 4.039 | A | DK-Z15-VIB | Viborg Amt |
| Lemvig | 859 | C | DK-Z19-RIN | Ringkøbing Amt |
| Holstebro | 1.305 | C | DK-Z19-RIN | Ringkøbing Amt |
| Ringkøbing | 1.274 | C | DK-Z19-RIN | Ringkøbing Amt |
| Varde | 1.774 | C | DK-Z20-RIB | Ribe Amt |
| Ribe | 2.984 | B | DK-Z20-RIB | Ribe Amt |

## Kontrolsum
- Antal købstæder: **68**
- Antal territoriale startzoner i Kongeriget: **20**
- København: **129.695**
- Øvrige 67: **160.870**
- Samlet købstadsbefolkning: **290.565**

## Strategiske settlements uden købstadsstatus

De 68 købstæder er ikke hele settlement-nettet. Strategisk relevante ikke-købstæder kan tilføjes som `Settlement`-noder på baggrund af kilder, fx. pga. havn/ladeplads/færgested, bro, jernbane, militært anlæg, industri, depot eller chokepoint. De får som udgangspunkt ikke Development City-funktioner eller fri tung militær udbygning.

Første researchliste omfatter bl.a. **Nørresundby, Løgstør, Silkeborg og Frederiksværk**. Alle sådanne settlements skal også tildeles `ZoneId`.

## Dynamisk historisk udvikling

Registeret er startstate. Byer/settlements kan vokse, få havn/station/industri og ændre strategisk betydning over kampagnens årtier. **Esbjerg er ikke en 1851-startby** og skal først kunne opstå senere i en lang kampagne efter relevante historiske/dynamiske beslutninger og havneanlæg.

Zone-systemet skal kunne håndtere historiske administrative ændringer uden at flytte byernes koordinater. Eksempel: Skanderborg Amt er separat ved start i 1851, men kan senere fusioneres administrativt med Aarhus efter 1867, mens de underliggende city/settlement-noder bevares.

## Næste registre

- `ZONE-REG-01 — Kongeriget Danmark 1851` — defineret i Del 3F
- `CITY-REG-02 — Hertugdømmet Slesvig 1851`
- `CITY-REG-03 — Hertugdømmet Holsten 1851`
- `CITY-REG-04 — Hertugdømmet Lauenborg 1851`
- separat register over strategiske ikke-købstads-settlements i Kongeriget

## Campaign3-implementeringsregel

Når næste Campaign3 city/zone-version implementeres skal:

1. alle 68 købstæder eksistere som city-noder,
2. hver by have bindende `ZoneId`, `Population1850` og `DevelopmentTier`,
3. 3D-visual størrelse følge population/tier,
4. C-byer være små men synlige/klikbare,
5. B-byer være tydeligere regionale noder,
6. A-byer være de største 3D-klynger,
7. city-panelet vise population, klasse og zone,
8. B/C-tung militær konstruktion håndhæves som blokeret,
9. historiske fixed/special buildings kunne overstyre den normale byggeblokering,
10. zone-overlay og zone-selection bruge samme `ZoneId` som city-data og simulation.

## Kilder

Primær befolkningskilde: Danmarks Statistik, *En detailleret Fremstilling af Folkemængden i Danmark i Aaret 1850...*, Statistisk Tabelværk, Ny Række, Bind 1.

Administrativ kontrol: Trap Danmark / Lex samt historiske Trap-bind for amtsinddeling og købstæder. Den første zone-baseline følger 1851-strukturen frem for senere administrative reformer.