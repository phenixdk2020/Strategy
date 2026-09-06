# PROJECT 1864 — Designmanual v00.02.00 — Feature Architecture

## 28. Feature-arkitektur – udviklingsklumper

Nedenstående chunks beskriver funktionelle leverancer, ikke kalenderestimater. Hver chunk skal kunne testes isoleret og har exit-kriterier. Nummereringen er foreslået dependency-order, men flere spor kan udvikles parallelt når fundamentet er stabilt.

### F00 – Projektfundament og simulationskerne

Formål. Skabe en stabil, data-drevet Unity-arkitektur, hvor simulation state er adskilt fra visuelle GameObjects.

Indhold.

- Stable IDs og registries for nationer, officers, formations, locations og equipment.
- Simulation clock, event queue, deterministic/random service og saveable state.
- Domain assemblies: Strategic, Military, Tactical, AI, UI, Data.
- Automated tests for state transitions og serialization.

Afhængigheder. Ingen. Dette er fundamentet.

Exit-kriterier.

- En headless simulation kan starte, køre 30 simulerede dage og gemme/indlæse uden Unity-sceneafhængighed.
- Samme unit state kan bindes til og afkobles fra en visual representation uden datatab.

Slutversionsnote. Alt andet bør bygge på dette lag.

### F01 – Strategisk kort, kamera og tid

Formål. Gøre kampagnen navigerbar og levende i realtid.

Indhold.

- 3D-reliefkort prototype.
- Pan/zoom/rotate, map selection og overlays.
- Pause + speed steps.
- World locations, roads og basic path graph.

Afhængigheder. F00

Exit-kriterier.

- To testformationer kan marchere samtidigt mens tiden kører.
- Pause/resume og speed ændrer ikke simuleringsresultatet ud over timing.

### F02 – Nationer, regioner og politisk kontrol

Formål. Etablere world-state for Danmark, Sverige-Norge, Preussen og Østrig.

Indhold.

- Nation definitions, ownership/control, regions, cities, ports og strategic objectives.
- War/peace relation og simple war goals.
- Basic occupation/control transition.

Afhængigheder. F00, F01

Exit-kriterier.

- Scenario kan indlæse alle kernemagter og regioner med stable state.
- Territorial control kan ændres og gemmes.

### F03 – OOB og enhedsdata

Formål. Implementere den historiske kommandostruktur som generisk tree/graph.

Indhold.

- CommandNode med valgfrie niveauer.
- Parent, attached-to, subunits og commander assignments.
- Strength, morale, cohesion, fatigue, weapons og supply fields.
- OOB browser/editor for debug.

Afhængigheder. F00, F02

Exit-kriterier.

- Dansk og preussisk eksempel-OOB kan eksistere med forskellige hierarkier.
- En unit kan detach/reattach uden at miste identitet eller historik.

### F04 – Officer-system og autonomi

Formål. Gøre officerer til aktive beslutningstagere.

Indhold.

- Skills, traits, personality og reputation.
- Commander intent evaluation.
- Autonomy levels og local reaction scoring.
- Officer succession ved casualty/removal.

Afhængigheder. F03

Exit-kriterier.

- To officerer med forskellige profiler reagerer dokumenterbart forskelligt på samme threat.
- AI-handling kan forklares via debug reason codes.

### F05 – Ordrer, couriers og kommunikation

Formål. Fjerne telepatisk kontrol og skabe command friction.

Indhold.

- Order object lifecycle.
- Courier/telegraph transmission model.
- Delay, delivery, acknowledgement og supersede.
- Order composer UI prototype.

Afhængigheder. F03, F04, F01

Exit-kriterier.

- En ordre kan sendes, forsinkes, leveres og udføres efter simuleret tid.
- En officer kan reagere lokalt før en ny ordre ankommer.

### F06 – Operationel march, congestion og contact

Formål. Skabe meningsfuld bevægelse mellem strategiske objectives.

Indhold.

- Road-aware movement, formation speed og fatigue.
- March column length og chokepoint congestion.
- Screening/recon radius.
- Encounter detection og deployment state.

Afhængigheder. F01, F03, F05

Exit-kriterier.

- To brigader kan marchere på samme vej og skabe realistisk kø.
- Enemy contact kan føre til stop, withdrawal eller battle trigger afhængigt af orders/AI.

### F07 – Logistik og supply network

Formål. Gøre jernbaner, veje, depoter og transport strategisk relevante.

