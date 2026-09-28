<#
.SYNOPSIS
    Bygger Strategy1864Editor manuelt og udskriver relevante compiler/UHT-fejl.

.VERSION
    1.1.0

.DESCRIPTION
    - Finder Unreal Engine 5.8 via parameter, miljøvariabel, Epic Launcher,
      manifestfiler, registry, registrerede builds og almindelige install-paths.
    - Validerer at fundet engine faktisk er UE 5.8 via Engine\Build\Build.version.
    - Kører Engine\Build\BatchFiles\Build.bat.
    - Gemmer komplet build-log under Tools\BuildLogs.
    - Viser relevante compiler/UHT/UBT-fejl til sidst.

.EXAMPLE
    .\Tools\Build-Strategy1864.ps1

.EXAMPLE
    .\Tools\Build-Strategy1864.ps1 -EngineRoot "D:\Epic Games\UE_5.8"
#>

param(
    [string]$EngineRoot
)

$ErrorActionPreference = "Stop"

$ProjectRoot = "R:\Onedrive\Dokumenter\Unreal Projects\Strategy1864"
$UProject = Join-Path $ProjectRoot "Unreal\Strategy1864.uproject"
$LogRoot = Join-Path $ProjectRoot "Tools\BuildLogs"

function Write-Status {
    param(
        [Parameter(Mandatory=$true)][string]$Message,
        [ValidateSet("INFO","OK","WARNING","ERROR","CHECKPOINT")][string]$Level = "INFO"
    )

    $Time = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

    switch ($Level) {
        "INFO"       { Write-Host "[$Time] [INFO] $Message" -ForegroundColor Cyan }
        "OK"         { Write-Host "[$Time] [OK] $Message" -ForegroundColor Green }
        "WARNING"    { Write-Host "[$Time] [WARNING] $Message" -ForegroundColor Yellow }
        "ERROR"      { Write-Host "[$Time] [ERROR] $Message" -ForegroundColor Red }
        "CHECKPOINT" { Write-Host "[$Time] [CHECKPOINT] $Message" -ForegroundColor Magenta }
    }
}

function Get-UnrealVersionInfo {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Root
    )

    $BuildVersion = Join-Path $Root "Engine\Build\Build.version"

    if (-not (Test-Path $BuildVersion)) {
        return $null
    }

    try {
        return Get-Content -Path $BuildVersion -Raw | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Test-Unreal58Root {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Root
    )

    if ([string]::IsNullOrWhiteSpace($Root)) {
        return $false
    }

    try {
        $Resolved = [System.IO.Path]::GetFullPath(
            [Environment]::ExpandEnvironmentVariables($Root.Trim('"'))
        )
    }
    catch {
        return $false
    }

    $BuildBat = Join-Path $Resolved "Engine\Build\BatchFiles\Build.bat"

    if (-not (Test-Path $BuildBat)) {
        return $false
    }

    $Version = Get-UnrealVersionInfo -Root $Resolved

    if (-not $Version) {
        # Build.bat is enough to keep a custom/source build usable,
        # but version validation is preferred when Build.version exists.
        return $true
    }

    return (
        [int]$Version.MajorVersion -eq 5 -and
        [int]$Version.MinorVersion -eq 8
    )
}

