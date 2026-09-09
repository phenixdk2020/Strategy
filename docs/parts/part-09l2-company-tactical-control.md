# PROJECT 1864 — v00.00.09l2 Company Tactical Control + Formation Geometry

## Formål

09l2 ændrer den første full-scale 1:1 prototype fra et regimentcentreret render-/control-layout til et hierarkisk tactical layout, hvor **kompagniet er den mindste direkte kontrollerbare infanteriformation**. Regiment, bataljon og brigade forbliver højere command-niveauer.

Den afgørende invariant er fortsat, at 1:1 visuals ikke betyder 1:1 AI/GameObjects. De ca. 8.000 almindelige infanterister er stadig GPU-instanced render-data. Kun company tactical centres, HQ'er og andre command-/special-arm entities er egentlige GameObjects.

## Command hierarchy

```text
Brigade HQ
  -> Regiment HQ
      -> Battalion
          -> Company tactical centre
              -> 1:1 instanced soldiers
```

- Dansk regiment: 2 bataljoner x 4 kompagnier = 8 tactical companies.
- Preussisk regiment: 3 bataljoner x 4 kompagnier = 12 tactical companies.
- Den aktuelle fire-regiments pilot indeholder dermed 40 company tactical centres: 16 danske og 24 preussiske.
- Company parent relation kommer fra OOB-data og må ikke udledes alene af kompagni-nummeret.

## Player control

09l2 indfører separat company-selection mode uden at fjerne den eksisterende regiment-control.

- Enkeltklik på et dansk company block vælger kun kompagniet.
- Shift + klik tilføjer company til selection.
- Ctrl + klik toggler company.
- LMB drag box-select vælger danske company centres i rammen.
- RMB giver movement-order til valgte companies.
- Alt + RMB appender waypoint.
- RMB-drag kan definere final facing.
- `F` = Line for valgte companies.
- `C` = Column for valgte companies.
- `H` = Hold/cancel company route.
- `Z/X` = drej selected company facing i 15 graders trin.
- Double-click på et company skifter tilbage til whole-regiment selection.
- Klik på regimentets centrale tactical/HQ-area kan fortsat vælge regimentet gennem den eksisterende PlayerCommander.

Company manual control og regiment manual control er gensidigt eksklusive selection modes. Ved en direkte company movement-order ryddes den aktive PlayerCommander-regimentroute for parent regimentet, parent Regiment sættes Hold, og aktiv regimental Officer AI slås fra. Dette forhindrer parent-transform og company-centre i at modtage konkurrerende movement writes.

## Company movement model i 09l2

Company-centret er child af parent Regiment-transformet. Det giver to modes:

1. **Whole-regiment movement:** V3 flytter fortsat regimentets authoritative transform. Alle companies følger deres lokale deployment offsets.
2. **Manual company manoeuvre:** parent regiment stoppes, og det valgte company flytter sin egen tactical centre-position relativt/under parent command entity.

Dette er en overgangsarkitektur. En senere gate kan gøre battalion/company navigation helt selvstændig, men 09l2 undgår at erstatte den validerede V3-navigation samtidig med company-control-indførelsen.

## Formation geometry

Den gamle 09k-renderer hængte alle 1:1 soldiers direkte på ét Regiment-pivot. Det gav unaturlige L-former, lange slanger og store visuelle sving ved rotation.

09l2 renderer i stedet hvert kompagni omkring sit eget tactical centre:

- Line: 3 ranks, ca. 0,52 m file spacing og ca. 0,76 m rank spacing.
- Column: 8 files across og kompakt march-depth.
- Companies i samme bataljon opstilles som fire separate line blocks.
- Bataljoner får egne depth rows i regimentets template.
- Formationens regiment-pivot ligger dermed centralt i hierarchy-layoutet i stedet for ved enden af én samlet soldatarray.

Casualty visuals bruger deterministisk slot-shuffle. Når CurrentStrength falder, forsvinder soldater fordelt gennem company footprintet i stedet for kun fra slutningen af arrayet. Det reducerer de kunstige L-/afskårne formationer.

## Casualty state

Regiment.CurrentStrength forbliver den authoritative combat-strength i 09l2, fordi eksisterende volley/morale/combat-system endnu arbejder på regimentniveau.

09l2 synkroniserer ændringer ned til company OOB-state:

- Regimental staff strength reserveres separat.
- Resten af CurrentStrength fordeles over companies.
- Ved tab reduceres company CurrentStrength balanceret efter resterende strength-fraction.
- 1:1 renderer bruger company CurrentStrength direkte.

Næste combat-gate kan flytte target exposure, fire groups, ammo og casualties til company-level uden at ændre OOB-identiteten igen.

## Rendering

- `PrototypeCompanyRenderer09L2` er den autoritative ordinary-infantry renderer.
- Den gamle 09k full-regiment foot renderer deaktiveres først efter at de fire mounted Regimental HQ groups er oprettet.
- Mounted Regimental HQ bevares.
- Brigade HQ og dual standards fra 09l bevares.
- Ordinary soldiers har fortsat ingen individuel MonoBehaviour, collider, NavMeshAgent eller AI.
- Company renderer følger terrain height pr. instanced soldier.
- Weapon-specific reload pose fra 09j/09k bevares via parent regimentets faktiske reload-window.

## AI relation

09l2 ændrer ikke den eksisterende Brigade AI mission model. Når Brigade/Regimental AI flytter et helt Regiment, følger company blocks som underformationer.

Den permanente næste AI-retning er, at Regimental AI fordeler companies/battalions til roller som:

- ENGAGED
- SUPPORT
- RESERVE
- MANOEUVRE
- WITHDRAW

Brigade AI beslutter hvor mange regimenter der committes. Regiment AI beslutter efterfølgende hvilke bataljoner/companies der committes lokalt.

## QA gate

1. Unity 6000.6.0f1 compiles uden røde errors.
2. Build marker er `PROJECT 1864 | v00.00.09l2 TEST`.
3. Console viser `COMPANY-09L2|Installed=True`.
4. Console viser `RENDER-09L2|Installed=True`.
5. Console viser `RENDER-AUTH-09L2|...DuplicateInfantry=False`.
6. Danske companies kan vælges enkeltvis med klik.
7. Shift/Ctrl multi-select virker.
8. LMB box-select vælger company centres, ikke kun regiment centres.
9. RMB flytter kun selected companies og stopper parent regiment route/AI for at undgå competing movement writes.
10. Double-click company giver whole-regiment selection igen.
11. F/C/H/Z/X virker på company selection.
12. Whole-regiment V3 movement virker fortsat, når regimentet er valgt i stedet for company.
13. Regimenterne vises som rene battalion/company blocks uden gamle L-former/slange-layout.
14. Casualties giver spredte visuelle huller i company blocks frem for at skære én formationende af.
15. 1:1 infantry, mounted regimental HQ, Brigade HQ, dual standards, cavalry og artillery forbliver synlige.
16. FPS observeres ved close, medium og full-map zoom.

## Senere gates

- Battalion direct selection som eget command-level.
- Regimental AI company allocation/reserve commitment.
- Company-level fire eligibility, ammo, LOS og casualty targeting.
- Independent company obstacle/path navigation efter separat V3 replacement QA.
- OOB Designer 09m med stable Unit IDs og drag/drop hierarchy.
