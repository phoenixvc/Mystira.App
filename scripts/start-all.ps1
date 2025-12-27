# Start all services and open browsers
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Mystira.App Development Launcher" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get the repository root directory
$repoRoot = Split-Path -Parent $PSScriptRoot

# Build solution first to catch any cross-project issues
Write-Host "Building solution..." -ForegroundColor Yellow
Set-Location $repoRoot
$solutionFile = Get-ChildItem -Filter "*.sln" | Select-Object -First 1
if ($solutionFile) {
    dotnet build $solutionFile.FullName
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Solution build failed! Exiting." -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "Solution build successful!" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "Warning: No solution file found. Skipping solution build." -ForegroundColor Yellow
    Write-Host ""
}

# Start API in background
Write-Host "Starting API..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-File", "$PSScriptRoot\start-api.ps1" -WindowStyle Minimized

# Wait a bit for API to start
Start-Sleep -Seconds 5

# Start PWA in background
Write-Host "Starting PWA..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-File", "$PSScriptRoot\start-pwa.ps1" -WindowStyle Minimized

# Wait a bit for PWA to start
Start-Sleep -Seconds 5

# Start DevHub in background
Write-Host "Starting DevHub..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-File", "$PSScriptRoot\start-devhub.ps1" -WindowStyle Minimized

# Wait for services to be ready
Write-Host ""
Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Open browsers
Write-Host "Opening browsers..." -ForegroundColor Green
Start-Process "https://localhost:7096/swagger"
Start-Sleep -Seconds 2
Start-Process "http://localhost:7000"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  All services started!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Services:" -ForegroundColor Yellow
Write-Host "  - API:        https://localhost:7096/swagger" -ForegroundColor White
Write-Host "  - PWA:        http://localhost:7000" -ForegroundColor White
Write-Host "  - DevHub:     Desktop application (Tauri)" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