function Add-EngineCandidate {
    param(
        [Parameter(Mandatory=$true)]
        [System.Collections.Generic.List[string]]$List,

        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $Expanded = [Environment]::ExpandEnvironmentVariables($Path.Trim('"'))

    if (-not $List.Contains($Expanded)) {
        $List.Add($Expanded)
    }
}

function Add-EpicLauncherInstalledCandidates {
    param(
        [Parameter(Mandatory=$true)]
        [System.Collections.Generic.List[string]]$List
    )

    $LauncherFiles = @(
        "C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat"
    )

    foreach ($LauncherFile in $LauncherFiles) {
        if (-not (Test-Path $LauncherFile)) {
            continue
        }

        try {
            $Data = Get-Content -Path $LauncherFile -Raw | ConvertFrom-Json

            foreach ($Item in @($Data.InstallationList)) {
                $AppName = [string]$Item.AppName
                $Location = [string]$Item.InstallLocation

                if ($AppName -like "UE_5.8*" -or $Location -match "UE[_ -]?5\.8") {
                    Add-EngineCandidate -List $List -Path $Location
                }
            }
        }
        catch {
            Write-Status -Message "Kunne ikke læse LauncherInstalled.dat: $($_.Exception.Message)" -Level WARNING
        }
    }
}

function Add-EpicManifestCandidates {
    param(
        [Parameter(Mandatory=$true)]
        [System.Collections.Generic.List[string]]$List
    )

    $ManifestRoot = "C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests"

    if (-not (Test-Path $ManifestRoot)) {
        return
    }

    foreach ($Manifest in @(Get-ChildItem -Path $ManifestRoot -Filter "*.item" -File -ErrorAction SilentlyContinue)) {
        try {
            $Data = Get-Content -Path $Manifest.FullName -Raw | ConvertFrom-Json

            $AppName = [string]$Data.AppName
            $DisplayName = [string]$Data.DisplayName
            $Location = [string]$Data.InstallLocation

            if (
                $AppName -like "UE_5.8*" -or
                $DisplayName -like "*5.8*" -or
                $Location -match "UE[_ -]?5\.8"
            ) {
                Add-EngineCandidate -List $List -Path $Location
            }
        }
        catch {
            # Ignore unrelated/corrupt manifest entries.
        }
    }
}

function Add-RegistryCandidates {
    param(
        [Parameter(Mandatory=$true)]
        [System.Collections.Generic.List[string]]$List
    )

    $EngineKeys = @(
        "HKLM:\SOFTWARE\EpicGames\Unreal Engine\5.8",
        "HKLM:\SOFTWARE\WOW6432Node\EpicGames\Unreal Engine\5.8",
        "HKCU:\SOFTWARE\EpicGames\Unreal Engine\5.8"
    )

    foreach ($Key in $EngineKeys) {
        if (-not (Test-Path $Key)) {
            continue
        }

        try {
            $Props = Get-ItemProperty $Key

            if ($Props.InstalledDirectory) {
                Add-EngineCandidate -List $List -Path ([string]$Props.InstalledDirectory)
            }
        }
        catch {}
    }

    $RegisteredBuildKeys = @(
        "HKCU:\SOFTWARE\Epic Games\Unreal Engine\Builds",
        "HKLM:\SOFTWARE\Epic Games\Unreal Engine\Builds"
    )

    foreach ($Key in $RegisteredBuildKeys) {
        if (-not (Test-Path $Key)) {
            continue
        }

        try {
            $Props = Get-ItemProperty $Key

            foreach ($Property in $Props.PSObject.Properties) {
                if ($Property.Name -like "PS*") {
                    continue
                }

                $Value = [string]$Property.Value

                if (-not [string]::IsNullOrWhiteSpace($Value)) {
                    Add-EngineCandidate -List $List -Path $Value
                }
            }
        }
        catch {}
    }
}

function Add-CommonDriveCandidates {
    param(
        [Parameter(Mandatory=$true)]
        [System.Collections.Generic.List[string]]$List
    )

    foreach ($Drive in @(Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue)) {
        if (-not $Drive.Root) {
            continue
        }

        $Root = $Drive.Root

        $Common = @(
            (Join-Path $Root "Program Files\Epic Games\UE_5.8"),
            (Join-Path $Root "Epic Games\UE_5.8"),
            (Join-Path $Root "EpicGames\UE_5.8"),
            (Join-Path $Root "Unreal Engine\UE_5.8"),
            (Join-Path $Root "UnrealEngine\UE_5.8"),
            (Join-Path $Root "UE_5.8")
        )

        foreach ($Path in $Common) {
            Add-EngineCandidate -List $List -Path $Path
        }

        # Shallow search only in common parent folders. Avoid full-drive recursion.
        $Parents = @(
            (Join-Path $Root "Epic Games"),
            (Join-Path $Root "EpicGames"),
            (Join-Path $Root "Program Files\Epic Games"),
            (Join-Path $Root "Unreal Engine"),
            (Join-Path $Root "UnrealEngine")
        )

        foreach ($Parent in $Parents) {
            if (-not (Test-Path $Parent)) {
                continue
            }

            foreach ($Folder in @(Get-ChildItem -Path $Parent -Directory -Filter "UE_5.8*" -ErrorAction SilentlyContinue)) {
                Add-EngineCandidate -List $List -Path $Folder.FullName
            }
        }
    }
}

function Get-UnrealEngineRoot {
    param(
        [string]$ExplicitRoot
    )

    $Candidates = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($ExplicitRoot)) {
        Write-Status -Message "Tester -EngineRoot: $ExplicitRoot" -Level INFO
        Add-EngineCandidate -List $Candidates -Path $ExplicitRoot
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UE_ENGINE_ROOT)) {
        Write-Status -Message "Tester UE_ENGINE_ROOT: $env:UE_ENGINE_ROOT" -Level INFO
        Add-EngineCandidate -List $Candidates -Path $env:UE_ENGINE_ROOT
    }

    Add-EpicLauncherInstalledCandidates -List $Candidates
    Add-EpicManifestCandidates -List $Candidates
    Add-RegistryCandidates -List $Candidates
    Add-CommonDriveCandidates -List $Candidates

    foreach ($Candidate in $Candidates) {
        if (Test-Unreal58Root -Root $Candidate) {
            return [System.IO.Path]::GetFullPath($Candidate.Trim('"'))
        }
    }

    return $null
}

Clear-Host
Write-Host ""
Write-Host "===================================================" -ForegroundColor DarkCyan
Write-Host " PROJECT 1864 - Unreal Build Diagnostic" -ForegroundColor Cyan
Write-Host " Version 1.1.0" -ForegroundColor DarkGray
Write-Host "===================================================" -ForegroundColor DarkCyan
Write-Host ""

