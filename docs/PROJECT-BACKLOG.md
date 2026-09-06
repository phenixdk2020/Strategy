# PROJECT 1864 — Backlog, beslutninger og idéer

**Baseline: v00.02.08 work branch**

Dette dokument er projektets centrale intake-log for beslutninger og idéer, der ikke nødvendigvis implementeres i den aktive prototype med det samme. Formålet er at sikre, at gameplay-idéer og designbeslutninger fra projektarbejdet ikke kun findes i chatten.

## Statusdefinitioner

- **AKTIV** — implementeres/testes i den aktuelle build.
- **BESLUTTET** — fastlagt designretning; implementering ligger senere.
- **PLANLAGT** — konkret backlogpunkt med kendte afhængigheder, men designet kan stadig finjusteres.
- **IDÉ** — relevant mulighed, som skal undersøges eller besluttes før den bliver bindende design.
- **RESEARCH** — historiske detaljer skal verificeres før endelige værdier/organisation låses.

## Aktiv gate — P0A v00.00.08

| ID | Status | Emne | Exit-kriterium |
| --- | --- | --- | --- |
| B-001 | AKTIV | Weapon profile + experience-modificeret reload | Samme våben + højere experience giver lavere reload; våbentypen er stadig dominerende faktor. |
| B-002 | AKTIV | Salver kan give 0 hits | 0-hit salve reducerer ikke strength, men kan stadig give shock. |
| B-003 | AKTIV | `Ramte N` combat feedback | Positive resolved hits vises kort over målet. |
| B-004 | AKTIV | P0A regressionstest | Kamera, selection/orders, formationer, range, pause/1x/2x/3x, rout, victory/defeat og restart må ikke regressere. |
| B-005 | PLANLAGT | Reproducerbar Unity repository-baseline | Efter Play-test klassificeres scene, `.meta`, `packages-lock.json` og relevante `ProjectSettings` til permanent versionsstyring. |

## Tactical combat — besluttet

| ID | Status | Emne | Beslutning / note |
| --- | --- | --- | --- |
| B-010 | BESLUTTET | Ammunition som konkret beholdning | Enheder har faktisk ammunition pr. kompatibel våbenprofil; skydning forbruger ammunition og supply kan genforsyne. |
| B-011 | BESLUTTET | Unit-status | Battle UI skal kunne vise fx `566 mænd | Tab 30 (8 dræbte / 22 sårede) | Ammunition 18.420` samt morale/cohesion/fatigue/experience. |
| B-012 | BESLUTTET | Casualty pipeline | Hits splittes senere i killed, wounded, missing/captured m.m.; `Ramte N` er ikke lig med dræbte. |
| B-013 | BESLUTTET | Fire discipline | Hold Fire, Fire at Will, volley/independent fire og ammunition conservation skal være separate ordrer/states. |
| B-014 | BESLUTTET | Directional cover | Hegn, mure, grøfter, bygninger, skovkanter, terrænfolder og fieldworks beskytter kun fra relevante retninger. |
| B-015 | BESLUTTET | Stance | Mindst standing og prone. Prone reducerer target profile, men påvirker movement/cohesion og reload afhængigt af loading method. |
| B-016 | BESLUTTET | Hasty fieldworks | Almindeligt infanteri kan etablere simple skyttehuller/jordvolde/barrikader; ingeniører gør det hurtigere og bedre. |
| B-017 | BESLUTTET | LOS og sortkrudtsrøg | Røg er simulation og skal påvirke LOS/accuracy; terræn/objekter må blokere ild. |
| B-018 | BESLUTTET | Formation-level pathfinding | Regimenter/bataljoner skal gå rundt om obstacles og bevare en brugbar formation uden individuel NavMesh-agent pr. soldat. |

## Skirmishers, marksmen og let infanteri

