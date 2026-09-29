<#
.SYNOPSIS
    Henter/opdaterer PROJECT 1864 fra GitHub.

.VERSION
    1.0.4

.CHANGELOG
    1.0.4
    - Tekst gjort Windows PowerShell 5.1/ANSI-sikker.
    - BuildLogs er nu ignoreret via .gitignore.


    1.0.3
    - Git startes via System.Diagnostics.Process.
    - StandardOutput og StandardError læses direkte fra processen.
    - Windows PowerShell 5.1 kan derfor ikke oprette NativeCommandError.
    - Kun process ExitCode <> 0 behandles som fejl.

    1.0.2
    - Forsøg med STDERR til midlertidig fil.
    - Kontrollerer lokal commit mod origin.
#>

$Version = "1.0.4"
$RepositoryUrl = "https://github.com/phenixdk2020/Strategy.git"
$Branch = "unreal-port"
$ProjectRoot = "R:\Onedrive\Dokumenter\Unreal Projects\Strategy1864"
$UnrealProject = Join-Path $ProjectRoot "Unreal\Strategy1864.uproject"
$OpenUnrealProject = $false

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

function ConvertTo-NativeArgument {
    param(
        [Parameter(Mandatory=$true)]
        [AllowEmptyString()]
        [string]$Value
    )

    if ($Value -notmatch '[\s"]') {
        return $Value
    }

    # Windows CreateProcess quoting:
    # - quotes around argument
    # - backslashes before embedded quote must be doubled
    # - trailing backslashes before closing quote must also be doubled
    $Builder = New-Object System.Text.StringBuilder
    [void]$Builder.Append('"')

    $BackslashCount = 0

    foreach ($Char in $Value.ToCharArray()) {
        if ($Char -eq '\\') {
            $BackslashCount++
            continue
        }

        if ($Char -eq '"') {
            [void]$Builder.Append(('\\' * (($BackslashCount * 2) + 1)))
            [void]$Builder.Append('"')
            $BackslashCount = 0
            continue
        }

        if ($BackslashCount -gt 0) {
            [void]$Builder.Append(('\\' * $BackslashCount))
            $BackslashCount = 0
        }

        [void]$Builder.Append($Char)
    }

    if ($BackslashCount -gt 0) {
        [void]$Builder.Append(('\\' * ($BackslashCount * 2)))
    }

    [void]$Builder.Append('"')
    return $Builder.ToString()
}

function Invoke-Git {
    param(
        [Parameter(Mandatory=$true)]
        [string[]]$Arguments,

        [Parameter(Mandatory=$true)]
        [string]$WorkingDirectory,

        [switch]$Quiet
    )

    $GitExe = (Get-Command git.exe -ErrorAction Stop).Source

    if (-not (Test-Path $WorkingDirectory)) {
        throw "Working directory findes ikke: $WorkingDirectory"
    }

    $ArgumentString = (
        $Arguments |
        ForEach-Object {
            ConvertTo-NativeArgument -Value ([string]$_)
        }
    ) -join " "

    if (-not $Quiet) {
        Write-Status -Message ("git " + ($Arguments -join " ")) -Level INFO
    }

    $StartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $StartInfo.FileName = $GitExe
    $StartInfo.Arguments = $ArgumentString
    $StartInfo.WorkingDirectory = $WorkingDirectory
    $StartInfo.UseShellExecute = $false
    $StartInfo.CreateNoWindow = $true
    $StartInfo.RedirectStandardOutput = $true
    $StartInfo.RedirectStandardError = $true

    $Process = New-Object System.Diagnostics.Process
    $Process.StartInfo = $StartInfo

    try {
        if (-not $Process.Start()) {
            throw "Kunne ikke starte Git-processen."
        }

        # ReadToEnd på begge streams før WaitForExit undgår at buffers fyldes.
        $StdOut = $Process.StandardOutput.ReadToEnd()
        $StdErr = $Process.StandardError.ReadToEnd()

        $Process.WaitForExit()
        $ExitCode = $Process.ExitCode

        if (-not $Quiet) {
            if (-not [string]::IsNullOrWhiteSpace($StdOut)) {
                $StdOut.TrimEnd() -split "\r?\n" | ForEach-Object {
                    if (-not [string]::IsNullOrWhiteSpace($_)) {
                        Write-Host $_
                    }
                }
            }

            if (-not [string]::IsNullOrWhiteSpace($StdErr)) {
                $StdErr.TrimEnd() -split "\r?\n" | ForEach-Object {
                    if (-not [string]::IsNullOrWhiteSpace($_)) {
                        if ($ExitCode -eq 0) {
                            Write-Host $_ -ForegroundColor DarkGray
                        }
                        else {
                            Write-Host $_ -ForegroundColor Red
                        }
                    }
                }
            }
        }

        if ($ExitCode -ne 0) {
            $ErrorText = $StdErr.Trim()

            if ([string]::IsNullOrWhiteSpace($ErrorText)) {
                $ErrorText = $StdOut.Trim()
            }

            throw ("Git afsluttede med fejlkode {0}. {1}" -f $ExitCode, $ErrorText)
        }

        return [PSCustomObject]@{
            ExitCode = $ExitCode
            StdOut   = $StdOut
            StdErr   = $StdErr
        }
    }
    finally {
        if ($Process) {
            $Process.Dispose()
        }
    }
}

function Test-GitInstalled {
    Write-Status -Message "Kontrollerer Git..." -Level CHECKPOINT
    $GitCommand = Get-Command git.exe -ErrorAction SilentlyContinue
    if (-not $GitCommand) { throw "Git er ikke installeret eller findes ikke i PATH." }
    Write-Status -Message "Git fundet: $($GitCommand.Source)" -Level OK
}

