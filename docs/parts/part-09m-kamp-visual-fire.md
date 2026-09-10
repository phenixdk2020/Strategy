# part-09m-kamp-visual-fire

Strategy-Kamp TEST gate for readable 1864 infantry and playable company fire.

## Visual

- Ordinary soldiers stay GPU-instanced at company tactical centres.
- Figure parts: coat, skirt, belt, trousers, boots, arms, hands, neck, head, faction headgear, pack, cross-straps, cartridge box, wooden stock, iron barrel, bayonet.
- Denmark = kepi + visor + trim band. Prussia = pickelhaube dome + spike + front plate.
- 09l2 capsule renderer and 09i representative Soldier_* rigs are suppressed while 09m is installed.

## Fire

- Parent regiment HQ remains HoldFire.
- Each living company searches enemy companies inside MaximumRange and FireArcHalfAngle.
- Hits use the 09l5 expected-hit formula and call Regiment.ReceiveVolley.
- Company CurrentStrength is reconciled by the existing 09l2 strength sync.
- Standing companies turn toward the target. Muzzle smoke emits from the company frontage.

## Console

- `KAMP-VISUAL-09M|Installed=True|Figure=1864Infantry|...`
- `KAMP-FIRE-09M|Installed=True|Owner=CompanyCentre|ParentHQ=HoldFire|Formula=09l5Volley|Smoke=CompanyMuzzle`
