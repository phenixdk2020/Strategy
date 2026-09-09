# PROJECT 1864 — parallel development tracks

## Purpose

PROJECT 1864 udvikles nu i to uafhængige spor, så tactical stabilization ikke blokerer campaign/strategic development.

## Track A — Tactical

- Branch: `test` / aktiv leveringskanal `channel-test`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Test`
- Aktuel revisionsserie: `v00.00.09l`.
- Fokus: battlefield controls, formations, Officer AI, combat, melee, navigation, obstacles, river/bridge, UI, tactical visuals, full-scale OOB, brigade command og tactical QA.
- `v00.00.09f` beholder **Navigation V3** som autoritativ movement/steering-owner. V4 og 09A er runtime-deaktiverede.
- `09B/09h4` fungerer som compatibility/soft-terrain policy: decorative trees og individuelle fence posts er pass-through; Farmhouse/Barn er hard obstacles; river/water er bridge-only.
- `v00.00.09g` leverer scenic battlefield og extended camera zoom.
- `v00.00.09h` leverer Soldier Visual Pass 1, Uniform Designer, RTS box selection, compact combat feedback og soft black-powder smoke.
- `v00.00.09h2` stabiliserer AI attack frontage; `09h3` march/deployment policy; `09h4` manual AI-OFF route recovery.
- `v00.00.09i`–`09i3` leverer Big Graphics Buff, procedural standards, medium-zoom readability, Farmhouse detour continuation og Unity 6.6 compatibility cleanup.
- `v00.00.09j` leverer fire/reload visual state baseret på faktisk udgående volley og weapon-specific reload cadence.
- `v00.00.09k` etablerer **Full-Scale OOB + 1:1 Render + Mounted Regimental HQ + Special Arms Pilot**.
- `v00.00.09l` er **Brigade HQ + Historical Danish OOB + Reserve AI + Dual Standards**.
- 09l bruger den dokumenterede danske **7. Brigade: 1. + 11. Infanteri-Regiment**, med ca. 1.540 og 1.584 mand i den aktuelle historiske arbejdsreference. Den preussiske 8th+18th formation forbliver tydeligt mærket QA indtil en tilsvarende date-specific brigade-OOB er kildevalideret.
- 09l bevarer den stabile 09k-renderer key for den anden danske slot internt, men OOB/display-identitet er 11. Infanteri-Regiment. 09m erstatter denne midlertidige bridge med stabile Unit IDs uafhængigt af displaynavn.
- Hvert regiment beholder 09k regimental HQ på 3 ryttere. Hver brigade får separat **Brigade HQ på 6 ryttere**: commander, adjutant, 2 staff officers og 2 couriers.
- Brigade missions: Hold / Defend Area / Attack-Capture Area. Regiment roles: Reserve / Support / Engaged / Manoeuvre / Withdraw.
- Dansk Brigade AI starter OFF og kan toggles med F11. Preussisk QA Brigade AI starter ON.
- Brigade AI committer ét regiment først og holder det andet i reserve. Reserve kan committes ved rout, væsentligt strength/morale/cohesion-pres eller manglende angrebsfremgang. Brigade-laget udsteder missioner via OfficerAIController og skriver ikke direkte til Regiment.destination.
- 09l tilføjer **dual standards** pr. regiment: national/hærfane + regimentsfane. Dansk regimentsfane følger Dannebrog/romertal-retningen, mens tradition er separat metadata/label. Fx understøtter datamodellen `5. Infanteri-Regiment — tradition: Sjællandske Livregiment` uden at gøre et senere traditionsnavn til det officielle 1864-navn.
- Cavalry manpower og serviceable horses er separate i 09l. Dansk cavalry pilot: ca. 135 personnel, 120 serviceable horses, 120 mounted effective. Prussian dragoon squadron remains QA.
- Dansk field battery er korrigeret til **8 guns / ca. 190 personnel** efter den aktuelle historiske arbejdsreference. Prussian battery forbliver QA 6 guns / 120 personnel indtil kildevalidering.
- Performance overlay i 09l viser ca. 8.024 infantry + 295 cavalry personnel + 310 artillery personnel + 14 guns.
- `docs/parts/part-09l-brigade-hq-historical-oob.md` er den autoritative 09l-supplementsspecifikation.
- `docs/parts/part-09m-oob-designer-campaign.md` definerer næste OOB Designer-gate: organisatorisk drag/drop i træet og movement orders ved drag/drop på campaign-kortet.
- Unity Input Manager deprecation er non-blocking. Migration til det nye Input System sker som separat control-platform revision, fordi LMB/RMB, box-select, F10/F11, keyboard hotkeys og UI pointer guards skal migreres samlet.
- Primær 09l QA: compile uden røde errors; OOB-09L korrekte DK-identiteter/styrker; 2 Brigade HQ synlige; dual flags pr. regiment; DK 8-gun battery; horse/manpower telemetry; F11 Brigade AI toggle; reserve commit telemetry; eksisterende infantry 1:1-render/movement/combat/box select må ikke regressere.

## Track B — Campaign

- Branch: `campaign`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy-Campaign`
- Aktuel revisionsserie starter ved `v00.00.10a`.
- Fokus: campaign scene/map, strategic formations, movement/time, battle trigger, tactical handoff og state return.
- OOB Designer-arbejdet i 09m skal dele stable Unit IDs og hierarchy-data med campaign-sporet, så tactical og campaign ikke bygger parallelle OOB-modeller.
- Campaign-kode/scener skal så vidt muligt ligge separat fra tactical navigation/combat-filer for at minimere merge-konflikter.

## Stable

- Branch: `main`
- Lokal standardmappe: `%USERPROFILE%\OneDrive\Strategy`
- Kun accepterede/promoverede gates.

## Integration rule

Campaign må ikke vente på mindre tactical polish. Kun tactical fixes der er nødvendige for campaign-state continuity eller som er accepteret gennem QA, integreres senere i campaign-sporet. Ingen destructive Git-operationer, force-push, reset --hard eller clean anvendes.
