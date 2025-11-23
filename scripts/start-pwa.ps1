# Start the PWA Frontend
Write-Host "Starting Mystira.App PWA..." -ForegroundColor Green
Write-Host "PWA will be available at:" -ForegroundColor Cyan
Write-Host "  - HTTP:  http://localhost:7000" -ForegroundColor Yellow
Write-Host "  - HTTPS: https://localhost:7000" -ForegroundColor Yellow
Write-Host ""

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location "$repoRoot\src\Mystira.App.PWA"
dotnet run

