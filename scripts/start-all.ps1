# Start all services and open browsers
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Mystira.App Development Launcher" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get the repository root directory
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Start API in background
Write-Host "Starting API..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-File", "$PSScriptRoot\start-api.ps1" -WindowStyle Minimized

# Wait a bit for API to start
Start-Sleep -Seconds 5

# Start Admin API in background
Write-Host "Starting Admin API..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-File", "$PSScriptRoot\start-admin-api.ps1" -WindowStyle Minimized

# Wait a bit for Admin API to start
Start-Sleep -Seconds 5

# Start PWA in background
Write-Host "Starting PWA..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-File", "$PSScriptRoot\start-pwa.ps1" -WindowStyle Minimized

# Wait for services to be ready
Write-Host ""
Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Open browsers
Write-Host "Opening browsers..." -ForegroundColor Green
Start-Process "https://localhost:7096/swagger"
Start-Sleep -Seconds 2
Start-Process "https://localhost:7096/admin"
Start-Sleep -Seconds 2
Start-Process "http://localhost:7000"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  All services started!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Services:" -ForegroundColor Yellow
Write-Host "  - API:        https://localhost:7096/swagger" -ForegroundColor White
Write-Host "  - Admin UI:   https://localhost:7096/admin" -ForegroundColor White
Write-Host "  - PWA:        http://localhost:7000" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

