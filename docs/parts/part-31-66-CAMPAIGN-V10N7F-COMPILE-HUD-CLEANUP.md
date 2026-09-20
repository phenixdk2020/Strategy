# Del 31: 66 — Campaign3 v00.00.10n7f: Compile + HUD Cleanup

**Designbaseline:** v00.02.32  
**Prototypeversion:** v00.00.10n7f  
**Work branch:** `work/channel-campaign3-v10n7f-compile-cleanup`

## Formål

n7f er et stabiliseringshotfix oven på n7e med to mål:

1. fjern `CS0162: Unreachable code detected` fra legacy city-art guards,
2. start campaign map i en ren visning uden ekstra info/debug-bokse.

Selve Proposal-3 city-art, city scale, city INFO-adfærd og de tre coastal visual offsets ændres ikke.

## CS0162 root cause

`CampaignBuildInfo.CurrentVersion` er en `const string`.

Direkte kode som:

```csharp
if (CampaignBuildInfo.CurrentVersion != "v00.00.10n7a")
    return;
```

kan evalueres af C#-compileren. I en nyere build ved compileren derfor, at resten af metoden aldrig kan nås og udsteder `CS0162`.

n7f flytter versionssammenligningen bag:

```csharp
CampaignBuildInfo.IsCurrentVersion(string version)
```

så legacy-komponenterne stadig kan self-guard'e ved runtime uden compile-warning.

## Clean HUD default

Ved første n7f-run sættes:

- HUD = ON,
- selection/INFO = OFF,
- debug = OFF.

Dette sker også for eksisterende brugere med gamle `PlayerPrefs`, så tidligere INFO/DBG-state ikke gør kortet rodet ved opstart.

### Altid synligt

- campaign topbar,
- dato/tid,
- pause/hastighed,
- INFO-knap,
- DBG-knap.

### Skjult som standard

- venstre selection/INFO-panel,
- top-left `PROJECT 1864 | version` badge,
- `WORLD IMAGERY + 3D TERRAIN` panel,
- `BASEMAP / WGS84` technical panel.

Build badge og technical boxes bliver vist sammen med DBG.

Et direkte klik på en by åbner fortsat city INFO automatisk.

## Input Manager warning

Unity 6.6 advarer om, at det gamle Input Manager-system er markeret for deprecation.

n7f migrerer **ikke** inputsystemet. Campaign- og battle-input er allerede omfattende, og migrationen skal være en separat teknisk opgave med regression af:

- keyboard movement/hotkeys,
- mouse selection,
- box selection,
- right-click orders,
- scroll zoom,
- MMB pan,
- campaign speed/pause,
- tactical controls.

Deprecation-beskeden er derfor non-blocking i n7f.

## QA

1. Ingen `CS0162` fra n7a/n7b/n7c/n7e city-art scripts.
2. n7f-version vises i topbaren.
3. INFO og DBG er OFF på første n7f-start.
4. separat build badge er skjult.
5. terrain/basemap technical boxes er skjult.
6. city click åbner INFO.
7. DBG viser technical boxes igen.
8. n7e city-art og coastal offsets fungerer uændret.
9. Amt/county forbliver OFF.

## Status

**IMPLEMENTERET PÅ WORK BRANCH — AFVENTER UNITY 6.6 COMPILE + PLAY MODE QA.**
