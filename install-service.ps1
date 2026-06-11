#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs or removes Bookmarker as a Windows Service.
    When the service already exists it is stopped, updated, and restarted
    without removing or re-registering it.

.PARAMETER Uninstall
    Stop and remove the service instead of installing it.

.EXAMPLE
    # Install / update
    .\install-service.ps1

    # Uninstall
    .\install-service.ps1 -Uninstall
#>

param(
    [switch]$Uninstall
)

$ServiceName    = "Bookmarker"
$DisplayName    = "Bookmarker - Start Page"
$Description    = "Self-hosted bookmark dashboard. Opens at http://localhost:5069"
$ExePath        = Join-Path $PSScriptRoot "Bookmarker.exe"

# Read port from appsettings.json if present, fall back to default
$AppSettings    = Join-Path $PSScriptRoot "appsettings.json"
$Port           = 5069
if (Test-Path $AppSettings) {
    $urls = (Get-Content $AppSettings | ConvertFrom-Json).Urls
    if ($urls -match ':(\d+)') { $Port = $Matches[1] }
}

# ── Uninstall ────────────────────────────────────────────────────────────────
if ($Uninstall) {
    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Yellow
        exit 0
    }

    Write-Host "Stopping '$ServiceName'..."
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2

    Write-Host "Removing '$ServiceName'..."
    sc.exe delete $ServiceName | Out-Null
    Write-Host "Done. Service removed." -ForegroundColor Green
    exit 0
}

# ── Install / update ─────────────────────────────────────────────────────────
if (-not (Test-Path $ExePath)) {
    Write-Error "Bookmarker.exe not found at: $ExePath"
    exit 1
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existing) {
    # Update: stop only, keep the registration
    if ($existing.Status -eq "Running") {
        Write-Host "Stopping existing service..."
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
        Write-Host "Service stopped."
    }
    Write-Host "Updating '$DisplayName'..."
} else {
    # Fresh install: register the service
    Write-Host "Installing '$DisplayName'..."
    New-Service `
        -Name           $ServiceName `
        -BinaryPathName $ExePath `
        -DisplayName    $DisplayName `
        -Description    $Description `
        -StartupType    Automatic
}

Write-Host "Starting service..."
Start-Service -Name $ServiceName

$svc = Get-Service -Name $ServiceName
if ($svc.Status -eq "Running") {
    Write-Host ""
    Write-Host "Bookmarker is running." -ForegroundColor Green
    Write-Host "Open http://localhost:$Port in your browser."
} else {
    Write-Warning "Service did not start. Status: $($svc.Status)"
    Write-Host "Check the Windows Event Log for details."
    exit 1
}
