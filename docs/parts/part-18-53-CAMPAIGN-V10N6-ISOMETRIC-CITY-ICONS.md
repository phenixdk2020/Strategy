# Del 18: 53 — Campaign3 v00.00.10n6: Isometric City Icons

**Designstatus:** IMPLEMENTERET I BUILD / AWAITS UNITY QA  
**Prototypeversion:** v00.00.10n6  
**Designbaseline:** v00.02.19  
**Work branch:** `work/channel-campaign3-v10n6-isometric-city-icons`

## 53.1 Formål

v10n6 erstatter de runde city-cylindre med **Proposal 3 – Isometric Miniature Town** som den nye visuelle standard for campaign-kortets byer.

Det er en ren visual/runtime-presentation ændring. Følgende forbliver authoritative og uændret:

- `CityId`
- `ZoneId`
- `Population1850`
- A/B/C-tier
- WGS84-position
- city selection
- RMB-på-by som movement destination til byens zone
- build-regler og øvrig city state

## 53.2 A/B/C visual standard

### C — Minor Town

- 3 små huse
- lille jord-/town footprint
- få træer
- ingen stor kirke som obligatorisk center
- mindst visuel størrelse

### B — Regional Town

- 6 huse
- lille kirke med tårn og spir
- tydeligere settlement footprint
- enkelte træer
- mellemstor visuel størrelse

### A — Development City

- 12 huse
- kirke med tårn og spir
- større civic/administrativ bygning
- tættere miniatureby
- største visuelle størrelse

Population giver kun en begrænset ekstra scale inden for samme tier. A/B/C er fortsat den primære klassifikation.

## 53.3 Runtime implementation

`CampaignCityIconV010N6` konverterer eksisterende `CITY_*`-markører efter campaign world creation.

Den gamle cylinder:

- beholder sit GameObject-navn og `GrandCampaignCityMarker`
- får renderer skjult
- får gamle colliders disabled
- får en ny enkel usynlig `BoxCollider`
- nulstilles til `transform.localScale = Vector3.one`

Det synlige ikon oprettes derefter som child `CITY_ICON_ISOMETRIC_10N6`.

Bygningerne er procedurale 3D-miniaturer med:

- cuboid bygningskroppe
- egne gabled-roof meshes
- kirketårn + pyramidespir
- variation i husbredde, dybde, højde, rotation og materialevalg
- deterministisk variation baseret på `CityId`
- shared materialer for at undgå et unødigt materiale pr. bygning

## 53.4 Klik og gameplay

Den visuelle miniatureby må ikke bestemme simulationen. Klik sker fortsat på city root via en simpel BoxCollider.

Det betyder:

- venstreklik på by resolver fortsat korrekt `CityId` og `ZoneId`
- RMB på by kan fortsat bruges som marchordre til byens zone
- de mange små bygningsmeshes har ingen colliders
- city labels bruger fortsat city root-positionen

## 53.5 Zoom

v10n6 ændrer ikke semantic-zoom-kontrakten.

- Close zoom: hele miniaturebyen er synlig.
- Operational zoom: A-byer fortsætter med label-prioritet som før.
- Strategic zoom: eksisterende labelregel bevares; senere semantic zoom kan erstatte miniaturebyen med enklere symboler uden at ændre city state.

## 53.6 Fremtidig udbygning

Arkitekturen er lavet, så procedural miniaturegrafik senere kan udskiftes eller suppleres med historiske assets og strategiske specialbygninger:

- kaserne
- arsenal/depot
- havn
- fæstning
- jernbanestation
- industri
- hospital
- stalde/remount-faciliteter

Disse skal senere kunne kobles til samme city/building state, som battle-map-generatoren kan læse.

## 53.7 QA-gates

1. Unity 6000.6.0f1 compiles uden errors.
2. Badge/topbar viser `v00.00.10n6`.
3. Alle 68 CITY-REG-01 byer findes fortsat.
4. Ingen gammel rund city-cylinder er synlig.
5. Alle city roots har et `CITY_ICON_ISOMETRIC_10N6` child.
6. C-byer viser 3 små huse og er visuelt mindst.
7. B-byer viser 6 huse + kirke.
8. A-byer viser 12 huse + kirke + civic centre og er visuelt størst.
9. København, Odense, Aarhus og Aalborg er tydeligt større end små C-byer.
10. City labels følger stadig byens korrekte position.
11. Venstreklik på by returnerer korrekt city/zone.
12. RMB på by fortsætter med at fungere som zone movement destination.
13. De procedurale building meshes har ingen colliders.
14. Den nye city BoxCollider er nem at ramme uden at dække urimeligt stort kortareal.
15. 20 zoner / 68 byer / population checksum 290565 regressionsbestår.
16. n5 withdrawal-system og zone fixes regressionsbestår.

## 53.8 Rollback

Backup før v10n6:

`backup/channel-campaign3-v10n5-before-v10n6-isometric-city-icons-20260914`
