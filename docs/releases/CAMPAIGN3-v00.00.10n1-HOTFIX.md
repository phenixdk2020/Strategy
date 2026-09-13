# Campaign3 v00.00.10n1 — version badge hotfix

## Formål

Denne hotfix retter den synlige Campaign3-buildidentitet. v10n var implementeret, men det øverste Game View-badge i `CampaignWorldImageryOverlayV010I` var stadig hardcodet til `v00.00.10k1`.

## Rettelser

- `CampaignBuildInfo.CurrentVersion` er nu den centrale buildversion for Campaign3 UI.
- Top-left badge viser `PROJECT 1864 | v00.00.10n1`.
- `CampaignMapStyleSwitcher` bruger ikke længere v10k som overordnet buildidentitet i root/logs.
- v10k/v10k1-navne på terrain-subsystemets klasse/filer bevares som subsystem-lineage og er ikke længere den viste samlede buildversion.
- Ingen gameplaydata eller simulation er ændret.

## QA

1. Unity 6.6 skal compile uden errors.
2. Top-left Game View badge skal vise `v00.00.10n1`.
3. Zone overlay skal stadig kunne toggles med `Z`.
4. 20 zoner / 68 byer / population 290565 skal fortsat bestå startup-valideringen.
5. World Imagery og 3D terrain skal fungere uændret.
