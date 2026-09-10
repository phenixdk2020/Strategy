# v00.01.00c3 implementation notes

This revision is intentionally limited to visual polish and drill-state tests. It does not introduce tactical translation, route ownership, navigation, combat or AI.

The Company entity remains first-class. Formation changes are written only to the selected Company entities. Rotation changes facing in place and preserves world position. The c3 renderer reads Company pose/formation and renders the 1:1 soldier presentation. The hover panel remains read-only UI.

No standards/flags are present in c3. This follows the current rebuild QA preference to keep the visual test focused.
