# B-280–B-289 — Grand Campaign: lande, zoner og fælles semantic zoom

## Status

**BESLUTTET / IMPLEMENTERING STARTET i v00.00.10b CAMPAIGN TEST**

Grand Campaign skal være et egentligt land-/statsniveau-spil og ikke kun en lineær 1864-scenarieoversigt. Spilleren vælger en stat og styrer dens strategiske ressourcer, formationer, territorier og operationer gennem et zoneopdelt 3D-kort.

## Campaign startdato — LÅST

**Grand Campaign starter 1. januar 1851.**

Designhensigt:

- spilleren får ca. 13 års strategisk forberedelse frem mod 1864,
- oprustning, mobilisering, våbenmodernisering, jernbane, depoter, officerer og doktrin skal kunne ændre et lands militære styrke over flere år,
- 1850'ernes og 1860'ernes internationale krige og teknologiske udvikling skal kunne påvirke campaign-verdenen,
- 1864 er en vigtig historisk milepæl, men ikke nødvendigvis kampagnens slutdato,
- arkitekturen skal kunne understøtte senere alternative startdatoer/scenarier uden at ændre hovedkampagnens baseline.

Første campaign clock/state skal derfor initialiseres til `1851-01-01` i stedet for 1864, når den historiske Danmark-first campaign erstatter QA-scaffolden.

## B-280 — Spilbare stater

Den første datamodel skal kunne repræsentere og vælge de stater, der allerede er besluttet i projektet:

- Danmark
- Sverige-Norge
- Preussen
- Østrig
- Frankrig
- Storbritannien
- Rusland
- Nederlandene
- Kongeriget Hannover
- Mecklenburg
- øvrige relevante stater i Det Tyske Forbund som særskilte eller midlertidigt grupperede campaign-entities, indtil fuld historisk state-list er research-valideret

Vigtig historisk modelregel: der findes ikke én samlet tysk nationalstat i 1864-campaignen. Den tyske del af kortet modelleres som Preussen, Østrig og de øvrige samtidige tyske stater/forbundsmedlemmer.

## B-281 — Alle lande opdeles i zoner

Hele Grand Campaign-kortet opdeles i navngivne strategiske **zoner/områder**. Zoner erstatter ikke det geografiske 3D-terræn; de ligger oven på det som simulationens territoriale lag.

Hver zone har mindst:

- stabil `ZoneId`
- navn
- ejer/kontrol
- geografisk centrum/polygon
- nabo-zoner
- terrain summary
- hovedveje
- jernbane senere
- floder/krydsninger
- havn/kyst
- by/administrativt center
- depot/supply-værdi
- fortifikationer
- population/recruitment hook senere
- economy/industry hooks senere
- tactical battlefield extraction reference

Første arkitektur skal kunne vokse til ca. **100–200+ zoner** uden at systemsiden skal skrives om.

## B-282 — Zonekontrol og bevægelse

Strategiske formationer står i eller bevæger sig gennem zoner. En formation kan have:

- current zone
- destination zone
- route gennem nabo-zoner
- march progress
- attacker/defender posture
- supply connection
- movement speed/terrain modifier

Højreklik på en gyldig zone giver movement order. Ruten skal vises som ghost route på Grand Campaign-kortet.

## B-283 — Geografisk fundament

Grand Campaign skal bygges oven på et rigtigt geografisk landkort og senere højdedata/DEM, ikke et opdigtet Unity-landskab.

Pipeline:

`geografi -> højdedata -> 3D terrain -> historiske veje/byer/jernbane/skov/vand -> zone polygons -> campaign simulation`

Moderne geografi/højdedata og historisk 1864-infrastruktur holdes som separate datalag.

Tidlige QA-zoner må bruges til at bevise UI/state/routing, men skal tydeligt være markeret som placeholders og senere erstattes af georefererede zonegrænser.

## B-284 — Spiller vælger nation

Grand Campaign starter med nation selection. Den valgte nation bliver spillerens strategiske command scope. Andre lande styres af campaign AI, diplomati og historisk/politisk state senere.

Arkitekturen må ikke antage, at Danmark altid er spilleren.

## B-285 — Strategic formations og OOB

Formationer skal have stabile IDs og kunne flyttes mellem campaign og battle mode. En strategic formation indeholder mindst:

- formation ID
- nation
- echelon
- underenheder/regimenter
- manpower
- ammunition
- morale/cohesion
- officer/HQ reference
- supply state
- current zone
- campaign orders

## B-286 — Campaign contact -> Battle mode

Når fjendtlige formationer mødes i en zone eller contact area, oprettes BattleContext med:

- zone/location
- angriber/forsvarer
- deltagende formationer
- campaign date/time
- strength/ammunition/morale/cohesion
- officer state
- approach directions
- geografiske terrain-features

Battle mode returnerer resultater til de samme campaign entities efter slaget.

## B-287 — Semantic zoom gælder både Campaign og Battle mode

Semantic zoom er et fælles visualiseringsprincip for **begge** lag.

### Battle mode

1. Tæt: fulde 3D-soldater, kanoner, heste, HQ og casualties.
2. Mellem: reduceret formationsgrafik/banners.
3. Lang: NATO/APP-6-lignende unit symbols for regiment/battalion/brigade efter echelon.
4. Meget lang: højere formationsniveauer kan aggregeres, men simulationen fortsætter uændret.

### Campaign mode

1. Tæt: terræn, byer, veje, zoner og konkrete formationsstacks.
2. Mellem: zone labels + formation symbols.
3. Lang: NATO/APP-6-lignende army/corps/division symbols og reduceret terrændetalje.
4. Meget lang: national/army overview med aggregation.

Krav på begge lag:

- samme stabile unit/formation ID gennem alle zoom-niveauer
- simulation må ikke ændre regler pga. visual LOD
- selection/order targets skal bevares
- hysteresis/fade for at undgå flimmer ved zoomgrænser
- NATO-symboler er UI-abstraktion, ikke 1864 in-world grafik

## B-288 — Første Grand Campaign CAMPAIGN TEST

Første runnable slice skal bevise:

1. Campaign mode starter i stedet for tactical bootstrap på `campaign`-branchen.
2. Nation kan vælges.
3. Kort viser flere lande opdelt i zoner.
4. Zone kan vælges.
5. Formation/token kan vælges.
6. Formation kan få ordre til en nabo-zone.
7. Campaign tid kan pause/accelereres.
8. Versionen vises permanent på skærmen.
9. Arkitekturen er klar til at erstatte QA-zonegeometri med rigtige georefererede polygons/DEM.

## B-289 — Ikke blokere på geodata

Grand Campaign-arkitekturen og game loop udvikles parallelt med research/geodata. Den første QA-zonevisning er kun en teknisk scaffold. Den må ikke blive opfattet som det endelige historiske kort.
