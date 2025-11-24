# Main launcher script - starts the DevHub Tauri app
# The DevHub app will manage all services internally

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Starting Mystira DevHub" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "DevHub will manage all services (API, Admin API, PWA)" -ForegroundColor Yellow
Write-Host "You can start/stop services and view them in the DevHub interface" -ForegroundColor Yellow
Write-Host ""

$repoRoot = $PSScriptRoot
Set-Location "$repoRoot\tools\Mystira.DevHub"

# Check if node_modules exists, install if needed
if (-not (Test-Path "node_modules")) {
    Write-Host "Installing npm dependencies..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "npm install failed! Exiting." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# Check if icons exist, generate if missing
if (-not (Test-Path "src-tauri\icons\icon.ico")) {
    Write-Host "Icons not found. Generating icons..." -ForegroundColor Yellow
    Set-Location "src-tauri"
    if (Test-Path "generate-icon.ps1") {
        & .\generate-icon.ps1
    } else {
        Write-Host "Warning: Icon generator script not found. Icons may be missing." -ForegroundColor Yellow
    }
    Set-Location ".."
}

# Build TypeScript and frontend before running
Write-Host "Building DevHub frontend..." -ForegroundColor Yellow
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Exiting." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Build successful. Starting DevHub (Tauri window)..." -ForegroundColor Green
Write-Host "Note: The app will open in a Tauri window, not a browser." -ForegroundColor Cyan
npm run tauri:dev

