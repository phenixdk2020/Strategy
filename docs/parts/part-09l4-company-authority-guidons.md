# v00.00.09l4 — Company selection, combat authority and guidons

## Purpose

09l4 stabilises the company-level tactical model introduced in 09l2/09l3 without changing Navigation V3 or the 1:1 instanced infantry architecture.

## Selection authority

Ordinary soldiers are GPU-instanced and therefore must not require individual colliders for interaction. A company is selected from its projected tactical footprint in screen space. The screen-space footprint is derived from the company tactical centre plus the current Line/Column footprint dimensions.

Short LMB click may therefore select any visible part of a Danish company formation. Shift adds and Ctrl toggles. LMB drag remains the 09l2 marquee path.

Design rule: interaction belongs to the tactical formation entity, not to thousands of individual render instances.

## Combat authority during transition

The mounted Regimental HQ is a command entity and must never visually act as the infantry firing origin simply because the legacy `Regiment` object still owns the old combat resolver.

Until company-level fire resolution is implemented, parent Regiment fire is suppressed while company tactical control is active. Parent range cones are hidden in this state. This avoids presenting a false simulation where an officer/HQ appears to fire a full regiment volley.

Future company combat authority must move fire eligibility, LOS, ammunition, firing state and casualty targeting to company-level state while keeping higher-echelon morale/command effects separate.

## Company identity markers

Full national/regimental colours remain at the Regimental HQ as the historically significant standards.

Every independently controllable company receives smaller tactical guidons for readability:

- national identity marker;
- regiment/company identity marker.

These company guidons are a gameplay readability device and are not presented as historical full regimental colours. They move and rotate with the company tactical centre and provide a visible command anchor for selection and movement.

For Denmark the national marker uses Dannebrog colours. The company marker carries regiment Roman identity plus company number. Equivalent QA markers are used for the current Prussian test regiment.

## QA acceptance

- company short-click works across the visible formation footprint;
- ordinary soldier colliders are not required;
- box selection still works;
- every active company has visible tactical markers;
- large historical standards remain at Regiment HQ;
- parent Regiment cannot fire and shows no range cone while company combat authority is deferred;
- company movement/formation controls and 1:1 rendering remain unchanged.
