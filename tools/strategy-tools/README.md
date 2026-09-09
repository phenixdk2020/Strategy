# Strategy Tools — PROJECT 1864

Denne mappe samler de dokumenter, der skal bruges som praktisk arbejdsgrundlag for PROJECT 1864 på tværs af tactical-, campaign- og OOB-arbejdet.

## Dokumenter

- [`PROJECT-1864-CURRENT-DESIGN-MANUAL.md`](PROJECT-1864-CURRENT-DESIGN-MANUAL.md) — samlet aktuel designmanual og arkitekturstatus, inklusive tactical recovery-plan efter 09l5.
- [`ARMY-1864-OOB-REFERENCE.md`](ARMY-1864-OOB-REFERENCE.md) — arbejdsreference for Danmark og Preussen i 1864: OOB-hierarki, typiske styrker, bataljoner/kompagnier, brigadeeksempler, kavaleri, artilleri og faner.

## Source-of-truth-regel

Den modulære designmanual under `docs/` er fortsat projektets detaljerede historiske dokumentationsstruktur. Filerne i denne mappe er den konsoliderede arbejdsreference, der skal gøre det muligt at starte en ny udviklingschat eller en ny implementation uden at skulle rekonstruere beslutninger fra mange tidligere TEST-revisioner.

Ved uoverensstemmelse gælder følgende prioritet:

1. Seneste eksplicit godkendte designbeslutning.
2. `PROJECT-1864-CURRENT-DESIGN-MANUAL.md`.
3. Seneste relevante supplement under `docs/parts/`.
4. Ældre release notes og tidligere prototypebeslutninger.

Historiske tal i `ARMY-1864-OOB-REFERENCE.md` er en arbejdsreference baseret på de indsamlede oplysninger til projektet. Tal eller stamlinjer, der ikke er kilde-låst, skal mærkes som working/QA og ikke behandles som endelig historisk canon.

## Tactical freeze efter 09l5

`v00.00.09l5` dokumenteres som en fejlet QA-retning for company-arkitekturen. Der skal ikke lægges flere authority-/compatibility-lag oven på 09l5. Næste company-implementation skal bygges rent fra en kendt stabil tactical baseline med Company som first-class tactical entity fra begyndelsen.