try {
    if (-not (Test-Path $UProject)) {
        throw "Uproject blev ikke fundet: $UProject"
    }

    Write-Status -Message "Finder Unreal Engine 5.8..." -Level CHECKPOINT

    $ResolvedEngineRoot = Get-UnrealEngineRoot -ExplicitRoot $EngineRoot

    if (-not $ResolvedEngineRoot) {
        Write-Host ""
        Write-Host "Engine blev ikke fundet automatisk." -ForegroundColor Yellow
        Write-Host "Find mappen der indeholder Engine\Build\BatchFiles\Build.bat og kør:" -ForegroundColor Yellow
        Write-Host ""
        Write-Host '  .\Tools\Build-Strategy1864.ps1 -EngineRoot "D:\sti\til\UE_5.8"' -ForegroundColor Cyan
        Write-Host ""
        throw "Kunne ikke finde en gyldig Unreal Engine 5.8 installation."
    }

    $VersionInfo = Get-UnrealVersionInfo -Root $ResolvedEngineRoot
    $BuildBat = Join-Path $ResolvedEngineRoot "Engine\Build\BatchFiles\Build.bat"

    Write-Status -Message "Engine: $ResolvedEngineRoot" -Level OK

    if ($VersionInfo) {
        $VersionText = "{0}.{1}.{2}" -f $VersionInfo.MajorVersion, $VersionInfo.MinorVersion, $VersionInfo.PatchVersion
        Write-Status -Message "Engine version: $VersionText" -Level OK
    }

    Write-Status -Message "Project: $UProject" -Level INFO

    if (-not (Test-Path $LogRoot)) {
        New-Item -Path $LogRoot -ItemType Directory -Force | Out-Null
    }

    $Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $LogFile = Join-Path $LogRoot ("Strategy1864_Build_{0}.log" -f $Timestamp)

    Write-Status -Message "Starter Strategy1864Editor Win64 Development build..." -Level CHECKPOINT
    Write-Status -Message "Komplet log: $LogFile" -Level INFO
    Write-Host ""

    $Arguments = @(
        "Strategy1864Editor",
        "Win64",
        "Development",
        ('-Project="{0}"' -f $UProject),
        "-WaitMutex",
        "-NoHotReloadFromIDE"
    )

    $ProcessInfo = New-Object System.Diagnostics.ProcessStartInfo
    $ProcessInfo.FileName = $BuildBat
    $ProcessInfo.Arguments = ($Arguments -join " ")
    $ProcessInfo.WorkingDirectory = Split-Path $BuildBat -Parent
    $ProcessInfo.UseShellExecute = $false
    $ProcessInfo.CreateNoWindow = $true
    $ProcessInfo.RedirectStandardOutput = $true
    $ProcessInfo.RedirectStandardError = $true

    $Process = New-Object System.Diagnostics.Process
    $Process.StartInfo = $ProcessInfo

    if (-not $Process.Start()) {
        throw "Kunne ikke starte Unreal Build Tool."
    }

    $StdOut = $Process.StandardOutput.ReadToEnd()
    $StdErr = $Process.StandardError.ReadToEnd()

    $Process.WaitForExit()
    $ExitCode = $Process.ExitCode
    $Process.Dispose()

    $FullLog = @(
        $StdOut
        $StdErr
    ) -join [Environment]::NewLine

    Set-Content -Path $LogFile -Value $FullLog -Encoding UTF8

    Write-Host ""

    if ($ExitCode -eq 0) {
        Write-Status -Message "BUILD PASS - Strategy1864Editor blev bygget korrekt." -Level OK
        Write-Host ""
        Write-Host "Du kan nu starte:" -ForegroundColor Green
        Write-Host "  $UProject"
        Write-Host ""
        exit 0
    }

    Write-Status -Message "BUILD FAIL - ExitCode $ExitCode" -Level ERROR
    Write-Host ""
    Write-Host "================ RELEVANTE FEJL ================" -ForegroundColor Red

    $Patterns = @(
        'error C\d+',
        'fatal error C\d+',
        'UnrealHeaderTool failed',
        'Unable to compile source files',
        'error:',
        'Error:',
        'UBT ERROR',
        'BUILD FAILED',
        'OtherCompilationError'
    )

    $AllLines = $FullLog -split "\r?\n"

    $ErrorLines = foreach ($Line in $AllLines) {
        foreach ($Pattern in $Patterns) {
            if ($Line -match $Pattern) {
                $Line
                break
            }
        }
    }

    $ErrorLines = @($ErrorLines | Select-Object -Unique)

    if ($ErrorLines.Count -eq 0) {
        Write-Host "Ingen standard error-linjer fundet. Viser de sidste 100 linjer:" -ForegroundColor Yellow
        $AllLines | Select-Object -Last 100 | ForEach-Object { Write-Host $_ }
    }
    else {
        $ErrorLines | Select-Object -First 120 | ForEach-Object {
            Write-Host $_ -ForegroundColor Red
        }
    }

    Write-Host ""
    Write-Host "=================================================" -ForegroundColor Red
    Write-Host "Komplet log:" -ForegroundColor Yellow
    Write-Host $LogFile
    Write-Host ""

    exit $ExitCode
}
catch {
    Write-Status -Message $_.Exception.Message -Level ERROR
    exit 1
}
