<#
.SYNOPSIS
    Builds Bookmarker in Release mode and installs or updates it on this machine.
    Automatically re-launches as administrator if not already elevated.
#>

# Self-elevate if not running as administrator
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process pwsh -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath) -Verb RunAs
    exit
}

$ServiceName = "Bookmarker"
$DisplayName = "Bookmarker - Start Page"
$Description = "Self-hosted bookmark dashboard."
$InstallDir  = "C:\Program Files\Bookmarker"
$ExePath     = Join-Path $InstallDir "Bookmarker.exe"
$RepoRoot    = Split-Path $PSScriptRoot -Parent
$ProjectPath = Join-Path $RepoRoot "src\Bookmarker\Bookmarker.csproj"
$PublishDir  = Join-Path $RepoRoot "artifacts\dev-publish"

# Read port from appsettings.json
$AppSettings = Join-Path $RepoRoot "src\Bookmarker\appsettings.json"
$Port        = 5069
if (Test-Path $AppSettings) {
    $urls = (Get-Content $AppSettings | ConvertFrom-Json).Urls
    if ($urls -match ':(\d+)') { $Port = $Matches[1] }
}

Write-Host ""
Write-Host "=== Bookmarker Dev Install ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build
Write-Host "[1/5] Building in Release mode..."

dotnet publish $ProjectPath -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Aborting."
    exit 1
}

Write-Host "      Build succeeded." -ForegroundColor Green

# Step 2: Stop service if running
Write-Host ""
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
$isUpdate  = $null -ne $existing

if ($existing -and $existing.Status -eq "Running") {
    Write-Host "[2/5] Stopping service '$ServiceName'..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
    Write-Host "      Service stopped." -ForegroundColor Green
} else {
    Write-Host "[2/5] Service is not running -- skipping stop." -ForegroundColor Gray
}

# Step 3: Copy files
Write-Host ""
Write-Host "[3/5] Copying files to '$InstallDir'..."

if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    Write-Host "      Created directory: $InstallDir" -ForegroundColor Green
} else {
    Write-Host "      Directory already exists: $InstallDir"
}

$files = Get-ChildItem -Path $PublishDir -File
foreach ($file in $files) {
    Copy-Item -Path $file.FullName -Destination $InstallDir -Force
    Write-Host "      Copied: $($file.Name)"
}

Write-Host "      All files copied." -ForegroundColor Green

# Step 4: Register service (fresh install only)
Write-Host ""
if ($isUpdate) {
    Write-Host "[4/5] Skipping registration -- existing service will be reused." -ForegroundColor Gray
} else {
    Write-Host "[4/5] Registering service '$DisplayName'..."

    New-Service `
        -Name           $ServiceName `
        -BinaryPathName $ExePath `
        -DisplayName    $DisplayName `
        -Description    $Description `
        -StartupType    Automatic

    Write-Host "      Service registered. Startup type: Automatic." -ForegroundColor Green
}

# Step 5: Start service
Write-Host ""
Write-Host "[5/5] Starting service..."
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

# Cleanup
Remove-Item -Recurse -Force $PublishDir

# Summary
$action = if ($isUpdate) { "Updated" } else { "Installed" }
Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "  $action to   : $InstallDir"
Write-Host "  Service name : $ServiceName"
Write-Host "  Startup type : Automatic (starts with Windows)"
Write-Host "  URL          : http://localhost:$Port"
Write-Host ""
Write-Host "Open http://localhost:$Port in your browser."
Write-Host ""
