<#
.SYNOPSIS
    PROJECT 1864 - sikker versionskontrol og GitHub-opdatering.

.DESCRIPTION
    Kontrollerer phenixdk2020/Strategy branch main.
    Gemmer sidst synkroniserede P0A-version i:
        %LOCALAPPDATA%\PROJECT1864\LastSyncedVersion.txt

    Efter fetch sammenlignes både VERSION.txt og Git commit-state.
    En sikker nyere origin/main fast-forwardes derfor også, selv hvis prototypeversionen
    er uændret (fx dokumentation eller hotfix inden for samme testversion):
        git pull --ff-only origin main

    Scriptet overskriver ALDRIG lokale ændringer.
    Hvis git status kun viser kendte, endnu ikke versionsstyrede Unity-bootstrapfiler
    (.meta, packages-lock.json, ProjectSettings/*.asset og PrototypeBattle-scenen),
    gemmes de automatisk midlertidigt med Git stash, opdateringen hentes, og
    Unity-filerne gendannes bagefter. Andre lokale ændringer stopper fortsat sync.

.VERSION
    1.00.03

.CHANGELOG
    1.00.03
    - VERSION.txt-parseren accepterer nu status/qualifier efter versionsnummeret, fx
      "Prototype version: v00.00.08 TEST".
    - Sync afgøres også af Git commit-state, så nye commits med samme prototypeversion
      stadig bliver hentet.
    - Stopper sikkert ved divergeret lokal branch eller ved remote versions-downgrade.
    - Verificerer at den aktive lokale branch matcher den ønskede branch før pull.

    1.00.02
    - Håndterer første Unity-bootstrap automatisk: kendte untracked Unity-filer
      stashes sikkert før pull og gendannes efter opdateringen.
    - Stopper fortsat ved tracked ændringer eller ukendte untracked filer.
    - Unity-filer bliver ikke slettet eller overskrevet; stash droppes først efter
      vellykket restore.

    1.00.01
    - Rettet Windows PowerShell 5.1-fejl hvor normal Git stderr-output fra
      f.eks. "git fetch" blev behandlet som terminating error, selv ved exit code 0.
    - Git-kommandoers succes afgøres nu af $LASTEXITCODE.
    - Native stderr/stdout normaliseres til almindelige tekstlinjer før behandling.
    - Bevarer sikkerhedsreglen: lokale ændringer overskrives aldrig automatisk.

    1.00.00
    - Første version.
    - Sammenligner lokal VERSION.txt med origin/main:VERSION.txt.
    - Gemmer sidst synkroniserede version separat i LOCALAPPDATA.
    - Fast-forward-only opdatering når repository er rent.

.REQUIREMENTS
    - Windows PowerShell 5.1 eller PowerShell 7+
    - Git installeret og tilgængelig i PATH
    - Lokal clone: %USERPROFILE%\Strategy
    - Remote 'origin' skal pege på phenixdk2020/Strategy
#>

[CmdletBinding()]
param(
    [string]$RepoPath = (Join-Path $env:USERPROFILE 'Strategy'),
    [string]$Branch = 'main',
    [switch]$NoUnityAutoStash
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptVersion = '1.00.03'
$StateDir  = Join-Path $env:LOCALAPPDATA 'PROJECT1864'
$StateFile = Join-Path $StateDir 'LastSyncedVersion.txt'

$LastCheckpoint = 'PROGRAM_START'

function Write-Log {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('INFO','OK','WARN','ERROR')]
        [string]$Level,

        [Parameter(Mandatory)]
        [string]$Message,

        [string]$Checkpoint = $script:LastCheckpoint
    )

    $caller = (Get-PSCallStack | Select-Object -Skip 1 -First 1)
    $func = if ($caller) { $caller.FunctionName } else { '<script>' }
    $line = if ($caller) { $caller.ScriptLineNumber } else { 0 }

    $stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Write-Host "[$stamp][$Level][CP:$Checkpoint][Line:$line][Func:$func] $Message"
}

function Set-Checkpoint {
    param([Parameter(Mandatory)][string]$Name)
    $script:LastCheckpoint = $Name
}

function Convert-NativeOutputToText {
    param(
        [AllowNull()]
        [object[]]$InputObject
    )

    $lines = New-Object System.Collections.Generic.List[string]

    foreach ($item in @($InputObject)) {
        if ($null -eq $item) {
            continue
        }

        if ($item -is [System.Management.Automation.ErrorRecord]) {
            $message = $item.Exception.Message
            if ([string]::IsNullOrWhiteSpace($message)) {
                $message = [string]$item
            }
            $lines.Add($message)
        }
        else {
            $lines.Add([string]$item)
        }
    }

    return $lines.ToArray()
}

function Invoke-Git {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,
        [switch]$AllowNonZeroExit
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $hasNativePreference = Test-Path variable:PSNativeCommandUseErrorActionPreference
    $previousNativePreference = $null

    try {
        $ErrorActionPreference = 'Continue'

        if ($hasNativePreference) {
            $previousNativePreference = $PSNativeCommandUseErrorActionPreference
            $PSNativeCommandUseErrorActionPreference = $false
        }

        $rawOutput = @(& git -C $RepoPath @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        if ($hasNativePreference) {
            $PSNativeCommandUseErrorActionPreference = $previousNativePreference
        }
        $ErrorActionPreference = $previousErrorActionPreference
    }

    $output = @(Convert-NativeOutputToText -InputObject $rawOutput)

    if ($exitCode -ne 0 -and -not $AllowNonZeroExit) {
        $detail = if ($output.Count -gt 0) {
            $output -join [Environment]::NewLine
        }
        else {
            '(ingen Git-output)'
        }

        throw "Git fejlede (exit $exitCode): git -C `"$RepoPath`" $($Arguments -join ' ')`n$detail"
    }

    $output
}

function Test-UnityBootstrapOnly {
    param(
        [Parameter(Mandatory)]
        [string[]]$StatusLines
    )

    if ($StatusLines.Count -eq 0) {
        return $false
    }

    foreach ($line in $StatusLines) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.Length -lt 4) {
            return $false
        }

        $code = $line.Substring(0, 2)
        if ($code -ne '??') {
            return $false
        }

        $path = $line.Substring(3).Trim().Trim('"') -replace '\\', '/'

        $knownUnityBootstrapFile = (
            $path -match '^Assets/(?:.+/)?[^/]+\.meta$' -or
            $path -match '^Assets/Scenes/.+\.unity$' -or
            $path -eq 'Packages/packages-lock.json' -or
            $path -match '^ProjectSettings/[^/]+\.asset$'
        )

        if (-not $knownUnityBootstrapFile) {
            return $false
        }
    }

    return $true
}

function Get-PrototypeVersion {
    param(
        [Parameter(Mandatory)]
        [string[]]$Text
    )

    $joined = $Text -join "`n"
    $match = [regex]::Match(
        $joined,
        '(?mi)^\s*Prototype version:\s*(v?\d+\.\d+\.\d+)(?:\s+.*)?$'
    )

    if (-not $match.Success) {
        throw 'Kunne ikke finde "Prototype version: vXX.XX.XX" i VERSION.txt (valgfri status efter versionsnummeret er tilladt).'
    }

    return $match.Groups[1].Value
}

function Convert-ToVersion {
    param([Parameter(Mandatory)][string]$VersionText)

    return [version]($VersionText.Trim().TrimStart('v','V'))
}

try {
    Write-Host ''
    Write-Host "PROJECT 1864 - GitHub Version Sync v$ScriptVersion"
    Write-Host '================================================'
    Write-Host ''

    Set-Checkpoint 'CHECK_PREREQUISITES'

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        throw 'Git blev ikke fundet i PATH.'
    }

    if (-not (Test-Path -LiteralPath $RepoPath -PathType Container)) {
        throw "Repository-mappen findes ikke: $RepoPath"
    }

    if (-not (Test-Path -LiteralPath (Join-Path $RepoPath '.git'))) {
        throw "Mappen er ikke en Git-clone: $RepoPath"
    }

    if (-not (Test-Path -LiteralPath $StateDir)) {
        New-Item -ItemType Directory -Path $StateDir -Force | Out-Null
    }

    Write-Log INFO "Repository: $RepoPath"
    Write-Log INFO "Versionsfil: $StateFile"

    Set-Checkpoint 'VERIFY_REMOTE'

    $originUrl = (@(Invoke-Git -Arguments @('remote','get-url','origin')) -join '').Trim()
    Write-Log INFO "Git origin: $originUrl"

    if ($originUrl -notmatch 'github\.com[/:]phenixdk2020/Strategy(?:\.git)?$') {
        Write-Log WARN 'origin matcher ikke forventet phenixdk2020/Strategy. Scriptet fortsætter, men kontrollér remote.'
    }

    Set-Checkpoint 'VERIFY_BRANCH'
    $currentBranch = (@(Invoke-Git -Arguments @('branch','--show-current')) -join '').Trim()
    if ([string]::IsNullOrWhiteSpace($currentBranch)) {
        throw 'Repository står i detached HEAD. Skift til main før sync.'
    }
    if ($currentBranch -ne $Branch) {
        throw "Aktiv lokal branch er '$currentBranch', men sync er sat til '$Branch'. Skift branch før sync."
    }
    Write-Log INFO "Aktiv branch: $currentBranch"

    Set-Checkpoint 'FETCH_REMOTE'

    Write-Log INFO "Kontrollerer GitHub origin/$Branch..."
    @(Invoke-Git -Arguments @('fetch','origin',$Branch,'--prune')) | Out-Null
    Write-Log OK "Git fetch gennemført uden fejl."

    Set-Checkpoint 'READ_REMOTE_VERSION'

    $remoteVersionText = @(Invoke-Git -Arguments @('show',"origin/$Branch`:VERSION.txt"))
    $remoteVersion = Get-PrototypeVersion -Text $remoteVersionText
    $remoteVersionObject = Convert-ToVersion $remoteVersion

    $localVersion = $null
    $repoVersionFile = Join-Path $RepoPath 'VERSION.txt'

    if (Test-Path -LiteralPath $repoVersionFile) {
        $localVersion = Get-PrototypeVersion -Text @(Get-Content -LiteralPath $repoVersionFile)
    }
    elseif (Test-Path -LiteralPath $StateFile) {
        $saved = (Get-Content -LiteralPath $StateFile -Raw).Trim()
        if ($saved) {
            try {
                [void](Convert-ToVersion $saved)
                $localVersion = $saved
                Write-Log WARN "Lokal VERSION.txt mangler. Bruger gemt versionsstatus som fallback: $localVersion"
            }
            catch {
                Write-Log WARN "Den gemte versionsstatus er ugyldig: '$saved'."
            }
        }
    }

    if (-not $localVersion) {
        $localVersion = 'v0.0.0'
        Write-Log WARN 'Ingen lokal eller gemt version fundet. Starter versionssammenligning fra v0.0.0.'
    }

    $localVersionObject = Convert-ToVersion $localVersion

    Set-Checkpoint 'COMPARE_VERSION_AND_COMMIT'

    $localHead  = (@(Invoke-Git -Arguments @('rev-parse','HEAD')) -join '').Trim()
    $remoteHead = (@(Invoke-Git -Arguments @('rev-parse',"origin/$Branch")) -join '').Trim()

    Write-Host ''
    Write-Host "Lokal version        : $localVersion"
    Write-Host "GitHub version       : $remoteVersion"
    Write-Host "Lokal commit         : $($localHead.Substring(0, [Math]::Min(12, $localHead.Length)))"
    Write-Host "GitHub commit        : $($remoteHead.Substring(0, [Math]::Min(12, $remoteHead.Length)))"
    Write-Host ''

    if ($remoteVersionObject -lt $localVersionObject) {
        throw "GitHub VERSION.txt ($remoteVersion) er ældre end lokal version ($localVersion). Automatisk downgrade er blokeret."
    }

    if ($localHead -eq $remoteHead) {
        Set-Content -LiteralPath $StateFile -Value $localVersion -Encoding UTF8
        Write-Log OK 'Lokal clone er allerede på samme commit som GitHub.'
        Write-Log OK "Versionsstatus gemt i: $StateFile"
        exit 0
    }

    $leftRight = (@(Invoke-Git -Arguments @('rev-list','--left-right','--count',"$localHead...$remoteHead")) -join ' ').Trim()
    $parts = @($leftRight -split '\s+' | Where-Object { $_ -ne '' })
    if ($parts.Count -lt 2) {
        throw "Kunne ikke afgøre branch-divergens fra Git. Output: '$leftRight'"
    }
    $localOnly  = [int]$parts[0]
    $remoteOnly = [int]$parts[1]

    if ($localOnly -gt 0) {
        throw "Lokal $Branch er divergeret fra origin/$Branch (lokale commits: $localOnly, remote commits: $remoteOnly). Automatisk sync er stoppet for at beskytte lokalt arbejde."
    }

    if ($remoteOnly -le 0) {
        Set-Content -LiteralPath $StateFile -Value $localVersion -Encoding UTF8
        Write-Log OK 'Ingen remote commits at hente.'
        exit 0
    }

    if ($remoteVersionObject -gt $localVersionObject) {
        Write-Log INFO "Ny prototypeversion fundet: $localVersion -> $remoteVersion ($remoteOnly commit(s))."
    }
    else {
        Write-Log INFO "GitHub har $remoteOnly nyere commit(s) inden for samme prototypeversion $remoteVersion. De hentes også."
    }

    Set-Checkpoint 'CHECK_LOCAL_CHANGES'

    $status = @(Invoke-Git -Arguments @('status','--porcelain','--untracked-files=all'))
    $hasLocalChanges = (($status -join "`n").Trim().Length -gt 0)
    $unityAutoStashCreated = $false

    if ($hasLocalChanges) {
        $unityBootstrapOnly = Test-UnityBootstrapOnly -StatusLines $status

        if ($unityBootstrapOnly -and -not $NoUnityAutoStash) {
            Set-Checkpoint 'STASH_UNITY_BOOTSTRAP'
            Write-Log WARN 'Kun kendte untracked Unity-bootstrapfiler blev fundet. De gemmes midlertidigt før Git-opdateringen.'
            Write-Host ''
            Write-Host 'Unity-filer der bevares:'
            $status | ForEach-Object { Write-Host "  $_" }
            Write-Host ''

            $stashMessage = "PROJECT1864 Unity bootstrap auto-stash $(Get-Date -Format 'yyyyMMdd-HHmmss')"
            $stashOutput = @(Invoke-Git -Arguments @('stash','push','--include-untracked','-m',$stashMessage))
            $stashOutput | ForEach-Object { Write-Host $_ }

            $postStashStatus = @(Invoke-Git -Arguments @('status','--porcelain','--untracked-files=all'))
            if ((($postStashStatus -join "`n").Trim().Length) -gt 0) {
                throw "Repository er stadig ikke rent efter Unity auto-stash:`n$($postStashStatus -join [Environment]::NewLine)"
            }

            $unityAutoStashCreated = $true
            Write-Log OK 'Unity-bootstrapfilerne er gemt sikkert i Git stash.'
        }
        else {
            Write-Log ERROR 'Der findes lokale ændringer, som ikke må auto-stashes. Automatisk pull er stoppet for at beskytte dit arbejde.'
            Write-Host ''
            Write-Host 'Lokale ændringer:'
            $status | ForEach-Object { Write-Host "  $_" }
            Write-Host ''
            if ($unityBootstrapOnly -and $NoUnityAutoStash) {
                Write-Host 'Unity auto-stash er slået fra med -NoUnityAutoStash.'
            }
            Write-Host 'Der er IKKE overskrevet eller slettet noget.'
            exit 2
        }
    }

    Set-Checkpoint 'PULL_UPDATE'

    Write-Log INFO "Opdaterer lokal clone fra origin/$Branch med fast-forward only..."
    $pullOutput = @(Invoke-Git -Arguments @('pull','--ff-only','origin',$Branch))
    $pullOutput | ForEach-Object { Write-Host $_ }

    Set-Checkpoint 'VERIFY_UPDATE'

    $updatedVersionFile = Join-Path $RepoPath 'VERSION.txt'
    if (-not (Test-Path -LiteralPath $updatedVersionFile)) {
        throw 'VERSION.txt mangler efter Git-opdateringen.'
    }

    $updatedVersion = Get-PrototypeVersion -Text @(Get-Content -LiteralPath $updatedVersionFile)

    if ((Convert-ToVersion $updatedVersion) -lt $remoteVersionObject) {
        throw "Lokal VERSION.txt er stadig ældre efter pull. Lokal=$updatedVersion, GitHub=$remoteVersion"
    }

    if ($unityAutoStashCreated) {
        Set-Checkpoint 'RESTORE_UNITY_BOOTSTRAP'
        Write-Log INFO 'Gendanner de lokale Unity-bootstrapfiler...'

        try {
            $applyOutput = @(Invoke-Git -Arguments @('stash','apply','stash@{0}'))
            $applyOutput | ForEach-Object { Write-Host $_ }
        }
        catch {
            Write-Log ERROR 'Git-opdateringen er hentet, men Unity-filerne kunne ikke gendannes automatisk uden konflikt.'
            Write-Log ERROR $_.Exception.Message
            Write-Host ''
            Write-Host 'VIGTIGT: Auto-stash er IKKE slettet. Dine Unity-filer er stadig bevaret i stash@{0}.'
            Write-Host 'Kør: git status --short'
            Write-Host 'og send outputtet, før der foretages yderligere Git-ændringer.'
            exit 3
        }

        @(Invoke-Git -Arguments @('stash','drop','stash@{0}')) | Out-Null
        Write-Log OK 'Unity-bootstrapfilerne er gendannet. Den midlertidige stash er derefter fjernet.'
    }

    Set-Checkpoint 'WRITE_STATE'

    Set-Content -LiteralPath $StateFile -Value $updatedVersion -Encoding UTF8

    $head = (@(Invoke-Git -Arguments @('rev-parse','--short','HEAD')) -join '').Trim()

    Write-Host ''
    Write-Log OK "PROJECT 1864 er opdateret til $updatedVersion."
    Write-Log OK "Aktuel commit: $head"
    Write-Log OK "Versionsstatus gemt i: $StateFile"
    Write-Host ''

    exit 0
}
catch {
    Write-Log ERROR $_.Exception.Message
    Write-Log ERROR "Sidste checkpoint: $LastCheckpoint"
    exit 1
}
