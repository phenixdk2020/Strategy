# PROJECT 1864 — City Register 1851

**Designbaseline: v00.02.09**  
**Register: CITY-REG-01 — Kongeriget Danmark ved campaign-start 1. januar 1851**

## Formål

Dette dokument er den kanoniske baseline for byer i **Kongeriget Danmark** ved PROJECT 1864's start i 1851. Registeret skal erstatte de hidtidige få QA-byer på campaign-kortet.

Populationstallene bruger **folketællingen 1. februar 1850**, fordi det er den nærmeste komplette samtidige landsdækkende opgørelse før campaign-start 1. januar 1851.

Danmarks Statistiks samtidige tabel opregner **68 købstæder** i Kongeriget. København havde 129.695 indbyggere, mens de øvrige 67 købstæder tilsammen havde 160.870. Registerets tal summerer derfor til **290.565 personer** i de 68 købstæder.

> **Geopolitisk afgrænsning:** Dette register dækker Kongeriget Danmark, ikke Hertugdømmerne Slesvig, Holsten og Lauenborg. De skal have egne registre. Fx. skal Ærøskøbing/Marstal behandles i den relevante hertugdømmekontekst for 1851 og må ikke bare indføres som almindelige kongerigsbyer.

## Kilder og datakontrakt

Primær kilde:

- Danmarks Statistik, *En detailleret Fremstilling af Folkemængden i Danmark i Aaret 1850...*, Statistisk Tabelværk, Ny Række, Bind 1. Den samtidige tabel rangerer Kongerigets 68 købstæder efter folketallet 1. februar 1850.
- Publikationsside: https://www.dst.dk/da/Statistik/udgivelser/VisPub?cid=19933
- Direkte historisk tabelværk: https://www.dst.dk/pubfile/19933/folkm1850

Kontrolkilder:

- Trap Danmark / Trap 5 byartikler via https://trap5.lex.dk/ anvendes som kontrol ved OCR-/transskriptionsusikkerhed.
- Historiske lokale kilder kan senere supplere med bygrænser, erhverv, havne, garnisoner, arsenaler og konkrete bygninger.

Byens population i simulationen er **ikke** automatisk lig med hele den omkringliggende zones/kommunens befolkning. Rural population skal ligge separat på region/zone-niveau.

## Udviklingsklasser

Byerne skal alle findes på strategikortet, men ikke alle må udvikles på samme måde.

| Klasse | Baseline | Tilladt udvikling | Tung militær udbygning |
| --- | --- | --- | --- |
| **A — Development City** | Som udgangspunkt mindst 4.000 indb. i 1850 | Fuld by-/infrastrukturudvikling efter økonomi, teknologi, tid og historiske rammer | **Ja**, men kun hvor nationale regler, ressourcer og øvrige krav tillader det |
| **B — Regional Town** | Som udgangspunkt 2.000–3.999 indb. | Marked, lager, vej/havn, civil kapacitet, mindre industri og lokal logistik | **Nej som normal byggeoption**. Historisk eksisterende militære anlæg kan være faste assets |
| **C — Minor Town** | Under 2.000 indb. | Findes på kortet; lokal handel, havn/færge hvor relevant, mindre lager/håndværk og lokal økonomi | **Nej**. Ingen ny kaserne, arsenal, større depot, våbenfabrik eller permanent fæstning som fri spillerudbygning |

### Historisk override

Klassen er en **gameplay-cap**, ikke en påstand om at små byer historisk aldrig kunne have militære anlæg.

Hvis en B- eller C-by dokumenteret havde fx. en fæstning, batteri, garnison, depot eller anden særlig installation ved scenariestart, må anlægget eksistere som et **historisk fixed/special building**. Byens udviklingsklasse giver ikke automatisk spilleren ret til at bygge flere tunge anlæg.

Eksempel: et historisk fæstningsanlæg må eksistere, selv hvis byens folketal placerer den under A-grænsen.

## City data model

Hver by-node skal mindst have:

- `CityId`
- historisk navn + normaliseret UI-navn
- WGS84 longitude/latitude
- `Population1850`
- `DevelopmentTier` A/B/C
- tilknyttet territorial zone
- owner/controller
- port/ferry status
- commerce/industry/infrastructure baseline
- liste over historisk dokumenterede special buildings
- bygge-/udviklingsrestriktioner
- evt. victory/logistics value
- kilde-/confidence-felter

Klik på en by skal senere åbne city-panelet. Panelet skal kun vise dokumenterede special buildings som historiske fakta; QA-/placeholder-faciliteter skal være markeret som sådanne.

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
- Øvrige 67 købstæder: **160.870**
- Samlet population i registeret: **290.565**

Kontrolsummen matcher den samtidige 1850-statistiks opgørelse.

## Ikke-købstæder og strategiske settlements

De 68 købstæder er den første kanoniske city-node baseline, men de er **ikke hele settlement-nettet**.

PROJECT 1864 skal også kunne have mindre, ikke-købstads-noder, når de er strategisk relevante, fx. på grund af:

- vigtig havn/ladeplads/færgested,
- bro/overgang,
- jernbaneknudepunkt,
- militært anlæg,
- industri,
- forsyningsdepot,
- geografisk chokepoint.

Disse noder skal have en særskilt `Settlement`-klasse og må som udgangspunkt **ikke** få Development City-funktioner eller fri tung militær udbygning.

Eksempler der skal researches/registreres separat omfatter bl.a. **Nørresundby, Løgstør, Silkeborg og Frederiksværk**. Listen udvides kun med kildegrundlag.

## Dynamisk historisk udvikling

Byregisteret er startstate, ikke et statisk verdenskort.

En by/settlement kan over kampagnens årtier:

- vokse eller stagnere,
- få ny havn, station eller industri,
- blive strategisk vigtigere,
- ændre kapacitet,
- i særlige historiske/dynamiske tilfælde udvikle sig til en ny egentlig by-node.

**Esbjerg må ikke være en 1851-startby.** Den kan senere opstå som historisk/dynamisk udvikling i en lang kampagne efter de relevante beslutninger og havneanlæg.

## Næste city-registre

Før et fuldt 1864-scenarie kan betragtes som geografisk/historisk komplet, skal der mindst oprettes:

1. `CITY-REG-02 — Hertugdømmet Slesvig 1851`
2. `CITY-REG-03 — Hertugdømmet Holsten 1851`
3. `CITY-REG-04 — Hertugdømmet Lauenborg 1851`
4. separat register over strategiske ikke-købstads-settlements i Kongeriget
5. senere tilsvarende registre for Sverige/Norge, Preussen og øvrige campaign-lande.

## Implementeringsregel for Campaign3

Når v10l implementeres:

- de nuværende 10 QA-byer må ikke længere være den kanoniske byliste;
- **Esbjerg fjernes fra 1851-startstate**;
- alle 68 købstæder skal eksistere som city-noder på kortet;
- 3D-visual størrelse/kompleksitet skal følge `DevelopmentTier` og population;
- C-byer skal være små men synlige og klikbare;
- B-byer skal være tydeligere regionale noder;
- A-byer skal have de største 3D-byklynger;
- city-panel skal kunne vise population, klasse og special buildings;
- special buildings må ikke opfindes som historiske facts uden kilde/confidence;
- tung militær konstruktion skal håndhæve tier-reglerne ovenfor.