| ID | Status | Emne | Beslutning / note |
| --- | --- | --- | --- |
| B-020 | BESLUTTET | Deploy skirmishers | Infanteri skal kunne sende en del af styrken frem i løs skyttekæde foran eller på flanken af hovedformationen. |
| B-021 | BESLUTTET | Skirmisher-roller | Screen, reconnaissance, harassment, contest cover, beskytte advance/retreat og skabe contact før hovedformationen. |
| B-022 | BESLUTTET | Skirmisher combat model | Lavere formation density gør dem sværere at ramme; de har lavere samlet volley density, mere autonomi og højere afhængighed af cover/terrain. |
| B-023 | BESLUTTET | Reform | Skirmishers skal kunne kaldes tilbage og reformere i moderformationen med tids-/cohesion-konsekvens. |
| B-024 | RESEARCH | Skarpskytter/marksmen | Udvalgte gode skytter kan være en mindre specialty/ability eller subunit; de skal ikke modelleres som moderne sniper teams uden historisk dokumentation. |
| B-025 | RESEARCH | Dansk organisation/terminologi | Fastlæg konkrete danske 1864-termer, andele og hvilke regimenter/jægertraditioner der havde særlige skirmisher-egenskaber. |

## Artilleri

| ID | Status | Emne | Beslutning / note |
| --- | --- | --- | --- |
| B-030 | BESLUTTET | Limber/unlimber | Kanoner skifter mellem transport- og skydestilling; klargøring tager tid. |
| B-031 | BESLUTTET | Hestetrukket artilleri | Feltkanoner, limbers og caissons transporteres normalt med hestespand; antal/tilstand af heste påvirker mobility. |
| B-032 | BESLUTTET | Manhandling | Crew kan skubbe/trække et unlimbered stykke korte afstande uden heste. Det er langsomt, terrænafhængigt og giver fatigue; tunge stykker begrænses mere end lette. |
| B-033 | BESLUTTET | Artilleriets crew/heste | Crew, gunners, drivers og horses er rigtige state-værdier og kan lide tab. Manglende heste kan gøre et ellers intakt batteri taktisk immobilt. |
| B-034 | BESLUTTET | Erobring af kanoner | Guns kan være operational, abandoned, disabled/destroyed eller captured; capture kræver fysisk kontrol. |
| B-035 | BESLUTTET | Genbrug af erobret artilleri | In-battle reuse kræver egnet crew, ammunition, træning og klargøringstid; ellers bjærges stykket efter slaget. |
| B-036 | PLANLAGT | Ammunitionstyper | Round shot/shell/shrapnel/canister m.m. får forskellige effekter mod open, prone, cover og fieldworks. |

## Kavaleri og dragoner

| ID | Status | Emne | Beslutning / note |
| --- | --- | --- | --- |
| B-040 | BESLUTTET | Dragoner mounted/dismounted | Dragoner kan sidde af og føre ildkamp som fodtropper samt senere remount. |
| B-041 | BESLUTTET | Horse-holder state | Heste bliver samlet bag/ved enheden under afsiddet kamp; remount tager tid, og heste/holdere kan lide tab. |
| B-042 | BESLUTTET | Kavaleri roller | Reconnaissance, screening, courier support, flank security og pursuit er mindst lige så centrale som charge. |
| B-043 | BESLUTTET | Horse fatigue/forage/casualties | Hestebestanden påvirker både taktisk og strategisk mobilitet. |

## Supply, capture og battlefield salvage

| ID | Status | Emne | Beslutning / note |
| --- | --- | --- | --- |
| B-050 | BESLUTTET | Supply-vogne | Fysiske enheder med inventory, vogn, trækdyr og crew/driver. Kan bære ammunition, mad og andre forsyninger. |
| B-051 | BESLUTTET | Capture/destruction | Supply-vogne kan erobres, forlades eller ødelægges; crew/heste og last kan gå tabt helt eller delvist. |
| B-052 | BESLUTTET | Battlefield salvage | Våben og materiel fra overgivne, faldne, forladte guns/limbers og wagons kan indsamles efter slaget. |
| B-053 | BESLUTTET | Damage/recovery percentage | Ikke alt materiel kan reddes; en dokumenteret andel er ødelagt, mistet eller kræver repair. |
| B-054 | BESLUTTET | Compatibility | Erobrede våben/ammunition kan kun bruges, hvis ammunition, træning og condition tillader det. |

## Command, officers og organisation

