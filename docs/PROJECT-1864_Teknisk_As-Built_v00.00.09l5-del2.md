# PROJECT 1864 — As-Built del 2 (kap. 8–13)

Kilde-build: v00.00.09l5 TEST. Kanal: strategy-kamp.

Se [oversigt](./PROJECT-1864_Teknisk_As-Built_v00.00.09l5.md) og [del 1](./PROJECT-1864_Teknisk_As-Built_v00.00.09l5-del1.md).

## 8. Træfberegning, morale, rout, melee og casualties

Tuning defaults: base hit 1,40 %, firing fraction 58 %, close 1,75, medium 1,00, long 0,30, ammo 60, casualties/body 8, morale floor 0,72, cohesion floor 0,68.

FiringMen = round(CurrentStrength × 0,58).
QualityX = MoraleX × CohesionX.
ExpectedHits = FiringMen × BaseHitChance × WeaponX × RangeX × QualityX.
Dreyse WeaponX ≈ 0,929.

Legacy kernel clamp hits 0..16 — kunstigt loft ved 1500–2500 mand. Skal væk i Company combat.

Hit: strength -= hits. Morale -= hits×0,32 + shock. Cohesion -= hits×0,25 + shock×0,70. underFireTimer = 7 s.
Rout hvis strength ≤ 0 ELLER morale ≤ 17 ELLER strength ≤ 24 % af initial.

Melee: pulse 1,25 s, contact 7 m, loss clamp 1..10. Stadig Regiment-baseret.

Company combat er ikke færdig. 09l4 sætter parent FirePolicy = HOLD.

## 9. Officer AI, difficulty og Brigade AI

Stats: Leadership, Inspiration, Tactical Skill, Initiative, Staff/Command, Discipline, Aggressiveness, Composure, Experience (QA-værdier).

Missions: DefendArea, Hold, MoveToPoint, AttackTarget, AttackNearest.
Doctrine: Defensive −12 / Balanced 0 / Offensive +12.

Easy reaction 1,28 noise 0,18. Normal 1,00 / 0,10. Hard 0,84 / 0,055. Ingen hidden combat bonus.

Brigade AI disabled i reduced 09l5 (kun 2 regimenter). F11-UI er stale.

## 10. Grafik, faner og uniformer

1:1 instancing superseded 09i representative rig for ordinary infantry.
Range ghosts: close/medium/long. Parent cones skjult i Company mode.
Dual regiment standards + company guidons (QA-markers).
Uniform Designer (F10) er Regiment-selection, ikke Company.

## 11. Kavaleri, artilleri og HQ

Cavalry QA: DK husarer 135/120 heste; PR dragoner 160/155. MountedEffective = min(personnel, horses). Ingen charge/dismount/combat endnu.
Artillery QA: DK 8 guns / 190; PR 6 / 120. Visual/data only.
Brigade HQ: 6 mounted staff. Regiment HQ: 25 staff, 3 mounted.

## 12. Aktivt i v00.00.09l5

AKTIV: 1:1 company rendering, 20 companies, click/box select, detached transforms, direct company movement, line/column, relative multi-move (via override), flags/guidons, special-arms visuals.

IKKE IMPLEMENTERET: company pathfinding, company fire, cavalry/artillery combat+movement, new Input System.

DEAKTIVERET/UNDERTRYKT: parent regiment fire, parent officer AI, brigade AI i QA.

FINDES men Regiment-ejet: V3 nav, melee, casualties, Uniform Designer.

## 13. Hvorfor 09l5 blev frosset, og hvad vi bevarer

Konkurrerende owners: 09l2 group line vs 09l5 relative offsets. Regiment vs Company movement/combat. Flere suppression-lag.

Bevares: 1:1 instancing, OOB, HQ-entities, officer/brigade formler, våbenprofiler, fire policy, ammo, experience-reload, morale/cohesion/rout concepts, V3-læring (én steering owner, persistent detours, river/bridge), selection/footprint, relative-offset semantics, uniforms/flags, separate horse state, performance-strategi.

Næste rebuild: Company = first-class tactical entity. Én movement owner. Én combat owner. AI/player giver intent, ikke parallel steering.

Gates: 1) ren company core 2) selection+group orders i samme owner 3) company nav 4) fire/ammo/morale/casualties på company 5) regiment aggregerer 6) officer AI på company goals 7) brigade AI når ≥2 aktive regimenter 8) cavalry/artillery som own types 9) full-scale collider/nav.