function Test-LocalChanges {
    param([Parameter(Mandatory=$true)][string]$RepositoryPath)
    Write-Status -Message "Kontrollerer lokale aendringer..." -Level CHECKPOINT
    $Result = Invoke-Git -WorkingDirectory $RepositoryPath -Arguments @("status","--porcelain") -Quiet
    if (-not [string]::IsNullOrWhiteSpace($Result.StdOut)) {
        Write-Status -Message "Der findes lokale aendringer." -Level WARNING
        Write-Host ""
        Write-Host "Lokale aendringer:" -ForegroundColor Yellow
        $Result.StdOut -split "\r?\n" | ForEach-Object {
            if (-not [string]::IsNullOrWhiteSpace($_)) { Write-Host "  $_" -ForegroundColor Yellow }
        }
        Write-Host ""
        throw "Git sync stoppet for at beskytte lokale filer. Commit, stash eller fjern aendringerne foerst."
    }
    Write-Status -Message "Ingen lokale aendringer fundet." -Level OK
}

Clear-Host
Write-Host ""
Write-Host "==============================================" -ForegroundColor DarkCyan
Write-Host " PROJECT 1864 - Git Sync" -ForegroundColor Cyan
Write-Host " Version $Version" -ForegroundColor DarkGray
Write-Host "==============================================" -ForegroundColor DarkCyan
Write-Host ""
Write-Status -Message "Repository : $RepositoryUrl"
Write-Status -Message "Branch     : $Branch"
Write-Status -Message "Destination: $ProjectRoot"
Write-Host ""

try {
    Test-GitInstalled
    $ParentFolder = Split-Path $ProjectRoot -Parent

    if (-not (Test-Path $ParentFolder)) {
        Write-Status -Message "Opretter mappe: $ParentFolder"
        New-Item -Path $ParentFolder -ItemType Directory -Force | Out-Null
    }

    if (-not (Test-Path $ProjectRoot)) {
        Write-Status -Message "Projektet findes ikke lokalt. Starter clone..." -Level CHECKPOINT
        Invoke-Git -WorkingDirectory $ParentFolder -Arguments @("clone","--branch",$Branch,"--single-branch",$RepositoryUrl,$ProjectRoot) | Out-Null
        Write-Status -Message "Repository cloned." -Level OK
    }
    else {
        Write-Status -Message "Projektmappen findes allerede."
        $GitFolder = Join-Path $ProjectRoot ".git"
        if (-not (Test-Path $GitFolder)) { throw "Mappen eksisterer, men er ikke et Git repository: $ProjectRoot" }

        Test-LocalChanges -RepositoryPath $ProjectRoot

        Write-Status -Message "Henter information fra GitHub..." -Level CHECKPOINT
        Invoke-Git -WorkingDirectory $ProjectRoot -Arguments @("fetch","--prune","origin") | Out-Null

        Write-Status -Message "Skifter til branch $Branch..." -Level CHECKPOINT
        Invoke-Git -WorkingDirectory $ProjectRoot -Arguments @("checkout",$Branch) | Out-Null

        Write-Status -Message "Henter seneste PROJECT 1864 version..." -Level CHECKPOINT
        Invoke-Git -WorkingDirectory $ProjectRoot -Arguments @("pull","--ff-only","origin",$Branch) | Out-Null
        Write-Status -Message "Repository er opdateret." -Level OK
    }

    $CurrentBranch = (Invoke-Git -WorkingDirectory $ProjectRoot -Arguments @("branch","--show-current") -Quiet).StdOut.Trim()
    $CurrentCommit = (Invoke-Git -WorkingDirectory $ProjectRoot -Arguments @("rev-parse","--short","HEAD") -Quiet).StdOut.Trim()
    $RemoteCommit = (Invoke-Git -WorkingDirectory $ProjectRoot -Arguments @("rev-parse","--short","origin/$Branch") -Quiet).StdOut.Trim()

    Write-Status -Message "Branch: $CurrentBranch" -Level OK
    Write-Status -Message "Lokal commit : $CurrentCommit" -Level OK
    Write-Status -Message "Remote commit: $RemoteCommit" -Level INFO

    if ($CurrentCommit -eq $RemoteCommit) {
        Write-Status -Message "Lokal repository matcher origin/$Branch." -Level OK
    }
    else {
        Write-Status -Message "Lokal commit matcher IKKE origin/$Branch." -Level WARNING
    }

    if (Test-Path $UnrealProject) {
        Write-Status -Message "Unreal project fundet:" -Level OK
        Write-Host ""
        Write-Host "  $UnrealProject" -ForegroundColor Green
        Write-Host ""
    }
    else {
        Write-Status -Message "Strategy1864.uproject blev ikke fundet." -Level WARNING
        Write-Host "Forventet: $UnrealProject" -ForegroundColor Yellow
    }

    if ($OpenUnrealProject -and (Test-Path $UnrealProject)) {
        Start-Process -FilePath $UnrealProject
    }

    Write-Host ""
    Write-Host "==============================================" -ForegroundColor DarkGreen
    Write-Host " PROJECT 1864 ER OPDATERET" -ForegroundColor Green
    Write-Host "==============================================" -ForegroundColor DarkGreen
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Status -Message $_.Exception.Message -Level ERROR
    Write-Host ""
    Write-Host "Scriptet blev stoppet." -ForegroundColor Red
    Write-Host ""
    exit 1
}
