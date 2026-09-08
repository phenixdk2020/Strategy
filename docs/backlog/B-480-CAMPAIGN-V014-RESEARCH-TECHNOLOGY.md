# B-480–B-489 — Campaign v00.00.14 Research, Technology & Historical Plausibility

## Status

**Target version:** `v00.00.14`  
**State:** PLANLAGT  
**Scope:** research, adoption and historical plausibility.

## B-480 — Research institution model
Research capacity comes from institutions such as arsenals, staff colleges, universities, engineering offices, railway administrations and industrial works.

Acceptance: research capacity is persistent, location/nation aware and cannot exceed available institutions/staff without explicit modifiers.

## B-481 — Research field taxonomy
Create data-driven fields for infantry weapons/drill, artillery, cavalry, staff/command, reconnaissance, logistics, rail, telegraph, fortification/engineering, medicine, industry/metallurgy and agriculture/horse breeding.

Acceptance: new fields/projects can be added as data without changing core research code.

## B-482 — Research project lifecycle
Lifecycle: `AVAILABLE -> PROPOSED -> FUNDED -> RESEARCHING -> PROTOTYPE/VALIDATION -> COMPLETED -> ADOPTION`.

Acceptance: research completion does not automatically place equipment in units; adoption/procurement/production are separate states.

## B-483 — Historical availability and plausibility envelope
Projects get earliest plausible date, prerequisites, national/institution requirements and optional scenario restrictions.

Acceptance: default 1864 scenario cannot jump to implausible future technologies; alternate development is possible inside documented plausibility bounds.

## B-484 — Equipment prototype and adoption
Weapons/equipment research may create prototypes before general service adoption.

Acceptance: prototype availability, trials, procurement approval and mass production are separate; experimental equipment cannot silently replace existing inventory.

## B-485 — Doctrine and procedure research
Research may unlock staff procedures, mobilization systems, railway concentration methods, artillery procedures, skirmisher doctrine, medical organization and logistics practices.

Acceptance: results primarily unlock capabilities/behaviours/procedures rather than arbitrary damage buffs.

## B-486 — Civil/industrial research and infrastructure technology
Research covers machining, metallurgy, rail engineering, telegraph, bridge engineering, agriculture, forage and industrial organization.

Acceptance: civil research can influence production/construction capacity through explicit systems and never by hidden global multipliers only.

## B-487 — Knowledge diffusion, foreign purchase and licensed adoption hooks
Allow ideas/equipment to spread through trade, observation, captured examples, foreign purchase or license where historically/scenario appropriate.

Acceptance: diffusion is traceable and bounded; owning an imported weapon does not automatically grant industrial production knowledge.

## B-488 — Research AI and portfolio priorities
Delegated AI can recommend/manage research within budget and national priorities.

Acceptance: AI explains project score, prerequisites, cost, expected strategic value and opportunity cost; AUTO cannot bypass historical gates or resources.

## B-489 — Research UI, historical-data validation and QA
Research screen shows institutions, available projects, prerequisites, progress, adoption state and source notes/data provenance hooks.

Acceptance: deterministic QA covers prerequisites, date gate, cancellation, parallel research capacity, prototype/adoption separation and save/load.