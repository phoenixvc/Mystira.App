# Start the DevHub Tauri Application
Write-Host "Starting Mystira DevHub..." -ForegroundColor Green
Write-Host "DevHub is a desktop application for development operations" -ForegroundColor Cyan
Write-Host ""

$repoRoot = Split-Path -Parent $PSScriptRoot
$devHubPath = "$repoRoot\tools\Mystira.DevHub"

if (-not (Test-Path $devHubPath)) {
    Write-Host "Error: DevHub directory not found at $devHubPath" -ForegroundColor Red
    exit 1
}

Set-Location $devHubPath

# Check if node_modules exists, install if needed
if (-not (Test-Path "node_modules")) {
    Write-Host "Installing npm dependencies..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "npm install failed! Exiting." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# Build TypeScript and frontend before running
Write-Host "Building DevHub frontend..." -ForegroundColor Yellow
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Exiting." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Build successful. Starting DevHub..." -ForegroundColor Green
npm run tauri:dev

