# part-09m1-company-march-hud

Company march policy and command readout for Strategy-Kamp.

## March

- Remaining path >= 18 m -> Column.
- Remaining path <= 14 m -> Line.
- Enemy company inside EffectiveRange + 8 m, or parent HasHitFeedback -> Line.
- Arrival / Hold -> Line only if the policy itself put the company in Column.
- F / C lock the formation for 14 seconds.

## HUD

- World chip on selected companies: short name, men, LIN/KOL, status.
- Command card when companies are selected: strength, formation, parent morale/cohesion, weapon.
- Hint strip: RMB / F / C / H / auto-column.

## Console

- `KAMP-MARCH-09M1|Installed=True|Policy=LongMarchColumn|...`
- `KAMP-HUD-09M1|Installed=True|SelectedCard=True|WorldChips=Selected|Formation=LIN/KOL`
