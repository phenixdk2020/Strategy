<#
.SYNOPSIS
    Bygger Strategy1864Editor manuelt og udskriver relevante compiler/UHT-fejl.

.VERSION
    1.0.0

.DESCRIPTION
    - Finder Unreal Engine 5.8 via standard Epic-path eller registry.
    - Kører Engine\Build\BatchFiles\Build.bat.
    - Gemmer komplet build-log under Tools\BuildLogs.
    - Viser relevante compiler/UHT/UBT-fejl til sidst.
#>

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

function Get-UnrealEngineRoot {
    $Candidates = New-Object System.Collections.Generic.List[string]

    $DefaultEpic = "C:\Program Files\Epic Games\UE_5.8"
    if (Test-Path $DefaultEpic) {
        $Candidates.Add($DefaultEpic)
    }

    $MachineKey = "HKLM:\SOFTWARE\EpicGames\Unreal Engine\5.8"
    if (Test-Path $MachineKey) {
        try {
            $InstalledDirectory = (Get-ItemProperty $MachineKey).InstalledDirectory
            if ($InstalledDirectory) {
                $Candidates.Add([string]$InstalledDirectory)
            }
        }
        catch {}
    }

    $UserBuildsKey = "HKCU:\SOFTWARE\Epic Games\Unreal Engine\Builds"
    if (Test-Path $UserBuildsKey) {
        try {
            $Props = Get-ItemProperty $UserBuildsKey

            foreach ($Property in $Props.PSObject.Properties) {
                if ($Property.Name -like "PS*") {
                    continue
                }

                $Value = [string]$Property.Value

                if ($Value -and
                    ((Split-Path $Value -Leaf) -like "UE_5.8*" -or
                     $Property.Name -like "*5.8*")) {
                    $Candidates.Add($Value)
                }
            }
        }
        catch {}
    }

    foreach ($Candidate in ($Candidates | Select-Object -Unique)) {
        $BuildBat = Join-Path $Candidate "Engine\Build\BatchFiles\Build.bat"

        if (Test-Path $BuildBat) {
            return $Candidate
        }
    }

    return $null
}

Clear-Host
Write-Host ""
Write-Host "===================================================" -ForegroundColor DarkCyan
Write-Host " PROJECT 1864 - Unreal Build Diagnostic" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor DarkCyan
Write-Host ""

try {
    if (-not (Test-Path $UProject)) {
        throw "Uproject blev ikke fundet: $UProject"
    }

    Write-Status -Message "Finder Unreal Engine 5.8..." -Level CHECKPOINT

    $EngineRoot = Get-UnrealEngineRoot

    if (-not $EngineRoot) {
        throw "Kunne ikke finde Unreal Engine 5.8 automatisk. Kontroller Epic installationen."
    }

    $BuildBat = Join-Path $EngineRoot "Engine\Build\BatchFiles\Build.bat"

    Write-Status -Message "Engine: $EngineRoot" -Level OK
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
        Write-Host "Ingen standard error-linjer fundet. Viser de sidste 80 linjer:" -ForegroundColor Yellow
        $AllLines | Select-Object -Last 80 | ForEach-Object { Write-Host $_ }
    }
    else {
        $ErrorLines | Select-Object -First 80 | ForEach-Object {
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
