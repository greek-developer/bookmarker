#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Copies Bookmarker to C:\Program Files\Bookmarker and installs it as a Windows Service.
    Run this from the folder where you extracted the release zip.
#>

$ServiceName    = "Bookmarker"
$DisplayName    = "Bookmarker - Start Page"
$Description    = "Self-hosted bookmark dashboard."
$InstallDir     = "C:\Program Files\Bookmarker"
$ExePath        = Join-Path $InstallDir "Bookmarker.exe"

# Read port from appsettings.json if present
$AppSettings    = Join-Path $PSScriptRoot "appsettings.json"
$Port           = 5069
if (Test-Path $AppSettings) {
    $urls = (Get-Content $AppSettings | ConvertFrom-Json).Urls
    if ($urls -match ':(\d+)') { $Port = $Matches[1] }
}

Write-Host ""
Write-Host "=== Bookmarker Installer ===" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Stop service if running ─────────────────────────────────────────
$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($svc) {
    if ($svc.Status -eq "Running") {
        Write-Host "[1/4] Stopping service '$ServiceName'..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
        Write-Host "      Service stopped." -ForegroundColor Green
    } else {
        Write-Host "[1/4] Service '$ServiceName' exists but is not running — skipping stop." -ForegroundColor Yellow
    }

    Write-Host "      Removing existing service registration..."
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
    Write-Host "      Service registration removed." -ForegroundColor Green
} else {
    Write-Host "[1/4] No existing service found — nothing to stop." -ForegroundColor Gray
}

# ── Step 2: Copy files to Program Files ─────────────────────────────────────
Write-Host ""
Write-Host "[2/4] Copying files to '$InstallDir'..."

if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    Write-Host "      Created directory: $InstallDir" -ForegroundColor Green
} else {
    Write-Host "      Directory already exists: $InstallDir"
}

$files = Get-ChildItem -Path $PSScriptRoot -File
foreach ($file in $files) {
    Copy-Item -Path $file.FullName -Destination $InstallDir -Force
    Write-Host "      Copied: $($file.Name)"
}

Write-Host "      All files copied." -ForegroundColor Green

# ── Step 3: Install service ──────────────────────────────────────────────────
Write-Host ""
Write-Host "[3/4] Installing service '$DisplayName'..."

if (-not (Test-Path $ExePath)) {
    Write-Error "Bookmarker.exe not found at: $ExePath"
    exit 1
}

New-Service `
    -Name           $ServiceName `
    -BinaryPathName $ExePath `
    -DisplayName    $DisplayName `
    -Description    $Description `
    -StartupType    Automatic

Write-Host "      Service registered. Startup type: Automatic." -ForegroundColor Green

# ── Step 4: Start service ────────────────────────────────────────────────────
Write-Host ""
Write-Host "[4/4] Starting service..."
Start-Service -Name $ServiceName
Start-Sleep -Seconds 2

$svc = Get-Service -Name $ServiceName
if ($svc.Status -eq "Running") {
    Write-Host "      Service is running." -ForegroundColor Green
} else {
    Write-Warning "Service did not start. Status: $($svc.Status)"
    Write-Host "      Check the Windows Event Log for details."
    exit 1
}

# ── Summary ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Installed to : $InstallDir"
Write-Host "  Service name : $ServiceName"
Write-Host "  Startup type : Automatic (starts with Windows)"
Write-Host "  URL          : http://localhost:$Port"
Write-Host ""
Write-Host "Open http://localhost:$Port in your browser."
Write-Host "To uninstall, run: install-service.ps1 -Uninstall"
Write-Host ""