Indhold.

- Depots, supply nodes og throughput.
- Food, ammunition, forage og transport capacity.
- Consumption og resupply priority.
- Supply overlay og alerts.

Afhængigheder. F02, F03, F06

Exit-kriterier.

- En formation afskåret fra depot mister supply over tid og får gameplay-effekter.
- En alternativ vej/depot kan reetablere supply.

### F08 – Fog of war og reconnaissance

Formål. Erstatte omniscience med tidsstemplet information.

Indhold.

- Observation records med confidence og age.
- Cavalry/scout spotting.
- Last-known-position og estimated strength.
- Intel map overlay.

Afhængigheder. F03, F06

Exit-kriterier.

- AI og spiller kan have forskellige knowledge states om samme unit.
- Gammel observation bliver tydeligt stale og kan være forkert.

### F09 – Tactical battle bootstrap

Formål. Kunne åbne en 3D-slagscene med de faktiske kampagneenheder.

Indhold.

- Battle instance generator.
- Transfer af units/commanders/time/weather fra strategic state.
- Deployment zones fra approach vectors.
- Return af resultat til campaign state.

Afhængigheder. F03, F06

Exit-kriterier.

- Et encounter kan starte et 3D-slag og returnere ændrede styrker uden duplikation af units.
- Campaign clock fremrykkes med battle duration.

### F10 – Formation rendering 1:10 og tactical movement

Formål. Vise regimenter/bataljoner som store levende formationer uden individual-agent overhead.

Indhold.

- Formation slots og render ratio.
- GPU/animation instancing baseline.
- Line/column/skirmish formations.
- Formation-level pathfinding + local avoidance.

Afhængigheder. F09

Exit-kriterier.

- 1.000 simulerede mænd kan vises som ca. 100 modeller og skifte formation stabilt.
- Render ratio kan ændres uden at combat state ændres.

### F11 – Infanteri-ild, range og smoke

Formål. Skabe den centrale synlige black-powder ildkamp.

Indhold.

- Weapon profiles og range curves.
- Volley/independent fire.
- Casualty/morale resolution.
- Muzzle effects, representative projectiles og smoke field.
- Range overlay.

Afhængigheder. F10, F03

Exit-kriterier.

- Effekten af 100 m og 400 m ild er forskellig efter weapon curve.
- Smoke reducerer faktisk LOS/accuracy, ikke kun grafik.

### F12 – Morale, cohesion, fatigue og rout

Formål. Gøre slag til kamp om organisation og vilje, ikke HP.

Indhold.

- Continuous morale/cohesion/fatigue values.
- State thresholds: shaken/wavering/rout.
- Rally, officer influence og contagion.
- Stragglers, prisoners og pursuit hooks.

Afhængigheder. F11, F04

Exit-kriterier.

- En formation kan rout’e før udslettelse og senere rally.
- Flank/rear threat og officer quality kan ændre udfald mærkbart.

### F13 – Artilleri

Formål. Tilføje feltartilleri som fuldt taktisk system.

Indhold.

- Gun/battery data, limber/unlimber, ammunition.
- Targeting, LOS, trajectory visuals, counter-battery.
- Crew/horses og capture/loss.

Afhængigheder. F09, F11, F12

Exit-kriterier.

- Et batteri kan deploye, skyde, bruge ammunition, flytte og blive erobret.
- Piece count returneres korrekt til kampagnen.

### F14 – Kavaleri og reconnaissance

Formål. Forbinde operationel information med taktisk mobility.

Indhold.

- Screening, reconnaissance og courier modifiers på strategic layer.
- Tactical charge, pursuit, dismounted/hold options efter type.
- Horse fatigue/casualties/forage.

Afhængigheder. F06, F08, F10, F12

Exit-kriterier.

- Kavaleri kan forbedre contact information og påvirke retreat/pursuit.
- Horse losses påvirker efterfølgende mobility.

### F15 – Ingeniører, broer og befæstning

Formål. Gøre terrænændringer og fortifikationer til vedvarende state.

Indhold.

- Field works, bridge tasks, demolition, repair.
- Task progress og material/tool requirements.
- Transfer mellem strategic improvements og tactical objects.

Afhængigheder. F06, F07, F09

Exit-kriterier.

- En bygget/ødelagt bro påvirker både strategic pathing og battle map.
- Entrenchment kan forbedres over tid og følge unit/position data.
