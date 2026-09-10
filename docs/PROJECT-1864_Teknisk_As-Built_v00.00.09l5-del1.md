# PROJECT 1864 — As-Built del 1 (kap. 1–7)

Kilde-build: v00.00.09l5 TEST. Kanal: strategy-kamp.

Se [oversigt](./PROJECT-1864_Teknisk_As-Built_v00.00.09l5.md) og [del 2](./PROJECT-1864_Teknisk_As-Built_v00.00.09l5-del2.md).

## 1. Systemets udvikling og nuværende arkitektur

### 1.1 Fra Regiment-prototype til Company-prototype

Projektet begyndte som en tactical prototype med fire Regiment-objekter. Regimentet ejede movement, combat, morale, selection, range og soldier-visuals. Senere kom OOB, Officer AI, Brigade AI, 1:1 GPU-rendering, Company-centre, Regiment HQ, Brigade HQ, faner, kavaleri og artilleri.

Ønsket struktur: hierarkisk i OOB-data, men taktiske units må ikke være tvunget til at være Transform-children. Et Company skal kunne bevæge sig selvstændigt og stadig høre til bataljon/regiment.

### 1.2 Arkitekturproblemet i 09l5

Company-systemet blev lagt oven på Regiment-arkitekturen. Derfor findes både gammel Regiment-owner og nyere Company-lag på samme state.

09l5 dokumenteres som lærings- og referencebuild. Én tactical entity skal kun have én movement-owner og én combat-owner.

Kilde: `Regiment.cs`, `PrototypeCompanyTacticalControl09L2.cs`, `PrototypeIndependentCompanyMovement09L5.cs`.

## 2. Input, mus og tastatur

| Input | Funktion |
| --- | --- |
| LMB kort klik på Company | Vælger Company |
| Shift + LMB | Føjer Company til selection |
| Ctrl + LMB | Toggle Company |
| LMB + træk | Box-select af danske Companies |
| Dobbeltklik på Company | Regiment-selection → alle Companies |
| RMB | Flytter valgte Companies |
| RMB + træk | Flyt + slut-facing hvis drag ≥ 4 m |
| Alt + RMB | Tilføj waypoint |
| H / F / C | Hold / Line / Column |
| Z / X | Facing −15° / +15° |
| Space | Pause |
| 0/1/2/3/4 | x0,5 / x1 / x2 / x5 / x20 |
| R | Genstart |
| F9 / F10 | Combat Tuning / Uniform Designer |
| F11 | Brigade AI (deaktiveret i reduced 09l5) |
| I | Officer AI (parent undertrykt i Company QA) |
| F6/F7/F8 | Easy / Normal / Hard |
| T | Regiment range-display (parent cones undertrykt) |

Click-drag threshold = 9 px. Screen-space padding = 7 px. Kun danske Companies kan vælges. 09l2 raycast + 09l4 screen-space footprint.

## 3. Kamera, tid og slagmark

RTS-kamera via gammel Input Manager og `unscaledDeltaTime`.

Base moveSpeed 72, RotateSpeed 82, ZoomStep 4,8, min clearance 3,8 m, max height 300 m, clamp X ±350 m, Z ±230 m. Shift pan x2,25. Ctrl pan x0,40.

Starttid: 1 Feb 1864 10:20. GameMinutesPerSimulationSecond = 2,2.

Tactical ground 360×240 m (144×96). Scenic pass 720×480 m uden collider.

## 4. OOB, manpower og Company-struktur

Full-scale: DK 1. IR 1540, DK 11. IR 1584 (teknisk nøgle '5. Regiment'), PR 8th 2460, PR 18th 2440. Total 8024.

Reduced 09l5 QA: DK 1. IR 1540 + PR 8th 2460 = 4000. Companies: 8 DK + 12 PR = 20.

Staff 25. DK 2×4 companies. PR 3×4 (3. = Füsilier-Bataillon).

1. IR: 1515/8 → 190,190,190,189,189,189,189,189.
8th: 2435/12 → elleve à 203 + én 202.

## 5. Company-rendering, selection og movement

Company-centre spacing 38 m. 09l5 detacher til world-space root.

Line: 3 ranks, file 0,52 m, rank 0,76 m, depth 2,7 m. ~190 mand → ~34 m frontage.
Column: 8 files, file 0,60 m, rank 0,72 m, width 5,2 m.

1:1 renderer: `Graphics.DrawMeshInstanced` batches ≤1023. Ingen MonoBehaviour/NavMesh/collider/AI/Animator pr. ordinary soldier.

Base speed DK 3,2 / PR 3,35. CohesionSpeedFactor = Lerp(0,72, 1,00, cohesion/100). Arrival 0,55 m. Turn slerp 4,5. Ingen V3 pathfinding på Company.

09l2 multi-select laver lateral linje. 09l5 overskriver med relative offsets fra centroid. Konkurrerende owners — skal væk i rebuildet.

## 6. Regiment movement, navigation og recovery

V3 ON. V4 OFF. 09A OFF. 09B tree/fence pass-through ON. Farmhouse/barn/river blocked. Bridge OK.

LineClearance 9,0 m. ColumnClearance 4,4 m. Persistent detour + 09i1 continuation.

Manual recovery er Regiment-only. Companies i 09l5 bruger det ikke.

## 7. Våben, ilddisciplin, reload og ammunition

| Side | Våben | Effective | Maximum | Base reload | Kernel acc |
| --- | --- | --- | --- | --- | --- |
| Danmark | Rifled muzzle-loader | 43 | 55 | 5,0 s | 0,014 |
| Preussen | Dreyse | 37 | 49 | 3,6 s | 0,013 |

FireArcHalfAngle 60°. Policies: HOLD / CLOSE / MEDIUM / LONG.

ReloadMultiplier = Lerp(1,20, 0,80, Experience/100). Experience 50 = neutral.
StartingAmmo 60 rounds/man. Volley forbruger 1. Ammo 0 → nextFireTime = +inf.

Reload-animation er presentation, ikke simulation-owner.
