# B-490–B-499 — Campaign v00.00.14 Military Doctrine & General Staff AI

## Status

**Target version:** `v00.00.14`  
**State:** PLANLAGT  
**Scope:** doctrine, staff planning and military decision preferences.

## B-490 — Doctrine data model
Create strategic, operational and tactical doctrine layers with versioned doctrine entries, prerequisites and national defaults.

Acceptance: doctrine is data-driven; doctrine changes decision preferences/capabilities, not hidden information or arbitrary combat cheats.

## B-491 — Strategic doctrine profiles
Examples: Defensive War, Offensive Concentration, Fortress Defence, Railway Concentration, Attritional Strategy, Manoeuvre Strategy and Force Preservation.

Acceptance: each doctrine exposes explicit effects on strategic AI priorities, reserve policy, deployment and acceptable risk.

## B-492 — Operational doctrine profiles
Cover corps/division concentration, march organization, screening, reserve use, supply-base placement, river crossing, pursuit and withdrawal planning.

Acceptance: operational doctrine influences plan generation and order preferences through explainable weights.

## B-493 — Tactical doctrine library
Define tactical preferences such as skirmisher-heavy advance, artillery preparation, column approach/line deployment, flank attack, reverse-slope defence, counterattack reserve and cavalry screening/pursuit.

Acceptance: tactical doctrine is handed off as intent/preferences only; tactical battlefield AI keeps its own legal perception/navigation rules.

## B-494 — General Staff planning cycle
Staff creates plans through `ASSESS -> OBJECTIVES -> COURSES OF ACTION -> LOGISTICS CHECK -> RISK -> APPROVAL -> ORDERS -> REVIEW`.

Acceptance: planning has campaign-time/staff-delay hooks; plans can be superseded; staff cannot instantly know enemy state beyond intelligence inputs.

## B-495 — Mobilization and deployment plans
Pre-war/war plans can define assembly areas, rail priorities, march routes, depots, reserve locations and theatre assignments.

Acceptance: execution consumes real transport capacity and real movement time; unavailable rolling stock/bridges/roads cause delay or replanning.

## B-496 — Reconnaissance assumptions and intelligence confidence
Plans use knowledge-state estimates rather than true enemy state.

Acceptance: plan records the intelligence snapshot/confidence used; stale/wrong reports can create realistic planning errors without random cheating.

## B-497 — Staff exercises, war games and lessons learned
Create peacetime/field exercise and post-battle learning hooks that may improve staff procedures/doctrine familiarity over time.

Acceptance: benefits are bounded and require time/institutions/experience; no instant universal buff.

## B-498 — Doctrine recommendations and player approval
Chief of General Staff can propose doctrine changes or plan-method changes based on war situation, observed problems and research.

Acceptance: recommendation shows evidence/reasons, transition cost/time and affected systems; MANUAL/ADVISORY modes execute nothing automatically.

## B-499 — Doctrine/General Staff UI and QA
Provide doctrine browser, current doctrine summary, active plans, assumptions, reserve policy and staff decision audit.

Acceptance: deterministic QA covers doctrine switch, plan creation, bad intelligence, rail shortage, plan cancellation, save/load and no tactical-AI rule violation.