| ID | Status | Emne | Beslutning / note |
| --- | --- | --- | --- |
| B-060 | BESLUTTET | Bataljoner som underformationer | Regimenter skal senere kunne bestå af operative bataljoner/underformationer. |
| B-061 | BESLUTTET | Order delay | Ordrer har transmission, delivery og acknowledgement; ingen telepatisk kontrol. |
| B-062 | BESLUTTET | Commander AI | Officer skill, personality, initiative og commander intent påvirker lokal udførelse. |
| B-063 | BESLUTTET | Persistent officers | Officerer har historik, performance, casualties, succession og reputation. |
| B-064 | BESLUTTET | Training/readiness/experience adskilt | Experience, training, morale, cohesion, fatigue og readiness må ikke være én samlet magisk stat. |

## Battle UI/UX

| ID | Status | Emne | Beslutning / note |
| --- | --- | --- | --- |
| B-070 | BESLUTTET | Kontekstuel bundmenu | Når en enhed vælges, kommer en lav command bar frem nederst i skærmen. Den skjules igen, når intet relevant er valgt. |
| B-071 | BESLUTTET | Unit-type-specific actions | Menuens knapper afhænger af enhedstype og state: infanteri, skirmishers, artilleri, dragoner, supply, ingeniører osv. |
| B-072 | BESLUTTET | Status i bundmenu | Samme panel viser nøgledata som mandskab, casualties, ammo, weapon/reload, morale/cohesion/fatigue/experience og aktiv ordre. |
| B-073 | PLANLAGT | Infanteriknapper | Formation, Hold/Move/Attack, Hold Fire/Fire at Will, Deploy/Reform Skirmishers, Standing/Prone, Use Cover, Dig In, Range overlay. |
| B-074 | PLANLAGT | Artilleriknapper | Limber/Unlimber, Move, Manhandle, ammunitionstype, target/fire control og evt. abandon/capture context. |
| B-075 | PLANLAGT | Dragon-knapper | Mounted/Dismounted, remount, screen/recon, charge/pursuit og fire control efter type. |
| B-076 | PLANLAGT | Multi-select UI | Fælles kompatible ordrer vises ved flere valgte enheder; blandede typer må ikke få ugyldige fælles commands. |
| B-077 | IDÉ | Layout/iconografi | Endelig grupperingsrækkefølge, ikoner, tooltips, hotkeys og om advanced actions ligger i submenus skal prototypetestes. |

## Strategisk/økonomisk slutvision — allerede besluttet i designmanualen

Følgende er fortsat bindende langsigtet scope og skal ikke genopfindes som isolerede backlog-idéer: real-time grand strategy, nation-/periodeafhængig OOB, economy/resources/industry/agriculture/cities/trade/rail/logistics/research, recruitment/reserves/replacements, casualty/medical/POW chain, regimental lineage/battle honours/medals, earned traits/specialisation, fog of war, persistent campaign aftermath, data-driven/modding og performance via formation-level simulation.

## Åbne idéer / skal afklares senere

| ID | Status | Emne | Spørgsmål |
| --- | --- | --- | --- |
| I-001 | IDÉ | Marksmen target priority | Skal udvalgte skytter kunne prioritere officerer/gunners, og hvordan begrænses dette af observation/FoW og historisk praksis? |
| I-002 | IDÉ | Skirmisher detached strength | Fast procent, company-based detachments eller doctrine/OOB-data pr. nation/periode? |
| I-003 | IDÉ | In-battle salvage | Skal små mængder ammunition/våben kunne samles under selve slaget, eller primært i aftermath? |
| I-004 | IDÉ | Captured gun immediate use | Hvor ofte bør et erobret stykke realistisk kunne vendes mod fjenden under samme slag? |
| I-005 | IDÉ | Command bar density | Hvor mange primære actions kan vises uden at gøre bundmenuen til et cockpit? Testes med prototype-UI. |

## Arbejdsregel

Når en ny idé opstår:

1. Hvis den er besluttet, opdateres relevant designmanual/feature-arkitektur og registreres her som **BESLUTTET** eller **PLANLAGT**.
2. Hvis den endnu ikke er afgjort, registreres den som **IDÉ** eller **RESEARCH**.
3. Når implementeringen starter, skifter den til **AKTIV** med konkrete exit-/testkriterier.
4. Når funktionen er implementeret og QA-valideret, flyttes den til versionshistorik/implementeringsstatus og backlogpunktet markeres afsluttet i en senere backlog-revision.
