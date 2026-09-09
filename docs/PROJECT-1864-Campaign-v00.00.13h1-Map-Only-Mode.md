# PROJECT 1864 — Campaign v00.00.13h1 MAP-ONLY DEV

## Beslutning

Mens Danmark-kortets premium presentation færdiggøres, skal campaign combat være midlertidigt gated væk fra runtime-visningen. Formålet er at isolere map QA fra kamp-/formation-systemer uden at slette eller omskrive den langsigtede strategiske/taktiske arkitektur.

## Midlertidig runtime-regel

`CampaignMap` kører i `MAP-ONLY DEV`:

- ingen autonome QA enemy march orders,
- ingen strategic formation movement,
- ingen hostile contact detection,
- ingen battle/contact popup,
- ingen `PrototypeBattle` transition,
- ingen formation markers/route ghost,
- ingen gamle v11 diagnostic/search UI-lag.

Campaign clock fortsætter separat, så state-drevne construction visuals kan testes. Aalborg-kasernen og Aarhus-farmen forbliver derfor aktive construction QA-assets.

## Danmark-first presentation

Kun danske presentation-elements må være synlige. Udenlandske noder, control markers, settlements og labels forbliver skjult, mens deres data bevares i `CampaignSession` til senere geografisk udvidelse.

## Reaktivering senere

MAP-ONLY er en udviklingsgate, ikke en permanent designændring. Når terrain, water, settlements, infrastructure, construction, vegetation, labels, camera og overall composition er godkendt, genaktiveres strategic formation movement/contact og tactical battle transition i en separat integration/QA-version.

## Acceptance gate

- CampaignMap kan køre uden battle UI eller tactical transition.
- Ingen autonome fjendtlige bevægelser afbryder map QA.
- Ingen foreign/duplicate legacy labels.
- Campaign clock og construction progression fungerer.
- Tactical TEST-kode er urørt.
