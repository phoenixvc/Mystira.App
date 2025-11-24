# Start the PWA Frontend
Write-Host "Starting Mystira.App PWA..." -ForegroundColor Green
Write-Host "PWA will be available at:" -ForegroundColor Cyan
Write-Host "  - HTTP:  http://localhost:7000" -ForegroundColor Yellow
Write-Host "  - HTTPS: https://localhost:7000" -ForegroundColor Yellow
Write-Host ""

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location "$repoRoot\src\Mystira.App.PWA"

# Build before running
Write-Host "Building PWA..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Exiting." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Build successful. Starting PWA..." -ForegroundColor Green
dotnet run

