# PROJECT 1864 — Designmanual

**Aktuel designbaseline: v00.02.09**  
**Aktuel campaign-workbranch: Campaign3 v00.00.10l CLEAN UI + 3D CITIES**

Grand Strategy i realtid + taktiske 3D-slag. Denne GitHub-udgave er opdelt i dele for overskuelig versionsstyring. Den layoutede Word-master opdateres parallelt som projektartefakt, mens GitHub-Markdown er den løbende designmæssige source of truth.

## Indhold
- [Del 3B: 20 — Befolkning, økonomi og byudvikling](parts/part-03b-20-20.md)
- [Del 3C: 20.16 — Strategisk landudvikling](parts/part-03c-20-16-strategic-development.md)
- [Del 3E: 20.17 — CITY-REG-01: Kongeriget Danmarks 68 købstæder i 1850, population og udviklingsklasser](parts/part-03e-20-17-city-register-1851.md)
- [Projekt-backlog](PROJECT-BACKLOG.md)

## City Register 1851 — kanonisk campaign-baseline
Designbaseline v00.02.09 fastlægger en kanonisk city-node baseline for Kongeriget Danmark ved campaign-start 1. januar 1851. Population bruger folketællingen 1. februar 1850. Alle 68 købstæder skal findes på strategikortet; de tidligere få QA-byer er ikke længere en kanonisk byliste.

Byer opdeles i **A — Development City**, **B — Regional Town** og **C — Minor Town**. Mindre byer er synlige, klikbare og økonomisk/logistisk relevante, men B/C-byer må ikke automatisk udbygges med kaserner, arsenaler, større militære depoter, våbenfabrikker eller permanente fæstninger. Historisk dokumenterede anlæg kan eksistere som fixed/special buildings uanset klasse.

Det fulde register, folketal og regler ligger i [CITY-REG-01](parts/part-03e-20-17-city-register-1851.md). Slesvig, Holsten, Lauenborg og strategiske ikke-købstads-settlements får særskilte registre. Esbjerg er ikke en 1851-startby.

## Versionshistorik
- **v00.02.09 / CITY-REG-01** — Kongeriget Danmarks 68 købstæder ved 1851-starten fastlagt med folketal fra 1. februar 1850. A/B/C-klasser indført. Mindre byer findes på kortet, men B/C har ikke fri tung militær udbygning. Esbjerg fjernes fra 1851-startstate.
- **v00.02.08** — Forrige designbaseline.

## Projektregel
Designmanualen skal opdateres i GitHub ved designbeslutninger. Git-historikken bevarer tidligere udgaver.
