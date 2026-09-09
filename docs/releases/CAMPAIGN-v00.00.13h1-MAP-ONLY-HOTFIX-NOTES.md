# PROJECT 1864 Campaign v00.00.13h1 — MAP-ONLY HOTFIX

Status: DEV / afventer Unity compile + runtime QA.

## Formål

Midlertidigt at fjerne kamp- og formationsstøj fra CampaignMap, så Danmark-kortets geografi, grafik, byer, kaserne/farm, vegetation, infrastruktur, labels og kamera kan færdiggøres uden at QA-fjenden starter kampe eller åbner tactical flow.

## Implementeret

- `CampaignMapController` deaktiveres på CampaignMap i MAP-ONLY DEV. Dermed er QA enemy orders, formation movement, hostile contact detection, contact panel og `PrototypeBattle` transition slået fra.
- `CampaignMapUsabilityV011` og `CampaignMapSearchHoverV011` deaktiveres midlertidigt for at fjerne gamle diagnostic/control/search presentation-lag.
- Strategiske formation renderers og route ghost skjules, men formation-data slettes ikke.
- Foreign node/control/settlement renderers force-hides fortsat; Sverige/Norge/Finland/Tyskland forbliver kun data i denne fase.
- Et separat map-only timepanel bevarer pause, x1, x5, x20 og reset.
- Campaign-tiden fortsætter via `CampaignSession.AdvanceHours`, så Aalborg-kasernen og Aarhus-farmen kan gennemløbe deres staged construction under visuel QA.
- Build overlay: `v00.00.13h1 DENMARK PREMIUM MAP-ONLY DEV`.

## Ikke ændret

- Tactical TEST kode, Officer AI og navigation.
- Strategisk node-/formation-data.
- Geospatial distance/ETA-design.
- Fremtidig battle integration; den er kun midlertidigt gated under map-first udvikling.

## QA

1. Ingen `FJENDTLIG KONTAKT`, `KAMPAGNESLAG`, `KÆMP TAKTISK` eller anden battle transition må vises på CampaignMap.
2. Fjendtlige QA-formationer må ikke starte autonome marchordrer.
3. Ingen svenske, norske, finske eller tyske legacy labels må vises.
4. Ingen duplicate legacy city-labels må ligge oven på v13h-labels.
5. Kamera skal fortsat kunne pan/zoom/rotate.
6. Pause/x1/x5/x20 skal flytte campaign clock.
7. Aalborg barracks og Aarhus farm construction skal fortsat kunne udvikle sig med campaign clock.
8. Unity skal compile uden errors.
