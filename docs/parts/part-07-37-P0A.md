# PROJECT 1864 — Designmanual v00.02.05

## 37. Implementeringsstatus – P0A Unity 3D Battle Prototype

P0A er den første spilbare tekniske vertical slice. Formålet er ikke endelig grafik eller historisk balance, men at bevise at den valgte regimentsmodel kan styres i realtid i 3D og at authoritative mandskab, formation, range, combat, morale og rout kan holdes adskilt fra den grafiske 1:10-repræsentation.

### 37.1 Spilbar scope v00.00.05

| System | P0A implementering |
| --- | --- |
| Battle setup | 2 danske regimenter mod 2 preussiske regimenter på et kompakt proceduralt 3D-kort. |
| Authoritative strength | Danmark: 1. Regiment 620 og 5. Regiment 585. Preussen: 8th Regiment 610 og 18th Regiment 560. |
| 3D representation | Ca. 1 grafisk soldat pr. 10 faktiske soldater; ca. 240 simple 3D-soldatmodeller samlet. |
| Camera | RTS-kamera med WASD/piletaster, Q/E rotation og musehjulszoom. |
| Selection & orders | Klik/Shift+klik på danske regimenter, højreklik for movement eller attack order. |
| Formation | Line og column kan skiftes under slaget. |
| Range | Valgte regimenter kan vise effektiv skudafstand som ring på terrænet. |
| Combat | Automatisk volleyild med distancefaktor, regimentskvalitet, casualties og sortkrudtsrøg. |
| Unit state | Strength, morale, cohesion og rout er simulerede regimentsværdier. |
| Enemy AI | Simpel preussisk AI; ét regiment angriber direkte, ét andet bruger en indledende flankerende waypoint. |
| Time | Pause samt 1x/2x/3x og en løbende battle clock. |
| Outcome | Battle manager afgør dansk sejr/nederlag, når modstanderens kampaktive regimenter er routed/ødelagt. |

### 37.2 Arkitektur der nu er praktisk bevist

- Det faktiske mandskabstal er authoritative. Antallet af 3D-modeller følger styrken, men modellerne er ikke selvstændige combat-agenter.
- Regimentet er den simulerede kampentitet; visual soldiers følger formation slots og kan senere erstattes af historiske modeller/animationer uden at omskrive kampberegningen.
- Tactical orders og enemy AI bruger samme `Regiment`-interface, hvilket giver et naturligt grundlag for senere brigade-/officer-AI.
- Range, fire interval, accuracy og movement speed er datafelter og kan senere flyttes til historiske weapon/unit databases.
- Battlefield-generation er adskilt fra unit simulation og kan senere erstattes af håndlavede eller geodata-baserede maps.

### 37.3 Bevidste prototypebegrænsninger

- Ingen NavMesh/formation pathfinding eller obstacle avoidance endnu.
- Ingen order delay, courier-system, commander intent eller selvstændig brigadechef-AI endnu.
- Ingen bataljons-/kompagniunderopdeling i den aktive 3D-simulation endnu.
- Ingen ammunition, wounded/killed-split, sanitet, POW eller persistent campaign aftermath endnu.
- Ingen historiske uniformsmodeller, faner, animationer, lyd eller endelig UI.
- Våbenparametre er prototype-tuning og må ikke betragtes som endeligt historisk balancegrundlag.

### 37.4 Foreslået næste build – P0B

1. Indfør bataljoner som underformationer i regimentet.
2. Implementér order delay, acknowledgement og brigadechefens selvstændige reaktioner ud fra skills/personality.
3. Tilføj rigtig terrain cover, line-of-sight og skov-/hegn-/højderygseffekter.
4. Tilføj ammunition, reload/fire discipline samt killed/wounded/missing som separate casualty states.
5. Udskift direkte movement med NavMesh/formation pathfinding.
6. Tilføj historiske uniformer, faner og animationssystem.
7. Tilføj mindst ét artilleribatteri og et simpelt casualty evacuation-/ambulance-hook som første support-system.

### 37.5 Repository- og Unity-layout

Fra P0A **v00.00.03** er GitHub-repositoryet `phenixdk2020/Strategy` selv det aktive Unity-projekt. Unity Hub skal derfor clone og åbne repository-roden direkte.

```text
Strategy/
├── Assets/
├── Packages/
├── ProjectSettings/
├── docs/
├── README.md
└── VERSION.txt
```

Den tidligere placering `prototype/P0A-Unity-Battle/` er fjernet for at undgå, at Unity Hub kloner repositoryet men ikke finder `ProjectSettings/ProjectVersion.txt` i projektroden. Tekniske P0A-noter ligger fremover under `docs/prototypes/`.

Fra P0A **v00.00.04** er Unity editor-baseline `6000.6.0f1` (Unity 6.6) med changeset `f7f8ed4d1e24`. Baseline er valgt efter faktisk testmiljø: Unity Hub på testmaskinen har 6000.6.0f1 installeret, mens den tidligere 6000.3.17f1 ikke var installeret og derfor udløste `Missing Editor Version`. Projektet skal som udgangspunkt følge den editor-version, der bruges til aktiv udvikling og compile-test, medmindre en senere releasebeslutning fastlåser en anden supporteret Unity-linje.

### 37.6 Unity 6.6 built-in module dependencies

Ved første compile-test i Unity 6000.6.0f1 viste projektet CS1069-fejl for `UnityEngine.GUIStyle` og `UnityEngine.ParticleSystem`. Årsagen var, at prototypeprojektets `Packages/manifest.json` oprindeligt var tomt, mens Unity 6.6 kræver, at relevante built-in moduler er aktiveret eksplicit.

Fra P0A **v00.00.05** indeholder `Packages/manifest.json` derfor:

```json
{
  "dependencies": {
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0"
  }
}
```

Modulerne understøtter følgende prototypefunktioner:

- **IMGUI** — `GUIStyle`, `GUI.Box` og det midlertidige battle-HUD.
- **Particle System** — sortkrudtsrøg ved volleyild.
- **Physics** — raycasts, colliders, selection og ordreinput.
- **Audio** — `AudioListener` på RTS-kameraet og senere lydeffekter.

Dette er en projektkonfigurationsrettelse, ikke en ændring af battle-design eller simulation.

### 37.7 Release-QA-regel for Unity-prototyper

Før en Unity-prototype markeres som spilbar skal følgende kontrolleres:

1. `Assets/`, `Packages/` og `ProjectSettings/` findes i den mappe Unity Hub åbner.
2. `ProjectSettings/ProjectVersion.txt` indeholder en syntaktisk gyldig Unity-version og revision.
3. Repository-roden kan identificeres af Unity Hub som et Unity-projekt.
4. Den valgte Unity-version er installeret på den aktive testmaskine, eller installationen er dokumenteret som et eksplicit krav.
5. Alle Unity built-in moduler, som scripts refererer til, er eksplicit til stede i `Packages/manifest.json`.
6. Projektet compile-testes i den valgte Unity-version, når Editor-runtime er tilgængelig.
7. Eventuelle editor-, package- eller module-ændringer dokumenteres i `VERSION.txt` og designmanualens implementeringsstatus.

P0A er en systems-prototype og ikke den endelige P3 Tactical Vertical Slice fra roadmapet. Den reducerede prototype bruges til at opdage arkitektur- og kontrolproblemer tidligt, før order-delay, bataljoner, historiske assets og persistent campaign state kobles på.
