# PROJECT 1864

*Teknisk As-Built Reference*

**Unity tactical prototype — strategy-kamp**

**Kilde-build: v00.00.09l5 TEST**

Dokumentversion 1.0  |  10. september 2026

> Samme tekniske as-built som Word-dokumentet `PROJECT-1864_Teknisk_As-Built_v00.00.09l5.docx`, lagt på Strategy-Kamp (`strategy-kamp`) som reference for det rene Company-rebuild. 09l5 er frosset. Byg ikke flere authority-/compatibility-lag ovenpå.

**Formål.** Dokumentet beskriver, hvad der faktisk er kodet frem til v00.00.09l5: input, musestyring, OOB, formationer, movement, navigation, combat-formler, morale, AI, grafik, faner, special arms og arkitekturbegrænsninger.

**Arkitekturstatus.** v00.00.09l5 er en fejlet QA-retning for Company-arkitekturen. Systemer og formler genbruges som læring. Næste Company-implementation skal have Company som first-class tactical entity.

Fuld tekst ligger i to dele på samme branch:

- [Del 1 — kap. 1–7](./PROJECT-1864_Teknisk_As-Built_v00.00.09l5-del1.md)
- [Del 2 — kap. 8–13 + appendiks](./PROJECT-1864_Teknisk_As-Built_v00.00.09l5-del2.md)

## Indhold

1. Systemets udvikling og nuværende arkitektur
2. Input, mus og tastatur
3. Kamera, tid og slagmark
4. OOB, manpower og Company-struktur
5. Company-rendering, selection og movement
6. Regiment movement, navigation og recovery
7. Våben, ilddisciplin, reload og ammunition
8. Træfberegning, morale, rout, melee og casualties
9. Officer AI, difficulty og Brigade AI
10. Grafik, range overlays, faner og uniformer
11. Kavaleri, artilleri og HQ-struktur
12. Hvad er aktivt i v00.00.09l5?
13. Hvorfor 09l5 blev frosset, og hvad vi bevarer

## Kamp-kanalens næste rebuild-princip

Company owns: position/facing, route/path, formation, movement, local combat, casualties, selection.

Battalion/Regiment/Brigade er OOB-parent og command/aggregation — ikke Transform-parent og ikke parallel steering owner.

## Dokumentstatus

| Felt | Værdi |
| --- | --- |
| Projekt | PROJECT 1864 |
| Repository | phenixdk2020/Strategy |
| Branch | strategy-kamp |
| Kilde-build | channel-test / v00.00.09l5 TEST |
| Dokumenttype | As-built technical reference |
| Dokumentversion | 1.0 |
| Dato | 10. september 2026 |
| Status | 09l5 frozen; clean Company rebuild required |
