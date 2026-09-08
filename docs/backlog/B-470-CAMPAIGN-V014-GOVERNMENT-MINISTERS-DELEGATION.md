# B-470–B-479 — Campaign v00.00.14 Government, Ministers & Delegation

## Status

**Target version:** `v00.00.14`  
**State:** PLANLAGT  
**Scope:** government, portfolios, delegation and policy guardrails  
**Tactical AI:** no direct tactical navigation/combat rewrites.

## B-470 — Government/cabinet state
Create persistent national government state with cabinet, offices, office holders, appointment dates and current portfolio assignments.

Acceptance: save/load safe; semantic office IDs separate from localized/historical titles; vacancies allowed; government state does not live only in UI objects.

## B-471 — Common delegation modes
Implement `MANUAL / ADVISORY / ASSISTED / AUTO` per portfolio.

Acceptance: MANUAL performs no autonomous writes; ADVISORY only proposes; ASSISTED obeys explicit routine-action limits; AUTO still obeys player guardrails and normal game rules.

## B-472 — Minister portfolio framework
Initial semantic portfolios: War, General Staff, Quartermaster, Finance, Public Works, Rail/Transport, Industry/Trade, Agriculture, Interior, Foreign Affairs and Intelligence.

Acceptance: scenario can rename offices historically without code changes; one office may own multiple sub-portfolios; authorities are explicit.

## B-473 — National strategy and policy guardrails
Player defines national posture, spending weights, treasury floor, debt tolerance, recruitment severity, infrastructure priorities, reserve floors, forbidden projects/regions and risk tolerance.

Acceptance: AI decisions outside limits escalate as recommendation instead of silently violating policy.

## B-474 — Ministry budgets and resource envelopes
Allocate money/resources/capacity to portfolios while keeping a national treasury/resource source of truth.

Acceptance: no double-spending; commitments/reservations are visible; Finance can reject or defer requests; budget changes have timestamp/reason.

## B-475 — Minister competence and administrative capacity
Office holders receive bounded skills relevant to administration, domain knowledge, initiative, caution and political influence.

Acceptance: competence changes decision quality/speed/friction rather than granting free resources or hidden information.

## B-476 — Minister personality and policy preference
Data-driven preferences such as cautious/aggressive, fiscal conservative/spender, infrastructure-focused, manpower-preserving and centralizing/delegating.

Acceptance: traits bias scoring only within legal/player constraints; UI can explain the bias.

## B-477 — Cabinet conflict and arbitration
Ministries can make competing requests for money, manpower, rail capacity, iron, horses or construction slots.

Acceptance: deterministic arbitration path; Head of Government/player can override; conflict never duplicates resources; losing request remains traceable.

## B-478 — Approval, lock and override controls
Player can approve once, auto-approve class, lock project/resource, cancel/defer proposal or reserve capacity.

Acceptance: locked player decisions cannot be overwritten by AUTO without explicit rule; cancellation has safe state rollback where applicable.

## B-479 — Government dashboard and delegation QA
Unified government UI showing ministers, modes, budgets, active decisions, pending recommendations, conflicts and policy violations.

Acceptance: deterministic QA preset covers all four delegation modes, budget rejection, override, vacancy and competing-ministry requests.