# PROJECT 1864 — City Register 1851

**Designbaseline: v00.02.09**  
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

## City data model

Hver by-node skal mindst have `CityId`, historisk/UI-navn, WGS84-koordinat, `Population1850`, `DevelopmentTier`, territorial zone, owner/controller, port/ferry-status, commerce/industry/infrastructure-baseline, liste over dokumenterede special buildings, byggebegrænsninger, evt. victory/logistics value samt kilde/confidence.

# CITY-REG-01 — 68 købstæder og folketal 1850

## København
| By | Indbyggere 1850 | Klasse |
| --- | ---: | :---: |
| København | 129.695 | A |

## Sjælland og Møn
| By | Indbyggere 1850 | Klasse |
| --- | ---: | :---: |
| Helsingør | 8.111 | A |
| Hillerød | 1.929 | C |
| Frederikssund | 612 | C |
| Roskilde | 3.805 | B |
| Køge | 2.436 | B |
| Holbæk | 2.638 | B |
| Nykøbing Sjælland | 1.282 | C |
| Kalundborg | 2.490 | B |
| Ringsted | 1.380 | C |
| Sorø | 901 | C |
| Slagelse | 4.011 | A |
| Korsør | 1.819 | C |
| Skælskør | 1.134 | C |
| Næstved | 2.735 | B |
| Store Heddinge | 1.076 | C |
| Præstø | 951 | C |
| Vordingborg | 1.579 | C |
| Stege | 1.808 | C |

## Bornholm
| By | Indbyggere 1850 | Klasse |
| --- | ---: | :---: |
| Rønne | 4.717 | A |
| Hasle | 853 | C |
| Allinge | 609 | C |
| Sandvig | 298 | C |
| Svaneke | 1.009 | C |
| Neksø | 1.403 | C |
| Åkirkeby | 561 | C |

## Lolland-Falster
| By | Indbyggere 1850 | Klasse |
| --- | ---: | :---: |
| Stubbekøbing | 1.081 | C |
| Nykøbing Falster | 2.123 | B |
| Nysted | 1.082 | C |
| Sakskøbing | 917 | C |
| Maribo | 1.667 | C |
| Rødby | 1.339 | C |
| Nakskov | 2.955 | B |

## Fyn og Langeland
| By | Indbyggere 1850 | Klasse |
| --- | ---: | :---: |
| Odense | 11.122 | A |
| Svendborg | 4.556 | A |
| Nyborg | 3.059 | B |
| Assens | 2.963 | B |
| Fåborg | 2.328 | B |
| Kerteminde | 1.833 | C |
| Middelfart | 1.633 | C |
| Bogense | 1.497 | C |
| Rudkøbing | 2.333 | B |

## Jylland
| By | Indbyggere 1850 | Klasse |
| --- | ---: | :---: |
| Kolding | 2.865 | B |
| Fredericia | 4.326 | A |
| Vejle | 3.300 | B |
| Horsens | 5.827 | A |
| Skanderborg | 1.042 | C |
| Aarhus | 7.886 | A |
| Ebeltoft | 1.112 | C |
| Grenaa | 1.099 | C |
| Randers | 7.338 | A |
| Mariager | 546 | C |
| Hobro | 1.173 | C |
| Nibe | 1.161 | C |
| Aalborg | 7.745 | A |
| Sæby | 895 | C |
| Frederikshavn | 1.374 | C |
| Skagen | 1.400 | C |
| Hjørring | 1.914 | C |
| Thisted | 2.343 | B |
| Nykøbing Mors | 1.398 | C |
| Skive | 1.256 | C |
| Viborg | 4.039 | A |
| Lemvig | 859 | C |
| Holstebro | 1.305 | C |
| Ringkøbing | 1.274 | C |
| Varde | 1.774 | C |
| Ribe | 2.984 | B |

## Kontrolsum
- Antal købstæder: **68**
- København: **129.695**
- Øvrige 67: **160.870**
- Samlet: **290.565**

## Strategiske settlements uden købstadsstatus

De 68 købstæder er ikke hele settlement-nettet. Strategisk relevante ikke-købstæder kan tilføjes som `Settlement`-noder på baggrund af kilder, fx. pga. havn/ladeplads/færgested, bro, jernbane, militært anlæg, industri, depot eller chokepoint. De får som udgangspunkt ikke Development City-funktioner eller fri tung militær udbygning.

Første researchliste omfatter bl.a. **Nørresundby, Løgstør, Silkeborg og Frederiksværk**.

## Dynamisk historisk udvikling

Registeret er startstate. Byer/settlements kan vokse, få havn/station/industri og ændre strategisk betydning over kampagnens årtier. **Esbjerg er ikke en 1851-startby** og skal først kunne opstå senere i en lang kampagne efter relevante historiske/dynamiske beslutninger og havneanlæg.

## Næste registre

- `CITY-REG-02 — Hertugdømmet Slesvig 1851`
- `CITY-REG-03 — Hertugdømmet Holsten 1851`
- `CITY-REG-04 — Hertugdømmet Lauenborg 1851`
- separat register over strategiske ikke-købstads-settlements i Kongeriget

## Campaign3-implementeringsregel

Når v10l implementeres skal alle 68 købstæder eksistere som city-noder; 3D-visual størrelse følger population/tier; C-byer er små men synlige/klikbare; B-byer tydeligere regionale noder; A-byer de største 3D-klynger. City-panelet skal vise population, klasse og kun kildebelagte special buildings. **B- og C-byer må ikke tilbyde nye kaserner, arsenaler, større militære depoter, våbenfabrikker eller permanente fæstninger i det normale byggeinterface. Kun A-byer kan kvalificere til disse byggeoptioner efter øvrige regler.** Historiske anlæg i B/C håndteres som fixed/special buildings.

## Kilder

Primær kilde: Danmarks Statistik, *En detailleret Fremstilling af Folkemængden i Danmark i Aaret 1850...*, Statistisk Tabelværk, Ny Række, Bind 1. Trap Danmark/Trap 5 anvendes som kontrol ved OCR-/transskriptionsusikkerhed og til senere lokalhistoriske detaljer.
